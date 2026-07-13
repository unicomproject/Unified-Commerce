using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class TenantAdminProductService : ITenantAdminProductService
{
    private static readonly ApplicationError PermissionDenied = new(
        "product.permission_denied",
        "Permission denied for product management.");

    private static readonly ApplicationError NotFound = new(
        "product.not_found",
        "Product was not found.");

    private readonly IProductRepository _productRepository;
    private readonly ITenantAdminProductRepository _tenantAdminProductRepository;
    private readonly ITenantAdminProductRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITenantAdminProductAuditLogger _auditLogger;

    public TenantAdminProductService(
        IProductRepository productRepository,
        ITenantAdminProductRepository tenantAdminProductRepository,
        ITenantAdminProductRequestValidator validator,
        IDateTimeProvider dateTimeProvider,
        ITenantAdminProductAuditLogger auditLogger)
    {
        _productRepository = productRepository;
        _tenantAdminProductRepository = tenantAdminProductRepository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
        _auditLogger = auditLogger;
    }

    public async Task<ApplicationResult<TenantAdminProductCreateResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminProductCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateCreateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(accessError);
        }

        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(validationError);
        }

        var tenantValidationError = await ValidateTenantReferencesAsync(context, request, cancellationToken);
        if (tenantValidationError is not null)
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(tenantValidationError);
        }

        var unitId = await _tenantAdminProductRepository.ResolveUnitIdAsync(
            context.TenantId,
            request.UnitType,
            cancellationToken);
        if (!unitId.HasValue)
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError("unitType", "Unit type is invalid for this tenant.")]));
        }

        var skuValues = GetSkuValues(request);
        foreach (var sku in skuValues)
        {
            if (await _productRepository.SkuExistsAsync(context.TenantId, sku, null, cancellationToken))
            {
                return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                    "product.duplicate_sku",
                    "SKU already exists."));
            }
        }

        var barcodeValues = GetBarcodeValues(request);
        foreach (var barcode in barcodeValues)
        {
            if (await _productRepository.BarcodeExistsAsync(context.TenantId, barcode, null, cancellationToken))
            {
                return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                    "product.duplicate_barcode",
                    "Barcode already exists."));
            }
        }

        var response = await _tenantAdminProductRepository.CreateProductAsync(
            context.TenantId,
            context.UserId,
            request,
            unitId.Value,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return ApplicationResult<TenantAdminProductCreateResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminProductDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateDetailsAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductDetailResponse>.Failure(accessError);
        }

        var response = await _tenantAdminProductRepository.GetDetailAsync(
            context.TenantId,
            productId,
            cancellationToken);

        return response is null
            ? ApplicationResult<TenantAdminProductDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminProductDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminProductDetailResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid productId,
        TenantAdminProductCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateUpdateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductDetailResponse>.Failure(accessError);
        }

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminProductDetailResponse>.Failure(validationError);
        }

        if (!await _productRepository.ProductExistsAsync(context.TenantId, productId, cancellationToken))
        {
            return ApplicationResult<TenantAdminProductDetailResponse>.Failure(NotFound);
        }

        var tenantValidationError = await ValidateTenantReferencesAsync(context, request, cancellationToken);
        if (tenantValidationError is not null)
        {
            return ApplicationResult<TenantAdminProductDetailResponse>.Failure(tenantValidationError);
        }

        var skuValues = GetSkuValues(request);
        foreach (var sku in skuValues)
        {
            if (await _tenantAdminProductRepository.SkuExistsOnOtherProductAsync(
                    context.TenantId,
                    sku,
                    productId,
                    cancellationToken))
            {
                return ApplicationResult<TenantAdminProductDetailResponse>.Failure(new ApplicationError(
                    "product.duplicate_sku",
                    "SKU already exists."));
            }
        }

        var barcodeValues = GetBarcodeValues(request);
        foreach (var barcode in barcodeValues)
        {
            if (await _tenantAdminProductRepository.BarcodeExistsOnOtherProductAsync(
                    context.TenantId,
                    barcode,
                    productId,
                    cancellationToken))
            {
                return ApplicationResult<TenantAdminProductDetailResponse>.Failure(new ApplicationError(
                    "product.duplicate_barcode",
                    "Barcode already exists."));
            }
        }

        var unitId = await _tenantAdminProductRepository.ResolveUnitIdAsync(
            context.TenantId,
            request.UnitType,
            cancellationToken);
        if (!unitId.HasValue)
        {
            return ApplicationResult<TenantAdminProductDetailResponse>.Failure(new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError("unitType", "Unit type is invalid for this tenant.")]));
        }

        var response = await _tenantAdminProductRepository.UpdateProductAsync(
            context.TenantId,
            context.UserId,
            productId,
            request,
            unitId.Value,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return response is null
            ? ApplicationResult<TenantAdminProductDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminProductDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminProductStatusUpdateResponse>> UpdateStatusAsync(
        TenantRequestContext context,
        Guid productId,
        TenantAdminProductStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateUpdateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(accessError);
        }

        var validationError = _validator.ValidateStatusUpdate(request);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(validationError);
        }

        var normalizedStatus = ProductConstants.NormalizeStatus(request.Status);
        if (normalizedStatus == ProductConstants.ActiveStatus)
        {
            var activationSnapshot = await _tenantAdminProductRepository.GetActivationSnapshotAsync(
                context.TenantId,
                productId,
                cancellationToken);

            if (activationSnapshot is null)
            {
                return ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(NotFound);
            }

            var activationError = ValidateActivationReadiness(activationSnapshot);
            if (activationError is not null)
            {
                return ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(activationError);
            }
        }
        else if (!await _productRepository.ProductExistsAsync(context.TenantId, productId, cancellationToken))
        {
            return ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(NotFound);
        }

        var response = await _tenantAdminProductRepository.UpdateProductStatusAsync(
            context.TenantId,
            context.UserId,
            productId,
            normalizedStatus,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return response is null
            ? ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminProductStatusUpdateResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminProductDeleteResponse>> DeleteAsync(
        TenantRequestContext context,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateDeleteAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductDeleteResponse>.Failure(accessError);
        }

        var result = await _tenantAdminProductRepository.DeleteProductAsync(
            context.TenantId,
            context.UserId,
            productId,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (result.ErrorCode == "product.not_found")
        {
            return ApplicationResult<TenantAdminProductDeleteResponse>.Failure(NotFound);
        }

        if (result.ErrorCode == "product.delete_blocked")
        {
            return ApplicationResult<TenantAdminProductDeleteResponse>.Failure(new ApplicationError(
                "product.delete_blocked",
                "Product is already deleted."));
        }

        var response = result.Response!;
        _auditLogger.LogProductDeleted(
            context.TenantId,
            context.UserId,
            response.ProductId,
            response.Outcome,
            response.Status);

        return ApplicationResult<TenantAdminProductDeleteResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminProductDashboardResponse>> GetDashboardAsync(
        TenantRequestContext context,
        TenantAdminProductDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateDashboardAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductDashboardResponse>.Failure(accessError);
        }

        if (query.DateFrom > query.DateTo)
        {
            return ApplicationResult<TenantAdminProductDashboardResponse>.Failure(new ApplicationError(
                "product.dashboard.invalid_date_range",
                "The selected date range is invalid."));
        }

        if (query.OutletId.HasValue &&
            !await _tenantAdminProductRepository.OutletsBelongToTenantAsync(
                context.TenantId,
                [query.OutletId.Value],
                cancellationToken))
        {
            return ApplicationResult<TenantAdminProductDashboardResponse>.Failure(new ApplicationError(
                "product.dashboard.invalid_outlet",
                "The selected outlet is not accessible."));
        }

        var raw = await _tenantAdminProductRepository.GetDashboardAsync(
            context.TenantId,
            query,
            cancellationToken);

        var summary = new TenantAdminProductDashboardSummaryDto(
            CanViewTotalProducts(context)
                ? ToMetric(raw.TotalProducts)
                : null,
            CanViewStock(context)
                ? ToMetric(raw.LowStock)
                : null,
            CanViewStock(context)
                ? ToMetric(raw.OutOfStock)
                : null,
            CanViewExpiry(context)
                ? ToMetric(raw.ExpiryAlerts)
                : null,
            CanViewStock(context)
                ? ToMetric(raw.StockAdded)
                : null,
            CanViewFastMoving(context)
                ? ToMetric(raw.FastMovingProducts)
                : null);

        TenantAdminProductDashboardStockValueDto? stockValue = null;
        if (CanViewStockValue(context))
        {
            stockValue = new TenantAdminProductDashboardStockValueDto(
                raw.CurrentStockValue,
                CalculateChangePercent(raw.CurrentStockValue, raw.PreviousStockValue),
                raw.StockValueTrend
                    .Select(point => new TenantAdminProductDashboardStockValuePointDto(point.Date, point.Value))
                    .ToList());
        }

        TenantAdminProductDashboardStockMovementDto? stockMovement = null;
        if (CanViewStockMovements(context))
        {
            var totalCount = raw.StockMovements.Sum(x => x.Count);
            stockMovement = new TenantAdminProductDashboardStockMovementDto(
                totalCount,
                raw.StockMovements
                    .Select(item => new TenantAdminProductDashboardMovementItemDto(
                        item.Type,
                        item.Count,
                        totalCount == 0
                            ? 0
                            : Math.Round(item.Count * 100m / totalCount, 1)))
                    .ToList());
        }

        var response = new TenantAdminProductDashboardResponse(
            _dateTimeProvider.UtcNow,
            raw.CurrencyCode,
            summary,
            stockValue,
            stockMovement);

        return ApplicationResult<TenantAdminProductDashboardResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminProductSummaryCardsResponse>> GetSummaryAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductSummaryCardsResponse>.Failure(accessError);
        }

        var summary = await _tenantAdminProductRepository.GetSummaryAsync(
            context.TenantId,
            cancellationToken);

        var response = new TenantAdminProductSummaryCardsResponse(
            summary.TotalProducts,
            summary.ActiveProducts,
            summary.InactiveProducts,
            summary.ProductCategories);

        return ApplicationResult<TenantAdminProductSummaryCardsResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminProductCreateOptionsResponse>> GetCreateOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateCreateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductCreateOptionsResponse>.Failure(accessError);
        }

        var response = await _tenantAdminProductRepository.GetCreateOptionsAsync(
            context.TenantId,
            cancellationToken);

        return ApplicationResult<TenantAdminProductCreateOptionsResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminProductListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductListResponse>.Failure(accessError);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var list = await _productRepository.ListAsync(
            context.TenantId,
            safePage,
            safePageSize,
            search,
            cancellationToken);

        var summary = await _tenantAdminProductRepository.GetSummaryAsync(context.TenantId, cancellationToken);

        var productIds = list.Items.Select(x => x.Id).ToArray();
        var categoryNames = productIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _tenantAdminProductRepository.GetPrimaryCategoryNamesAsync(
                context.TenantId,
                productIds,
                cancellationToken);

        var items = list.Items
            .Select(item => new TenantAdminProductListItemResponse(
                item.Id,
                item.Name,
                categoryNames.TryGetValue(item.Id, out var categoryName) ? categoryName : null,
                item.Sku,
                item.Barcode,
                item.Price,
                item.Status,
                OutletCount: 0))
            .ToList();

        var response = new TenantAdminProductListResponse(
            summary,
            items,
            list.PageNumber,
            list.PageSize,
            list.TotalCount);

        return ApplicationResult<TenantAdminProductListResponse>.Success(response);
    }

    private static IEnumerable<string> GetSkuValues(TenantAdminProductCreateRequest request)
    {
        if (request.HasVariants && request.Variants is { Count: > 0 })
        {
            return request.Variants
                .Select(variant => variant.Sku.Trim())
                .Where(sku => !string.IsNullOrWhiteSpace(sku))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        return [request.Sku.Trim()];
    }

    private static IEnumerable<string> GetBarcodeValues(TenantAdminProductCreateRequest request)
    {
        if (request.HasVariants && request.Variants is { Count: > 0 })
        {
            return request.Variants
                .Select(variant => variant.Barcode?.Trim())
                .Where(barcode => !string.IsNullOrWhiteSpace(barcode))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        return string.IsNullOrWhiteSpace(request.Barcode)
            ? []
            : [request.Barcode.Trim()];
    }

    private async Task<ApplicationError?> ValidateTenantReferencesAsync(
        TenantRequestContext context,
        TenantAdminProductCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _tenantAdminProductRepository.CategoryBelongsToTenantAsync(
                context.TenantId,
                request.CategoryId,
                parentCategoryId: null,
                cancellationToken))
        {
            return new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError("categoryId", "Category was not found for this tenant.")]);
        }

        if (request.SubCategoryId.HasValue &&
            !await _tenantAdminProductRepository.CategoryBelongsToTenantAsync(
                context.TenantId,
                request.SubCategoryId.Value,
                request.CategoryId,
                cancellationToken))
        {
            return new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError("subCategoryId", "Sub-category was not found for the selected category.")]);
        }

        if (request.BrandId.HasValue &&
            !await _tenantAdminProductRepository.BrandBelongsToTenantAsync(
                context.TenantId,
                request.BrandId.Value,
                cancellationToken))
        {
            return new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError("brandId", "Brand was not found for this tenant.")]);
        }

        if (request.TaxId.HasValue &&
            !await _tenantAdminProductRepository.TaxClassBelongsToTenantAsync(
                context.TenantId,
                request.TaxId.Value,
                cancellationToken))
        {
            return new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError("taxId", "Tax option was not found for this tenant.")]);
        }

        if (request.TrackInventory &&
            request.OutletIds is { Count: > 0 } &&
            !await _tenantAdminProductRepository.OutletsBelongToTenantAsync(
                context.TenantId,
                request.OutletIds,
                cancellationToken))
        {
            return new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError("outletIds", "One or more outlets are invalid for this tenant.")]);
        }

        return null;
    }

    private static ApplicationError? ValidateActivationReadiness(TenantAdminProductActivationSnapshot snapshot)
    {
        var fieldErrors = new List<ApplicationFieldError>();

        if (string.IsNullOrWhiteSpace(snapshot.ProductName))
        {
            fieldErrors.Add(new ApplicationFieldError("productName", "Product name is required to activate."));
        }

        if (string.IsNullOrWhiteSpace(snapshot.Sku))
        {
            fieldErrors.Add(new ApplicationFieldError("sku", "SKU is required to activate."));
        }

        if (!snapshot.HasCategory)
        {
            fieldErrors.Add(new ApplicationFieldError("categoryId", "Category is required to activate."));
        }

        if (string.IsNullOrWhiteSpace(snapshot.UnitType))
        {
            fieldErrors.Add(new ApplicationFieldError("unitType", "Unit type is required to activate."));
        }

        if (snapshot.SellingPrice <= 0)
        {
            fieldErrors.Add(new ApplicationFieldError("sellingPrice", "Selling price is required to activate."));
        }

        if (fieldErrors.Count == 0)
        {
            return null;
        }

        return new ApplicationError(
            "product.validation_failed",
            "Product cannot be activated because required fields are missing.",
            fieldErrors);
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TenantAdminProductPermissions.View) ||
               context.HasPermission(ProductConstants.ViewPermission) ||
               context.HasPermission(ProductConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateCreateAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TenantAdminProductPermissions.Create) ||
               context.HasPermission(ProductConstants.CreatePermission) ||
               context.HasPermission(ProductConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateDetailsAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TenantAdminProductPermissions.DetailsView) ||
               context.HasPermission(TenantAdminProductPermissions.View) ||
               context.HasPermission(ProductConstants.ViewPermission) ||
               context.HasPermission(ProductConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateUpdateAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TenantAdminProductPermissions.Update) ||
               context.HasPermission(ProductConstants.UpdatePermission) ||
               context.HasPermission(ProductConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }

    private static TenantAdminProductDashboardMetricDto ToMetric(
        TenantAdminProductDashboardRawMetric metric) =>
        new(
            metric.CurrentValue,
            CalculateChangePercent(metric.CurrentValue, metric.PreviousValue));

    private static decimal CalculateChangePercent(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0 : 100;
        }

        return Math.Round((current - previous) / previous * 100m, 1);
    }

    private static bool CanViewTotalProducts(TenantRequestContext context) =>
        context.HasPermission(TenantAdminProductPermissions.View) ||
        context.HasPermission(ProductConstants.ViewPermission);

    private static bool CanViewStock(TenantRequestContext context) =>
        context.HasPermission(TenantAdminStockPermissions.View) ||
        context.HasPermission(TenantAdminStockPermissions.LegacyInventoryView);

    private static bool CanViewExpiry(TenantRequestContext context) =>
        context.HasPermission(TenantAdminStockPermissions.ExpiryView);

    private static bool CanViewFastMoving(TenantRequestContext context) =>
        context.HasPermission(TenantAdminProductReportPermissions.ProductsView);

    private static bool CanViewStockValue(TenantRequestContext context) =>
        context.HasPermission(TenantAdminStockPermissions.ValueView);

    private static bool CanViewStockMovements(TenantRequestContext context) =>
        context.HasPermission(TenantAdminStockPermissions.MovementsView);

    private static ApplicationError? ValidateDashboardAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TenantAdminProductPermissions.DashboardView)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateDeleteAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TenantAdminProductPermissions.Delete) ||
               context.HasPermission(ProductConstants.DeletePermission) ||
               context.HasPermission(ProductConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }
}
