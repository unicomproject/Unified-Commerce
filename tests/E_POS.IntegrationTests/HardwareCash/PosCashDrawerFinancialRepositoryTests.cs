using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.POSOperations.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.HardwareCash;

public sealed class PosCashDrawerFinancialRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MovementTypes_ReturnActiveGlobalAndTenantInTypesOnly()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 1000m);
        AddType(db, Guid.NewGuid(), null, "FLOAT_ADDED", "Float Added", "IN", false);
        AddType(db, Guid.NewGuid(), ids.Tenant, "LOCAL_FLOAT", "Local Float", "IN", true);
        AddType(db, Guid.NewGuid(), null, "CASH_DROP", "Cash Drop", "OUT", true);
        await db.SaveChangesAsync();

        var types = await new PosDrawerRepository(db).GetMovementTypesAsync(ids.Tenant, "IN", default);

        Assert.Equal(2, types.Count);
        Assert.All(types, x => Assert.Equal("IN", x.Direction));
        Assert.DoesNotContain(types, x => x.Code == "CASH_DROP");
    }

    [Fact]
    public async Task CashIn_PersistsCanonicalMovementAndUpdatesExpectedCash()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 25000m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "FLOAT_ADDED", "Float Added", "IN", false);
        await db.SaveChangesAsync();
        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), ids.Device, typeId, 2000m, "Extra float");

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till, request, Now, default);

        Assert.Null(result.ErrorCode);
        var saved = await db.CashMovements.SingleAsync();
        Assert.Equal(ids.Session, saved.TillSessionId);
        Assert.Equal(ids.Device, saved.PosDeviceId);
        Assert.Equal(request.RequestId, saved.RequestId);
        Assert.Equal(typeId, saved.MovementTypeId);
        Assert.Equal("LKR", saved.CurrencyCode);
        Assert.Single(db.CashMovements);
        Assert.Equal(27000m, result.Movement!.CurrentExpectedCash);
    }

    [Fact]
    public async Task SameRequest_ReplaysSameMovement_ChangedPayloadConflicts()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "FLOAT_ADDED", "Float Added", "IN", false);
        await db.SaveChangesAsync();
        var repo = new PosDrawerRepository(db);
        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), ids.Device, typeId, 1000m, "Float");

        var first = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request, Now, default);
        var replay = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request, Now.AddSeconds(1), default);
        var conflict = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request with { Amount = 2000m }, Now, default);

        Assert.Null(first.ErrorCode);
        Assert.Equal(first.Movement!.MovementId, replay.Movement!.MovementId);
        Assert.Equal("cash_drawer.idempotency_conflict", conflict.ErrorCode);
        Assert.Single(db.CashMovements);
    }

    [Fact]
    public async Task MovementTypes_ReturnActiveOutTypesExcludingIn()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 1000m);
        AddType(db, Guid.NewGuid(), null, "FLOAT_ADDED", "Float Added", "IN", false);
        AddType(db, Guid.NewGuid(), null, "CASH_DROP", "Safe Drop", "OUT", false);
        AddType(db, Guid.NewGuid(), ids.Tenant, "LOCAL_DROP", "Local Drop", "OUT", true);
        await db.SaveChangesAsync();

        var types = await new PosDrawerRepository(db).GetMovementTypesAsync(ids.Tenant, "OUT", default);

        Assert.Equal(2, types.Count);
        Assert.All(types, x => Assert.Equal("OUT", x.Direction));
        Assert.DoesNotContain(types, x => x.Code == "FLOAT_ADDED");
    }

    [Fact]
    public async Task CashDrop_PersistsOutMovementAndDecreasesExpectedCash()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 10000m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "CASH_DROP", "Safe Drop", "OUT", false);
        await db.SaveChangesAsync();
        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), ids.Device, typeId, 2500m, "Safe drop");

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till, request, Now, default);

        Assert.Null(result.ErrorCode);
        var saved = await db.CashMovements.SingleAsync();
        Assert.Equal(typeId, saved.MovementTypeId);
        Assert.Equal(request.Amount, saved.Amount);
        Assert.Equal("OUT", result.Movement!.Direction);
        Assert.Equal("CASH_DROP", result.Movement.MovementType);
        Assert.Equal(7500m, result.Movement.CurrentExpectedCash);
        Assert.Empty(db.TillCashMovements);
    }

    [Fact]
    public async Task CashDrop_AmountExceedingAvailableCash_IsRejectedWithoutPersistence()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 1000m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "CASH_DROP", "Safe Drop", "OUT", false);
        await db.SaveChangesAsync();

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, typeId, 1000.01m), Now, default);

        Assert.Equal("cash_drawer.insufficient_expected_cash", result.ErrorCode);
        Assert.Empty(db.CashMovements);
    }

    [Fact]
    public async Task CashDrop_AmountEqualToAvailableCash_Succeeds()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 500m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "CASH_DROP", "Safe Drop", "OUT", false);
        await db.SaveChangesAsync();

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, typeId, 500m), Now, default);

        Assert.Null(result.ErrorCode);
        Assert.Equal(0m, result.Movement!.CurrentExpectedCash);
        Assert.Single(db.CashMovements);
    }

    [Fact]
    public async Task CashDrop_SameRequest_ReplaysAndConflictingPayloadConflicts()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "CASH_DROP", "Safe Drop", "OUT", false);
        await db.SaveChangesAsync();
        var repo = new PosDrawerRepository(db);
        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), ids.Device, typeId, 1000m, "Drop");

        var first = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request, Now, default);
        var replay = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request, Now.AddSeconds(1), default);
        var conflict = await repo.CreateFinancialMovementAsync(ids.Tenant, ids.User, ids.Till, request with { Amount = 2000m }, Now, default);

        Assert.Null(first.ErrorCode);
        Assert.Equal(first.Movement!.MovementId, replay.Movement!.MovementId);
        Assert.Equal("cash_drawer.idempotency_conflict", conflict.ErrorCode);
        Assert.Single(db.CashMovements);
        Assert.Equal(4000m, first.Movement.CurrentExpectedCash);
    }

    [Fact]
    public async Task CashDrop_SecondOutCannotOverdrawAfterFirstDrop()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 1000m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "CASH_DROP", "Safe Drop", "OUT", false);
        await db.SaveChangesAsync();
        var repo = new PosDrawerRepository(db);

        var first = await repo.CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, typeId, 800m), Now, default);
        var second = await repo.CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, typeId, 800m), Now, default);

        Assert.Null(first.ErrorCode);
        Assert.Equal("cash_drawer.insufficient_expected_cash", second.ErrorCode);
        Assert.Single(db.CashMovements);
        Assert.Equal(200m, first.Movement!.CurrentExpectedCash);
    }

    [Fact]
    public async Task RequiredReasonType_RejectsBlankNote()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "CASH_CORRECTION", "Cash Correction", "IN", true);
        await db.SaveChangesAsync();

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, typeId, 100m), Now, default);

        Assert.Equal("cash_drawer.reason_required", result.ErrorCode);
        Assert.Empty(db.CashMovements);
    }

    [Fact]
    public async Task ClosedTill_IsRejectedWithoutPersistence()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "FLOAT_ADDED", "Float Added", "IN", false);
        await db.SaveChangesAsync();
        var session = await db.TillSessions.SingleAsync();
        session.Close(ids.User, ids.Device, "Test", Now.AddHours(1));
        await db.SaveChangesAsync();

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, typeId, 100m), Now, default);

        Assert.Equal("cash_drawer.till_session_not_open", result.ErrorCode);
        Assert.Empty(db.CashMovements);
    }

    [Fact]
    public async Task MovementTypes_DoesNotReturnOtherTenantTypes()
    {
        await using var db = CreateDb();
        var idsA = SeedOpenContext(db, 1000m);
        var otherTenantId = Guid.NewGuid();
        AddType(db, Guid.NewGuid(), null, "GLOBAL_IN", "Global In", "IN", false);
        AddType(db, Guid.NewGuid(), idsA.Tenant, "TENANT_A_IN", "Tenant A In", "IN", false);
        AddType(db, Guid.NewGuid(), otherTenantId, "TENANT_B_IN", "Tenant B In", "IN", false);
        await db.SaveChangesAsync();

        var types = await new PosDrawerRepository(db).GetMovementTypesAsync(idsA.Tenant, "IN", default);

        Assert.Equal(2, types.Count);
        Assert.Contains(types, x => x.Code == "GLOBAL_IN");
        Assert.Contains(types, x => x.Code == "TENANT_A_IN");
        Assert.DoesNotContain(types, x => x.Code == "TENANT_B_IN");
    }

    [Fact]
    public async Task CashIn_ForeignTenantMovementType_IsRejectedWithoutPersistence()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        var foreignTenantId = Guid.NewGuid();
        var foreignTypeId = Guid.NewGuid();
        AddType(db, foreignTypeId, foreignTenantId, "FOREIGN_FLOAT", "Foreign Float", "IN", false);
        await db.SaveChangesAsync();

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, foreignTypeId, 1000m), Now, default);

        Assert.Equal("cash_drawer.movement_type_not_found", result.ErrorCode);
        Assert.Empty(db.CashMovements);
    }

    [Fact]
    public async Task CashIn_InactiveMovementType_IsRejectedWithoutPersistence()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        var typeId = Guid.NewGuid();
        db.CashMovementTypes.Add(CashMovementType.Create(
            typeId, ids.Tenant, "INACTIVE_FLOAT", "Inactive Float", "IN", true, false, false, "INACTIVE", Now));
        await db.SaveChangesAsync();

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, typeId, 1000m), Now, default);

        Assert.Equal("cash_drawer.movement_type_not_found", result.ErrorCode);
        Assert.Empty(db.CashMovements);
    }

    [Fact]
    public async Task CashDrop_InactiveOutMovementType_IsRejectedWithoutPersistence()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        var typeId = Guid.NewGuid();
        db.CashMovementTypes.Add(CashMovementType.Create(
            typeId, ids.Tenant, "INACTIVE_DROP", "Inactive Drop", "OUT", true, false, false, "INACTIVE", Now));
        await db.SaveChangesAsync();

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, typeId, 1000m), Now, default);

        Assert.Equal("cash_drawer.movement_type_not_found", result.ErrorCode);
        Assert.Empty(db.CashMovements);
        Assert.Equal(5000m, (await new PosDrawerRepository(db)
            .GetFinancialSummaryAsync(ids.Tenant, ids.Session, default))!.CurrentExpectedCash);
    }

    [Fact]
    public async Task CashDrop_ForeignTenantOutMovementType_IsRejectedWithoutPersistence()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m);
        var foreignTypeId = Guid.NewGuid();
        AddType(db, foreignTypeId, Guid.NewGuid(), "FOREIGN_DROP", "Foreign Drop", "OUT", false);
        await db.SaveChangesAsync();

        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, foreignTypeId, 1000m), Now, default);

        Assert.Equal("cash_drawer.movement_type_not_found", result.ErrorCode);
        Assert.Empty(db.CashMovements);
    }

    [Fact]
    public async Task CashIn_DerivesCurrencyFromTillSessionAuthoritatively()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m, currency: "USD");
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "FLOAT_ADDED", "Float Added", "IN", false);
        await db.SaveChangesAsync();

        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), ids.Device, typeId, 500m, "USD Float");
        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till, request, Now, default);

        Assert.Null(result.ErrorCode);
        Assert.Equal("USD", result.Movement!.CurrencyCode);
        var saved = await db.CashMovements.SingleAsync();
        Assert.Equal("USD", saved.CurrencyCode);
        Assert.Equal(5500m, result.Movement.CurrentExpectedCash);
    }

    [Fact]
    public async Task CashDrop_DerivesCurrencyFromTillSessionAuthoritatively()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 5000m, currency: "USD");
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "CASH_DROP", "Safe Drop", "OUT", false);
        await db.SaveChangesAsync();

        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), ids.Device, typeId, 500m, "USD Drop");
        var result = await new PosDrawerRepository(db).CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till, request, Now, default);

        Assert.Null(result.ErrorCode);
        Assert.Equal("OUT", result.Movement!.Direction);
        Assert.Equal("USD", result.Movement.CurrencyCode);
        var saved = await db.CashMovements.SingleAsync();
        Assert.Equal("USD", saved.CurrencyCode);
        Assert.Equal(4500m, result.Movement.CurrentExpectedCash);
    }

    [Fact]
    public async Task FinancialSummaryAndMovements_IncludeCanonicalCashInCorrectly()
    {
        await using var db = CreateDb();
        var ids = SeedOpenContext(db, 10000m);
        var typeId = Guid.NewGuid();
        AddType(db, typeId, null, "FLOAT_ADDED", "Float Added", "IN", false);
        await db.SaveChangesAsync();
        var repo = new PosDrawerRepository(db);

        await repo.CreateFinancialMovementAsync(
            ids.Tenant, ids.User, ids.Till,
            new(Guid.NewGuid(), ids.Device, typeId, 3000m, "First In"), Now, default);

        var summary = await repo.GetFinancialSummaryAsync(ids.Tenant, ids.Session, default);
        Assert.NotNull(summary);
        Assert.Equal(10000m, summary!.OpeningCash);
        Assert.Equal(3000m, summary.CashIn);
        Assert.Equal(13000m, summary.CurrentExpectedCash);

        var movements = await repo.GetFinancialMovementsAsync(ids.Tenant, ids.Session, 1, 10, default);
        Assert.Equal(1, movements.TotalCount);
        Assert.Equal(3000m, movements.Items[0].Amount);
        Assert.Equal("FLOAT_ADDED", movements.Items[0].MovementType);
        Assert.Equal("IN", movements.Items[0].Direction);
    }

    private static void AddType(EPosDbContext db, Guid id, Guid? tenantId, string code, string name, string direction, bool requiresReason) =>
        db.CashMovementTypes.Add(CashMovementType.Create(
            id, tenantId, code, name, direction, true, requiresReason, tenantId is null, "ACTIVE", Now));

    private static EPosDbContext CreateDb() => new(
        new DbContextOptionsBuilder<EPosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static (Guid Tenant, Guid Outlet, Guid Till, Guid Device, Guid User, Guid Session) SeedOpenContext(
        EPosDbContext db, decimal opening, string currency = "LKR")
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        var tenant = Guid.NewGuid(); var outlet = Guid.NewGuid(); var till = Guid.NewGuid();
        var device = Guid.NewGuid(); var user = Guid.NewGuid(); var session = Guid.NewGuid();
        db.Tenants.Add(Tenant.Create(tenant, $"T-{tag}", $"t-{tag}", "Tenant", "active", currency, "UTC", null, null, Now));
        db.Outlets.Add(Outlet.Create(outlet, tenant, "Outlet", $"OUT-{tag}", "ACTIVE", "STORE", "UTC", true, null, null, null, Now));
        db.Tills.Add(Till.Create(till, tenant, outlet, "Till", "Till", 1, $"TILL-{tag}", "STANDARD", 0m, currency, true, "ACTIVE", null, Now));
        db.PosDevices.Add(PosDevice.Create(device, tenant, outlet, $"POS-{tag}", "POS", "TABLET", "ACTIVE", null, Now));
        db.TenantUsers.Add(TenantUser.Create(user, tenant, $"cashier-{tag}@test.com", "Cashier", null, null,
            "hash", "salt", "ACTIVE", "cashier", "outlet", "default", Now, staffCode: $"USR-2026-{user:N}"));
        db.TillSessions.Add(TillSession.Open(session, tenant, outlet, till, $"TS-{tag}",
            DateOnly.FromDateTime(Now.UtcDateTime), user, device, opening, currency, null, Now));
        return (tenant, outlet, till, device, user, session);
    }
}
