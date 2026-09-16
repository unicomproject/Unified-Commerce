using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;

public sealed class HardwareMutationRunner(EPosDbContext db, IIdempotencyService idempotency) : IHardwareMutationRunner
{
    public async Task<ApplicationResult<T>> ExecuteAsync<T>(TenantRequestContext context, string operation,
        Guid? entityId, string key, object payload, Func<CancellationToken, Task<ApplicationResult<T>>> execute,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return ApplicationResult<T>.Failure(new("hardware.invalid_tenant_context", "Invalid tenant context."));
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100)
            return ApplicationResult<T>.Failure(new("hardware.idempotency_key_required", "Supply an Idempotency-Key of at most 100 characters."));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));
        var result = await idempotency.ExecuteAsync(context.TenantId, context.UserId, "hardware." + operation,
            key, hash, async ct => {
                // One tenant-scoped transaction lock prevents parent release/assignment/edit races.
                if (db.Database.ProviderName?.Contains("Npgsql") == true)
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT pg_advisory_xact_lock(hashtextextended({"hardware:" + context.TenantId.ToString("N")}, 0))", ct);
                var before = await db.HardwareDevices.AsNoTracking().Where(d => d.TenantId == context.TenantId && d.Id == entityId)
                    .Select(d => new { d.ConfigurationVersion, d.Status }).FirstOrDefaultAsync(ct);
                var response = await execute(ct);
                if (response.IsSuccess)
                {
                    var target = response.Value switch {
                        TenantAdminHardwareDeviceDetailResponse detail => detail.HardwareDeviceId,
                        TenantAdminHardwareAssignmentResponse assignment => assignment.HardwareDeviceId,
                        _ => entityId
                    };
                    var after = await db.HardwareDevices.AsNoTracking().Where(d => d.TenantId == context.TenantId && d.Id == target)
                        .Select(d => new { d.ConfigurationVersion, d.Status }).FirstOrDefaultAsync(ct);
                    db.AuditLogs.Add(new AuditLog {
                        TenantId = context.TenantId, ActorUserId = context.UserId, ActorType = "TENANT_USER",
                        EntityType = "HARDWARE", EntityId = target, Action = operation,
                        OldValues = before is null ? null : JsonSerializer.Serialize(before),
                        NewValues = JsonSerializer.Serialize(new { operation, entityId = target, state = after }), CreatedAt = DateTimeOffset.UtcNow
                    });
                    await db.SaveChangesAsync(ct);
                }
                return response;
            }, cancellationToken);
        if (result.IsFailure && result.Error.Code.StartsWith("user.idempotency_"))
            return ApplicationResult<T>.Failure(new("hardware.idempotency_conflict", "This operation key is already in use. Retry the same request or start a new operation."));
        return result;
    }
}
