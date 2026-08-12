using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Validators;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Platform.Subscription.Services;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace E_POS.IntegrationTests.SubscriptionBilling;

/// <summary>
/// Proves Phase 3 advisory-lock capacity enforcement against real PostgreSQL.
/// Uses an isolated disposable database (EnsureCreated) so shared-dev schema drift cannot invalidate the proof.
/// Soft-skips when local PostgreSQL is unavailable (same pattern as ManualPaymentPostgreSqlConcurrencyTests).
/// </summary>
public sealed class TenantResourceLimitPostgreSqlConcurrencyTests
{
    private const string BaseConnectionString =
        "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

    private static readonly DateTimeOffset Now = new(2026, 8, 6, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ConcurrentOutletCreate_FinalSlot_AllowsExactlyOne_OnPostgreSql()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync();
        var fixture = FixtureIds.Create();
        await SeedAsync(harness.ConnectionString, fixture, preSeedOutletCount: 2, planLimit: 3);

        await using (var providerProbe = CreateDb(harness.ConnectionString))
        {
            Assert.Contains("Npgsql", providerProbe.Database.ProviderName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.True(providerProbe.Database.IsNpgsql());
        }

        await using var firstDb = CreateDb(harness.ConnectionString);
        await using var secondDb = CreateDb(harness.ConnectionString);
        var firstService = CreateOutletService(firstDb);
        var secondService = CreateOutletService(secondDb);

        var barrier = new Barrier(2);
        var results = new ApplicationResult<OutletResponse>?[2];

        async Task RunCreateAsync(int index, OutletService service, OutletCreateRequest request)
        {
            barrier.SignalAndWait(TimeSpan.FromSeconds(30));
            results[index] = await service.CreateAsync(
                CreateOutletContext(fixture.TenantId, fixture.ActorUserId),
                request,
                CancellationToken.None);
        }

        await Task.WhenAll(
            RunCreateAsync(0, firstService, CreateOutletRequest("Concurrent-A")),
            RunCreateAsync(1, secondService, CreateOutletRequest("Concurrent-B")));

        Assert.NotNull(results[0]);
        Assert.NotNull(results[1]);

        var successCount = results.Count(x => x!.IsSuccess);
        var limitDenied = results.Where(x => x!.IsFailure && x.Error.Code == SubscriptionLimitErrorCodes.LimitReached).ToList();

        Assert.Equal(1, successCount);
        Assert.Single(limitDenied);

        await using var assertDb = CreateDb(harness.ConnectionString);
        Assert.True(assertDb.Database.IsNpgsql());

        var finalCount = await assertDb.Outlets.CountAsync(x =>
            x.TenantId == fixture.TenantId &&
            x.Status != OutletConstants.DeletedStatus);
        Assert.Equal(3, finalCount);

        var names = await assertDb.Outlets
            .AsNoTracking()
            .Where(x => x.TenantId == fixture.TenantId && x.Status != OutletConstants.DeletedStatus)
            .Select(x => x.OutletName)
            .ToListAsync();
        Assert.Contains("Seed-1", names);
        Assert.Contains("Seed-2", names);
        Assert.Single(names, name => name is "Concurrent-A" or "Concurrent-B");
    }

    [Fact]
    public async Task ConcurrentOutletCreate_DifferentTenants_DoNotBlockEachOther_OnPostgreSql()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await using var harness = await DisposablePostgresHarness.CreateAsync();
        var tenantA = FixtureIds.Create();
        var tenantB = FixtureIds.Create();
        await SeedAsync(harness.ConnectionString, tenantA, preSeedOutletCount: 0, planLimit: 1);
        await SeedAsync(harness.ConnectionString, tenantB, preSeedOutletCount: 0, planLimit: 1);

        await using var firstDb = CreateDb(harness.ConnectionString);
        await using var secondDb = CreateDb(harness.ConnectionString);
        var barrier = new Barrier(2);
        var results = new ApplicationResult<OutletResponse>?[2];

        async Task RunCreateAsync(int index, Guid tenantId, OutletService service)
        {
            barrier.SignalAndWait(TimeSpan.FromSeconds(30));
            results[index] = await service.CreateAsync(
                CreateOutletContext(tenantId, index == 0 ? tenantA.ActorUserId : tenantB.ActorUserId),
                CreateOutletRequest($"Tenant-{index}"),
                CancellationToken.None);
        }

        await Task.WhenAll(
            RunCreateAsync(0, tenantA.TenantId, CreateOutletService(firstDb)),
            RunCreateAsync(1, tenantB.TenantId, CreateOutletService(secondDb)));

        Assert.True(results[0]!.IsSuccess);
        Assert.True(results[1]!.IsSuccess);

        await using var assertDb = CreateDb(harness.ConnectionString);
        Assert.Equal(1, await assertDb.Outlets.CountAsync(x => x.TenantId == tenantA.TenantId && x.Status != OutletConstants.DeletedStatus));
        Assert.Equal(1, await assertDb.Outlets.CountAsync(x => x.TenantId == tenantB.TenantId && x.Status != OutletConstants.DeletedStatus));
    }

    private static OutletService CreateOutletService(EPosDbContext db) =>
        new(
            new OutletRepository(db),
            new CodeSequenceRepository(db),
            new OutletRequestValidator(),
            new FakeOutletAuditLogger(),
            new FixedDateTimeProvider(Now),
            new TenantFeatureEntitlementEvaluator(db, NullLogger<TenantFeatureEntitlementEvaluator>.Instance),
            new TenantResourceLimitGuard(
                db,
                new TenantSubscriptionLimitResolver(db, new FixedDateTimeProvider(Now), NullLogger<TenantSubscriptionLimitResolver>.Instance),
                new FixedDateTimeProvider(Now),
                NullLogger<TenantResourceLimitGuard>.Instance));

    private static async Task SeedAsync(
        string connectionString,
        FixtureIds fixture,
        int preSeedOutletCount,
        int planLimit)
    {
        await using var db = CreateDb(connectionString);

        if (!await db.Currencies.AnyAsync(x => x.CurrencyCode == "LKR"))
        {
            db.Currencies.Add(Currency.Create(
                Guid.NewGuid(),
                "LKR",
                "Sri Lankan Rupee",
                "Rs",
                2,
                true,
                1,
                Now));
        }

        if (!await db.PlatformModules.AnyAsync(x => x.Id == PlatformModuleCatalogPrerequisiteSeedConstants.OutletTillCoreModuleId))
        {
            db.PlatformModules.Add(PlatformModule.Create(
                PlatformModuleCatalogPrerequisiteSeedConstants.OutletTillCoreModuleId,
                PlatformModuleCatalogPrerequisiteSeedConstants.OutletTillCoreModuleCode,
                "Outlet Till Core",
                "Outlet and till core module.",
                PlatformAuthConstants.ActiveStatus,
                1,
                Now,
                isCoreModule: true));
        }

        if (!await db.PlatformFeatures.AnyAsync(x => x.Id == PlatformModuleCatalogPrerequisiteSeedConstants.OutletManagementFeatureId))
        {
            db.PlatformFeatures.Add(PlatformFeature.Create(
                PlatformModuleCatalogPrerequisiteSeedConstants.OutletManagementFeatureId,
                PlatformModuleCatalogPrerequisiteSeedConstants.OutletTillCoreModuleId,
                PlatformTenantFeatureCodes.OutletManagement,
                "Outlet Management",
                SubscriptionCatalogConstants.RecordStatus.Active,
                Now));
        }

        db.Tenants.Add(Tenant.Create(
            fixture.TenantId,
            $"LCK-{fixture.Suffix}",
            $"lck-{fixture.Suffix}",
            "Limit Lock Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "UTC",
            null,
            null,
            Now));

        db.SubscriptionPlans.Add(SubscriptionPlan.Create(
            fixture.PlanId,
            $"LCK-{fixture.Suffix}",
            "Lock Plan",
            SubscriptionPlanConstants.Status.Active,
            SubscriptionPlanConstants.BillingInterval.Monthly,
            1000m,
            Now,
            maxOutlets: planLimit,
            maxUsers: 10,
            maxTills: 10));

        // Null override → plan fallback (F-P3-01 closure contract).
        db.TenantSubscriptions.Add(TenantSubscription.Create(
            fixture.SubscriptionId,
            fixture.TenantId,
            fixture.PlanId,
            TenantSubscriptionStatusConstants.Active,
            TenantSubscriptionBillingConstants.BillingCycleMonthly,
            trialStartAt: null,
            trialEndAt: null,
            billingStartAt: Now,
            nextBillingAt: null,
            autoRenew: true,
            discountType: null,
            discountValue: null,
            taxPercentage: 0m,
            invoiceEmail: null,
            paymentMethod: null,
            notes: null,
            maxOutletsOverride: null,
            maxTillsOverride: null,
            maxUsersOverride: null,
            currencyCode: "LKR",
            planPrice: 1000m,
            startedAt: Now,
            currentPeriodStart: Now,
            currentPeriodEnd: null,
            assignedByPlatformUserId: null,
            Now));

        db.TenantFeatureEntitlements.Add(TenantFeatureEntitlement.Create(
            Guid.NewGuid(),
            fixture.TenantId,
            PlatformModuleCatalogPrerequisiteSeedConstants.OutletManagementFeatureId,
            TenantEntitlementStatusConstants.Enabled,
            Now));

        db.TenantUsers.Add(TenantUser.Create(
            fixture.ActorUserId,
            fixture.TenantId,
            $"actor-{fixture.Suffix}@example.test",
            "Limit Actor",
            null,
            null,
            "hash",
            "salt",
            TenantUserConstants.StatusActive,
            "admin",
            "admin",
            "HQ",
            Now,
            staffCode: $"USR-2026-{Math.Abs(fixture.ActorUserId.GetHashCode()) % 90000 + 10000:00000}"));

        for (var i = 1; i <= preSeedOutletCount; i++)
        {
            var outletId = Guid.NewGuid();
            db.Outlets.Add(Outlet.Create(
                outletId,
                fixture.TenantId,
                $"Seed-{i}",
                $"SEED{i:000}",
                OutletConstants.ActiveStatus,
                OutletConstants.StoreOutletType,
                "UTC",
                isDefaultOutlet: i == 1,
                phone: null,
                email: null,
                createdByTenantUserId: null,
                Now));
            db.OutletAddresses.Add(OutletAddress.Create(
                Guid.NewGuid(),
                fixture.TenantId,
                outletId,
                "1 Street",
                null,
                "Colombo",
                "Western",
                "00100",
                "LK",
                null,
                null,
                null,
                null,
                Now));
            db.OutletBusinessHours.Add(OutletBusinessHour.Create(
                Guid.NewGuid(),
                fixture.TenantId,
                outletId,
                1,
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                false,
                null,
                null,
                Now));
        }

        await db.SaveChangesAsync();
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(BaseConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static EPosDbContext CreateDb(string connectionString) =>
        new(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options);

    private static TenantRequestContext CreateOutletContext(Guid tenantId, Guid actorUserId) =>
        new(tenantId, actorUserId, [OutletConstants.ManagePermission]);

    private static OutletCreateRequest CreateOutletRequest(string name) =>
        new(
            name,
            OutletConstants.ActiveStatus,
            "STORE",
            "UTC",
            false,
            null,
            null,
            new OutletAddressRequest("1 Street", null, "Colombo", "Western", "00100", "LK", null, null, null),
            [new OutletBusinessHourRequest(1, new TimeOnly(9, 0), new TimeOnly(17, 0), false, null, null)],
            false);

    private sealed class DisposablePostgresHarness : IAsyncDisposable
    {
        private readonly string _databaseName;
        private readonly string _adminConnectionString;

        private DisposablePostgresHarness(string databaseName, string connectionString, string adminConnectionString)
        {
            _databaseName = databaseName;
            ConnectionString = connectionString;
            _adminConnectionString = adminConnectionString;
        }

        public string ConnectionString { get; }

        public static async Task<DisposablePostgresHarness> CreateAsync()
        {
            var databaseName = $"flow4_limit_concurrency_{Guid.NewGuid():N}";
            var adminConnectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString)
            {
                Database = "postgres"
            }.ConnectionString;

            await using (var admin = new NpgsqlConnection(adminConnectionString))
            {
                await admin.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
                await create.ExecuteNonQueryAsync();
            }

            var connectionString = new NpgsqlConnectionStringBuilder(BaseConnectionString)
            {
                Database = databaseName,
                IncludeErrorDetail = true
            }.ConnectionString;

            await using (var db = new EPosDbContext(
                             new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(connectionString).Options))
            {
                await db.Database.EnsureCreatedAsync();
                Assert.True(db.Database.IsNpgsql());
            }

            return new DisposablePostgresHarness(databaseName, connectionString, adminConnectionString);
        }

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(_adminConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                             "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()",
                             admin))
            {
                terminate.Parameters.AddWithValue("database", _databaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed record FixtureIds(
        Guid TenantId,
        Guid PlanId,
        Guid SubscriptionId,
        Guid ActorUserId,
        string Suffix)
    {
        public static FixtureIds Create()
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            return new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), suffix);
        }
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FakeOutletAuditLogger : IOutletAuditLogger
    {
        public void LogOutletCreated(Guid tenantId, Guid actorTenantUserId, Guid outletId, string outletCode, string outletType, string status) { }
        public void LogManagerAssigned(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid managerTenantUserId) { }
        public void LogManagerRemoved(Guid tenantId, Guid actorTenantUserId, Guid outletId) { }
        public void LogImageAssociated(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid mediaAssetId) { }
        public void LogImageRemoved(Guid tenantId, Guid actorTenantUserId, Guid outletId) { }
        public void LogImageUploaded(Guid tenantId, Guid actorTenantUserId, Guid mediaAssetId) { }
        public void LogImageReplaced(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid previousMediaAssetId, Guid newMediaAssetId) { }
        public void LogImageDetached(Guid tenantId, Guid actorTenantUserId, Guid outletId, Guid detachedMediaAssetId) { }
        public void LogStatusChanged(Guid tenantId, Guid actorTenantUserId, Guid outletId, string status) { }
    }
}
