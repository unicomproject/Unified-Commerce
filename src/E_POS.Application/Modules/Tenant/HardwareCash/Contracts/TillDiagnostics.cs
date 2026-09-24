using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

public sealed record TillRuntimeBinding(Guid TenantId, Guid OutletId, Guid TillId,
    Guid PosDeviceId, Guid AssignmentId, string FingerprintHash);
public sealed record TillRuntimeConnection(string ConnectionId, TenantRequestContext Actor,
    TillRuntimeBinding Binding, DateTimeOffset ConnectedAt, DateTimeOffset LastSeenAt,
    System.Security.Claims.ClaimsPrincipal? Principal = null);
public sealed record TillDiagnosticDevice(Guid Id, string Name, string Type, string Connection,
    int Version, Dictionary<string, string> Identifiers);
public sealed record TillDiagnosticResult(Guid Id, string Detection, string Health,
    Dictionary<string, string> Identifiers, string? Message);
public sealed record TillDiagnosticScan(Guid Id, Guid TenantId, Guid OutletId, Guid TillId,
    Guid PosDeviceId, Guid AssignmentId, Guid RequestedBy, DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt, string Status, IReadOnlyList<TillDiagnosticDevice> Devices,
    IReadOnlyList<TillDiagnosticResult> Results, DateTimeOffset? CompletedAt = null);
public sealed record TillRuntimeStatus(Guid TillId, Guid? PosDeviceId, string Status,
    DateTimeOffset? LastSeenAt);

public interface ITillConnectionRegistry
{
    void Register(TillRuntimeConnection connection);
    void Remove(string connectionId);
    void Touch(string connectionId, DateTimeOffset now);
    TillRuntimeConnection? Get(string connectionId);
    IReadOnlyList<TillRuntimeConnection> ForTill(Guid tenantId, Guid tillId, DateTimeOffset now);
}

public interface ITillDiagnosticService
{
    DateTimeOffset Now { get; }
    Task<Guid> AuthorizeAsync(TenantRequestContext actor, Guid tillId, bool manage, CancellationToken ct);
    Task<TillRuntimeBinding> BindAsync(TenantRequestContext actor, string proof, CancellationToken ct);
    Task<bool> IsCurrentAsync(TillRuntimeConnection connection, CancellationToken ct);
    Task<TillDiagnosticScan> CreateAsync(TenantRequestContext actor, Guid tillId, Guid requestId, TillRuntimeConnection? connection, CancellationToken ct);
    Task<TillDiagnosticScan?> GetAsync(TenantRequestContext actor, Guid tillId, Guid id, CancellationToken ct);
    Task<TillDiagnosticScan> AdvanceAsync(Guid tenant, Guid id, string status, TillRuntimeConnection? connection, IReadOnlyList<TillDiagnosticResult>? results, CancellationToken ct);
    Task<IReadOnlyList<TillDiagnosticScan>> ExpireAsync(CancellationToken ct);
}

/// <summary>Detection evidence never substitutes for a physical print or scan test.</summary>
public static class HardwareIdentityMatcher
{
    public static readonly string[] Keys = ["serialNumber", "devicePath", "usbVendorId",
        "usbProductId", "bluetoothAddress", "printerQueue", "ipAddress", "portName", "url", "printerName"];

    public static bool IsStrong(IReadOnlyDictionary<string, string> ids) =>
        new[] { "serialNumber", "devicePath", "bluetoothAddress", "printerQueue", "ipAddress", "portName", "url", "printerName" }
            .Any(k => ids.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v));

    public static bool Matches(IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual) => IsStrong(expected) && expected
        .Where(x => Keys.Contains(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
        .All(x => actual.TryGetValue(x.Key, out var value) &&
            string.Equals(x.Value.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class TillDiagnosticEvidence
{
    public static bool Owns(TillDiagnosticScan scan, TillRuntimeBinding binding) =>
        scan.TenantId == binding.TenantId && scan.TillId == binding.TillId && scan.OutletId == binding.OutletId &&
        scan.PosDeviceId == binding.PosDeviceId && scan.AssignmentId == binding.AssignmentId;

    public static void Validate(TillDiagnosticDevice expected, TillDiagnosticResult result)
    {
        if (expected.Id != result.Id || result.Detection is not ("DETECTED" or "NOT_DETECTED" or "UNSUPPORTED" or "UNKNOWN") ||
            result.Health is not ("READY" or "UNKNOWN" or "UNSUPPORTED" or "ERROR" or "OFFLINE" or "BUSY" or "PAPER_OUT" or "COVER_OPEN") ||
            result.Identifiers == null || result.Identifiers.Count > 8 ||
            result.Identifiers.Any(x => !HardwareIdentityMatcher.Keys.Contains(x.Key) || x.Value == null || x.Value.Length > 256) ||
            result.Message?.Length > 300)
            throw new ArgumentException("Invalid diagnostic evidence.");
        if (result.Detection == "DETECTED" && !HardwareIdentityMatcher.Matches(expected.Identifiers, result.Identifiers))
            throw new ArgumentException("Detected identity does not match registration.");
        if (result.Health == "READY" && result.Detection != "DETECTED")
            throw new ArgumentException("Readiness requires detection.");
    }
}
