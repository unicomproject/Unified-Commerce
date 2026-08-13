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

        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(Guid tenantId, string featureCode, DateTimeOffset evaluationTime, CancellationToken cancellationToken = default)
        {
            if (IsEntitled)
            {
                return Task.FromResult(TenantFeatureEntitlementEvaluation.Allowed(featureCode, featureCode, false, true, false));
            }

            return Task.FromResult(TenantFeatureEntitlementEvaluation.Denied(TenantFeatureEntitlementDecision.Disabled, featureCode, featureCode, false, true, false, "Feature disabled"));
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

        var error = await policy.ValidateWizardAccessAsync(context, productId: null, isCreateAction: true, CancellationToken.None);

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

        var error = await policy.ValidateWizardAccessAsync(context, productId: null, isCreateAction: true, CancellationToken.None);

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

        var error = await policy.ValidateWizardAccessAsync(context, productId, isCreateAction: false, CancellationToken.None);

        Assert.Null(error);
    }
}
