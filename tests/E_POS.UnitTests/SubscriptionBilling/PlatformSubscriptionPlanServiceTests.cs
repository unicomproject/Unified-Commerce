using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.SubscriptionBilling;

public sealed class PlatformSubscriptionPlanServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetPlansAsync_WithPermission_ReturnsPlans()
    {
        var service = CreateService(
            new FakePlatformSubscriptionPlanRepository
            {
                ListResponse = new SubscriptionPlanListResponse([], 1, 10, 0, 0, true, true, true, true, true)
            },
            hasView: true);

        var result = await service.GetPlansAsync(new SubscriptionPlanListQuery(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanCreate);
    }

    [Fact]
    public async Task GetPlansAsync_WithoutPermission_ReturnsForbidden()
    {
        var service = CreateService(new FakePlatformSubscriptionPlanRepository(), hasView: false);

        var result = await service.GetPlansAsync(new SubscriptionPlanListQuery(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_subscription_plans.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateDraftAsync_WithValidRequest_ReturnsDraftPlan()
    {
        var repository = new FakePlatformSubscriptionPlanRepository();
        var service = CreateService(repository, hasCreate: true);

        var result = await service.CreateDraftAsync(
            new CreateSubscriptionPlanRequest
            {
                PlanCode = "slice8_test",
                Name = "Slice 8 Test",
                BillingCycle = "monthly",
                BaseCurrency = "LKR",
                BasePrice = 99.99m
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("SLICE8_TEST", result.Value!.PlanCode);
        Assert.Equal(SubscriptionPlanConstants.Status.Draft, result.Value.Status);
        Assert.True(repository.AddCalled);
    }

    [Fact]
    public async Task CreateDraftAsync_WithDuplicatePlanCode_ReturnsConflict()
    {
        var service = CreateService(
            new FakePlatformSubscriptionPlanRepository { PlanCodeExists = true },
            hasCreate: true);

        var result = await service.CreateDraftAsync(
            new CreateSubscriptionPlanRequest
            {
                PlanCode = "starter",
                Name = "Starter",
                BillingCycle = "monthly"
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_subscription_plans.conflict", result.Error.Code);
    }

    [Fact]
    public async Task UpdatePricingAsync_ForActivePlan_ReturnsInvalidTransition()
    {
        var service = CreateService(
            new FakePlatformSubscriptionPlanRepository
            {
                PlanEntity = SubscriptionPlan.Create(
                    Guid.NewGuid(),
                    "STARTER",
                    "Starter",
                    SubscriptionPlanConstants.Status.Active,
                    SubscriptionPlanConstants.BillingInterval.Monthly,
                    49.99m,
                    Now)
            },
            hasEdit: true);

        var result = await service.UpdatePricingAsync(
            Guid.NewGuid(),
            new UpdateSubscriptionPlanPricingRequest { BasePrice = 59.99m },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_subscription_plans.invalid_transition", result.Error.Code);
    }

    [Fact]
    public async Task UpdateFeaturesAsync_WithUnknownFeature_ReturnsValidationFailed()
    {
        var planId = Guid.NewGuid();
        var service = CreateService(
            new FakePlatformSubscriptionPlanRepository
            {
                PlanEntity = SubscriptionPlan.Create(
                    planId,
                    "DRAFT_PLAN",
                    "Draft Plan",
                    SubscriptionPlanConstants.Status.Draft,
                    SubscriptionPlanConstants.BillingInterval.Monthly,
                    0m,
                    Now),
                ActiveFeatureIds = new HashSet<Guid>()
            },
            hasEdit: true);

        var result = await service.UpdateFeaturesAsync(
            planId,
            new UpdateSubscriptionPlanFeaturesRequest
            {
                FeatureIds = [Guid.NewGuid()]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_subscription_plans.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task PublishAsync_ForDraftWithLimits_ReturnsActivePlan()
    {
        var planId = Guid.NewGuid();
        var repository = new FakePlatformSubscriptionPlanRepository
        {
            PlanEntity = SubscriptionPlan.Create(
                planId,
                "DRAFT_PLAN",
                "Draft Plan",
                SubscriptionPlanConstants.Status.Draft,
                SubscriptionPlanConstants.BillingInterval.Monthly,
                49.99m,
                Now,
                maxOutlets: 1),
            MutationResponse = new SubscriptionPlanMutationResponse(
                planId,
                "DRAFT_PLAN",
                "Draft Plan",
                SubscriptionPlanConstants.Status.Active,
                "monthly",
                "LKR",
                49.99m,
                1,
                null,
                null,
                0,
                Now)
        };

        var service = CreateService(repository, hasEdit: true);

        var result = await service.PublishAsync(planId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionPlanConstants.Status.Active, result.Value!.Status);
        Assert.True(repository.SaveCalled);
    }

    [Fact]
    public async Task PublishAsync_WithoutLimits_ReturnsValidationFailed()
    {
        var planId = Guid.NewGuid();
        var service = CreateService(
            new FakePlatformSubscriptionPlanRepository
            {
                PlanEntity = SubscriptionPlan.Create(
                    planId,
                    "DRAFT_PLAN",
                    "Draft Plan",
                    SubscriptionPlanConstants.Status.Draft,
                    SubscriptionPlanConstants.BillingInterval.Monthly,
                    49.99m,
                    Now)
            },
            hasEdit: true);

        var result = await service.PublishAsync(planId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_subscription_plans.validation_failed", result.Error.Code);
    }

    private static PlatformSubscriptionPlanService CreateService(
        FakePlatformSubscriptionPlanRepository repository,
        bool hasView = false,
        bool hasCreate = false,
        bool hasEdit = false)
    {
        return new PlatformSubscriptionPlanService(
            repository,
            new FakePlatformPermissionRepository(hasView, hasCreate, hasEdit),
            new FakePlatformPermissionChecker(hasView, hasCreate, hasEdit),
            new FakeDateTimeProvider());
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly bool _hasView;
        private readonly bool _hasCreate;
        private readonly bool _hasEdit;

        public FakePlatformPermissionChecker(bool hasView, bool hasCreate, bool hasEdit)
        {
            _hasView = hasView;
            _hasCreate = hasCreate;
            _hasEdit = hasEdit;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            var allowed = permissionCode switch
            {
                _ when permissionCode == PlatformPermissionCodes.SubscriptionPlansView => _hasView,
                _ when permissionCode == PlatformPermissionCodes.SubscriptionPlansCreate => _hasCreate,
                _ when permissionCode == PlatformPermissionCodes.SubscriptionPlansEdit => _hasEdit,
                _ => false
            };

            return Task.FromResult(allowed);
        }
    }

    private sealed class FakePlatformPermissionRepository : IPlatformPermissionRepository
    {
        private readonly bool _hasView;
        private readonly bool _hasCreate;
        private readonly bool _hasEdit;

        public FakePlatformPermissionRepository(bool hasView, bool hasCreate, bool hasEdit)
        {
            _hasView = hasView;
            _hasCreate = hasCreate;
            _hasEdit = hasEdit;
        }

        public Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            var permissions = new HashSet<string>(StringComparer.Ordinal);
            if (_hasView)
            {
                permissions.Add(PlatformPermissionCodes.SubscriptionPlansView);
            }

            if (_hasCreate)
            {
                permissions.Add(PlatformPermissionCodes.SubscriptionPlansCreate);
            }

            if (_hasEdit)
            {
                permissions.Add(PlatformPermissionCodes.SubscriptionPlansEdit);
                permissions.Add(PlatformPermissionCodes.SubscriptionPlansDuplicate);
                permissions.Add(PlatformPermissionCodes.SubscriptionPlansArchive);
                permissions.Add(PlatformPermissionCodes.SubscriptionPlansDelete);
            }

            return Task.FromResult<IReadOnlySet<string>>(permissions);
        }
    }

    private sealed class FakePlatformSubscriptionPlanRepository : IPlatformSubscriptionPlanRepository
    {
        public SubscriptionPlanListResponse ListResponse { get; init; } = new([], 1, 10, 0, 0, false, false, false, false, false);
        public SubscriptionPlanCatalogResponse CatalogResponse { get; init; } = new([]);
        public SubscriptionPlan? PlanEntity { get; init; }
        public SubscriptionPlanMutationResponse? MutationResponse { get; init; }
        public bool PlanCodeExists { get; init; }
        public IReadOnlySet<Guid> ActiveFeatureIds { get; init; } = new HashSet<Guid>();
        public bool AddCalled { get; private set; }
        public bool SaveCalled { get; private set; }
        public bool ReplaceFeaturesCalled { get; private set; }
        private SubscriptionPlan? _addedPlan;

        public Task<SubscriptionPlanListResponse> GetPlansAsync(
            SubscriptionPlanListQuery query,
            SubscriptionPlanPermissionFlags permissionFlags,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ListResponse);
        }

        public Task<SubscriptionPlanCatalogResponse> GetCatalogAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(CatalogResponse);
        }

        public Task<bool> PlanCodeExistsAsync(string planCode, CancellationToken cancellationToken)
        {
            return Task.FromResult(PlanCodeExists);
        }

        public Task<SubscriptionPlan?> GetPlanEntityByIdAsync(Guid planId, CancellationToken cancellationToken)
        {
            return Task.FromResult(PlanEntity?.Id == planId ? PlanEntity : PlanEntity);
        }

        public Task<SubscriptionPlanMutationResponse?> GetPlanByIdAsync(
            Guid planId,
            SubscriptionPlanPermissionFlags permissionFlags,
            CancellationToken cancellationToken)
        {
            if (MutationResponse is not null)
            {
                return Task.FromResult<SubscriptionPlanMutationResponse?>(MutationResponse);
            }

            if (PlanEntity is null && _addedPlan is not null && _addedPlan.Id == planId)
            {
                return Task.FromResult<SubscriptionPlanMutationResponse?>(
                    new SubscriptionPlanMutationResponse(
                        _addedPlan.Id,
                        _addedPlan.PlanCode,
                        _addedPlan.Name,
                        _addedPlan.Status,
                        "monthly",
                        _addedPlan.BaseCurrency,
                        _addedPlan.PriceAmount,
                        _addedPlan.MaxOutlets,
                        _addedPlan.MaxUsers,
                        _addedPlan.MaxTills,
                        0,
                        _addedPlan.UpdatedAt ?? Now));
            }

            if (PlanEntity is null)
            {
                return Task.FromResult<SubscriptionPlanMutationResponse?>(null);
            }

            return Task.FromResult<SubscriptionPlanMutationResponse?>(
                new SubscriptionPlanMutationResponse(
                    PlanEntity.Id,
                    PlanEntity.PlanCode,
                    PlanEntity.Name,
                    PlanEntity.Status,
                    "monthly",
                    PlanEntity.BaseCurrency,
                    PlanEntity.PriceAmount,
                    PlanEntity.MaxOutlets,
                    PlanEntity.MaxUsers,
                    PlanEntity.MaxTills,
                    0,
                    PlanEntity.UpdatedAt ?? Now));
        }

        public Task AddPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken)
        {
            AddCalled = true;
            _addedPlan = plan;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalled = true;
            if (PlanEntity is not null &&
                string.Equals(PlanEntity.Status, SubscriptionPlanConstants.Status.Draft, StringComparison.Ordinal))
            {
                PlanEntity.Publish(Now);
            }

            return Task.CompletedTask;
        }

        public Task ReplacePlanFeaturesAsync(
            Guid planId,
            IReadOnlyList<Guid> featureIds,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ReplaceFeaturesCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlySet<Guid>> GetActiveFeatureIdsAsync(
            IReadOnlyCollection<Guid> featureIds,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ActiveFeatureIds);
        }

        public Task<int> GetFeatureCountAsync(Guid planId, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task UpsertLegacyPlanLimitsAsync(
            Guid planId,
            int? maxOutlets,
            int? maxUsers,
            int? maxTills,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<string, decimal?>> GetPlanLimitValuesByKeyAsync(
            Guid planId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyDictionary<string, decimal?>>(new Dictionary<string, decimal?>());
        }
    }
}


