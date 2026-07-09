using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformTenantWizardServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 20, 0, 0, TimeSpan.Zero);
    private static readonly Guid PlanId = Guid.Parse("81111111-1111-4111-8111-111111111111");
    private static readonly Guid FeatureId = Guid.Parse("82222222-2222-4222-8222-222222222222");
    private static readonly Guid AddonId = Guid.Parse("83333333-3333-4333-8333-333333333333");
    private static readonly Guid PermissionId = Guid.Parse("84444444-4444-4444-8444-444444444444");

    [Fact]
    public async Task GetCreateOptionsAsync_WithoutCreatePermission_ReturnsAccessDenied()
    {
        var service = CreateService(
            new FakeWizardTenantRepository(),
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsView });

        var result = await service.GetCreateOptionsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_WithCreatePermission_ReturnsOptions()
    {
        var repository = new FakeWizardTenantRepository();
        var service = CreateService(
            repository,
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsCreate });

        var result = await service.GetCreateOptionsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Plans);
        Assert.Single(result.Value.Addons);
        Assert.Single(result.Value.CountryCodes);
        Assert.Equal("LK", result.Value.CountryCodes[0].Code);
        Assert.Equal("Sri Lanka", result.Value.CountryCodes[0].Name);
    }

    [Fact]
    public async Task CreateTenantAsync_WizardRequest_AutoSeedsPlanFeatures()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeWizardTenantRepository
        {
            DetailResponse = CreateDetail(tenantId)
        };
        var service = CreateService(
            repository,
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsCreate });

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-WIZ-001",
                Name = "Wizard Tenant",
                SubscriptionPlanId = PlanId,
                TenantAdmin = new CreatePlatformTenantAdminRequest
                {
                    FirstName = "Ada",
                    LastName = "Lovelace",
                    Email = "ada@tenant.com",
                    SendInvite = true
                }
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.CreateWizardCalled);
        Assert.NotNull(repository.LastWriteModel);
        Assert.Single(repository.LastWriteModel!.Entitlements);
        Assert.Equal(FeatureId, repository.LastWriteModel.Entitlements[0].PlatformFeatureId);
        Assert.NotNull(repository.LastWriteModel.TenantAdminInvite);
    }

    [Fact]
    public async Task CreateTenantAsync_WizardWithPassword_ReturnsValidationFailureBecauseTempPasswordIsDeferred()
    {
        var repository = new FakeWizardTenantRepository();
        var service = CreateService(
            repository,
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsCreate });

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-WIZ-002",
                Name = "Wizard Tenant",
                SubscriptionPlanId = PlanId,
                TenantAdmin = new CreatePlatformTenantAdminRequest
                {
                    FirstName = "Grace",
                    Email = "grace@tenant.com",
                    SendInvite = false,
                    TemporaryPassword = "Temp#1234"
                }
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
        Assert.Contains("Temporary password provisioning is deferred", result.Error.Message, StringComparison.Ordinal);
        Assert.False(repository.CreateWizardCalled);
    }

    [Fact]
    public async Task CreateTenantAsync_WizardWithInvalidCountryCode_ReturnsValidationFailureBeforeSave()
    {
        var repository = new FakeWizardTenantRepository();
        var service = CreateService(
            repository,
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsCreate });

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-WIZ-COUNTRY",
                Name = "Wizard Tenant",
                CountryCode = "Sri Lanka",
                BaseCurrency = "LKR",
                SubscriptionPlanId = PlanId,
                TenantAdmin = new CreatePlatformTenantAdminRequest
                {
                    FirstName = "Nimal",
                    Email = "nimal@tenant.com",
                    SendInvite = true
                }
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, item => item.Field == "countryCode");
        Assert.False(repository.CreateWizardCalled);
    }

    [Fact]
    public async Task CreateTenantAsync_WizardWithInvalidLimit_ReturnsValidationFailure()
    {
        var repository = new FakeWizardTenantRepository();
        var service = CreateService(
            repository,
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsCreate });

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-WIZ-003",
                Name = "Wizard Tenant",
                SubscriptionPlanId = PlanId,
                Limits = new CreatePlatformTenantLimitsRequest { MaxOutlets = 99 },
                TenantAdmin = new CreatePlatformTenantAdminRequest
                {
                    FirstName = "Alan",
                    Email = "alan@tenant.com",
                    SendInvite = true
                }
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
        Assert.False(repository.CreateWizardCalled);
    }

    [Fact]
    public async Task CreateTenantAsync_WizardWithUnknownAddon_ReturnsValidationFailure()
    {
        var repository = new FakeWizardTenantRepository();
        var service = CreateService(
            repository,
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsCreate });

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-WIZ-004",
                Name = "Wizard Tenant",
                SubscriptionPlanId = PlanId,
                Addons = [new CreatePlatformTenantAddonSelectionRequest { AddonId = Guid.NewGuid(), Quantity = 1 }],
                TenantAdmin = new CreatePlatformTenantAdminRequest
                {
                    FirstName = "Ken",
                    Email = "ken@tenant.com",
                    SendInvite = true
                }
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateTenantAsync_WizardWhenAdminEmailExists_ReturnsConflict()
    {
        var repository = new FakeWizardTenantRepository { TenantUserEmailExists = true };
        var service = CreateService(
            repository,
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsCreate });

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-WIZ-005",
                Name = "Wizard Tenant",
                SubscriptionPlanId = PlanId,
                TenantAdmin = new CreatePlatformTenantAdminRequest
                {
                    FirstName = "Tim",
                    Email = "tim@tenant.com",
                    SendInvite = true
                }
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_tenants.conflict", result.Error.Code);
    }

    [Fact]
    public async Task CreateTenantAsync_WizardWithPendingBilling_CreatesDraftInvoice()
    {
        var tenantId = Guid.NewGuid();
        var repository = new FakeWizardTenantRepository
        {
            DetailResponse = CreateDetail(tenantId)
        };
        var service = CreateService(
            repository,
            permissions: new HashSet<string>(StringComparer.Ordinal) { PlatformPermissionCodes.TenantsCreate });

        var result = await service.CreateTenantAsync(
            new CreatePlatformTenantRequest
            {
                Code = "TEN-WIZ-006",
                Name = "Wizard Tenant",
                BillingStatus = TenantBillingStatusConstants.Pending,
                SubscriptionPlanId = PlanId,
                Addons = [new CreatePlatformTenantAddonSelectionRequest { AddonId = AddonId, Quantity = 2 }],
                TenantAdmin = new CreatePlatformTenantAdminRequest
                {
                    FirstName = "Jude",
                    Email = "jude@tenant.com",
                    SendInvite = true
                }
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastWriteModel);
        Assert.NotNull(repository.LastWriteModel!.DraftInvoice);
        Assert.Equal(60m, repository.LastWriteModel.DraftInvoice!.TotalAmount);
        Assert.Equal(60m, repository.LastWriteModel.DraftInvoice.SubtotalAmount);
        Assert.Equal(60m, repository.LastWriteModel.DraftInvoice.BalanceDue);
        Assert.Equal("LKR", repository.LastWriteModel.DraftInvoice.CurrencyCode);
        Assert.Equal(SubscriptionBillingAlignmentConstants.InvoiceTypeSubscription, repository.LastWriteModel.DraftInvoice.InvoiceType);
    }

    private static PlatformTenantService CreateService(
        FakeWizardTenantRepository repository,
        IReadOnlySet<string> permissions,
        IPasswordHashService? passwordHashService = null)
    {
        var subscriptionRepository = new FakePlatformSubscriptionPlanRepository
        {
            PlanEntity = SubscriptionPlan.Create(
                PlanId,
                "STARTER",
                "Starter",
                SubscriptionPlanConstants.Status.Active,
                SubscriptionPlanConstants.BillingInterval.Monthly,
                50m,
                Now,
                maxOutlets: 5,
                maxUsers: 5,
                maxTills: 5)
        };

        return new PlatformTenantService(
            repository,
            subscriptionRepository,
            new FakePermissionChecker(permissions),
            new FakePermissionRepository(permissions),
            new FakeDateTimeProvider(),
            passwordHashService ?? new FakePasswordHashService());
    }

    private static PlatformTenantDetailResponse CreateDetail(Guid tenantId) =>
        new(
            tenantId,
            "TEN-001",
            "Tenant One",
            TenantStatusConstants.Draft,
            TenantBillingStatusConstants.Pending,
            TenantOperatingModeConstants.UnifiedEpos,
            "LKR",
            "Asia/Colombo",
            "en-LK",
            "retail",
            null,
            null,
            null,
            0,
            0,
            0,
            false,
            false,
            false,
            [],
            [],
            Now,
            Now,
            Now,
            false,
            false,
            false,
            false);

    private sealed class FakeWizardTenantRepository : IPlatformTenantRepository
    {
        public bool TenantCodeExists { get; set; }
        public bool TenantUserEmailExists { get; set; }
        public bool CreateWizardCalled { get; private set; }
        public PlatformTenantCreateWriteModel? LastWriteModel { get; private set; }
        public PlatformTenantDetailResponse? DetailResponse { get; init; }

        public Task<PlatformTenantListResponse> GetTenantsAsync(
            PlatformTenantListQuery query,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlatformTenantSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlatformTenantFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<PlatformTenantDetailResponse?> GetTenantDetailAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(DetailResponse);

        public Task<PlatformTenantEntitlementOptionsResponse?> GetEntitlementOptionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PlatformTenantEntitlementOptionsResponse?>(null);

        public Task<bool> TenantCodeExistsAsync(string tenantCode, CancellationToken cancellationToken) =>
            Task.FromResult(TenantCodeExists);

        public Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<Tenant?>(null);

        public Task AddTenantWithSubscriptionAndEntitlementsAsync(
            Tenant tenant,
            TenantSubscription subscription,
            IReadOnlyList<Guid> enabledFeatureIds,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UpdateTenantAsync(Tenant tenant, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<TenantSubscription?> GetCurrentTenantSubscriptionEntityAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<TenantSubscription?>(null);

        public Task UpdateTenantSubscriptionAsync(
            TenantSubscription subscription,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ReplaceTenantEntitlementsAsync(
            Guid tenantId,
            IReadOnlyList<Guid> enabledFeatureIds,
            DateTimeOffset now,
            Guid? actorPlatformUserId,
            string? revokedReason,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlySet<Guid>> GetIncludedFeatureIdsForPlanAsync(
            Guid planId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid> { FeatureId });

        public Task<IReadOnlyList<ResolvedTenantFeature>> ResolveActiveFeaturesAsync(
            IReadOnlyList<Guid>? featureIds,
            IReadOnlyList<string>? featureCodes,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ResolvedTenantFeature>>(
            [
                new ResolvedTenantFeature(FeatureId, "online_store")
            ]);
        }

        public Task<PlatformTenantCreateOptionsResponse> GetCreateOptionsAsync(CancellationToken cancellationToken)
        {
            var response = new PlatformTenantCreateOptionsResponse(
                Plans:
                [
                    new PlatformTenantCreatePlanOptionDto(
                        PlanId,
                        "STARTER",
                        "Starter",
                        null,
                        "active",
                        "MONTHLY",
                        "LKR",
                        50m,
                        5,
                        5,
                        5,
                        [FeatureId],
                        ["online_store"])
                ],
                Addons:
                [
                    new PlatformTenantCreateAddonOptionDto(
                        AddonId,
                        "EXTRA_OUTLET",
                        "Extra Outlet",
                        null,
                        5m,
                        "LKR",
                        "online_store",
                        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["max_outlets"] = 1
                        })
                ],
                CatalogModules: [],
                BillingStatuses: [],
                PaymentMethods: [],
                CountryCodes:
                [
                    new PlatformTenantCreateCountryOptionDto("LK", "Sri Lanka")
                ],
                Currencies: [],
                Timezones: [],
                Locales: [],
                BusinessTypes: [],
                OperatingModes: [],
                SubscriptionStatuses: [],
                BillingCycles: []);

            return Task.FromResult(response);
        }

        public Task<bool> TenantUserEmailExistsAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(TenantUserEmailExists);

        public Task CreateTenantWizardAsync(PlatformTenantCreateWriteModel model, CancellationToken cancellationToken)
        {
            CreateWizardCalled = true;
            LastWriteModel = model;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> GetTenantAdminBootstrapPermissionIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([PermissionId]);
    }

    private sealed class FakePermissionChecker : IPlatformPermissionChecker
    {
        private readonly IReadOnlySet<string> _permissions;

        public FakePermissionChecker(IReadOnlySet<string> permissions)
        {
            _permissions = permissions;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(_permissions.Contains(permissionCode));
    }

    private sealed class FakePermissionRepository : IPlatformPermissionRepository
    {
        private readonly IReadOnlySet<string> _permissions;

        public FakePermissionRepository(IReadOnlySet<string> permissions)
        {
            _permissions = permissions;
        }

        public Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_permissions);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string? LastPassword { get; private set; }

        public string HashPassword(string password)
        {
            LastPassword = password;
            return $"HASHED:{password}";
        }

        public bool VerifyPassword(string password, string passwordHash) =>
            passwordHash == $"HASHED:{password}";
    }

    private sealed class FakePlatformSubscriptionPlanRepository : IPlatformSubscriptionPlanRepository
    {
        public SubscriptionPlan? PlanEntity { get; init; }

        public Task<SubscriptionPlan?> GetPlanEntityByIdAsync(Guid planId, CancellationToken cancellationToken) =>
            Task.FromResult(PlanEntity?.Id == planId ? PlanEntity : null);

        public Task<SubscriptionPlanListResponse> GetPlansAsync(
            SubscriptionPlanListQuery query,
            SubscriptionPlanPermissionFlags permissionFlags,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<SubscriptionPlanCatalogResponse> GetCatalogAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> PlanCodeExistsAsync(string planCode, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<SubscriptionPlanMutationResponse?> GetPlanByIdAsync(
            Guid planId,
            SubscriptionPlanPermissionFlags permissionFlags,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task ReplacePlanFeaturesAsync(
            Guid planId,
            IReadOnlyList<Guid> featureIds,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlySet<Guid>> GetActiveFeatureIdsAsync(
            IReadOnlyCollection<Guid> featureIds,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<int> GetFeatureCountAsync(Guid planId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task UpsertLegacyPlanLimitsAsync(
            Guid planId,
            int? maxOutlets,
            int? maxUsers,
            int? maxTills,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlyDictionary<string, decimal?>> GetPlanLimitValuesByKeyAsync(
            Guid planId,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}


