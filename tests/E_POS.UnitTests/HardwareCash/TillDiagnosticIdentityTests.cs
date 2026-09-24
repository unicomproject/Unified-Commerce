using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;
using Xunit;

namespace E_POS.UnitTests.HardwareCash;

public sealed class TillDiagnosticIdentityTests
{
    [Fact]
    public void WrongTenantTillPosOutletOrAssignmentCannotOwnScan()
    {
        var binding = new TillRuntimeBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hash");
        var now = DateTimeOffset.UtcNow;
        var scan = new TillDiagnosticScan(Guid.NewGuid(), binding.TenantId, binding.OutletId, binding.TillId, binding.PosDeviceId,
            binding.AssignmentId, Guid.NewGuid(), now, now.AddMinutes(1), "RUNNING", [], []);
        Assert.True(TillDiagnosticEvidence.Owns(scan, binding));
        Assert.False(TillDiagnosticEvidence.Owns(scan, binding with { TenantId = Guid.NewGuid() }));
        Assert.False(TillDiagnosticEvidence.Owns(scan, binding with { TillId = Guid.NewGuid() }));
        Assert.False(TillDiagnosticEvidence.Owns(scan, binding with { PosDeviceId = Guid.NewGuid() }));
        Assert.False(TillDiagnosticEvidence.Owns(scan, binding with { OutletId = Guid.NewGuid() }));
        Assert.False(TillDiagnosticEvidence.Owns(scan, binding with { AssignmentId = Guid.NewGuid() }));
    }
    [Fact]
    public void ForgedIdentityAndReadinessWithoutDetectionAreRejected()
    {
        var device = new TillDiagnosticDevice(Guid.NewGuid(), "Scanner", "BARCODE_SCANNER", "USB", 1, new() { ["serialNumber"] = "ABC" });
        var result = new TillDiagnosticResult(device.Id, "DETECTED", "UNKNOWN", new() { ["serialNumber"] = "ABC" }, null);
        TillDiagnosticEvidence.Validate(device, result);
        Assert.Throws<ArgumentException>(() => TillDiagnosticEvidence.Validate(device, result with { Identifiers = new() { ["serialNumber"] = "FAKE" } }));
        Assert.Throws<ArgumentException>(() => TillDiagnosticEvidence.Validate(device, result with { Detection = "NOT_DETECTED", Health = "READY" }));
        Assert.Throws<ArgumentException>(() => TillDiagnosticEvidence.Validate(device, result with { Id = Guid.NewGuid() }));
    }
    [Fact]
    public void VidPidAloneCannotIdentifyAnIndividualScanner()
    {
        var ids = new Dictionary<string, string> { ["usbVendorId"] = "123", ["usbProductId"] = "456" };
        Assert.False(HardwareIdentityMatcher.Matches(ids, ids));
    }
    [Fact]
    public void EveryConfiguredIdentifierMustMatch()
    {
        var expected = new Dictionary<string, string> { ["serialNumber"] = "ABC", ["usbVendorId"] = "123" };
        Assert.True(HardwareIdentityMatcher.Matches(expected, new Dictionary<string, string> { ["serialNumber"] = "abc", ["usbVendorId"] = "123" }));
        Assert.False(HardwareIdentityMatcher.Matches(expected, new Dictionary<string, string> { ["serialNumber"] = "ABC", ["usbVendorId"] = "999" }));
        Assert.False(HardwareIdentityMatcher.Matches(expected, new Dictionary<string, string>()));
    }
    [Fact]
    public void RegistrySeparatesFiveTillsAndTenantAndExpiresStaleHeartbeats()
    {
        var registry = new TillConnectionRegistry();
        var tenant = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var connections = Enumerable.Range(0, 5).Select(i => Connection(tenant, now, $"pos-{i}")).ToArray();
        foreach (var connection in connections) registry.Register(connection);
        Assert.Equal("pos-2", Assert.Single(registry.ForTill(tenant, connections[2].Binding.TillId, now)).ConnectionId);
        Assert.Empty(registry.ForTill(Guid.NewGuid(), connections[2].Binding.TillId, now));
        Assert.Empty(registry.ForTill(tenant, connections[2].Binding.TillId, now.AddSeconds(61)));
        registry.Touch("pos-2", now.AddSeconds(61));
        Assert.Single(registry.ForTill(tenant, connections[2].Binding.TillId, now.AddSeconds(61)));
    }
    [Fact]
    public void ReconnectedPosInvalidatesOldSocketAndOldDisconnectCannotRemoveNewSocket()
    {
        var registry = new TillConnectionRegistry();
        var first = Connection(Guid.NewGuid(), DateTimeOffset.UtcNow, "first");
        registry.Register(first);
        registry.Register(first with { ConnectionId = "second" });
        Assert.Null(registry.Get("first"));
        registry.Remove("first");
        Assert.NotNull(registry.Get("second"));
    }
    [Fact]
    public void CompletedDiagnosticCannotBecomePhysicalSuccessOrBeOverwritten()
    {
        var now = DateTimeOffset.UtcNow;
        var log = HardwareTestLog.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, Guid.NewGuid(), null, null,
            Guid.NewGuid(), "hash", 0, "SESSION", "REMOTE_SCAN", "REQUESTED", "DIAGNOSTIC", null, "{}", now, now);
        Assert.Throws<ArgumentException>(() => log.Complete("PASSED", "{}", null, null, null, now));
        log.Complete("DISPATCHED", "{}", null, null, null, now);
        log.Complete("COMPLETED", "{}", null, null, null, now);
        Assert.Null(log.PhysicalConfirmation);
        Assert.Throws<InvalidOperationException>(() => log.Complete("RUNNING", "{}", null, null, null, now));
    }
    private static TillRuntimeConnection Connection(Guid tenant, DateTimeOffset now, string id) =>
        new(id, new TenantRequestContext(tenant, Guid.NewGuid(), ["pos.hardware.settings"]),
            new(tenant, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "fingerprint"), now, now);
}



