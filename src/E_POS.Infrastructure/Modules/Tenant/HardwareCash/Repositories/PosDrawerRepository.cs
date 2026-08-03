using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories;

public sealed class PosDrawerRepository : IPosDrawerRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly EPosDbContext _db;

    public PosDrawerRepository(EPosDbContext db) => _db = db;

    public async Task<CashDrawerOperationDto?> GetOperationByIdAsync(
        Guid tenantId, Guid operationId, CancellationToken cancellationToken)
    {
        var op = await _db.CashDrawerOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == operationId, cancellationToken);
        return op is null ? null : Map(op);
    }

    public async Task<CashDrawerOperationDto?> GetOperationByRequestIdAsync(
        Guid tenantId, Guid requestId, CancellationToken cancellationToken)
    {
        var op = await _db.CashDrawerOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.RequestId == requestId, cancellationToken);
        return op is null ? null : Map(op);
    }

    public async Task<CashDrawerSettingsDto?> GetActiveDrawerSettingsAsync(
        Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        var configDevice = await GetActiveDrawerDeviceAsync(tenantId, posDeviceId, cancellationToken);
        if (configDevice is null || configDevice.Status != "ACTIVE")
            return null;

        return JsonSerializer.Deserialize<CashDrawerSettingsDto>(configDevice.ConfigJson ?? "{}", JsonOptions);
    }

    public async Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> RegisterOperationAsync(
        Guid tenantId,
        Guid userId,
        RegisterDrawerOperationRequest request,
        Guid? approverId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // 1. Request ID idempotency check
        var canonical = JsonSerializer.Serialize(request, JsonOptions);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));

        var existing = await _db.CashDrawerOperations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.RequestId == request.RequestId, cancellationToken);

        if (existing is not null)
        {
            if (existing.PayloadHash != payloadHash)
                return ("pos_drawer.idempotency_conflict", null);

            return (null, Map(existing));
        }

        // 2. Resolve active till
        var activeTillId = request.TillId ?? await ResolveActiveTillIdAsync(tenantId, request.PosDeviceId, cancellationToken);
        if (activeTillId == Guid.Empty)
            return ("pos_drawer.till_not_assigned", null);

        // 3. Resolve active till session
        var activeSession = await ActiveSessionAsync(tenantId, request.PosDeviceId, activeTillId, cancellationToken);
        if (activeSession is null)
            return ("pos_drawer.till_session_not_open", null);

        // 4. Fetch drawer device
        var configDevice = await GetActiveDrawerDeviceAsync(tenantId, request.PosDeviceId, cancellationToken);
        if (configDevice is null || configDevice.Status != "ACTIVE")
            return ("pos_drawer.configuration_missing", null);

        var settings = JsonSerializer.Deserialize<CashDrawerSettingsDto>(configDevice.ConfigJson ?? "{}", JsonOptions);
        if (settings is null)
            return ("pos_drawer.configuration_invalid", null);

        // 5. Check purpose policy
        var purpose = request.DrawerPurpose.Trim().ToLowerInvariant();
        if (purpose == "cashsale" && !settings.OpenOnCashSale)
            return ("pos_drawer.purpose_disabled", null);
        if (purpose == "cashrefund" && !settings.OpenOnCashRefund)
            return ("pos_drawer.purpose_disabled", null);
        if (purpose == "splitpaymentcash" && !settings.OpenOnCashSplit)
            return ("pos_drawer.purpose_disabled", null);

        var op = CashDrawerOperation.Create(
            Guid.NewGuid(),
            tenantId,
            configDevice.OutletId,
            configDevice.Id,
            request.PosDeviceId,
            activeTillId,
            activeSession.Id,
            userId,
            approverId,
            request.RequestId,
            request.DrawerPurpose,
            request.Reason,
            request.BusinessReferenceType,
            request.BusinessReferenceId,
            configDevice.Id,
            configDevice.ConfigurationVersion,
            settings.DrawerPort ?? "drawerPin2",
            settings.PulseOnMilliseconds ?? 100,
            settings.PulseOffMilliseconds ?? 200,
            "Pending",
            null,
            null,
            false,
            null,
            now,
            payloadHash,
            now);

        _db.CashDrawerOperations.Add(op);
        await _db.SaveChangesAsync(cancellationToken);

        return (null, Map(op));
    }

    public async Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> FinalizeOperationAsync(
        Guid tenantId,
        Guid userId,
        Guid operationId,
        FinalizeDrawerOperationRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var op = await _db.CashDrawerOperations
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == operationId, cancellationToken);

        if (op is null)
            return ("pos_drawer.operation_not_found", null);

        if (op.Status is "OPENED" or "FAILED" or "CANCELLED")
            return ("pos_drawer.already_finalized", Map(op));

        op.FinalizeOperation(
            request.Status,
            request.ResultCategory,
            request.FailureCategory,
            request.AgentAccepted,
            request.PhysicalConfirmation,
            now);

        await _db.SaveChangesAsync(cancellationToken);
        return (null, Map(op));
    }

    public async Task<IReadOnlyList<CashDrawerOperationDto>> GetHistoryAsync(
        Guid tenantId, Guid posDeviceId, int take, CancellationToken cancellationToken)
    {
        var ops = await _db.CashDrawerOperations.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PosDeviceId == posDeviceId)
            .OrderByDescending(x => x.InitiatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return ops.Select(Map).ToList();
    }

    private async Task<Guid> ResolveActiveTillIdAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return await _db.TillDeviceAssignments.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PosDeviceId == posDeviceId && x.ReleasedAt == null)
            .Select(x => x.TillId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<TillSession?> ActiveSessionAsync(Guid tenantId, Guid posDeviceId, Guid tillId, CancellationToken cancellationToken)
    {
        return await _db.TillSessions.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        (x.OpenedFromPosDeviceId == posDeviceId || x.TillId == tillId) &&
                        x.Status == "OPEN")
            .OrderByDescending(x => x.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<HardwareDevice?> GetActiveDrawerDeviceAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return await (
            from assignment in _db.HardwareDeviceAssignments.AsNoTracking()
            join device in _db.HardwareDevices.AsNoTracking()
                on assignment.HardwareDeviceId equals device.Id
            where assignment.TenantId == tenantId &&
                  assignment.PosDeviceId == posDeviceId &&
                  assignment.ReleasedAt == null &&
                  device.HardwareDeviceType == "CASHDRAWER" &&
                  device.Status != "DELETED"
            select device).SingleOrDefaultAsync(cancellationToken);
    }

    private static CashDrawerOperationDto Map(CashDrawerOperation op)
    {
        return new CashDrawerOperationDto(
            op.Id,
            op.TenantId,
            op.OutletId,
            op.HardwareDeviceId,
            op.PosDeviceId,
            op.TillId,
            op.TillSessionId,
            op.ProcessedByUserId,
            op.ApproverId,
            op.RequestId,
            op.DrawerPurpose,
            op.Reason,
            op.BusinessReferenceType,
            op.BusinessReferenceId,
            op.ConfigurationId,
            op.ConfigurationVersion,
            op.DrawerPort,
            op.PulseOnTime,
            op.PulseOffTime,
            op.Status,
            op.ResultCategory,
            op.FailureCategory,
            op.AgentAccepted,
            op.PhysicalConfirmation,
            op.InitiatedAt,
            op.CompletedAt);
    }
}
