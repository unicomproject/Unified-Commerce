using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace E_POS.UnitTests.OutletTillDevice;

public sealed class TillActivationCodeServiceTests
{
    [Theory]
    [InlineData("valid")]
    [InlineData("permission")]
    [InlineData("tenant")]
    [InlineData("outlet")]
    [InlineData("till_scope")]
    [InlineData("device_outlet")]
    [InlineData("released")]
    [InlineData("inactive")]
    [InlineData("entitlement")]
    public async Task IssueCode_RequiresAuthorizedActiveBinding_AndStoresOnlyHash(string scenario)
    {
        using var db = new EPosDbContext(new DbContextOptionsBuilder<EPosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var now = DateTimeOffset.UtcNow;
        var actor = new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), ["till.activation_code.generate"]);
        var user = TenantUser.Create(actor.UserId, actor.TenantId, "test@example.invalid", "Test", null, null, null, null, "ACTIVE", "admin", "admin", null, now, staffCode: "TEST");
        var outlet = Outlet.Create(Guid.NewGuid(), actor.TenantId, "Main", "MAIN", "ACTIVE", "STORE", "UTC", false, null, null, null, now);
        var till = Till.Create(Guid.NewGuid(), actor.TenantId, outlet.Id, "Till", "Front", 1, "FRONT-01", "FIXED", 0, "LKR", false, scenario == "inactive" ? "INACTIVE" : "ACTIVE", null, now);
        var device = PosDevice.Create(Guid.NewGuid(), actor.TenantId, scenario == "device_outlet" ? Guid.NewGuid() : outlet.Id, "POS", "POS", "DESKTOP", "ACTIVE", null, now);
        var assignment = TillDeviceAssignment.Create(Guid.NewGuid(), actor.TenantId, outlet.Id, till.Id, device.Id, null, now);
        if (scenario == "released") assignment.Release(null, "test", now);
        if (scenario == "outlet") user.SetAccessScope("SELECTED_OUTLETS", null, "ALL_ACCESSIBLE_TILLS", null, null, now);
        if (scenario == "till_scope") user.SetAccessScope("ALL_OUTLETS", null, "SELECTED_TILLS", null, null, now);
        db.AddRange(user, outlet, till, device, assignment); await db.SaveChangesAsync();
        var clock = new Mock<IDateTimeProvider>(); clock.SetupGet(c => c.UtcNow).Returns(now);
        var entitlement = new Mock<ITenantFeatureEntitlementEvaluator>();
        entitlement.Setup(e => e.IsEnabledAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(scenario != "entitlement");
        var service = new TillActivationCodeService(db, clock.Object, entitlement.Object);
        if (scenario == "permission") actor = actor with { Permissions = [] };
        if (scenario == "tenant") actor = actor with { TenantId = Guid.NewGuid() };
        if (scenario == "valid")
        {
            var issued = await service.IssueAsync(actor, till.Id, default);
            Assert.Equal(now.AddMinutes(10), issued.ExpiresAt);
            Assert.Equal(32, issued.ActivationCode.Length);
            var stored = await db.TillActivationCodes.SingleAsync();
            Assert.Equal(DeviceFingerprintHasher.Hash(issued.ActivationCode), stored.ActivationCodeHash);
            Assert.NotEqual(issued.ActivationCode, stored.ActivationCodeHash);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.IssueAsync(actor, till.Id, default));
        }
        else
        {
            if (scenario is "device_outlet" or "released")
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.IssueAsync(actor, till.Id, default));
            else
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.IssueAsync(actor, till.Id, default));
            Assert.Empty(db.TillActivationCodes);
        }
    }
}
