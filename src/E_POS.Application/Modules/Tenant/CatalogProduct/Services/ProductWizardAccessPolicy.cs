using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
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
        SaveProductDraftRequest? request,
        CancellationToken cancellationToken)
    {
        var baseline = await ValidateBaselineAsync(context, cancellationToken);
        if (baseline is not null)
        {
            return baseline;
        }

        var requiredPermission = isCreateAction
            ? ProductConstants.CreatePermission
            : ProductConstants.UpdatePermission;

        if (productId.HasValue && !isCreateAction)
        {
            var isInitialCreationDraft = await _repository.IsInitialCreationDraftAsync(
                context.TenantId,
                productId.Value,
                cancellationToken);

            if (isInitialCreationDraft)
            {
                requiredPermission = ProductConstants.CreatePermission;
            }
        }

        if (!context.HasPermission(requiredPermission))
        {
            return PermissionDenied;
        }

        if (request is null)
        {
            return null;
        }

        return await ValidatePayloadAsync(context, request, cancellationToken);
    }

    public async Task<ApplicationError?> ValidateReadAccessAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var baseline = await ValidateBaselineAsync(context, cancellationToken);
        if (baseline is not null)
        {
            return baseline;
        }

        if (!context.HasPermission(ProductConstants.ViewPermission) &&
            !context.HasPermission(ProductConstants.CreatePermission) &&
            !context.HasPermission(ProductConstants.UpdatePermission))
        {
            return PermissionDenied;
        }

        return null;
    }

    public async Task<ApplicationError?> ValidatePublishAccessAsync(
        TenantRequestContext context,
        SaveProductDraftRequest? request,
        ProductSetupWizardDto? existing,
        CancellationToken cancellationToken)
    {
        var baseline = await ValidateBaselineAsync(context, cancellationToken);
        if (baseline is not null)
        {
            return baseline;
        }

        if (!context.HasPermission(ProductConstants.PublishPermission))
        {
            return PermissionDenied;
        }

        var payloadError = await ValidatePayloadAsync(context, request ?? new SaveProductDraftRequest(), cancellationToken);
        if (payloadError is not null)
        {
            return payloadError;
        }

        return await ValidatePublishSubgraphAsync(context, request, existing, cancellationToken);
    }

    private async Task<ApplicationError?> ValidateBaselineAsync(
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

        return null;
    }

    private async Task<ApplicationError?> ValidatePayloadAsync(
        TenantRequestContext context,
        SaveProductDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (HasVariantMutation(request) &&
            !context.HasPermission(ProductConstants.VariantsManagePermission))
        {
            return PermissionDenied;
        }

        if (HasBundleMutation(request) &&
            !context.HasPermission(ProductConstants.ComboComponentsManagePermission))
        {
            return PermissionDenied;
        }

        if (HasBarcodeMutation(request) &&
            !context.HasPermission(ProductConstants.BarcodesManagePermission))
        {
            return PermissionDenied;
        }

        if (HasPricingMutation(request) &&
            !context.HasPermission(ProductConstants.ProductPricingManagePermission))
        {
            return PermissionDenied;
        }

        if (request.PricingTax?.CostPrice is not null &&
            !context.HasPermission(ProductConstants.ProductCostViewPermission))
        {
            return PermissionDenied;
        }

        var hasVariantImageMutation =
            request.VariantConfiguration?.Options?.Any(o => o.Values?.Any(v => v.ImageMediaAssetId.HasValue) == true) == true ||
            request.VariantConfiguration?.Variants?.Any(v => v.ExactImageMediaAssetId.HasValue) == true;

        if (hasVariantImageMutation &&
            !context.HasPermission(ProductConstants.MediaManagePermission))
        {
            return PermissionDenied;
        }

        var hasNonEmptyTracking = ProductSetupInitialTrackingRules.HasAnyValues(
            request.InitialBatchNumber,
            request.InitialExpiryDate,
            request.InitialSerialNumber);

        var enablesAdvancedTracking = request.BatchTracking || request.ExpiryTracking || request.SerialTracking;

        if ((hasNonEmptyTracking || enablesAdvancedTracking) &&
            !await HasInventoryTrackingEntitlementAsync(context, cancellationToken))
        {
            return EntitlementDenied;
        }

        if (request.InitialTrackingAssignedVariantId.HasValue &&
            !context.HasPermission(ProductConstants.VariantsManagePermission))
        {
            return PermissionDenied;
        }

        return null;
    }

    private async Task<ApplicationError?> ValidatePublishSubgraphAsync(
        TenantRequestContext context,
        SaveProductDraftRequest? request,
        ProductSetupWizardDto? existing,
        CancellationToken cancellationToken)
    {
        var structure = request?.ProductStructure ?? existing?.ProductStructure ?? "SIMPLE";
        var hasMedia = (request?.StagedMediaAssetIds?.Count ?? 0) > 0 || (existing?.Images?.Count ?? 0) > 0;
        if (hasMedia && !context.HasPermission(ProductConstants.MediaManagePermission))
        {
            return PermissionDenied;
        }

        if (string.Equals(structure, "VARIANT", StringComparison.OrdinalIgnoreCase) &&
            !context.HasPermission(ProductConstants.VariantsManagePermission))
        {
            return PermissionDenied;
        }

        if (string.Equals(structure, "BUNDLE", StringComparison.OrdinalIgnoreCase) &&
            !context.HasPermission(ProductConstants.ComboComponentsManagePermission))
        {
            return PermissionDenied;
        }

        var hasBarcodes = request?.BarcodeSkuConfiguration is not null ||
                          existing?.BarcodeSkuConfiguration is not null;
        if (hasBarcodes && !context.HasPermission(ProductConstants.BarcodesManagePermission))
        {
            return PermissionDenied;
        }

        var hasPricing = request?.PricingTax is not null || existing?.PricingTax is not null;
        if (hasPricing && !context.HasPermission(ProductConstants.ProductPricingManagePermission))
        {
            return PermissionDenied;
        }

        var cost = request?.PricingTax?.CostPrice ?? existing?.PricingTax?.CostPrice;
        if (cost is not null && !context.HasPermission(ProductConstants.ProductCostViewPermission))
        {
            return PermissionDenied;
        }

        var hasIdentity = ProductSetupInitialTrackingRules.HasAnyValues(
            request?.InitialBatchNumber ?? existing?.InitialBatchNumber,
            request?.InitialExpiryDate ?? existing?.InitialExpiryDate,
            request?.InitialSerialNumber ?? existing?.InitialSerialNumber);

        if (hasIdentity && !await HasInventoryTrackingEntitlementAsync(context, cancellationToken))
        {
            return EntitlementDenied;
        }

        return null;
    }

    private async Task<bool> HasInventoryTrackingEntitlementAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var entitlement = await _featureEntitlementEvaluator.EvaluateAsync(
            context.TenantId,
            PlatformTenantFeatureCodes.InventoryTracking,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return entitlement.IsAllowed;
    }

    private static bool HasVariantMutation(SaveProductDraftRequest request) =>
        request.VariantConfiguration is not null ||
        (request.CurrentSetupStep == ProductWizardStage.ProductConfiguration &&
         string.Equals(request.ProductStructure, "VARIANT", StringComparison.OrdinalIgnoreCase));

    private static bool HasBundleMutation(SaveProductDraftRequest request) =>
        request.BundleConfiguration is not null ||
        (request.CurrentSetupStep == ProductWizardStage.ProductConfiguration &&
         string.Equals(request.ProductStructure, "BUNDLE", StringComparison.OrdinalIgnoreCase));

    private static bool HasBarcodeMutation(SaveProductDraftRequest request) =>
        request.BarcodeSkuConfiguration is not null ||
        request.CurrentSetupStep == ProductWizardStage.BarcodeSku;

    private static bool HasPricingMutation(SaveProductDraftRequest request) =>
        request.PricingTax is not null ||
        request.CurrentSetupStep == ProductWizardStage.PricingTax;
}
