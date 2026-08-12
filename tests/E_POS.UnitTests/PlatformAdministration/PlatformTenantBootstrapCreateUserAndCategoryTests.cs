using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Repositories;
using E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using E_POS.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantBootstrapCreateUserAndCategoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);
    private const string CorrelationId = "corr-test-1";

    [Fact]
    public async Task BootstrapCreateUser_PersistsHashedInviteAndPlatformActorAudit()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var platformUserId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        dbContext.TenantRoles.Add(TenantRole.Create(
            roleId,
            tenantId,
            null,
            null,
            "STORE_MANAGER",
            "Store Manager",
            "Bootstrap role",
            isCustom: true,
            isActive: true,
            createdByTenantUserId: null,
            Now));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.CreateUserAsync(
            tenantId,
            platformUserId,
            new PlatformTenantBootstrapUserCreateRequest
            {
                DisplayName = "Jane Doe",
                Email = "jane@example.com",
                RoleId = roleId
            },
            "user-key-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var invite = await dbContext.UserInvites
            .AsNoTracking()
            .SingleAsync(item => item.TenantId == tenantId);
        Assert.Equal("hmac-hash-value", invite.InviteTokenHash);
        Assert.Equal(roleId, invite.InitialRoleId);
        Assert.Equal(platformUserId, invite.InvitedByPlatformUserId);

        var audit = await dbContext.TenantSubscriptionHistory
            .AsNoTracking()
            .SingleAsync(item => item.TenantId == tenantId);
        Assert.Equal("platform.tenant_bootstrap.user_created", audit.ChangeType);
        Assert.Equal(platformUserId, audit.ChangedByPlatformUserId);
        Assert.Contains(CorrelationId, audit.ChangeData ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains(platformUserId.ToString(), audit.ChangeData ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProduct_WithCrossTenantCategory_ReturnsDependencyMissing()
    {
        await using var dbContext = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var foreignCategoryId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        SeedTenant(dbContext, tenantA, "BOOT-A");
        SeedTenant(dbContext, tenantB, "BOOT-B");
        dbContext.Categories.Add(Category.Create(
            foreignCategoryId,
            tenantB,
            Guid.Empty,
            null,
            "FOREIGN",
            "Foreign",
            "foreign",
            null,
            null,
            1,
            CategoryConstants.ActiveStatus,
            null,
            Now));
        dbContext.UnitOfMeasures.Add(UnitOfMeasure.Create(
            unitId,
            tenantA,
            "EA",
            "Each",
            "COUNT",
            "ea",
            null,
            1m,
            ProductConstants.ActiveStatus,
            Now));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.CreateProductAsync(
            tenantA,
            Guid.NewGuid(),
            new PlatformTenantBootstrapProductCreateRequest
            {
                ProductName = "Blocked Product",
                Sku = "BLK-1",
                SellingPrice = 10m,
                CategoryId = foreignCategoryId,
                TrackInventory = false,
                Status = "ACTIVE"
            },
            "product-key-1",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.bootstrap.dependency_missing", result.Error.Code);
        Assert.Equal(0, await dbContext.Products.CountAsync(product => product.TenantId == tenantA));
        Assert.Equal(0, await dbContext.ProductCategories.CountAsync());
    }

    [Fact]
    public async Task ProductImport_Commit_PartialSuccessAndIdempotentReplay()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var platformUserId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        SeedTenant(dbContext, tenantId);
        dbContext.UnitOfMeasures.Add(UnitOfMeasure.Create(
            unitId,
            tenantId,
            "EA",
            "Each",
            "COUNT",
            "ea",
            null,
            1m,
            ProductConstants.ActiveStatus,
            Now));

        var validRequest = new PlatformTenantBootstrapProductCreateRequest
        {
            ProductName = "Import Rice",
            Sku = "IMP-RICE-1",
            SellingPrice = 11m,
            TrackInventory = false,
            Status = "ACTIVE"
        };
        var batch = Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportBatch.CreateValidated(
            importId,
            tenantId,
            "products.csv",
            2,
            1,
            1,
            platformUserId,
            Now);
        var validRow = Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportRow.Create(
            Guid.NewGuid(),
            importId,
            tenantId,
            1,
            System.Text.Json.JsonSerializer.Serialize(validRequest),
            true,
            null,
            null,
            Now);
        var invalidRow = Domain.Modules.Platform.PlatformAdmin.Entities.PlatformTenantBootstrapProductImportRow.Create(
            Guid.NewGuid(),
            importId,
            tenantId,
            2,
            """{"productName":"","sku":""}""",
            false,
            "import.invalid_row",
            "bad row",
            Now);
        dbContext.PlatformTenantBootstrapProductImportBatches.Add(batch);
        dbContext.PlatformTenantBootstrapProductImportRows.AddRange(validRow, invalidRow);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var first = await service.CommitProductImportAsync(
            tenantId,
            platformUserId,
            importId,
            "import-commit-key",
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value!.CommittedRows);
        Assert.Equal(1, first.Value.SkippedRows);
        Assert.Equal(1, await dbContext.Products.CountAsync(product => product.TenantId == tenantId));

        var replay = await service.CommitProductImportAsync(
            tenantId,
            platformUserId,
            importId,
            "import-commit-key",
            CancellationToken.None);

        Assert.True(replay.IsSuccess);
        Assert.Equal(1, replay.Value!.CommittedRows);
        Assert.Equal(1, replay.Value.SkippedRows);
        Assert.Equal(1, await dbContext.Products.CountAsync(product => product.TenantId == tenantId));
    }

    private static PlatformTenantBootstrapService CreateService(EPosDbContext dbContext)
    {
        var bootstrapRepository = new PlatformTenantBootstrapRepository(dbContext);
        var permissionChecker = new AllowAllPermissionChecker();
        var accessPolicy = new PlatformSelectedTenantAccessPolicy(bootstrapRepository, permissionChecker);
        var codeSequence = new CodeSequenceRepository(dbContext);

        return new PlatformTenantBootstrapService(
            accessPolicy,
            bootstrapRepository,
            new PlatformTenantRepository(dbContext),
            permissionChecker,
            new AlwaysEnabledEntitlementEvaluator(),
            new UnusedOutletRepository(),
            new UnusedOutletRequestValidator(),
            codeSequence,
            new AllowingTenantResourceLimitGuard(),
            new UnusedTillRepository(),
            new TenantAdminUserRepository(dbContext),
            new ProductRepository(dbContext),
            new TenantAdminProductRepository(dbContext, codeSequence),
            new FixedDateTimeProvider(Now),
            new FixedCorrelationAccessor(CorrelationId),
            new FakeInvitationTokenService(),
            new Lazy<IInvitationDeliverySecretProtector>(() => new FakeDeliverySecretProtector()),
            new FakeStaffCodeService());
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new EPosDbContext(options);
    }

    private static void SeedTenant(EPosDbContext dbContext, Guid tenantId, string tenantCode = "BOOT-001")
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            tenantCode,
            tenantCode.ToLowerInvariant(),
            "Bootstrap Tenant",
            TenantStatusConstants.Active,
            "LKR",
            "Asia/Colombo",
            "en-LK",
            null,
            Now));
    }

    private sealed class AllowAllPermissionChecker : IPlatformPermissionChecker
    {
        public Task<bool> HasPermissionAsync(Guid platformUserId, string permissionCode, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class AlwaysEnabledEntitlementEvaluator : ITenantFeatureEntitlementEvaluator
    {
        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(
            Guid tenantId,
            string featureCode,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(TenantFeatureEntitlementEvaluation.Allowed(
                featureCode,
                featureCode,
                usedLegacyAlias: false,
                foundCanonicalRecord: true,
                foundLegacyRecord: false));

        public Task<bool> IsEnabledAsync(
            Guid tenantId,
            string featureCode,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class FixedDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FixedCorrelationAccessor(string correlationId) : IRequestCorrelationAccessor
    {
        public string CorrelationId => correlationId;
        public void Set(string value) { }
    }

    private sealed class FakeInvitationTokenService : IInvitationTokenService
    {
        public string GenerateToken() => "raw-secret-token";
        public string HashToken(string rawToken) => "hmac-hash-value";
    }

    private sealed class FakeDeliverySecretProtector : IInvitationDeliverySecretProtector
    {
        public ProtectedInvitationDeliverySecret Protect(string rawToken) =>
            new("cipher:" + rawToken, "test");

        public string Unprotect(string ciphertext, string keyVersion) => ciphertext[7..];
    }

    private sealed class FakeStaffCodeService : ITenantUserStaffCodeService
    {
        public Task<string> GenerateAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult("USR-2026-00001");
    }

    private sealed class UnusedOutletRequestValidator : IOutletRequestValidator
    {
        public ApplicationError? ValidateCreate(OutletCreateRequest request) => null;
        public ApplicationError? ValidateUpdate(OutletUpdateRequest request) => null;
    }

    private sealed class UnusedOutletRepository : IOutletRepository
    {
        public Task<bool> OutletCodeExistsAsync(Guid tenantId, string outletCode, Guid? excludeOutletId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task<Guid?> GetActivePickupFulfillmentMethodIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(null);
        public Task<OutletSummaryDashboardResponse> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<OutletListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, string? outletType, string? status, string? sortBy, string? sortDirection, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<OutletResponse?> GetByIdAsync(Guid tenantId, Guid outletId, bool includeDeleted, CancellationToken cancellationToken) =>
            Task.FromResult<OutletResponse?>(null);
        public Task<OutletEditAggregate?> GetEditAggregateAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) =>
            Task.FromResult<OutletEditAggregate?>(null);
        public Task<bool> HasActiveTillOrDeviceAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task<bool> AllOutletsBelongToTenantAsync(Guid tenantId, Guid[] outletIds, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(TenantStatusConstants.Active);
        public Task<bool> IsOutletManagementFeatureEnabledAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task<bool> IsClickCollectFeatureEnabledAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task<OutletCreateOptionsResponse> GetCreateOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> AddAsync(Outlet outlet, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? pickupMapping, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task<bool> SaveUpdatedAsync(OutletEditAggregate aggregate, OutletAddress address, IReadOnlyCollection<OutletBusinessHour> businessHours, FulfillmentMethodOutlet? newPickupMapping, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class UnusedTillRepository : ITenantAdminTillRepository
    {
        public Task<bool> OutletBelongsToTenantAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task<string> GetTenantBaseCurrencyCodeAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult("LKR");
        public Task<bool> IsValidCashierAsync(Guid tenantId, Guid outletId, Guid tenantUserId, CancellationToken cancellationToken) =>
            Task.FromResult(true);
        public Task<bool> TillCodeExistsForTenantAsync(Guid tenantId, string tillCode, Guid? excludeTillId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task<int> GetNextTillNumberAsync(Guid tenantId, Guid outletId, string tillAreaName, CancellationToken cancellationToken) =>
            Task.FromResult(1);
        public Task<(IReadOnlyList<TillMonitoringReadModel> Items, int TotalCount)> ListAsync(
            Guid tenantId, string? search, string? status, Guid? outletId, int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<TillMonitoringReadModel>, int)>(([], 0));
        public Task<TenantAdminTillSummaryResponse> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<TillMonitoringReadModel?> GetDetailAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) =>
            Task.FromResult<TillMonitoringReadModel?>(null);
        public Task AddAsync(Till till, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Till?> GetEditableAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) =>
            Task.FromResult<Till?>(null);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> HasActiveDeviceAssignmentAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task<bool> HasActiveSessionAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task<bool> HasSalesAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task<bool> HasCashMovementsAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public Task<IReadOnlyList<TenantAdminOutletOptionResponse>> GetOutletOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TenantAdminOutletOptionResponse>>([]);
        public Task<TenantAdminTillCreateOptionsResponse> GetCreateOptionsAsync(Guid tenantId, Guid? outletId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<TillHardwareReadinessReadModel>> GetHardwareReadinessDataAsync(
            Guid tenantId, Guid tillId, Guid? activePosDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TillHardwareReadinessReadModel>>([]);
        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken) => operation();
    }
}
