using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class ProductWizardAccessPolicy
{
    private readonly ITenantFeatureEntitlementEvaluator _featureEntitlementEvaluator;
    private readonly ITenantAdminProductRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public static readonly ApplicationError InvalidContext = new(
        "product.invalid_tenant_context",
        "Invalid tenant context.");

    public static readonly ApplicationError TenantBlocked = new(
        "product.tenant_blocked",
        "Tenant account is blocked or inactive.");

    public static readonly ApplicationError EntitlementDenied = new(
        "product.entitlement_denied",
        "Product management feature is not included in the tenant subscription.");

    public static readonly ApplicationError PermissionDenied = new(
        "product.permission_denied",
        "Insufficient permission for product operation.");

    public ProductWizardAccessPolicy(
        ITenantFeatureEntitlementEvaluator featureEntitlementEvaluator,
        ITenantAdminProductRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _featureEntitlementEvaluator = featureEntitlementEvaluator;
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationError?> ValidateWizardAccessAsync(
        TenantRequestContext context,
        Guid? productId,
        bool isCreateAction,
        E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin.SaveProductDraftRequest? request,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return InvalidContext;
        }

        var tenantStatus = await _repository.GetTenantStatusAsync(context.TenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(tenantStatus) || !TenantAuthConstants.IsTenantLoginStatusAllowed(tenantStatus))
        {
            return TenantBlocked;
        }

        var entitlement = await _featureEntitlementEvaluator.EvaluateAsync(
            context.TenantId,
            PlatformTenantFeatureCodes.ProductCatalog,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (!entitlement.IsAllowed)
        {
            return EntitlementDenied;
        }

        var requiredPermission = isCreateAction
            ? TenantAdminProductPermissions.Create
            : TenantAdminProductPermissions.Update;

        if (productId.HasValue && !isCreateAction)
        {
            var isInitialCreationDraft = await _repository.IsInitialCreationDraftAsync(
                context.TenantId,
                productId.Value,
                cancellationToken);

            if (isInitialCreationDraft)
            {
                requiredPermission = TenantAdminProductPermissions.Create;
            }
        }

        if (!context.HasPermission(requiredPermission))
        {
            return PermissionDenied;
        }

        if (request?.CurrentSetupStep == E_POS.Application.Modules.Tenant.CatalogProduct.Constants.ProductWizardStage.ProductConfiguration)
        {
            if (!context.HasPermission(TenantAdminProductPermissions.VariantsManage))
            {
                return PermissionDenied;
            }

            var hasMediaMutation = request.VariantConfiguration?.Options?.Any(o => o.Values?.Any(v => v.ImageMediaAssetId.HasValue) == true) == true ||
                                   request.VariantConfiguration?.Variants?.Any(v => v.ExactImageMediaAssetId.HasValue) == true;

            if (hasMediaMutation && !context.HasPermission(TenantAdminProductPermissions.ProductMediaManage))
            {
                return PermissionDenied;
            }
        }

        if (request?.CurrentSetupStep == E_POS.Application.Modules.Tenant.CatalogProduct.Constants.ProductWizardStage.BarcodeSku)
        {
            if (!context.HasPermission(TenantAdminProductPermissions.BarcodesManage))
            {
                return PermissionDenied;
            }
        }

        return null;
    }

    public async Task<ApplicationError?> ValidateReadAccessAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return InvalidContext;
        }

        var tenantStatus = await _repository.GetTenantStatusAsync(context.TenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(tenantStatus) || !TenantAuthConstants.IsTenantLoginStatusAllowed(tenantStatus))
        {
            return TenantBlocked;
        }

        var entitlement = await _featureEntitlementEvaluator.EvaluateAsync(
            context.TenantId,
            PlatformTenantFeatureCodes.ProductCatalog,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (!entitlement.IsAllowed)
        {
            return EntitlementDenied;
        }

        if (!context.HasPermission(TenantAdminProductPermissions.View) &&
            !context.HasPermission(TenantAdminProductPermissions.Create) &&
            !context.HasPermission(TenantAdminProductPermissions.Update))
        {
            return PermissionDenied;
        }

        return null;
    }
}
