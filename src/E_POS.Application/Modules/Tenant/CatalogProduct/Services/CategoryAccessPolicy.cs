using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using Microsoft.Extensions.Logging;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class CategoryAccessPolicy
{
    public static readonly ApplicationError PermissionDenied = new(
        "category.permission_denied",
        "Permission denied for category management.");

    public static readonly ApplicationError EntitlementDenied = new(
        "category.entitlement_denied",
        "Product catalog feature is not included in the tenant subscription.");

    public static readonly ApplicationError InvalidTenantContext = new(
        "category.invalid_tenant_context",
        "Invalid tenant context.");

    public static readonly ApplicationError UnexpectedFailure = new(
        "category.unexpected_failure",
        "Category access evaluation failed.");

    private readonly ICategoryRepository _repository;
    private readonly ITenantFeatureEntitlementEvaluator _featureEntitlementEvaluator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CategoryAccessPolicy>? _logger;

    public CategoryAccessPolicy(
        ICategoryRepository repository,
        ITenantFeatureEntitlementEvaluator featureEntitlementEvaluator,
        IDateTimeProvider dateTimeProvider,
        ILogger<CategoryAccessPolicy>? logger = null)
    {
        _repository = repository;
        _featureEntitlementEvaluator = featureEntitlementEvaluator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<ApplicationError?> ValidateAsync(
        TenantRequestContext context,
        string requiredPermission,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return InvalidTenantContext;
        }

        var tenantStatus = await _repository.GetTenantStatusAsync(context.TenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(tenantStatus) || !TenantAuthConstants.IsTenantLoginStatusAllowed(tenantStatus))
        {
            return InvalidTenantContext;
        }

        TenantFeatureEntitlementEvaluation entitlement;
        try
        {
            entitlement = await _featureEntitlementEvaluator.EvaluateAsync(
                context.TenantId,
                PlatformTenantFeatureCodes.ProductCatalog,
                _dateTimeProvider.UtcNow,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "CATEGORY_ENTITLEMENT_EVALUATION_FAILED TenantId={TenantId} Feature={Feature}",
                context.TenantId,
                PlatformTenantFeatureCodes.ProductCatalog);
            return UnexpectedFailure;
        }

        if (!entitlement.IsAllowed)
        {
            return EntitlementDenied;
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(CategoryConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }
}
