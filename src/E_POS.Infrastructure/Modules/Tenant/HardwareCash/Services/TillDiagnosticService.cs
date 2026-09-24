using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using E_POS.Application.Common.Security;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;

/// <summary>Coordinates diagnostics using existing hardware history and assignment records.</summary>
public sealed class TillDiagnosticService(EPosDbContext db, IDeviceContextRepository devices,
    ITenantFeatureEntitlementEvaluator entitlements, IDateTimeProvider clock, IConfiguration configuration,
    IAuthSessionValidator sessions, ITillConnectionRegistry registry, ILogger<TillDiagnosticService> logger) : ITillDiagnosticService
{
    public const string Marker = "REMOTE_SCAN";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public DateTimeOffset Now => clock.UtcNow;

    public async Task<Guid> AuthorizeAsync(TenantRequestContext actor, Guid tillId, bool manage, CancellationToken ct)
    {
        if (!(actor.HasPermission("tenant.hardware.manage") ||
            (!manage && (actor.HasPermission("tenant.hardware.view") || actor.HasPermission("pos.hardware.settings")))))
            throw new UnauthorizedAccessException("Hardware permission required.");
        if (!await entitlements.IsEnabledAsync(actor.TenantId, PlatformTenantFeatureCodes.HardwareDeviceManagement, Now, ct))
            throw new UnauthorizedAccessException("Hardware entitlement required.");
        var till = await db.Tills.AsNoTracking().SingleOrDefaultAsync(t => t.TenantId == actor.TenantId && t.Id == tillId && t.Status == "ACTIVE", ct);
        var user = await db.TenantUsers.AsNoTracking().SingleOrDefaultAsync(u => u.TenantId == actor.TenantId && u.Id == actor.UserId && u.AccountStatus == "ACTIVE", ct);
        if (till == null || user == null || !await db.Outlets.AnyAsync(o => o.TenantId == actor.TenantId && o.Id == till.OutletId && o.Status == "ACTIVE", ct))
            throw new UnauthorizedAccessException("Till access denied.");
        var outletAllowed = user.OutletAccessScope == "ALL_OUTLETS" ||
            (user.OutletAccessScope == "SELECTED_OUTLETS" &&
            (await db.OutletUserRoles.AnyAsync(x => x.TenantId == actor.TenantId && x.TenantUserId == actor.UserId && x.OutletId == till.OutletId && x.RevokedAt == null, ct) ||
             await db.OutletUserPermissions.AnyAsync(x => x.TenantId == actor.TenantId && x.TenantUserId == actor.UserId && x.OutletId == till.OutletId && x.RevokedAt == null, ct)));
        var tillAllowed = user.TillAccessScope == "ALL_ACCESSIBLE_TILLS" ||
            (user.TillAccessScope == "SELECTED_TILLS" && await db.TenantUserTillAccess.AnyAsync(x => x.TenantId == actor.TenantId && x.TenantUserId == actor.UserId && x.TillId == tillId && x.RevokedAt == null, ct));
        var restrictions = configuration.GetSection("HardwareAccess:Restrictions").Get<Restriction[]>() ?? [];
        var rules = restrictions.Where(r => r.TenantId == actor.TenantId && r.UserId == actor.UserId).ToArray();
        if (!outletAllowed || !tillAllowed || (rules.Length > 0 &&
            (rules.Length != 1 || rules[0].OutletId != till.OutletId || rules[0].TillId != tillId)))
            throw new UnauthorizedAccessException("Till access denied.");
        return till.OutletId;
    }

    public async Task<TillRuntimeBinding> BindAsync(TenantRequestContext actor, string proof, CancellationToken ct)
    {
        if (!actor.HasPermission("pos.hardware.settings") || string.IsNullOrEmpty(proof) || proof.Length != 78 ||
            !proof.StartsWith("pos-device-v2-", StringComparison.Ordinal) || !proof.AsSpan(14).ToString().All(char.IsAsciiHexDigit))
            throw new UnauthorizedAccessException("Native device proof required.");
        var device = await devices.GetEditableByFingerprintAsync(actor.TenantId, proof, ct);
        if (device == null || !device.IsTrusted || device.Status != "ACTIVE" || device.Platform?.ToLowerInvariant() is not ("windows" or "android" or "ios"))
            throw new UnauthorizedAccessException("Trusted native device required.");
        var assignments = await db.TillDeviceAssignments.AsNoTracking().Where(a => a.TenantId == actor.TenantId && a.PosDeviceId == device.Id && a.ReleasedAt == null).ToListAsync(ct);
        if (assignments.Count != 1 || assignments[0].OutletId != device.OutletId)
            throw new UnauthorizedAccessException("Current till binding required.");
        var a = assignments[0];
        var outletId = await AuthorizeAsync(actor, a.TillId, false, ct);
        if (outletId != device.OutletId ||
            await db.TillDeviceAssignments.CountAsync(x => x.TenantId == actor.TenantId && x.TillId == a.TillId && x.ReleasedAt == null, ct) != 1)
            throw new UnauthorizedAccessException("Current till binding required.");
        return new(actor.TenantId, device.OutletId, a.TillId, device.Id, a.Id, device.DeviceFingerprintHash!);
    }

    public async Task<bool> IsCurrentAsync(TillRuntimeConnection connection, CancellationToken ct)
    {
        var b = connection.Binding;
        if (registry.Get(connection.ConnectionId) == null || connection.Principal == null ||
            !await sessions.IsCurrentSessionActiveAsync(connection.Principal, ct)) return false;
        try { await AuthorizeAsync(connection.Actor, b.TillId, false, ct); }
        catch (UnauthorizedAccessException) { return false; }
        return await db.PosDevices.AnyAsync(p => p.TenantId == b.TenantId && p.Id == b.PosDeviceId && p.OutletId == b.OutletId &&
            p.IsTrusted && p.Status == "ACTIVE" && p.DeviceFingerprintHash == b.FingerprintHash, ct) &&
            await db.TillDeviceAssignments.AnyAsync(a => a.Id == b.AssignmentId && a.TenantId == b.TenantId && a.TillId == b.TillId &&
                a.PosDeviceId == b.PosDeviceId && a.OutletId == b.OutletId && a.ReleasedAt == null, ct) &&
            await db.TillDeviceAssignments.CountAsync(a => a.TenantId == b.TenantId && a.TillId == b.TillId && a.ReleasedAt == null, ct) == 1 &&
            await db.TillDeviceAssignments.CountAsync(a => a.TenantId == b.TenantId && a.PosDeviceId == b.PosDeviceId && a.ReleasedAt == null, ct) == 1;
    }

    public async Task<TillDiagnosticScan> CreateAsync(TenantRequestContext actor, Guid tillId, Guid requestId,
        TillRuntimeConnection? connection, CancellationToken ct)
    {
        var outlet = await AuthorizeAsync(actor, tillId, true, ct);
        if (requestId == Guid.Empty) throw new ArgumentException("Request ID required.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(actor.TenantId, ct);
        var prior = await db.HardwareTestLogs.SingleOrDefaultAsync(x => x.TenantId == actor.TenantId && x.RequestId == requestId, ct);
        if (prior != null)
        {
            if (prior.TestType != Marker || prior.TillId != tillId || prior.TestedByTenantUserId != actor.UserId)
                throw new InvalidOperationException("Request ID conflict.");
            return Decode(prior);
        }
        var binding = connection?.Binding;
        var online = connection != null && binding!.TenantId == actor.TenantId && binding.TillId == tillId && await IsCurrentAsync(connection, ct);
        var assignedPosId = online ? binding!.PosDeviceId : Guid.Empty;
        var now = Now;
        var deadline = now.AddSeconds(Math.Clamp(configuration.GetValue("HardwareDiagnostics:TimeoutSeconds", 60), 10, 300));
        var active = await db.HardwareTestLogs.AnyAsync(x => x.TenantId == actor.TenantId && x.TillId == tillId && x.TestType == Marker &&
            (x.TestStatus == "REQUESTED" || x.TestStatus == "DISPATCHED" || x.TestStatus == "RUNNING") && x.TestedAt > now.AddSeconds(-300), ct);
        if (active) throw new InvalidOperationException("A diagnostic session is already in progress.");
        if (await db.HardwareTestLogs.AnyAsync(x => x.TenantId == actor.TenantId && x.TillId == tillId && x.TestType == Marker && x.TestedAt > now.AddSeconds(-5), ct))
            throw new InvalidOperationException("Wait before requesting another scan.");
        var registered = await db.HardwareDevices.AsNoTracking().Where(d => d.TenantId == actor.TenantId && d.OutletId == outlet && d.Status == "ACTIVE" &&
            db.HardwareDeviceAssignments.Any(a => a.TenantId == actor.TenantId && a.HardwareDeviceId == d.Id && a.OutletId == outlet && a.ReleasedAt == null &&
                (a.TillId == tillId || (online && a.PosDeviceId == assignedPosId)))).OrderBy(d => d.Id).Take(51).ToListAsync(ct);
        if (registered.Count > 50) throw new InvalidOperationException("At most 50 devices per scan.");
        var scan = new TillDiagnosticScan(Guid.NewGuid(), actor.TenantId, outlet, tillId,
            online ? binding!.PosDeviceId : Guid.Empty, online ? binding!.AssignmentId : Guid.Empty,
            actor.UserId, now, deadline, online ? "REQUESTED" : "TILL_OFFLINE", registered.Select(ToDevice).ToArray(), [], online ? null : now);
        db.HardwareTestLogs.Add(HardwareTestLog.Create(scan.Id, actor.TenantId, outlet, null,
            online ? binding!.PosDeviceId : null, tillId, null, actor.UserId, requestId, requestId.ToString("N"), 0,
            "SESSION", Marker, scan.Status, "DIAGNOSTIC", null, JsonSerializer.Serialize(scan, Json), now, now));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        logger.LogInformation("Hardware scan requested. Tenant {TenantId} Till {TillId} Scan {ScanId} Actor {ActorId} Status {Status}",
            actor.TenantId, tillId, scan.Id, actor.UserId, scan.Status);
        return scan;
    }

    public async Task<TillDiagnosticScan?> GetAsync(TenantRequestContext actor, Guid tillId, Guid id, CancellationToken ct)
    {
        await AuthorizeAsync(actor, tillId, false, ct);
        var row = await db.HardwareTestLogs.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == actor.TenantId && x.TillId == tillId && x.Id == id && x.TestType == Marker, ct);
        return row == null ? null : Decode(row);
    }

    public async Task<TillDiagnosticScan> AdvanceAsync(Guid tenant, Guid id, string status,
        TillRuntimeConnection? connection, IReadOnlyList<TillDiagnosticResult>? results, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(tenant, ct);
        var row = await db.HardwareTestLogs.SingleAsync(x => x.TenantId == tenant && x.Id == id && x.TestType == Marker, ct);
        var scan = Decode(row);
        if (connection != null && (!TillDiagnosticEvidence.Owns(scan, connection.Binding) || !await IsCurrentAsync(connection, ct)))
            throw new UnauthorizedAccessException("Scan does not belong to this current POS binding.");
        if (scan.Status is "COMPLETED" or "FAILED" or "TIMED_OUT" or "TILL_OFFLINE") return scan;
        if (Now >= scan.ExpiresAt) status = "TIMED_OUT";
        if (status == "RUNNING" && scan.Status == "RUNNING") return scan;
        if (status == "RUNNING" && scan.Status != "DISPATCHED") throw new InvalidOperationException("Scan not dispatched.");
        if (status == "DISPATCHED" && scan.Status != "REQUESTED") throw new InvalidOperationException("Scan already dispatched.");
        if (status == "COMPLETED")
        {
            if (connection == null || scan.Status is not ("DISPATCHED" or "RUNNING")) throw new InvalidOperationException("Scan not dispatched.");
            if (results == null || results.Any(x => x == null) || results.Count != scan.Devices.Count || results.Select(x => x.Id).Distinct().Count() != results.Count)
                throw new ArgumentException("Exact registered device result set required.");
            foreach (var result in results)
            {
                var expected = scan.Devices.SingleOrDefault(x => x.Id == result.Id) ?? throw new ArgumentException("Unexpected device result.");
                if (!await db.HardwareDevices.AnyAsync(d => d.TenantId == tenant && d.Id == expected.Id && d.Status == "ACTIVE" && d.ConfigurationVersion == expected.Version, ct) ||
                    !await db.HardwareDeviceAssignments.AnyAsync(a => a.TenantId == tenant && a.HardwareDeviceId == expected.Id && a.ReleasedAt == null &&
                        a.AssignedAt <= scan.RequestedAt && (a.TillId == scan.TillId || a.PosDeviceId == scan.PosDeviceId), ct))
                    throw new InvalidOperationException("Hardware configuration or assignment changed. Start a new scan.");
                TillDiagnosticEvidence.Validate(expected, result);
            }
            var detectedIds = results!.Where(r => r.Detection == "DETECTED").Select(r => r.Id).ToArray();
            if (detectedIds.Length > 0)
            {
                var detected = await db.HardwareDevices
                    .Where(d => d.TenantId == tenant && detectedIds.Contains(d.Id))
                    .ToListAsync(ct);
                foreach (var device in detected) device.RecordHeartbeat(Now);
            }
            // DETECTED/NOT_DETECTED are authoritative, current evidence from the assigned POS.
            // Mirror them into a TELEMETRY row (same channel the periodic client heartbeat
            // uses) so a device that just went missing shows Issues immediately instead of
            // waiting out the last-seen staleness window, and a device that just reappeared
            // clears a stale Issues state on its next scan.
            foreach (var result in results!.Where(r => r.Detection is "DETECTED" or "NOT_DETECTED"))
            {
                var expectedDevice = scan.Devices.Single(x => x.Id == result.Id);
                var newStatus = result.Detection == "DETECTED" ? "SUCCESS" : "FAILED";
                var previous = await db.HardwareTestLogs.AsNoTracking()
                    .Where(x => x.TenantId == tenant && x.HardwareDeviceId == result.Id && x.TestType == "TELEMETRY")
                    .OrderByDescending(x => x.TestedAt).ThenByDescending(x => x.Id)
                    .Select(x => new { x.TestStatus, x.ConfigurationVersion })
                    .FirstOrDefaultAsync(ct);
                if (previous != null && previous.TestStatus == newStatus && previous.ConfigurationVersion == expectedDevice.Version)
                    continue;
                var payload = JsonSerializer.Serialize(new { status = newStatus, source = Marker }, Json);
                db.HardwareTestLogs.Add(HardwareTestLog.Create(
                    Guid.NewGuid(), tenant, scan.OutletId, result.Id,
                    scan.PosDeviceId == Guid.Empty ? null : scan.PosDeviceId, scan.TillId, null, null,
                    Guid.NewGuid(), Guid.NewGuid().ToString("N"), expectedDevice.Version, expectedDevice.Type,
                    "TELEMETRY", newStatus, null,
                    newStatus == "SUCCESS" ? "Detected during remote scan." : "Not detected during remote scan.",
                    payload, Now, Now));
            }
        }
        var terminal = status is "COMPLETED" or "FAILED" or "TIMED_OUT" or "TILL_OFFLINE";
        scan = scan with { Status = status, Results = status == "COMPLETED" ? results! : scan.Results, CompletedAt = terminal ? Now : null };
        row.Complete(status, "DIAGNOSTIC_RESULT", null, JsonSerializer.Serialize(scan, Json), null, Now);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        logger.LogInformation("Hardware scan transitioned. Tenant {TenantId} Till {TillId} Scan {ScanId} Status {Status}", tenant, scan.TillId, id, status);
        return scan;
    }

    public async Task<IReadOnlyList<TillDiagnosticScan>> ExpireAsync(CancellationToken ct)
    {
        var rows = await db.HardwareTestLogs.AsNoTracking().Where(x => x.TestType == Marker &&
            (x.TestStatus == "REQUESTED" || x.TestStatus == "DISPATCHED" || x.TestStatus == "RUNNING")).OrderBy(x => x.TestedAt).Take(100).ToListAsync(ct);
        var expired = new List<TillDiagnosticScan>();
        foreach (var row in rows.Where(x => Decode(x).ExpiresAt <= Now))
            expired.Add(await AdvanceAsync(row.TenantId, row.Id, "TIMED_OUT", null, null, ct));
        return expired;
    }

    private Task LockAsync(Guid tenant, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_advisory_xact_lock(hashtextextended({"hardware:" + tenant.ToString("N")}, 0))", ct);
    private static TillDiagnosticScan Decode(HardwareTestLog row) => JsonSerializer.Deserialize<TillDiagnosticScan>(row.ResultPayloadJson!, Json)!;
    private static TillDiagnosticDevice ToDevice(HardwareDevice d)
    {
        var ids = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(d.ConfigJson))
        {
            using var config = JsonDocument.Parse(d.ConfigJson);
            foreach (var key in HardwareIdentityMatcher.Keys)
                if (config.RootElement.TryGetProperty(key, out var value) && value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    ids[key] = value.ToString();
            if (!ids.ContainsKey("devicePath") && config.RootElement.TryGetProperty("usbDeviceName", out var usbName) && usbName.ValueKind == JsonValueKind.String)
                ids["devicePath"] = usbName.GetString()!;
        }
        if (!string.IsNullOrWhiteSpace(d.SerialNumber)) ids["serialNumber"] = d.SerialNumber;
        return new(d.Id, d.HardwareDeviceName, d.HardwareDeviceType, d.ConnectionType, d.ConfigurationVersion, ids);
    }
    private sealed record Restriction(Guid TenantId, Guid UserId, Guid OutletId, Guid TillId);
}
