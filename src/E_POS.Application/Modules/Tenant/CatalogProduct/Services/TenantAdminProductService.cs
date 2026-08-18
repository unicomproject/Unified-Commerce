
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
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
    private readonly ProductWizardAccessPolicy _accessPolicy;
    private readonly ProductVariantGenerationService _variantGenerationService;

    public TenantAdminProductService(
        IProductRepository productRepository,
        ITenantAdminProductRepository tenantAdminProductRepository,
        ITenantAdminProductRequestValidator validator,
        IDateTimeProvider dateTimeProvider,
        ITenantAdminProductAuditLogger auditLogger,
        ProductWizardAccessPolicy accessPolicy,
        ProductVariantGenerationService variantGenerationService)
    {
        _productRepository = productRepository;
        _tenantAdminProductRepository = tenantAdminProductRepository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
        _auditLogger = auditLogger;
        _accessPolicy = accessPolicy;
        _variantGenerationService = variantGenerationService;
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

    public async Task<ApplicationResult<TenantAdminProductCreateResponse>> CreateFromWizardAsync(
        TenantRequestContext context,
        TenantAdminWizardProductCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateCreateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(accessError);
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            if (_wizardCreateIdempotency.TryGetValue(request.IdempotencyKey.Trim(), out var cached) &&
                cached.TenantId == context.TenantId &&
                cached.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return ApplicationResult<TenantAdminProductCreateResponse>.Success(cached.Response);
            }
        }

        var validationError = ValidateWizardCreateRequest(request);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(validationError);
        }

        if (!await _tenantAdminProductRepository.ActiveCategoryExistsAsync(
                context.TenantId, request.CategoryId, cancellationToken))
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError("categoryId", "Category is invalid for this tenant.")]));
        }

        if (request.BrandId.HasValue &&
            !await _tenantAdminProductRepository.BrandBelongsToTenantAsync(
                context.TenantId, request.BrandId.Value, cancellationToken))
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                "product.validation_failed",
                "Product validation failed.",
                [new ApplicationFieldError("brandId", "Brand is invalid for this tenant.")]));
        }

        if (request.PricingTax?.TaxClassId is Guid taxId)
        {
            if (!await _tenantAdminProductRepository.TaxClassBelongsToTenantAsync(
                    context.TenantId, taxId, cancellationToken))
            {
                return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                    "product.validation_failed",
                    "Selected tax is invalid, inactive, or no longer available. Choose a valid tax.",
                    [new ApplicationFieldError("taxId", "Tax is invalid for this tenant.")]));
            }
        }
        else
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                "product.validation_failed",
                "Tax is required.",
                [new ApplicationFieldError("taxId", "Tax is required.")]));
        }

        if (request.PricingTax.StandardSellingPrice is null or <= 0)
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                "product.validation_failed",
                "Standard selling price is required.",
                [new ApplicationFieldError("standardSellingPrice", "Standard selling price must be greater than zero.")]));
        }

        var skuValues = CollectWizardSkuValues(request);
        var skuSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sku in skuValues)
        {
            if (!skuSet.Add(sku))
            {
                return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                    "product.duplicate_sku",
                    "Duplicate SKU values are not allowed within this product."));
            }

            if (await _productRepository.SkuExistsAsync(context.TenantId, sku, null, cancellationToken))
            {
                return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                    "product.duplicate_sku",
                    $"SKU '{sku}' already exists."));
            }
        }

        var barcodeValues = CollectWizardBarcodeValues(request);
        var barcodeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var barcode in barcodeValues)
        {
            if (!barcodeSet.Add(barcode))
            {
                return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                    "product.duplicate_barcode",
                    "Duplicate barcode values are not allowed within this product."));
            }

            if (await _productRepository.BarcodeExistsAsync(context.TenantId, barcode, null, cancellationToken))
            {
                return ApplicationResult<TenantAdminProductCreateResponse>.Failure(new ApplicationError(
                    "product.duplicate_barcode",
                    $"Barcode '{barcode}' already exists."));
            }
        }

        var result = await _tenantAdminProductRepository.CreateProductFromWizardAsync(
            context.TenantId,
            context.UserId,
            request,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (result.Error is not null)
        {
            return ApplicationResult<TenantAdminProductCreateResponse>.Failure(result.Error);
        }

        var draft = result.Response!;
        var response = new TenantAdminProductCreateResponse(
            draft.ProductId,
            draft.ProductName,
            draft.Sku ?? draft.ProductCode ?? string.Empty,
            draft.Status);

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            _wizardCreateIdempotency[request.IdempotencyKey.Trim()] = new WizardCreateIdempotencyEntry(
                context.TenantId,
                response,
                DateTimeOffset.UtcNow.AddMinutes(10));
        }

        return ApplicationResult<TenantAdminProductCreateResponse>.Success(response);
    }

    private static ApplicationError? ValidateWizardCreateRequest(TenantAdminWizardProductCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName) ||
            request.ProductName.Trim().Equals("untitled product", StringComparison.OrdinalIgnoreCase))
        {
            return new ApplicationError(
                "product.validation_failed",
                "Product name is required.",
                [new ApplicationFieldError("productName", "Product name is required.")]);
        }

        if (request.CategoryId == Guid.Empty)
        {
            return new ApplicationError(
                "product.validation_failed",
                "Category is required.",
                [new ApplicationFieldError("categoryId", "Category is required.")]);
        }

        var structure = (request.ProductStructure ?? "SIMPLE").Trim().ToUpperInvariant();
        if (structure == "SIMPLE")
        {
            var unitId = request.BaseUnitId ?? request.ProductUnitId;
            if (!unitId.HasValue || unitId.Value == Guid.Empty)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "Product unit is required for SIMPLE products.",
                    [new ApplicationFieldError("productUnitId", "Product unit is required.")]);
            }

            var sku = request.BarcodeSkuConfiguration?.Assignments?
                .Select(a => a.Sku)
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            if (string.IsNullOrWhiteSpace(sku))
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "Base SKU is required.",
                    [new ApplicationFieldError("sku", "Base SKU is required.")]);
            }
        }
        else if (structure == "VARIANT")
        {
            var included = request.VariantConfiguration?.Variants?.Where(v => v.Included).ToList()
                           ?? [];
            if (included.Count == 0)
            {
                return new ApplicationError(
                    "product.validation_failed",
                    "At least one included variant is required.");
            }

            var assignments = request.BarcodeSkuConfiguration?.Assignments ?? [];
            foreach (var variant in included)
            {
                var match = assignments.FirstOrDefault(a =>
                    string.Equals(a.ClientCombinationKey, variant.ClientCombinationKey, StringComparison.Ordinal));
                if (match is null || string.IsNullOrWhiteSpace(match.Sku))
                {
                    return new ApplicationError(
                        "product.validation_failed",
                        $"SKU is required for variant '{variant.DisplayLabel ?? variant.CombinationLabel}'.");
                }
            }
        }
        else
        {
            return new ApplicationError(
                "product.validation_failed",
                "Product structure must be SIMPLE or VARIANT.");
        }

        return null;
    }

    private static IEnumerable<string> CollectWizardSkuValues(TenantAdminWizardProductCreateRequest request)
    {
        if (request.BarcodeSkuConfiguration?.Assignments is null)
        {
            yield break;
        }

        foreach (var assignment in request.BarcodeSkuConfiguration.Assignments)
        {
            if (!string.IsNullOrWhiteSpace(assignment.Sku))
            {
                yield return assignment.Sku.Trim();
            }
        }
    }

    private static IEnumerable<string> CollectWizardBarcodeValues(TenantAdminWizardProductCreateRequest request)
    {
        if (request.BarcodeSkuConfiguration?.Assignments is null)
        {
            yield break;
        }

        foreach (var assignment in request.BarcodeSkuConfiguration.Assignments)
        {
            if (!string.IsNullOrWhiteSpace(assignment.Barcode))
            {
                yield return assignment.Barcode.Trim();
            }
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, WizardCreateIdempotencyEntry>
        _wizardCreateIdempotency = new();

    private sealed record WizardCreateIdempotencyEntry(
        Guid TenantId,
        TenantAdminProductCreateResponse Response,
        DateTimeOffset ExpiresAt);

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
        var accessError = ValidateWizardCreateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductCreateOptionsResponse>.Failure(accessError);
        }

        var response = await _tenantAdminProductRepository.GetCreateOptionsAsync(
            context.TenantId,
            cancellationToken);

        return ApplicationResult<TenantAdminProductCreateOptionsResponse>.Success(response);
    }

    public async Task<ApplicationResult<ProductDraftResponse>> SaveDraftAsync(
        TenantRequestContext context,
        SaveProductDraftRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = await _accessPolicy.ValidateWizardAccessAsync(
            context,
            productId: null,
            isCreateAction: true,
            request,
            cancellationToken);
        if (accessError is not null)
        {
            return ApplicationResult<ProductDraftResponse>.Failure(accessError);
        }

        return await SaveOrUpdateDraftAsync(context, productId: null, request, cancellationToken);
    }

    public async Task<ApplicationResult<ProductDraftResponse>> UpdateDraftAsync(
        TenantRequestContext context,
        Guid productId,
        SaveProductDraftRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = await _accessPolicy.ValidateWizardAccessAsync(
            context,
            productId,
            isCreateAction: false,
            request,
            cancellationToken);
        if (accessError is not null)
        {
            return ApplicationResult<ProductDraftResponse>.Failure(accessError);
        }

        return await SaveOrUpdateDraftAsync(context, productId, request, cancellationToken);
    }

    public async Task<ApplicationResult<ProductSetupWizardDto>> GetSetupAsync(
        TenantRequestContext context,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var accessError = await _accessPolicy.ValidateReadAccessAsync(context, cancellationToken);
        if (accessError is not null)
        {
            return ApplicationResult<ProductSetupWizardDto>.Failure(accessError);
        }

        var response = await _tenantAdminProductRepository.GetSetupAsync(
            context.TenantId,
            productId,
            cancellationToken);

        return response is null
            ? ApplicationResult<ProductSetupWizardDto>.Failure(NotFound)
            : ApplicationResult<ProductSetupWizardDto>.Success(response);
    }

    private async Task<ApplicationResult<ProductDraftResponse>> SaveOrUpdateDraftAsync(
        TenantRequestContext context,
        Guid? productId,
        SaveProductDraftRequest request,
        CancellationToken cancellationToken)
    {
        var currentStage = Math.Clamp(request.CurrentSetupStep, 1, 8);
        var isSaveAndContinue = string.Equals(request.WizardAction, "SAVE_AND_CONTINUE", StringComparison.OrdinalIgnoreCase) || request.AdvanceStep;
        var isSkip = string.Equals(request.WizardAction, "SKIP", StringComparison.OrdinalIgnoreCase);

        var validationError = isSaveAndContinue
            ? _validator.ValidateSaveAndContinue(request)
            : _validator.ValidateSaveDraft(request);
        if (validationError is not null)
        {
            return ApplicationResult<ProductDraftResponse>.Failure(validationError);
        }

        if (currentStage == ProductWizardStage.ProductTypeTracking && (!productId.HasValue || productId.Value == Guid.Empty))
        {
            return ApplicationResult<ProductDraftResponse>.Failure(new ApplicationError(
                "product.draft_not_found",
                "Product draft must be initialized in Basic Details before updating Product Type & Tracking."));
        }

        var existingSetup = productId.HasValue
            ? await _tenantAdminProductRepository.GetSetupAsync(context.TenantId, productId.Value, cancellationToken)
            : null;

        if (productId.HasValue && existingSetup == null)
        {
            return ApplicationResult<ProductDraftResponse>.Failure(NotFound);
        }

        if (currentStage == ProductWizardStage.BasicDetails)
        {
            if (request.CategoryId.HasValue && request.CategoryId != Guid.Empty)
            {
                if (!await _tenantAdminProductRepository.ActiveCategoryExistsAsync(
                        context.TenantId,
                        request.CategoryId.Value,
                        cancellationToken))
                {
                    return ApplicationResult<ProductDraftResponse>.Failure(new ApplicationError(
                        "product.validation_failed",
                        "Product validation failed.",
                        [new ApplicationFieldError("categoryId", "Category was not found or is not active for this tenant.")]));
                }
            }

            if (request.BrandId.HasValue &&
                !await _tenantAdminProductRepository.BrandBelongsToTenantAsync(
                    context.TenantId,
                    request.BrandId.Value,
                    cancellationToken))
            {
                return ApplicationResult<ProductDraftResponse>.Failure(new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [new ApplicationFieldError("brandId", "Brand was not found for this tenant.")]));
            }

            var productCodeCheck = ResolveProductCode(request);
            if (!string.IsNullOrWhiteSpace(productCodeCheck) &&
                await _tenantAdminProductRepository.ProductCodeExistsAsync(
                    context.TenantId,
                    productCodeCheck,
                    productId,
                    cancellationToken))
            {
                return ApplicationResult<ProductDraftResponse>.Failure(new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [new ApplicationFieldError("productCode", "Product code already exists for this tenant.")]));
            }
        }

        string resolvedStructure;
        if (ProductStructureConstants.TryNormalize(request.ProductStructure, out var parsedStructure))
        {
            resolvedStructure = parsedStructure;
        }
        else if (existingSetup != null && !string.IsNullOrWhiteSpace(existingSetup.ProductStructure))
        {
            resolvedStructure = existingSetup.ProductStructure;
        }
        else
        {
            resolvedStructure = ProductStructureConstants.DefaultDraftStructure;
        }

        if (currentStage == ProductWizardStage.ProductTypeTracking && productId.HasValue && existingSetup != null)
        {
            var oldStructure = existingSetup.ProductStructure;
            if (!string.Equals(oldStructure, resolvedStructure, StringComparison.OrdinalIgnoreCase))
            {
                var hasHistory = await _tenantAdminProductRepository.HasOperationalHistoryAsync(
                    context.TenantId,
                    productId.Value,
                    cancellationToken);

                if (hasHistory)
                {
                    return ApplicationResult<ProductDraftResponse>.Failure(new ApplicationError(
                        "product.structure_change_prohibited_has_history",
                        "Product structure cannot be changed because operational inventory movements or sales history exist."));
                }
            }
        }

        if (currentStage == ProductWizardStage.ProductConfiguration && 
            string.Equals(resolvedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase) && 
            request.BundleConfiguration != null)
        {
            var bundleErrors = await ValidateBundleConfigurationAsync(
                context.TenantId,
                productId,
                request.BundleConfiguration,
                cancellationToken);

            if (bundleErrors.Count > 0)
            {
                return ApplicationResult<ProductDraftResponse>.Failure(new ApplicationError(
                    "product.bundle.validation_failed",
                    "Bundle configuration validation failed.",
                    bundleErrors));
            }
        }

        if (currentStage == ProductWizardStage.ProductConfiguration && 
            string.Equals(resolvedStructure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase) && 
            request.VariantConfiguration != null)
        {
            request.VariantConfiguration = GenerateAndReconcileVariants(request.VariantConfiguration);
        }

        if (currentStage == ProductWizardStage.BarcodeSku && request.BarcodeSkuConfiguration != null)
        {
            var skuBarcodeErrors = await ValidateBarcodeSkuConfigurationAsync(
                context.TenantId,
                productId,
                request.BarcodeSkuConfiguration,
                cancellationToken);

            if (skuBarcodeErrors.Count > 0)
            {
                return ApplicationResult<ProductDraftResponse>.Failure(new ApplicationError(
                    "product.barcode_sku_validation_failed",
                    "Barcode & SKU validation failed.",
                    skuBarcodeErrors));
            }
        }

        var trackInventory = isSkip ? (resolvedStructure != ProductStructureConstants.Bundle) : request.TrackInventory;
        var batchTracking = isSkip ? false : request.BatchTracking;
        var expiryTracking = isSkip ? false : request.ExpiryTracking;
        var serialTracking = isSkip ? false : request.SerialTracking;

        var targetSetupStep = (isSaveAndContinue || isSkip)
            ? ResolveNextApplicableStage(resolvedStructure, trackInventory, currentStage)
            : currentStage;

        var desiredPublishStatus = request.DesiredPublishActive
            ? ProductConstants.DesiredPublishActive
            : ProductConstants.DesiredPublishInactive;

        var productCode = ResolveProductCode(request) ?? string.Empty;
        var resolvedProductName = string.IsNullOrWhiteSpace(request.ProductName)
            ? ProductConstants.DraftProductNamePlaceholder
            : request.ProductName.Trim();

        var slugSourceCode = string.IsNullOrWhiteSpace(productCode)
            ? $"DRF-{Guid.NewGuid():N}"[..Math.Min(80, 36)]
            : productCode;
        var productSlug = GenerateDraftSlug(resolvedProductName, slugSourceCode);

        var isExplicitDraftSave = string.Equals(request.WizardAction, "SAVE_DRAFT", StringComparison.OrdinalIgnoreCase);

        var variantConfiguration = request.VariantConfiguration;
        if (currentStage == ProductWizardStage.ProductConfiguration && 
            string.Equals(resolvedStructure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase))
        {
            // The variants have already been generated and reconciled on line 643.
            variantConfiguration = request.VariantConfiguration;
        }

        var command = new SaveProductDraftCommand(
            productId,
            resolvedProductName,
            productCode,
            productSlug,
            resolvedStructure,
            request.CategoryId is null || request.CategoryId == Guid.Empty ? null : request.CategoryId,
            request.BrandId,
            string.IsNullOrWhiteSpace(request.ShortDescription) ? null : request.ShortDescription.Trim(),
            string.IsNullOrWhiteSpace(request.LongDescription) ? null : request.LongDescription.Trim(),
            desiredPublishStatus,
            request.PosSellable,
            trackInventory,
            batchTracking,
            expiryTracking,
            serialTracking,
            request.AllowOnlineSale,
            currentStage,
            targetSetupStep,
            request.ExpectedRowVersion,
            request.StagedMediaAssetIds?.Distinct().ToArray() ?? [],
            request.UnitModel,
            request.ProductUnitId ?? request.BaseUnitId,
            request.SellingUnitId,
            request.PurchaseUnitId,
            request.OuterPackUnitId,
            request.ItemsPerPurchaseUnit,
            request.PurchaseUnitsPerOuterPack,
            request.AllowDecimalQuantity,
            isExplicitDraftSave,
            request.WizardAction,
            variantConfiguration,
            request.BundleConfiguration,
            request.BarcodeSkuConfiguration,
            request.PricingTax);

        var result = await _tenantAdminProductRepository.SaveProductDraftAsync(
            context.TenantId,
            context.UserId,
            command,
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return result.IsSuccess
            ? ApplicationResult<ProductDraftResponse>.Success(result.Response!)
            : ApplicationResult<ProductDraftResponse>.Failure(result.Error!);
    }

    private static int ResolveNextApplicableStage(string productStructure, bool trackInventory, int currentStage)
    {
        var normalizedStructure = ProductStructureConstants.Normalize(productStructure);

        if (currentStage == ProductWizardStage.ProductTypeTracking)
        {
            if (string.Equals(normalizedStructure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedStructure, ProductStructureConstants.Variant, StringComparison.OrdinalIgnoreCase))
            {
                return ProductWizardStage.ProductConfiguration;
            }

            if (!trackInventory)
            {
                return string.Equals(normalizedStructure, ProductStructureConstants.Simple, StringComparison.OrdinalIgnoreCase)
                    ? ProductWizardStage.BarcodeSku
                    : ProductWizardStage.ProductConfiguration;
            }

            return ProductWizardStage.UnitsPackConversion;
        }

        if (currentStage == ProductWizardStage.UnitsPackConversion)
        {
            if (string.Equals(normalizedStructure, ProductStructureConstants.Simple, StringComparison.OrdinalIgnoreCase))
            {
                return ProductWizardStage.BarcodeSku;
            }

            return ProductWizardStage.ProductConfiguration;
        }

        if (currentStage == ProductWizardStage.ProductConfiguration &&
            string.Equals(normalizedStructure, ProductStructureConstants.Simple, StringComparison.OrdinalIgnoreCase))
        {
            return ProductWizardStage.BarcodeSku;
        }

        return Math.Min(currentStage + 1, ProductWizardStage.ReviewCreate);
    }

    private static ApplicationError? ValidateWizardCreateAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(ProductConstants.CreatePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateWizardUpdateAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(ProductConstants.UpdatePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateWizardViewAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("product.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(ProductConstants.ViewPermission)
            ? null
            : PermissionDenied;
    }

    private static string? ResolveProductCode(SaveProductDraftRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ProductCode))
        {
            return request.ProductCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ShortName))
        {
            return request.ShortName.Trim();
        }

        return null;
    }

    private static string GenerateDraftSlug(string name, string code)
    {
        var normalizedName = new string(name
            .Trim()
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_')
            .ToArray());

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = "product";
        }

        return $"{normalizedName}-{code.Trim().ToLowerInvariant()}";
    }

    public async Task<ApplicationResult<TenantAdminProductFilterOptionsResponse>> GetFilterOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductFilterOptionsResponse>.Failure(accessError);
        }

        var response = await _tenantAdminProductRepository.GetFilterOptionsAsync(
            context.TenantId,
            cancellationToken);

        var canViewStock = CanViewStock(context);
        if (!canViewStock)
        {
            response = new TenantAdminProductFilterOptionsResponse(
                response.Categories,
                response.Brands,
                response.ProductStatuses,
                []);
        }

        return ApplicationResult<TenantAdminProductFilterOptionsResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminProductListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        Guid? categoryId,
        Guid? brandId,
        string? productStatus,
        string? stockStatus,
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminProductListResponse>.Failure(accessError);
        }

        var canViewStock = CanViewStock(context);
        if (!string.IsNullOrWhiteSpace(stockStatus) && !canViewStock)
        {
            return ApplicationResult<TenantAdminProductListResponse>.Failure(PermissionDenied);
        }

        var validationError = _validator.ValidateListQuery(
            productStatus,
            stockStatus,
            pageNumber,
            pageSize,
            sortBy,
            sortDirection);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminProductListResponse>.Failure(validationError);
        }

        var list = await _tenantAdminProductRepository.GetPagedListAsync(
            context.TenantId,
            search,
            categoryId,
            brandId,
            productStatus,
            stockStatus,
            pageNumber,
            pageSize,
            sortBy,
            sortDirection,
            canViewStock,
            cancellationToken);

        return ApplicationResult<TenantAdminProductListResponse>.Success(list);
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
        context.HasPermission(StockPermissions.View) ||
        context.HasPermission(StockPermissions.LegacyInventoryView);

    private static bool CanViewExpiry(TenantRequestContext context) =>
        context.HasPermission(StockPermissions.ExpiryView);

    private static bool CanViewFastMoving(TenantRequestContext context) =>
        context.HasPermission(TenantAdminProductReportPermissions.ProductsView);

    private static bool CanViewStockValue(TenantRequestContext context) =>
        context.HasPermission(StockPermissions.ValueView);

    private static bool CanViewStockMovements(TenantRequestContext context) =>
        context.HasPermission(StockPermissions.MovementsView);

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

    private async Task<IReadOnlyList<ApplicationFieldError>> ValidateBundleConfigurationAsync(
        Guid tenantId,
        Guid? currentBundleProductId,
        BundleConfigurationDto configuration,
        CancellationToken cancellationToken)
    {
        var errors = new List<ApplicationFieldError>();

        if (configuration.Components == null || configuration.Components.Count == 0)
        {
            return errors;
        }

        var distinctProductIds = configuration.Components.Select(c => c.ComponentProductId).Distinct().ToList();
        var distinctVariantIds = configuration.Components.Where(c => c.ComponentVariantId.HasValue).Select(c => c.ComponentVariantId!.Value).Distinct().ToList();
        var distinctUomIds = configuration.Components.Select(c => c.ComponentUomId).Distinct().ToList();

        var productsList = await _tenantAdminProductRepository.GetProductsForBundleValidationAsync(tenantId, distinctProductIds, cancellationToken);
        var products = productsList.ToDictionary(p => p.ProductId);

        var variantsList = await _tenantAdminProductRepository.GetVariantsForBundleValidationAsync(tenantId, distinctVariantIds, cancellationToken);
        var variants = variantsList.ToDictionary(v => v.ProductVariantId);

        var uomsList = await _tenantAdminProductRepository.GetComponentUomValidationDataAsync(tenantId, distinctProductIds, distinctVariantIds, distinctUomIds, cancellationToken);

        var seenIdentities = new HashSet<string>();

        for (int i = 0; i < configuration.Components.Count; i++)
        {
            var component = configuration.Components[i];

            if (!products.TryGetValue(component.ComponentProductId, out var compProduct))
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentProductId", "product.bundle.component_no_longer_eligible"));
                continue;
            }

            if (currentBundleProductId.HasValue && component.ComponentProductId == currentBundleProductId.Value)
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentProductId", "product.bundle.self_reference_not_allowed"));
            }

            if (compProduct.ProductStructure == ProductStructureConstants.Bundle)
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentProductId", "product.bundle.nested_bundle_not_allowed"));
            }

            if (compProduct.Status == ProductConstants.InactiveStatus)
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentProductId", "product.bundle.component_inactive"));
            }
            else if (compProduct.Status == ProductConstants.ArchivedStatus)
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentProductId", "product.bundle.component_archived"));
            }
            else if (compProduct.Status == ProductConstants.DeletedStatus || compProduct.Status == ProductConstants.DraftStatus)
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentProductId", "product.bundle.component_no_longer_eligible"));
            }
            else if (!compProduct.IsSellable)
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentProductId", "product.bundle.component_no_longer_eligible"));
            }

            if (!compProduct.TrackInventory)
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentProductId", "product.bundle.component_not_inventory_tracked"));
            }

            string identity = $"{component.ComponentProductId}";

            if (compProduct.ProductStructure == ProductStructureConstants.Variant)
            {
                if (!component.ComponentVariantId.HasValue)
                {
                    errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentVariantId", "product.bundle.exact_variant_required"));
                }
                else
                {
                    identity = $"{component.ComponentProductId}_{component.ComponentVariantId.Value}";

                    if (!variants.TryGetValue(component.ComponentVariantId.Value, out var compVariant))
                    {
                        errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentVariantId", "product.bundle.component_no_longer_eligible"));
                    }
                    else
                    {
                        if (compVariant.ProductId != component.ComponentProductId)
                        {
                            errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentVariantId", "product.bundle.variant_product_mismatch"));
                        }

                        if (compVariant.Status == ProductConstants.ArchivedStatus || compVariant.Status == ProductConstants.DeletedStatus || compVariant.Status == ProductConstants.InactiveStatus || !compVariant.Included)
                        {
                            errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentVariantId", "product.bundle.component_no_longer_eligible"));
                        }
                    }
                }
            }
            else if (compProduct.ProductStructure == ProductStructureConstants.Simple)
            {
                if (component.ComponentVariantId.HasValue)
                {
                    errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentVariantId", "product.bundle.variant_product_mismatch"));
                }
            }

            var validUom = uomsList.FirstOrDefault(u => 
                u.UomId == component.ComponentUomId && 
                u.ComponentProductId == component.ComponentProductId && 
                ((u.ComponentVariantId == component.ComponentVariantId) || (!u.ComponentVariantId.HasValue && !component.ComponentVariantId.HasValue)));

            if (validUom == null)
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentUomId", "product.bundle.component_uom_invalid"));
            }
            else
            {
                if (!validUom.AllowDecimalQuantity && (component.RequiredQuantity % 1 != 0))
                {
                    errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].requiredQuantity", "product.bundle.component_quantity_precision_invalid"));
                }
            }

            if (!seenIdentities.Add(identity))
            {
                errors.Add(new ApplicationFieldError($"bundleConfiguration.components[{i}].componentProductId", "product.bundle.duplicate_component"));
            }
        }

        return errors;
    }

    private async Task<List<ApplicationFieldError>> ValidateBarcodeSkuConfigurationAsync(
        Guid tenantId,
        Guid? productId,
        BarcodeSkuConfigurationDto configuration,
        CancellationToken cancellationToken)
    {
        var errors = new List<ApplicationFieldError>();

        if (configuration.Assignments == null || configuration.Assignments.Count == 0)
        {
            return errors;
        }

        for (int i = 0; i < configuration.Assignments.Count; i++)
        {
            var assignment = configuration.Assignments[i];
            var prefix = $"barcodeSkuConfiguration.assignments[{i}]";

            if (!string.IsNullOrWhiteSpace(assignment.Sku))
            {
                if (await _tenantAdminProductRepository.SkuExistsAsync(
                        tenantId,
                        assignment.Sku,
                        assignment.ProductVariantId ?? Guid.Empty,
                        cancellationToken))
                {
                    errors.Add(new ApplicationFieldError(
                        $"{prefix}.sku",
                        "SKU already exists in the system."));
                }
            }

            if (!string.IsNullOrWhiteSpace(assignment.Barcode))
            {
                if (await _tenantAdminProductRepository.BarcodeExistsAsync(
                        tenantId,
                        assignment.Barcode,
                        assignment.ProductVariantId ?? Guid.Empty,
                        cancellationToken))
                {
                    errors.Add(new ApplicationFieldError(
                        $"{prefix}.barcode",
                        "Barcode already exists in the system."));
                }
            }
        }

        return errors;
    }

    private static VariantConfigurationDto GenerateAndReconcileVariants(VariantConfigurationDto input)
    {
        if (input.Options == null || input.Options.Count == 0) return input;
        var validOptions = input.Options.Where(o => o.Values != null && o.Values.Count > 0).OrderBy(o => o.SortOrder).ToList();
        if (validOptions.Count == 0) return input;

        IEnumerable<List<VariantConfigurationSelectedValueDto>> currentCombinations = new List<List<VariantConfigurationSelectedValueDto>> { new List<VariantConfigurationSelectedValueDto>() };

        foreach (var option in validOptions)
        {
            currentCombinations = currentCombinations.SelectMany(combo =>
                option.Values.OrderBy(v => v.SortOrder).Select(val =>
                {
                    var newCombo = new List<VariantConfigurationSelectedValueDto>(combo);
                    newCombo.Add(new VariantConfigurationSelectedValueDto(
                        option.SourceOptionTemplateId,
                        val.SourceOptionTemplateValueId,
                        option.OptionName,
                        val.ValueName
                    ));
                    return newCombo;
                })
            );
        }

        var generatedVariants = new List<VariantConfigurationVariantDto>();
        var existingVariants = input.Variants ?? Array.Empty<VariantConfigurationVariantDto>();
        var deletedVariants = input.ExcludedCombinationHashes ?? Array.Empty<VariantConfigurationDeletedCombinationDto>();

        foreach (var combo in currentCombinations)
        {
            var ordered = combo.OrderBy(x => x.SourceOptionTemplateId?.ToString() ?? x.OptionName).ToList();
            var hashInput = string.Join("|", ordered.Select(x => $"{x.SourceOptionTemplateId?.ToString() ?? x.OptionName}:{x.SourceOptionTemplateValueId?.ToString() ?? x.ValueName}"));
            var hashBytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(hashInput));
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            
            if (deletedVariants.Any(d => d.OptionCombinationHash == hash))
            {
                continue;
            }

            var existing = existingVariants.FirstOrDefault(v => v.OptionCombinationHash == hash || 
                (v.SelectedValues.Count == ordered.Count && v.SelectedValues.All(sv => ordered.Any(o => 
                    (o.SourceOptionTemplateId != null && o.SourceOptionTemplateId == sv.SourceOptionTemplateId && o.SourceOptionTemplateValueId == sv.SourceOptionTemplateValueId) ||
                    (o.SourceOptionTemplateId == null && o.OptionName == sv.OptionName && o.ValueName == sv.ValueName)))));
            
            if (existing != null)
            {
                generatedVariants.Add(existing with { OptionCombinationHash = hash, SelectedValues = ordered });
            }
            else
            {
                var label = string.Join(" / ", ordered.Select(x => x.ValueName));
                generatedVariants.Add(new VariantConfigurationVariantDto(
                    Guid.NewGuid().ToString("N"),
                    null,
                    null,
                    hash,
                    label,
                    label,
                    true,
                    null,
                    null,
                    ordered
                ));
            }
        }

        return new VariantConfigurationDto(input.Options, generatedVariants, deletedVariants);
    }
}
