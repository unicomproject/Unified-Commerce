using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.POSOperations;

/// <summary>
/// Exercises PosHoldRepository.CreateHoldAsync/GetActiveHoldsAsync against a real
/// PostgreSQL instance to validate the tenant-scoped filtered unique idempotency index
/// (uq_pos_order_holds_tenant_id_idempotency_key) and hold-expiry behavior under
/// realistic concurrency, mirroring ManualPaymentPostgreSqlConcurrencyTests. Skips
/// gracefully when no local PostgreSQL is reachable.
/// </summary>
public sealed class PosHoldPostgreSqlConcurrencyTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateHoldAsync_ConcurrentSameIdempotencyKeyAndFingerprint_ResultsInExactlyOneHold()
    {
        if (!await CanConnectAsync()) return;
        var ids = HoldFixtureIds.Create();
        await SeedFixtureAsync(ids);
        try
        {
            var lines = new[] { new PosCheckoutLineRequestDto(ids.VariantId, 1) };
            var request = new PosCreateHoldRequestDto(
                ids.DeviceId, "NewSale", null, lines, "Concurrent replay test",
                null, $"idem-{ids.Suffix}");

            await using var firstDb = CreateDb();
            await using var secondDb = CreateDb();
            var first = CreateRepository(firstDb);
            var second = CreateRepository(secondDb);

            var results = await Task.WhenAll(
                first.CreateHoldAsync(ids.TenantId, ids.UserId, [], request, Now, Now.AddHours(24), default),
                second.CreateHoldAsync(ids.TenantId, ids.UserId, [], request, Now, Now.AddHours(24), default));

            Assert.All(results, x => Assert.True(x.IsSuccess, x.ErrorCode));
            Assert.Equal(results[0].Hold!.SaleId, results[1].Hold!.SaleId);
            Assert.Equal(results[0].Hold!.HoldId, results[1].Hold!.HoldId);

            await using var assertDb = CreateDb();
            Assert.Equal(1, await assertDb.PosOrderHolds
                .CountAsync(x => x.TenantId == ids.TenantId && x.IdempotencyKey == $"idem-{ids.Suffix}"));
        }
        finally
        {
            await CleanupAsync(ids);
        }
    }

    [Fact]
    public async Task CreateHoldAsync_SameKeyDifferentFingerprint_ReturnsConflictForLoser()
    {
        if (!await CanConnectAsync()) return;
        var ids = HoldFixtureIds.Create();
        await SeedFixtureAsync(ids);
        try
        {
            var idempotencyKey = $"idem-{ids.Suffix}";
            var firstRequest = new PosCreateHoldRequestDto(
                ids.DeviceId, "NewSale", null,
                [new PosCheckoutLineRequestDto(ids.VariantId, 1)],
                "First payload", null, idempotencyKey);
            var secondRequest = new PosCreateHoldRequestDto(
                ids.DeviceId, "NewSale", null,
                [new PosCheckoutLineRequestDto(ids.VariantId, 2)],
                "Different payload, same key", null, idempotencyKey);

            await using var firstDb = CreateDb();
            await using var secondDb = CreateDb();
            var first = CreateRepository(firstDb);
            var second = CreateRepository(secondDb);

            var results = await Task.WhenAll(
                first.CreateHoldAsync(ids.TenantId, ids.UserId, [], firstRequest, Now, Now.AddHours(24), default),
                second.CreateHoldAsync(ids.TenantId, ids.UserId, [], secondRequest, Now, Now.AddHours(24), default));

            Assert.Single(results, x => x.IsSuccess);
            Assert.Single(results, x => !x.IsSuccess && x.ErrorCode == "pos_holds.idempotency_conflict");

            await using var assertDb = CreateDb();
            Assert.Equal(1, await assertDb.PosOrderHolds
                .CountAsync(x => x.TenantId == ids.TenantId && x.IdempotencyKey == idempotencyKey));
        }
        finally
        {
            await CleanupAsync(ids);
        }
    }

    [Fact]
    public async Task CreateHoldAsync_ConcurrentDifferentKeys_ProducesDistinctParkReferences()
    {
        if (!await CanConnectAsync()) return;
        var ids = HoldFixtureIds.Create();
        await SeedFixtureAsync(ids);
        try
        {
            var firstRequest = new PosCreateHoldRequestDto(
                ids.DeviceId, "NewSale", null,
                [new PosCheckoutLineRequestDto(ids.VariantId, 1)],
                "First hold", null, $"idem-a-{ids.Suffix}");
            var secondRequest = new PosCreateHoldRequestDto(
                ids.DeviceId, "NewSale", null,
                [new PosCheckoutLineRequestDto(ids.VariantId, 1)],
                "Second hold", null, $"idem-b-{ids.Suffix}");

            await using var firstDb = CreateDb();
            await using var secondDb = CreateDb();
            var first = CreateRepository(firstDb);
            var second = CreateRepository(secondDb);

            var results = await Task.WhenAll(
                first.CreateHoldAsync(ids.TenantId, ids.UserId, [], firstRequest, Now, Now.AddHours(24), default),
                second.CreateHoldAsync(ids.TenantId, ids.UserId, [], secondRequest, Now, Now.AddHours(24), default));

            Assert.All(results, x => Assert.True(x.IsSuccess, x.ErrorCode));
            Assert.NotEqual(results[0].Hold!.HoldNumber, results[1].Hold!.HoldNumber);
            Assert.NotEqual(results[0].Hold!.SaleId, results[1].Hold!.SaleId);

            await using var assertDb = CreateDb();
            Assert.Equal(2, await assertDb.PosOrderHolds.CountAsync(x => x.TenantId == ids.TenantId));
        }
        finally
        {
            await CleanupAsync(ids);
        }
    }

    [Fact]
    public async Task GetActiveHoldsAsync_WhenHoldIsDue_PersistsExpiredStatusAndAuditEvent()
    {
        if (!await CanConnectAsync()) return;
        var ids = HoldFixtureIds.Create();
        await SeedFixtureAsync(ids);
        try
        {
            await using var createDb = CreateDb();
            var repository = CreateRepository(createDb);
            var request = new PosCreateHoldRequestDto(
                ids.DeviceId, "NewSale", null,
                [new PosCheckoutLineRequestDto(ids.VariantId, 1)],
                "Expiry test", null, $"idem-{ids.Suffix}");

            var created = await repository.CreateHoldAsync(
                ids.TenantId, ids.UserId, [], request, Now, Now.AddMinutes(-1), default);
            Assert.True(created.IsSuccess, created.ErrorCode);
            var holdId = created.Hold!.HoldId;

            await using var readDb = CreateDb();
            var readRepository = CreateRepository(readDb);
            var active = await readRepository.GetActiveHoldsAsync(
                ids.TenantId, ids.UserId, new PosHoldListQueryDto(ids.DeviceId), Now, default);
            Assert.True(active.IsSuccess, active.ErrorCode);
            Assert.DoesNotContain(active.Holds!, x => x.HoldId == holdId);

            await using var assertDb = CreateDb();
            var persisted = await assertDb.PosOrderHolds.AsNoTracking().SingleAsync(x => x.Id == holdId);
            Assert.Equal("EXPIRED", persisted.HoldStatus);
            Assert.Contains(
                await assertDb.PosOrderHoldEvents.AsNoTracking()
                    .Where(x => x.HoldId == holdId).ToListAsync(),
                x => x.EventType == "PARK_EXPIRED");
        }
        finally
        {
            await CleanupAsync(ids);
        }
    }

    private static PosHoldRepository CreateRepository(EPosDbContext db)
    {
        var tillSessionRepository = new PosTillSessionRepository(
            db, new CodeSequenceRepository(db), NullLogger<PosTillSessionRepository>.Instance);
        var checkoutRepository = new PosCheckoutRepository(
            db, tillSessionRepository, new StubReceiptTemplateResolutionService(), null);
        return new PosHoldRepository(db, checkoutRepository, tillSessionRepository);
    }

    private sealed class StubReceiptTemplateResolutionService : IReceiptTemplateResolutionService
    {
        public Task<ResolvedReceiptTemplateDto?> ResolveTemplateAsync(
            Guid tenantId, Guid outletId, Guid tillId, Guid deviceId, CancellationToken cancellationToken) =>
            Task.FromResult<ResolvedReceiptTemplateDto?>(null);
    }

    private static async Task SeedFixtureAsync(HoldFixtureIds ids)
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();

        if (!await db.Currencies.AnyAsync(x => x.CurrencyCode == "LKR"))
        {
            db.Currencies.Add(Currency.Create(
                Guid.NewGuid(), "LKR", "Sri Lankan Rupee", "Rs", 2, true, 0, Now));
        }

        db.Tenants.Add(Tenant.Create(
            ids.TenantId, $"HOLD-{ids.Suffix}", $"hold-{ids.Suffix}", "Park Hold Test Tenant",
            "active", "LKR", "UTC", null, null, Now));

        db.Outlets.Add(Outlet.Create(
            ids.OutletId, ids.TenantId, "Main Outlet", $"MAIN-{ids.Suffix}", "ACTIVE",
            "STORE", "UTC", true, null, null, null, Now));

        db.Tills.Add(Till.Create(
            ids.TillId, ids.TenantId, ids.OutletId, "Front Till", "Front", 1,
            $"FRONT-{ids.Suffix}", "STANDARD", 0m, "LKR", true, "ACTIVE", null, Now));

        var device = PosDevice.Create(
            ids.DeviceId, ids.TenantId, ids.OutletId, $"POS-{ids.Suffix}", "Front POS Device",
            "TABLET", "ACTIVE", null, Now);
        typeof(PosDevice).GetProperty(nameof(PosDevice.IsTrusted))!.SetValue(device, true);
        db.PosDevices.Add(device);

        db.TenantUsers.Add(TenantUser.Create(
            ids.UserId, ids.TenantId, $"cashier-{ids.Suffix}@example.test", "Cashier",
            null, null, "hash", "salt", "ACTIVE", "cashier", "outlet", "default", Now,
            staffCode: "USR-2026-95001"));

        db.TillSessions.Add(TillSession.Open(
            Guid.NewGuid(), ids.TenantId, ids.OutletId, ids.TillId, $"TS-{ids.Suffix}",
            DateOnly.FromDateTime(Now.UtcDateTime), ids.UserId, ids.DeviceId, 100m, "LKR",
            null, Now));

        db.UnitOfMeasures.Add(UnitOfMeasure.Create(
            ids.UomId, ids.TenantId, "EA", "Each", "COUNT", "ea", null, 1m,
            ProductConstants.ActiveStatus, Now));

        db.Products.Add(Product.Create(
            ids.ProductId, ids.TenantId, $"SKU-{ids.Suffix}", "Test Product", $"test-product-{ids.Suffix}",
            "STANDARD", "SIMPLE", null, null, null, null, null, true, true,
            ProductConstants.ActiveStatus, null, Now));

        db.ProductVariants.Add(ProductVariant.Create(
            ids.VariantId, ids.TenantId, ids.ProductId, "DEFAULT", "Test Product",
            $"VAR-{ids.Suffix}", ids.UomId, ids.UomId, true, true, false,
            ProductConstants.ActiveStatus, null, Now));

        await db.SaveChangesAsync();

        // till_device_assignments has NOT NULL created_at/updated_at columns that the
        // TillDeviceAssignment entity does not map (pre-existing model/schema gap,
        // unrelated to Park Sale), so EF's own INSERT can't satisfy them. Insert via raw
        // SQL instead of TillDeviceAssignment.Create()/db.TillDeviceAssignments.Add(...).
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO till_device_assignments
                (id, tenant_id, outlet_id, till_id, pos_device_id, assigned_at,
                 assigned_by_tenant_user_id, created_at, updated_at)
            VALUES
                ({Guid.NewGuid()}, {ids.TenantId}, {ids.OutletId}, {ids.TillId}, {ids.DeviceId},
                 {Now}, {ids.UserId}, {Now}, {Now})
            """);

        // price_lists.price_includes_tax/is_default_price_list/priority are NOT NULL with
        // no store-side DEFAULT actually applied yet (pre-existing schema drift, unrelated
        // to Park Sale). EF's value-generation convention for HasDefaultValue() properties
        // omits any column whose CLR value equals its configured default from the INSERT,
        // which would come back NULL here. Insert via raw SQL to sidestep that entirely.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO price_lists
                (id, tenant_id, price_list_code, price_list_name, price_list_type,
                 currency_code, price_includes_tax, is_default_price_list, priority,
                 status, created_at, updated_at)
            VALUES
                ({ids.PriceListId}, {ids.TenantId}, {$"DEFAULT-{ids.Suffix}"}, {"Default Price List"},
                 {"POS"}, {"LKR"}, {false}, {true}, {0}, {"ACTIVE"}, {Now}, {Now})
            """);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO price_list_items
                (id, tenant_id, price_list_id, product_id, product_variant_id,
                 selling_price, min_quantity, status, created_at, updated_at)
            VALUES
                ({Guid.NewGuid()}, {ids.TenantId}, {ids.PriceListId}, {ids.ProductId}, {ids.VariantId},
                 {500m}, {1m}, {"ACTIVE"}, {Now}, {Now})
            """);
    }

    private static async Task CleanupAsync(HoldFixtureIds ids)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            DELETE FROM pos_order_hold_events WHERE tenant_id = @tenant;
            DELETE FROM pos_order_holds WHERE tenant_id = @tenant;
            DELETE FROM sales_order_lines WHERE tenant_id = @tenant;
            DELETE FROM sales_orders WHERE tenant_id = @tenant;
            DELETE FROM sales_channels WHERE tenant_id = @tenant;
            DELETE FROM price_list_items WHERE tenant_id = @tenant;
            DELETE FROM price_lists WHERE tenant_id = @tenant;
            DELETE FROM product_variants WHERE tenant_id = @tenant;
            DELETE FROM products WHERE tenant_id = @tenant;
            DELETE FROM unit_of_measures WHERE tenant_id = @tenant;
            DELETE FROM till_sessions WHERE tenant_id = @tenant;
            DELETE FROM till_device_assignments WHERE tenant_id = @tenant;
            DELETE FROM pos_devices WHERE tenant_id = @tenant;
            DELETE FROM tills WHERE tenant_id = @tenant;
            DELETE FROM outlets WHERE tenant_id = @tenant;
            DELETE FROM tenant_users WHERE tenant_id = @tenant;
            DELETE FROM tenants WHERE id = @tenant;
            """, connection);
        command.Parameters.AddWithValue("tenant", ids.TenantId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static EPosDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(ConnectionString).Options);

    private sealed record HoldFixtureIds(
        Guid TenantId, Guid OutletId, Guid TillId, Guid DeviceId, Guid UserId,
        Guid ProductId, Guid VariantId, Guid UomId, Guid PriceListId, string Suffix)
    {
        public static HoldFixtureIds Create()
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            return new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), suffix);
        }
    }
}
