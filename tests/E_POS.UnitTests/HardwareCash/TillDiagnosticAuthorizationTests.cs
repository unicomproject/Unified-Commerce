using System.Security.Claims;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Services;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace E_POS.UnitTests.HardwareCash;
public sealed class TillDiagnosticAuthorizationTests
{
    [Fact]
    public async Task CrossTenantAndMissingPermissionDeniedBeforeReturningTill()
    {
        using var f = new Fixture();
        Assert.Equal(f.Outlet.Id, await f.Service.AuthorizeAsync(f.Actor, f.Till.Id, true, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.AuthorizeAsync(f.Actor with { TenantId = Guid.NewGuid() }, f.Till.Id, true, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.AuthorizeAsync(f.Actor with { Permissions = [] }, f.Till.Id, true, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.AuthorizeAsync(f.Actor with { Permissions = ["tenant.hardware.view"] }, f.Till.Id, true, default));
    }
    [Fact]
    public async Task SelectedTillAndOutletScopesAreEnforcedAndRecheckedAfterRevocation()
    {
        using var f = new Fixture();
        f.User.SetAccessScope("ALL_OUTLETS", null, "SELECTED_TILLS", null, null, f.Now);
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.AuthorizeAsync(f.Actor, f.Till.Id, true, default));
        var grant = TenantUserTillAccess.Create(Guid.NewGuid(), f.Actor.TenantId, f.Actor.UserId, f.Till.Id, null, f.Now);
        f.Db.TenantUserTillAccess.Add(grant); await f.Db.SaveChangesAsync();
        Assert.Equal(f.Outlet.Id, await f.Service.AuthorizeAsync(f.Actor, f.Till.Id, true, default));
        grant.Revoke(null, f.Now); await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.AuthorizeAsync(f.Actor, f.Till.Id, true, default));
        f.User.SetAccessScope("SELECTED_OUTLETS", null, "ALL_ACCESSIBLE_TILLS", null, null, f.Now);
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.AuthorizeAsync(f.Actor, f.Till.Id, true, default));
    }
    [Fact]
    public async Task DeviceProofRequiredAndReleasedAssignmentInvalidatesConnection()
    {
        using var f = new Fixture();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.BindAsync(f.Actor, "guess", default));
        var device = PosDevice.Create(Guid.NewGuid(), f.Actor.TenantId, f.Outlet.Id, "POS-1", "POS", "DESKTOP", "ACTIVE", null, f.Now);
        device.PairForActivation("POS", "DESKTOP", "windows", "1", "hash", f.Actor.UserId, f.Now);
        var assignment = TillDeviceAssignment.Create(Guid.NewGuid(), f.Actor.TenantId, f.Outlet.Id, f.Till.Id, device.Id, null, f.Now);
        f.Db.PosDevices.Add(device); f.Db.TillDeviceAssignments.Add(assignment); await f.Db.SaveChangesAsync();
        var proof = "pos-device-v2-" + new string('a', 64);
        f.Devices.Setup(x => x.GetEditableByFingerprintAsync(f.Actor.TenantId, proof, It.IsAny<CancellationToken>())).ReturnsAsync(device);
        var binding = await f.Service.BindAsync(f.Actor, proof, default);
        Assert.Equal(f.Till.Id, binding.TillId);
        Assert.Equal(assignment.Id, binding.AssignmentId);
        var connection = new TillRuntimeConnection("socket", f.Actor, new(f.Actor.TenantId, f.Outlet.Id, f.Till.Id, device.Id, assignment.Id, "hash"), f.Now, f.Now, new ClaimsPrincipal());
        f.Registry.Register(connection);
        Assert.True(await f.Service.IsCurrentAsync(connection, default));
        assignment.Release(null, "reassignment", f.Now); await f.Db.SaveChangesAsync();
        Assert.False(await f.Service.IsCurrentAsync(connection, default));
        var nextTill = Till.Create(Guid.NewGuid(), f.Actor.TenantId, f.Outlet.Id, "Second", "Back", 2, "FRONT-02", "FIXED", 0, "LKR", false, "ACTIVE", null, f.Now);
        var nextAssignment = TillDeviceAssignment.Create(Guid.NewGuid(), f.Actor.TenantId, f.Outlet.Id, nextTill.Id, device.Id, null, f.Now);
        f.Db.AddRange(nextTill, nextAssignment); await f.Db.SaveChangesAsync();
        var nextBinding = await f.Service.BindAsync(f.Actor, proof, default);
        Assert.Equal(nextTill.Id, nextBinding.TillId);
        Assert.Equal(nextAssignment.Id, nextBinding.AssignmentId);
        Assert.False(await f.Service.IsCurrentAsync(connection, default));
        var current = connection with { Binding = nextBinding };
        f.Registry.Register(current);
        Assert.True(await f.Service.IsCurrentAsync(current, default));
        Assert.False(await f.Service.IsCurrentAsync(current with { Binding = nextBinding with { TillId = f.Till.Id } }, default));
    }
    private sealed class Fixture : IDisposable
    {
        public DateTimeOffset Now { get; } = DateTimeOffset.UtcNow;
        public EPosDbContext Db { get; } = new(new DbContextOptionsBuilder<EPosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public TenantRequestContext Actor { get; } = new(Guid.NewGuid(), Guid.NewGuid(), ["tenant.hardware.manage", "pos.hardware.settings"]);
        public TenantUser User { get; }
        public Outlet Outlet { get; }
        public Till Till { get; }
        public TillConnectionRegistry Registry { get; } = new();
        public Mock<IDeviceContextRepository> Devices { get; } = new();
        public TillDiagnosticService Service { get; }
        public Fixture()
        {
            User = TenantUser.Create(Actor.UserId, Actor.TenantId, "test@example.invalid", "Tester", null, null, null, null, "ACTIVE", "admin", "admin", null, Now, staffCode: "TEST-01");
            Outlet = Outlet.Create(Guid.NewGuid(), Actor.TenantId, "Store", "STORE", "ACTIVE", "STORE", "UTC", false, null, null, null, Now);
            Till = Till.Create(Guid.NewGuid(), Actor.TenantId, Outlet.Id, "Till", "Front", 1, "FRONT-01", "FIXED", 0, "LKR", false, "ACTIVE", null, Now);
            Db.AddRange(User, Outlet, Till); Db.SaveChanges();
            var entitlement = new Mock<ITenantFeatureEntitlementEvaluator>();
            entitlement.Setup(x => x.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(x => x.UtcNow).Returns(Now);
            var sessions = new Mock<IAuthSessionValidator>(); sessions.Setup(x => x.IsCurrentSessionActiveAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            Service = new(Db, Devices.Object, entitlement.Object, clock.Object, new ConfigurationBuilder().Build(), sessions.Object, Registry, NullLogger<TillDiagnosticService>.Instance);
        }
        public void Dispose() => Db.Dispose();
    }
}
