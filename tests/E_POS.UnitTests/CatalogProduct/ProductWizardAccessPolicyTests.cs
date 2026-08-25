using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class ProductWizardAccessPolicyTests
{
    private class FakeEntitlementEvaluator : ITenantFeatureEntitlementEvaluator
    {
        public bool IsEntitled { get; set; } = true;
        public HashSet<string> DeniedFeatureCodes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(Guid tenantId, string featureCode, DateTimeOffset evaluationTime, CancellationToken cancellationToken = default)
        {
            if (DeniedFeatureCodes.Contains(featureCode) || !IsEntitled)
            {
                return Task.FromResult(TenantFeatureEntitlementEvaluation.Denied(TenantFeatureEntitlementDecision.Disabled, featureCode, featureCode, false, true, false, "Feature disabled"));
            }

            return Task.FromResult(TenantFeatureEntitlementEvaluation.Allowed(featureCode, featureCode, false, true, false));
        }

        public Task<bool> IsEnabledAsync(Guid tenantId, string featureCode, DateTimeOffset evaluationTime, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsEntitled);
        }
    }

    private class FakeRepo : TenantAdminProductDraftServiceTests.FakeTenantAdminProductRepository
    {
    }

    private class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    [Fact]
    public async Task ValidateWizardAccessAsync_Blocks_WhenTenantIsNotActive()
    {
        var repo = new FakeRepo { TenantStatus = "SUSPENDED" };
        var evaluator = new FakeEntitlementEvaluator();
        var policy = new ProductWizardAccessPolicy(evaluator, repo, new FakeClock());

        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [TenantAdminProductPermissions.Create]);

        var error = await policy.ValidateWizardAccessAsync(context, productId: null, isCreateAction: true, null, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("product.tenant_blocked", error.Code);
    }

    [Fact]
    public async Task ValidateWizardAccessAsync_Blocks_WhenFeatureEntitlementMissing()
    {
        var repo = new FakeRepo { TenantStatus = "ACTIVE" };
        var evaluator = new FakeEntitlementEvaluator { IsEntitled = false };
        var policy = new ProductWizardAccessPolicy(evaluator, repo, new FakeClock());

        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [TenantAdminProductPermissions.Create]);

        var error = await policy.ValidateWizardAccessAsync(context, productId: null, isCreateAction: true, null, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("product.entitlement_denied", error.Code);
    }

    [Fact]
    public async Task ValidateWizardAccessAsync_AllowsCreatePermission_ForInitialDraftUpdate()
    {
        var productId = Guid.NewGuid();
        var repo = new FakeRepo { TenantStatus = "ACTIVE", IsInitialDraft = true };
        var evaluator = new FakeEntitlementEvaluator { IsEntitled = true };
        var policy = new ProductWizardAccessPolicy(evaluator, repo, new FakeClock());

        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [TenantAdminProductPermissions.Create]);

        var error = await policy.ValidateWizardAccessAsync(context, productId, isCreateAction: false, null, CancellationToken.None);

        Assert.Null(error);
    }

    [Fact]
    public async Task ValidateWizardAccessAsync_Blocks_Step4_WhenMissingVariantsManage()
    {
        var repo = new FakeRepo { TenantStatus = "ACTIVE" };
        var evaluator = new FakeEntitlementEvaluator { IsEntitled = true };
        var policy = new ProductWizardAccessPolicy(evaluator, repo, new FakeClock());

        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [TenantAdminProductPermissions.Create]);

        var request = new E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.SaveProductDraftRequest 
        { 
            CurrentSetupStep = E_POS.Application.Modules.Tenant.CatalogProduct.Constants.ProductWizardStage.ProductConfiguration,
            ProductStructure = "VARIANT"
        };

        var error = await policy.ValidateWizardAccessAsync(context, productId: null, isCreateAction: true, request, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("product.permission_denied", error.Code);
    }

    [Fact]
    public async Task ValidateWizardAccessAsync_Allows_Step4_WithVariantsManage()
    {
        var repo = new FakeRepo { TenantStatus = "ACTIVE" };
        var evaluator = new FakeEntitlementEvaluator { IsEntitled = true };
        var policy = new ProductWizardAccessPolicy(evaluator, repo, new FakeClock());

        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [TenantAdminProductPermissions.Create, TenantAdminProductPermissions.VariantsManage]);

        var request = new E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.SaveProductDraftRequest 
        { 
            CurrentSetupStep = E_POS.Application.Modules.Tenant.CatalogProduct.Constants.ProductWizardStage.ProductConfiguration,
            ProductStructure = "VARIANT"
        };

        var error = await policy.ValidateWizardAccessAsync(context, productId: null, isCreateAction: true, request, CancellationToken.None);

        Assert.Null(error);
    }

    [Fact]
    public async Task ValidateWizardAccessAsync_Blocks_Step4_WhenMediaMutation_MissingMediaManage()
    {
        var repo = new FakeRepo { TenantStatus = "ACTIVE" };
        var evaluator = new FakeEntitlementEvaluator { IsEntitled = true };
        var policy = new ProductWizardAccessPolicy(evaluator, repo, new FakeClock());

        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [TenantAdminProductPermissions.Create, TenantAdminProductPermissions.VariantsManage]);

        var request = new E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.SaveProductDraftRequest 
        { 
            CurrentSetupStep = E_POS.Application.Modules.Tenant.CatalogProduct.Constants.ProductWizardStage.ProductConfiguration,
            VariantConfiguration = new E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.VariantConfigurationDto(
                Array.Empty<E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.VariantConfigurationOptionDto>(),
                new[]
                {
                    new E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.VariantConfigurationVariantDto(
                        "clientKey",
                        null,
                        "123",
                        "abc",
                        "label",
                        "x",
                        true,
                        null,
                        Guid.NewGuid(),
                        Array.Empty<E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.VariantConfigurationSelectedValueDto>()
                    )
                },
                Array.Empty<E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.VariantConfigurationDeletedCombinationDto>()
            )
        };

        var error = await policy.ValidateWizardAccessAsync(context, productId: null, isCreateAction: true, request, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("product.permission_denied", error.Code);
    }

    [Fact]
    public async Task ValidateWizardAccessAsync_Blocks_PricingPayload_OnStep1_WithoutPricingManage()
    {
        var repo = new FakeRepo { TenantStatus = "ACTIVE" };
        var evaluator = new FakeEntitlementEvaluator { IsEntitled = true };
        var policy = new ProductWizardAccessPolicy(evaluator, repo, new FakeClock());

        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [TenantAdminProductPermissions.Create]);

        var request = new E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.SaveProductDraftRequest
        {
            CurrentSetupStep = 1,
            PricingTax = new E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.PricingTaxConfigurationDto(
                1m, 10m, null, null, true)
        };

        var error = await policy.ValidateWizardAccessAsync(context, productId: null, isCreateAction: true, request, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("product.permission_denied", error.Code);
    }

    [Fact]
    public async Task ValidateWizardAccessAsync_Blocks_NonEmptyInitialTracking_WithoutInventoryTrackingEntitlement()
    {
        var repo = new FakeRepo { TenantStatus = "ACTIVE" };
        var evaluator = new FakeEntitlementEvaluator { IsEntitled = true };
        evaluator.DeniedFeatureCodes.Add("inventory_tracking");
        var policy = new ProductWizardAccessPolicy(evaluator, repo, new FakeClock());

        var context = new TenantRequestContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [TenantAdminProductPermissions.Create]);

        var request = new E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.SaveProductDraftRequest
        {
            CurrentSetupStep = 1,
            InitialBatchNumber = "BAT-1"
        };

        var error = await policy.ValidateWizardAccessAsync(context, productId: null, isCreateAction: true, request, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("product.entitlement_denied", error.Code);
    }
}
