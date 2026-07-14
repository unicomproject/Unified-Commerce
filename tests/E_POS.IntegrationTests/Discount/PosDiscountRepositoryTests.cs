using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Application.Modules.Tenant.Discount.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.Discount.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.Discount.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using Xunit;

namespace E_POS.IntegrationTests.Discount;

public sealed class PosDiscountRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateApplicationAsync_IsIdempotentAndWritesRequestedEvent()
    {
        await using var db = CreateDbContext();
        var repository = CreateRepository(db);
        var command = Command(requiresApproval: true);
        SeedApplicationContext(db, command);
        await db.SaveChangesAsync();

        var first = await repository.CreateApplicationAsync(command, default);
        var second = await repository.CreateApplicationAsync(command with { ApplicationId = Guid.NewGuid() }, default);

        Assert.True(first.IsSuccess, first.ErrorCode);
        Assert.True(second.IsSuccess);
        Assert.True(second.WasExisting);
        Assert.Equal(first.ApplicationId, second.ApplicationId);
        Assert.Single(await db.PosDiscountApplications.ToListAsync());
        Assert.Single(await db.PosDiscountApplicationEvents.ToListAsync());
    }

    [Fact]
    public async Task DecideAsync_Approve_WritesApprovalAudit()
    {
        await using var db = CreateDbContext();
        var repository = CreateRepository(db);
        var command = Command(requiresApproval: true);
        SeedApplicationContext(db, command);
        await db.SaveChangesAsync();
        await repository.CreateApplicationAsync(command, default);
        var managerId = Guid.NewGuid();

        var result = await repository.DecideAsync(
            command.TenantId, managerId, command.ApplicationId, "APPROVE", "Checked", Now.AddMinutes(1), default);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal("APPROVED", result.Decision!.Status);
        Assert.Contains(await db.PosDiscountApplicationEvents.ToListAsync(), x => x.EventType == "APPROVED");
    }

    [Fact]
    public async Task CreateApplicationAsync_WhenTillSessionDeviceDoesNotMatch_ReturnsContextError()
    {
        await using var db = CreateDbContext();
        var repository = CreateRepository(db);
        var command = Command(requiresApproval: false);
        SeedApplicationContext(db, command, sessionDeviceId: Guid.NewGuid());
        await db.SaveChangesAsync();

        var result = await repository.CreateApplicationAsync(command, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_discounts.till_session_context_mismatch", result.ErrorCode);
        Assert.Empty(await db.PosDiscountApplications.ToListAsync());
        Assert.Empty(await db.PosDiscountApplicationEvents.ToListAsync());
    }

    [Fact]
    public async Task ResolveManualContextAsync_LineScope_ReturnsLineManualPolicy()
    {
        await using var db = CreateDbContext();
        var repository = CreateRepository(db);
        var command = Command(
            requiresApproval: false,
            policyCode: "POS_MANUAL_PERCENTAGE_LINE",
            discountScope: "LINE");
        SeedApplicationContext(db, command);
        await db.SaveChangesAsync();

        var result = await repository.ResolveManualContextAsync(
            command.TenantId, command.RequestedByTenantUserId, command.DeviceId,
            "PERCENTAGE", "LINE", Now, default);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal("POS_MANUAL_PERCENTAGE_LINE", result.Policy!.Code);
        Assert.Equal("LINE", result.Policy.Scope);
    }

    [Fact]
    public async Task ResolveManualContextAsync_LineScope_WhenOnlyOrderPolicyExists_ReturnsConfigurationError()
    {
        await using var db = CreateDbContext();
        var repository = CreateRepository(db);
        var command = Command(
            requiresApproval: false,
            policyCode: "POS_MANUAL_PERCENTAGE",
            discountScope: "ORDER");
        SeedApplicationContext(db, command);
        await db.SaveChangesAsync();

        var result = await repository.ResolveManualContextAsync(
            command.TenantId, command.RequestedByTenantUserId, command.DeviceId,
            "PERCENTAGE", "LINE", Now, default);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_discounts.manual_configuration_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task ListAvailableAsync_LineScope_ReturnsOnlyLinePolicies()
    {
        await using var db = CreateDbContext();
        var repository = CreateRepository(db);
        var command = Command(requiresApproval: false);
        SeedApplicationContext(db, command);
        db.DiscountPolicies.Add(DiscountPolicy.Create(
            Guid.NewGuid(), command.TenantId, command.DiscountTypeId,
            "WELCOME10", "Welcome 10%", null, "ORDER",
            10m, null, null, null, null, false, true,
            null, 10, null, null, "ACTIVE",
            command.RequestedByTenantUserId, Now));
        db.DiscountPolicies.Add(DiscountPolicy.Create(
            Guid.NewGuid(), command.TenantId, command.DiscountTypeId,
            "ITEM5", "Item 5%", null, "LINE",
            5m, null, null, null, null, false, true,
            null, 20, null, null, "ACTIVE",
            command.RequestedByTenantUserId, Now));
        await db.SaveChangesAsync();

        var result = await repository.ListAvailableAsync(
            command.TenantId,
            command.RequestedByTenantUserId,
            new PosDiscountCatalogQueryDto(
                command.DeviceId,
                "LINE",
                Guid.NewGuid(),
                [],
                null,
                1,
                1000,
                "LKR"),
            Now,
            default);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.DoesNotContain(result.Catalog!.Discounts, x => x.Scope == "ORDER");
        Assert.Contains(result.Catalog.Discounts, x => x.Code == "ITEM5" && x.Scope == "LINE");
    }

    private static PosDiscountApplicationCommand Command(
        bool requiresApproval,
        string policyCode = "MANUAL20",
        string discountScope = "ORDER")
    {
        var tenantId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        return new(
            Guid.NewGuid(), tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), requesterId, null, null,
            "idem-key", "MANUAL", discountScope, policyCode, "Manual 20%", "PERCENTAGE",
            20m, 10m, 50m, 5000m, 5000m, 1000m, 4000m, "LKR",
            "{}", new string('a', 64), "Reason", requiresApproval, true, null, Now.AddMinutes(15), Now);
    }

    private static PosDiscountRepository CreateRepository(EPosDbContext dbContext) =>
        new(dbContext, NullLogger<PosDiscountRepository>.Instance);

    private static void SeedApplicationContext(
        EPosDbContext dbContext,
        PosDiscountApplicationCommand command,
        Guid? sessionDeviceId = null)
    {
        dbContext.Currencies.Add(Currency.Create(
            Guid.NewGuid(), command.CurrencyCode, "Sri Lankan Rupee", "Rs", 2, true, 1, Now));

        dbContext.Tenants.Add(Tenant.Create(
            command.TenantId, "TEST-001", "test-001", "Test Tenant", "active",
            command.CurrencyCode, "UTC", null, null, Now));

        dbContext.TenantUsers.Add(TenantUser.Create(
            command.RequestedByTenantUserId, command.TenantId, "cashier@test.local",
            "Test Cashier", null, null, "hash", "salt", "ACTIVE",
            "cashier", "outlet", "TEST", Now));

        dbContext.Outlets.Add(Outlet.Create(
            command.OutletId, command.TenantId, "Main Outlet", "MAIN-01",
            "ACTIVE", "STORE", "UTC", true, null, null, null, Now));

        dbContext.Tills.Add(Till.Create(
            command.TillId, command.TenantId, command.OutletId, "Front Till 01",
            "Front", 1, "FRONT-01", "STANDARD", 0m, command.CurrencyCode,
            true, "ACTIVE", null, Now));

        var device = PosDevice.Create(
            command.DeviceId, command.TenantId, command.OutletId, "POS-01",
            "Front POS Device", "TABLET", "ACTIVE", null, Now);
        Set(device, nameof(PosDevice.IsTrusted), true);
        dbContext.PosDevices.Add(device);
        dbContext.TillDeviceAssignments.Add(TillDeviceAssignment.Create(
            Guid.NewGuid(), command.TenantId, command.OutletId, command.TillId,
            command.DeviceId, command.RequestedByTenantUserId, Now));

        dbContext.TillSessions.Add(TillSession.Open(
            command.TillSessionId, command.TenantId, command.OutletId, command.TillId,
            "TS-0001", DateOnly.FromDateTime(Now.UtcDateTime),
            command.RequestedByTenantUserId, sessionDeviceId ?? command.DeviceId, 0m,
            command.CurrencyCode, null, Now));

        dbContext.DiscountTypes.Add(DiscountType.Create(
            command.DiscountTypeId, "TEST_PERCENT", "Test Percent",
            command.CalculationMethod, true, "ACTIVE", Now));
        dbContext.DiscountPolicies.Add(DiscountPolicy.Create(
            command.DiscountPolicyId, command.TenantId, command.DiscountTypeId,
            command.PolicyCode, command.PolicyName, null, command.DiscountScope,
            command.AbsoluteLimit, null, null, null, null, false, command.IsStackable,
            command.StackingGroupCode, 1, null, null, "ACTIVE",
            command.RequestedByTenantUserId, Now));
    }

    private static void Set<T>(object entity, string propertyName, T value)
    {
        var property = entity.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        property?.SetValue(entity, value);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }
}
