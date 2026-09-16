using System.Text.Json;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;

public sealed record HardwareBatchMember(Guid Id, string Name, string Type, int Version);
public sealed record HardwareBatchItem(Guid Id, string Name, string Type, string Status);
public sealed record HardwareBatchResult(Guid Id, Guid TillId, Guid PosDeviceId, DateTimeOffset ExpiresAt, string Status, IReadOnlyList<HardwareBatchItem> Items);

// A coordination record, never physical test evidence (HardwareDeviceId is null).
public sealed class HardwareRemoteTestService(EPosDbContext db, HardwareQueryScope scope)
{
    const string Marker = "REMOTE_BATCH";
    public async Task<HardwareBatchResult> StartAsync(TenantRequestContext actor, Guid tillId, Guid requestId, CancellationToken ct)
    {
        if (!actor.HasPermission("tenant.hardware.manage") || requestId == Guid.Empty) throw new InvalidOperationException("Hardware management permission and request ID are required.");
        if (scope.TillId is not null && scope.TillId != tillId) throw new InvalidOperationException("Till is outside hardware access.");
        await using var tx = db.Database.CurrentTransaction == null ? await db.Database.BeginTransactionAsync(ct) : null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"hardware:" + actor.TenantId.ToString("N")}, 0))", ct);
        var retry = await db.HardwareTestLogs.SingleOrDefaultAsync(x => x.TenantId == actor.TenantId && x.RequestId == requestId, ct);
        if (retry != null) {
            if (retry.TestType != Marker || retry.TillId != tillId || retry.TestedByTenantUserId != actor.UserId) throw new InvalidOperationException("Request ID conflict.");
            return await DescribeAsync(retry, ct);
        }
        var till = await db.Tills.SingleOrDefaultAsync(x => x.TenantId == actor.TenantId && x.Id == tillId && x.Status == "ACTIVE", ct);
        if (till == null) throw new InvalidOperationException("Active till not found.");
        var mappings = await db.TillDeviceAssignments.Where(x => x.TenantId == actor.TenantId && x.TillId == tillId && x.OutletId == till.OutletId && x.ReleasedAt == null).ToListAsync(ct);
        if (mappings.Count != 1) throw new InvalidOperationException("Assign exactly one trusted native POS to this till first.");
        var posId = mappings[0].PosDeviceId;
        if (!await ValidPosAsync(actor.TenantId, till.OutletId, tillId, posId, ct)) throw new InvalidOperationException("Trusted native POS assignment is unavailable.");
        var now = DateTimeOffset.UtcNow;
        var running = await db.HardwareTestLogs.FirstOrDefaultAsync(x => x.TenantId == actor.TenantId && x.TestType == Marker && x.InitiatedFromPosDeviceId == posId && x.TestedAt > now.AddMinutes(-10), ct);
        if (running != null) {
            if (running.TestedByTenantUserId == actor.UserId && running.TillId == tillId) return await DescribeAsync(running, ct);
            throw new InvalidOperationException("A Test All session already exists for this POS. Wait for its 10-minute window to end.");
        }
        var members = await (from d in db.HardwareDevices
            where d.TenantId == actor.TenantId && d.OutletId == till.OutletId && d.Status == "ACTIVE"
            where db.HardwareDeviceAssignments.Any(a => a.TenantId == actor.TenantId && a.OutletId == till.OutletId && a.HardwareDeviceId == d.Id && a.ReleasedAt == null && (a.TillId == tillId || (scope.TillId == null && a.PosDeviceId == posId)))
            orderby d.HardwareDeviceName, d.Id
            select new HardwareBatchMember(d.Id, d.HardwareDeviceName, d.HardwareDeviceType, d.ConfigurationVersion)).Take(51).ToListAsync(ct);
        if (members.Count == 0 || members.Count > 50) throw new InvalidOperationException("Test All requires between 1 and 50 active assigned devices.");
        var batch = HardwareTestLog.Create(Guid.NewGuid(), actor.TenantId, till.OutletId, null, posId, tillId, null, actor.UserId, requestId, requestId.ToString("N"), 0, "SESSION", Marker, "PENDING", null, null, JsonSerializer.Serialize(members), now, now);
        db.HardwareTestLogs.Add(batch); await db.SaveChangesAsync(ct); if (tx != null) await tx.CommitAsync(ct);
        return await DescribeAsync(batch, ct);
    }
    public async Task<HardwareBatchResult?> GetAsync(TenantRequestContext actor, Guid tillId, Guid id, CancellationToken ct)
    {
        if (!actor.HasPermission("tenant.hardware.manage")) return null;
        var batch = await db.HardwareTestLogs.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == actor.TenantId && x.Id == id && x.TillId == tillId && x.TestType == Marker && x.TestedByTenantUserId == actor.UserId, ct);
        return batch == null ? null : await DescribeAsync(batch, ct);
    }
    public async Task<IReadOnlyList<HardwareBatchResult>> PendingAsync(TenantRequestContext actor, Guid posDeviceId, CancellationToken ct)
    {
        if (!actor.HasPermission("pos.hardware.settings")) throw new InvalidOperationException("Hardware settings permission is required.");
        var since = DateTimeOffset.UtcNow.AddMinutes(-10);
        var batches = await db.HardwareTestLogs.AsNoTracking().Where(x => x.TenantId == actor.TenantId && x.TestType == Marker && x.InitiatedFromPosDeviceId == posDeviceId && x.TestedAt > since).OrderByDescending(x => x.TestedAt).Take(1).ToListAsync(ct);
        var results = new List<HardwareBatchResult>();
        foreach (var batch in batches)
            if (batch.TillId is Guid till && await ValidPosAsync(batch.TenantId, batch.OutletId, till, posDeviceId, ct))
                results.Add(await DescribeAsync(batch, ct));
        return results;
    }
    async Task<bool> ValidPosAsync(Guid tenant, Guid outlet, Guid till, Guid pos, CancellationToken ct) =>
        await db.PosDevices.AnyAsync(p => p.TenantId == tenant && p.OutletId == outlet && p.Id == pos && p.IsTrusted && p.Status == "ACTIVE" && (p.Platform == "windows" || p.Platform == "android" || p.Platform == "ios"), ct)
        && await db.TillDeviceAssignments.AnyAsync(a => a.TenantId == tenant && a.OutletId == outlet && a.TillId == till && a.PosDeviceId == pos && a.ReleasedAt == null, ct)
        && !await db.TillDeviceAssignments.AnyAsync(a => a.TenantId == tenant && a.PosDeviceId == pos && a.TillId != till && a.ReleasedAt == null, ct);
    async Task<HardwareBatchResult> DescribeAsync(HardwareTestLog batch, CancellationToken ct)
    {
        var expires = batch.TestedAt.AddMinutes(10);
        var members = JsonSerializer.Deserialize<List<HardwareBatchMember>>(batch.ResultPayloadJson ?? "[]") ?? [];
        var items = new List<HardwareBatchItem>();
        var valid = await ValidPosAsync(batch.TenantId, batch.OutletId, batch.TillId!.Value, batch.InitiatedFromPosDeviceId!.Value, ct);
        foreach (var m in members) {
            var status = "WAITING_FOR_POS";
            var types = m.Type.ToUpperInvariant() switch { "RECEIPT_PRINTER" => new[]{"TESTPRINT"}, "BARCODE_SCANNER" => new[]{"HIDINPUT","CAMERASCAN"}, "CASH_DRAWER" => new[]{"DRAWERPULSE"}, _ => Array.Empty<string>() };
            if (!valid) status = "ASSIGNMENT_CHANGED";
            else if (types.Length == 0) status = "UNSUPPORTED";
            else if (!await db.HardwareDevices.AnyAsync(d => d.TenantId == batch.TenantId && d.Id == m.Id && d.Status == "ACTIVE" && d.ConfigurationVersion == m.Version, ct) ||
                !await db.HardwareDeviceAssignments.AnyAsync(a => a.TenantId == batch.TenantId && a.OutletId == batch.OutletId && a.HardwareDeviceId == m.Id && a.ReleasedAt == null && a.AssignedAt <= batch.TestedAt && (a.TillId == batch.TillId || a.PosDeviceId == batch.InitiatedFromPosDeviceId), ct)) status = "CONFIGURATION_CHANGED";
            else {
                var test = await db.HardwareTestLogs.AsNoTracking().Where(t => t.TenantId == batch.TenantId && t.HardwareDeviceId == m.Id && t.InitiatedFromPosDeviceId == batch.InitiatedFromPosDeviceId && t.TillId == batch.TillId && t.ConfigurationVersion == m.Version && types.Contains(t.TestType) && t.TestedAt >= batch.TestedAt && t.TestedAt <= expires).OrderByDescending(t => t.TestedAt).ThenByDescending(t => t.Id).FirstOrDefaultAsync(ct);
                if (test != null) status = test.TestStatus == "PASSED" ? (test.PhysicalConfirmation == true && test.CompletedAt != null && test.CompletedAt <= expires ? "PASSED" : "CONFIRMATION_REQUIRED") : test.TestStatus;
                if (DateTimeOffset.UtcNow > expires && status is "WAITING_FOR_POS" or "PENDING" or "CONFIRMATION_REQUIRED") status = "EXPIRED";
            }
            items.Add(new(m.Id, m.Name, m.Type, status));
        }
        var overall = items.Count > 0 && items.All(x => x.Status == "PASSED") ? "PASSED" : DateTimeOffset.UtcNow > expires ? "EXPIRED" : items.Any(x => x.Status is "FAILED" or "UNSUPPORTED" or "CONFIGURATION_CHANGED" or "ASSIGNMENT_CHANGED") ? "NEEDS_ATTENTION" : "WAITING_FOR_POS";
        return new(batch.Id, batch.TillId.Value, batch.InitiatedFromPosDeviceId.Value, expires, overall, items);
    }
}
