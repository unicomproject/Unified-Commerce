using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.HardwareCash;

/// <summary>
/// Exercises PosDrawerRepository.CreateFinancialMovementAsync against PostgreSQL to prove
/// tenant-scoped request-id uniqueness (uq_cash_movements_tenant_id_request_id) under
/// concurrent duplicate submission. Soft-skips when local PostgreSQL is unreachable.
/// </summary>
public sealed class PosCashDrawerPostgreSqlConcurrencyTests
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateFinancialMovement_ConcurrentSameRequestIdAndPayload_PersistsExactlyOneRow()
    {
        if (!await CanConnectAsync()) return;

        var ids = FixtureIds.Create();
        await SeedFixtureAsync(ids, openingFloat: 25000m, direction: "IN", code: "CONCURRENT_FLOAT");
        try
        {
            var request = new CreatePosCashMovementRequest(
                ids.RequestId, ids.DeviceId, ids.MovementTypeId, 1000m, "Concurrent float");

            await using var firstDb = CreateDb();
            await using var secondDb = CreateDb();
            var first = new PosDrawerRepository(firstDb);
            var second = new PosDrawerRepository(secondDb);

            var results = await Task.WhenAll(
                first.CreateFinancialMovementAsync(ids.TenantId, ids.UserId, ids.TillId, request, Now, default),
                second.CreateFinancialMovementAsync(ids.TenantId, ids.UserId, ids.TillId, request, Now, default));

            Assert.All(results, x => Assert.True(x.ErrorCode is null, x.ErrorCode));
            Assert.Equal(results[0].Movement!.MovementId, results[1].Movement!.MovementId);

            await using var assertDb = CreateDb();
            Assert.Equal(1, await assertDb.CashMovements.CountAsync(
                x => x.TenantId == ids.TenantId && x.RequestId == ids.RequestId));
        }
        finally
        {
            await CleanupAsync(ids);
        }
    }

    [Fact]
    public async Task CreateOutMovement_ConcurrentOverDrop_AllowsExactlyOneAndLeavesExpectedCashCorrect()
    {
        if (!await CanConnectAsync()) return;

        var ids = FixtureIds.Create();
        const decimal opening = 10000m;
        const decimal drop = 7000m;
        await SeedFixtureAsync(ids, openingFloat: opening, direction: "OUT", code: "CASH_DROP");
        try
        {
            var requestA = new CreatePosCashMovementRequest(
                Guid.NewGuid(), ids.DeviceId, ids.MovementTypeId, drop, "Drop A");
            var requestB = new CreatePosCashMovementRequest(
                Guid.NewGuid(), ids.DeviceId, ids.MovementTypeId, drop, "Drop B");

            await using var firstDb = CreateDb();
            await using var secondDb = CreateDb();
            var first = new PosDrawerRepository(firstDb);
            var second = new PosDrawerRepository(secondDb);

            var results = await Task.WhenAll(
                first.CreateFinancialMovementAsync(ids.TenantId, ids.UserId, ids.TillId, requestA, Now, default),
                second.CreateFinancialMovementAsync(ids.TenantId, ids.UserId, ids.TillId, requestB, Now, default));

            var successes = results.Where(x => x.ErrorCode is null).ToList();
            var failures = results.Where(x => x.ErrorCode is not null).ToList();
            Assert.Single(successes);
            Assert.Single(failures);
            Assert.Equal("cash_drawer.insufficient_expected_cash", failures[0].ErrorCode);
            Assert.Equal(opening - drop, successes[0].Movement!.CurrentExpectedCash);

            await using var assertDb = CreateDb();
            var rows = await assertDb.CashMovements
                .Where(x => x.TenantId == ids.TenantId)
                .ToListAsync();
            Assert.Single(rows);
            Assert.Equal(drop, rows[0].Amount);

            var summary = await new PosDrawerRepository(assertDb)
                .GetFinancialSummaryAsync(ids.TenantId, ids.SessionId, default);
            Assert.NotNull(summary);
            Assert.Equal(opening - drop, summary!.CurrentExpectedCash);
            Assert.True(summary.CurrentExpectedCash >= 0m);
        }
        finally
        {
            await CleanupAsync(ids);
        }
    }

    private static EPosDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(ConnectionString).Options);

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

    private static async Task SeedFixtureAsync(
        FixtureIds ids,
        decimal openingFloat,
        string direction,
        string code)
    {
        await using var db = CreateDb();
        db.Tenants.Add(Tenant.Create(
            ids.TenantId, $"CD-{ids.Suffix}", $"cd-{ids.Suffix}", "Cash Drawer Concurrent",
            "active", "LKR", "UTC", null, null, Now));
        db.Outlets.Add(Outlet.Create(
            ids.OutletId, ids.TenantId, "Outlet", $"OUT-{ids.Suffix}", "ACTIVE",
            "STORE", "UTC", true, null, null, null, Now));
        db.Tills.Add(Till.Create(
            ids.TillId, ids.TenantId, ids.OutletId, "Till", "Till", 1,
            $"TILL-{ids.Suffix}", "STANDARD", 0m, "LKR", true, "ACTIVE", null, Now));

        var device = PosDevice.Create(
            ids.DeviceId, ids.TenantId, ids.OutletId, $"POS-{ids.Suffix}", "POS",
            "TABLET", "ACTIVE", null, Now);
        typeof(PosDevice).GetProperty(nameof(PosDevice.IsTrusted))!.SetValue(device, true);
        db.PosDevices.Add(device);

        db.TenantUsers.Add(TenantUser.Create(
            ids.UserId, ids.TenantId, $"cd-{ids.Suffix}@example.test", "Cashier",
            null, null, "hash", "salt", "ACTIVE", "cashier", "outlet", "default", Now,
            staffCode: "USR-2026-95002"));
        db.CashMovementTypes.Add(CashMovementType.Create(
            ids.MovementTypeId, ids.TenantId, code, $"Concurrent {code}", direction,
            true, true, false, "ACTIVE", Now));
        db.TillSessions.Add(TillSession.Open(
            ids.SessionId, ids.TenantId, ids.OutletId, ids.TillId, $"TS-{ids.Suffix}",
            DateOnly.FromDateTime(Now.UtcDateTime), ids.UserId, ids.DeviceId, openingFloat, "LKR", null, Now));
        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO till_device_assignments
                (id, tenant_id, outlet_id, till_id, pos_device_id, assigned_at,
                 assigned_by_tenant_user_id, created_at, updated_at)
            VALUES
                ({Guid.NewGuid()}, {ids.TenantId}, {ids.OutletId}, {ids.TillId}, {ids.DeviceId},
                 {Now}, {ids.UserId}, {Now}, {Now})
            """);
    }

    private static async Task CleanupAsync(FixtureIds ids)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            DELETE FROM cash_movements WHERE tenant_id = @tenant;
            DELETE FROM cash_movement_types WHERE tenant_id = @tenant;
            DELETE FROM till_device_assignments WHERE tenant_id = @tenant;
            DELETE FROM till_sessions WHERE tenant_id = @tenant;
            DELETE FROM pos_devices WHERE tenant_id = @tenant;
            DELETE FROM tills WHERE tenant_id = @tenant;
            DELETE FROM outlets WHERE tenant_id = @tenant;
            DELETE FROM tenant_users WHERE tenant_id = @tenant;
            DELETE FROM tenants WHERE id = @tenant;
            """, connection);
        command.Parameters.AddWithValue("tenant", ids.TenantId);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record FixtureIds(
        Guid TenantId,
        Guid OutletId,
        Guid TillId,
        Guid DeviceId,
        Guid UserId,
        Guid SessionId,
        Guid RequestId,
        Guid MovementTypeId,
        string Suffix)
    {
        public static FixtureIds Create()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            return new(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), suffix);
        }
    }
}
