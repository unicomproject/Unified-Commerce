using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Inventory.Contracts;
using E_POS.Application.Modules.Tenant.Inventory.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;

namespace E_POS.Application.Modules.Tenant.Inventory.Services;

public sealed class TenantAdminInventoryService : ITenantAdminInventoryService
{
    private static readonly ApplicationError PermissionDenied = new(
        "inventory.permission_denied",
        "Permission denied for inventory management.");

    private static readonly ApplicationError TenantBlocked = new(
        "inventory.tenant_blocked",
        "Tenant status does not allow inventory operations.");

    private static readonly ApplicationError OutletAccessDenied = new(
        "inventory.outlet_access_denied",
        "Outlet access denied.");

    private static readonly ApplicationError NotFound = new(
        "inventory.not_found",
        "Inventory resource was not found.");

    private static readonly ApplicationError OutletNotFound = new(
        "inventory.outlet_not_found",
        "Outlet was not found.");

    private static readonly ApplicationError DuplicateIdempotencyKey = new(
        "inventory.duplicate_idempotency_key",
        "A stock-in with this idempotency key already exists.");

    private readonly ITenantAdminInventoryRepository _repository;
    private readonly ITenantAdminInventoryRequestValidator _validator;
    private readonly ITenantAdminInventoryAuditLogger _auditLogger;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TenantAdminInventoryService(
        ITenantAdminInventoryRepository repository,
        ITenantAdminInventoryRequestValidator validator,
        ITenantAdminInventoryAuditLogger auditLogger,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _validator = validator;
        _auditLogger = auditLogger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<TenantAdminCurrentStockListResponse>> GetCurrentStockAsync(
        TenantRequestContext context,
        TenantAdminCurrentStockQuery query,
        CancellationToken cancellationToken)
    {
        if (!CanViewStock(context))
        {
            return ApplicationResult<TenantAdminCurrentStockListResponse>.Failure(PermissionDenied);
        }

        var tenantError = await ValidateTenantStatusAsync(context.TenantId, cancellationToken);
        if (tenantError is not null)
        {
            return ApplicationResult<TenantAdminCurrentStockListResponse>.Failure(tenantError);
        }

        var validationError = _validator.ValidateCurrentStockQuery(query);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminCurrentStockListResponse>.Failure(validationError);
        }

        if (query.OutletId.HasValue)
        {
            if (!await _repository.OutletExistsAsync(context.TenantId, query.OutletId.Value, cancellationToken))
            {
                return ApplicationResult<TenantAdminCurrentStockListResponse>.Failure(OutletNotFound);
            }

            if (!await _repository.UserHasOutletAccessAsync(
                    context.TenantId,
                    context.UserId,
                    query.OutletId.Value,
                    cancellationToken))
            {
                return ApplicationResult<TenantAdminCurrentStockListResponse>.Failure(OutletAccessDenied);
            }
        }

        var response = await _repository.GetCurrentStockAsync(
            context.TenantId,
            context.UserId,
            query,
            cancellationToken);

        return ApplicationResult<TenantAdminCurrentStockListResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminCurrentStockSummaryResponse>> GetCurrentStockSummaryAsync(
        TenantRequestContext context,
        Guid? outletId,
        CancellationToken cancellationToken)
    {
        if (!CanViewStock(context))
        {
            return ApplicationResult<TenantAdminCurrentStockSummaryResponse>.Failure(PermissionDenied);
        }

        var tenantError = await ValidateTenantStatusAsync(context.TenantId, cancellationToken);
        if (tenantError is not null)
        {
            return ApplicationResult<TenantAdminCurrentStockSummaryResponse>.Failure(tenantError);
        }

        if (outletId.HasValue)
        {
            if (!await _repository.OutletExistsAsync(context.TenantId, outletId.Value, cancellationToken))
            {
                return ApplicationResult<TenantAdminCurrentStockSummaryResponse>.Failure(OutletNotFound);
            }

            if (!await _repository.UserHasOutletAccessAsync(
                    context.TenantId,
                    context.UserId,
                    outletId.Value,
                    cancellationToken))
            {
                return ApplicationResult<TenantAdminCurrentStockSummaryResponse>.Failure(OutletAccessDenied);
            }
        }

        var response = await _repository.GetCurrentStockSummaryAsync(
            context.TenantId,
            context.UserId,
            outletId,
            cancellationToken);

        return ApplicationResult<TenantAdminCurrentStockSummaryResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminStockInResponse>> StockInAsync(
        TenantRequestContext context,
        TenantAdminStockInRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanStockIn(context))
        {
            return ApplicationResult<TenantAdminStockInResponse>.Failure(PermissionDenied);
        }

        var tenantError = await ValidateTenantStatusAsync(context.TenantId, cancellationToken);
        if (tenantError is not null)
        {
            return ApplicationResult<TenantAdminStockInResponse>.Failure(tenantError);
        }

        var validationError = _validator.ValidateStockIn(request);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminStockInResponse>.Failure(validationError);
        }

        if (!await _repository.OutletExistsAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminStockInResponse>.Failure(OutletNotFound);
        }

        if (!await _repository.UserHasOutletAccessAsync(
                context.TenantId,
                context.UserId,
                request.OutletId,
                cancellationToken))
        {
            return ApplicationResult<TenantAdminStockInResponse>.Failure(OutletAccessDenied);
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) &&
            await _repository.StockInIdempotencyKeyExistsAsync(
                context.TenantId,
                request.IdempotencyKey.Trim(),
                cancellationToken))
        {
            return ApplicationResult<TenantAdminStockInResponse>.Failure(DuplicateIdempotencyKey);
        }

        var lineValidationError = await ValidateStockInLinesAsync(context.TenantId, request, cancellationToken);
        if (lineValidationError is not null)
        {
            return ApplicationResult<TenantAdminStockInResponse>.Failure(lineValidationError);
        }

        try
        {
            var response = await _repository.StockInAsync(
                context.TenantId,
                context.UserId,
                request,
                _dateTimeProvider.UtcNow,
                cancellationToken);

            _auditLogger.LogStockInCompleted(
                context.TenantId,
                context.UserId,
                response.OperationId,
                response.OutletId,
                response.ReferenceNumber,
                response.ItemCount,
                response.TotalQuantity,
                response.Items.Select(x => x.ProductVariantId).ToList());

            return ApplicationResult<TenantAdminStockInResponse>.Success(response);
        }
        catch (InventoryOperationException ex)
        {
            return ApplicationResult<TenantAdminStockInResponse>.Failure(ex.Error);
        }
    }

    public async Task<ApplicationResult<TenantAdminInventoryVariantLookupResponse>> GetProductVariantsForStockInAsync(
        TenantRequestContext context,
        Guid productId,
        CancellationToken cancellationToken)
    {
        if (!CanStockIn(context))
        {
            return ApplicationResult<TenantAdminInventoryVariantLookupResponse>.Failure(PermissionDenied);
        }

        var tenantError = await ValidateTenantStatusAsync(context.TenantId, cancellationToken);
        if (tenantError is not null)
        {
            return ApplicationResult<TenantAdminInventoryVariantLookupResponse>.Failure(tenantError);
        }

        var response = await _repository.GetProductVariantsForStockInAsync(
            context.TenantId,
            productId,
            cancellationToken);

        if (response is null)
        {
            return ApplicationResult<TenantAdminInventoryVariantLookupResponse>.Failure(NotFound);
        }

        return ApplicationResult<TenantAdminInventoryVariantLookupResponse>.Success(response);
    }

    private async Task<ApplicationError?> ValidateStockInLinesAsync(
        Guid tenantId,
        TenantAdminStockInRequest request,
        CancellationToken cancellationToken)
    {
        var fieldErrors = new List<ApplicationFieldError>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var index = 0; index < request.Items.Count; index++)
        {
            var line = request.Items[index];
            var prefix = $"items[{index}]";
            var profile = await _repository.GetVariantTrackingProfileAsync(
                tenantId,
                line.ProductVariantId,
                cancellationToken);

            if (profile is null)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    $"{prefix}.productVariantId",
                    "Product variant was not found or is not active."));
                continue;
            }

            if (!profile.IsStockTracked)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    $"{prefix}.productVariantId",
                    "Stock tracking is not enabled for this variant."));
            }

            if (profile.RequiresBatchTracking && string.IsNullOrWhiteSpace(line.BatchNumber))
            {
                fieldErrors.Add(new ApplicationFieldError(
                    $"{prefix}.batchNumber",
                    "Batch number is required for this variant."));
            }

            if (!profile.RequiresBatchTracking && !string.IsNullOrWhiteSpace(line.BatchNumber))
            {
                fieldErrors.Add(new ApplicationFieldError(
                    $"{prefix}.batchNumber",
                    "Batch number is not allowed for this variant."));
            }

            if (profile.RequiresExpiryTracking && !line.ExpiryDate.HasValue)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    $"{prefix}.expiryDate",
                    "Expiry date is required for this variant."));
            }

            if (!profile.RequiresExpiryTracking && line.ExpiryDate.HasValue)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    $"{prefix}.expiryDate",
                    "Expiry date is not allowed for this variant."));
            }

            if (line.ExpiryDate.HasValue && line.ExpiryDate.Value < today)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    $"{prefix}.expiryDate",
                    "Expired stock cannot be received."));
            }
        }

        return fieldErrors.Count == 0
            ? null
            : new ApplicationError("inventory.validation_failed", "Inventory validation failed.", fieldErrors);
    }

    private async Task<ApplicationError?> ValidateTenantStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenantStatus = await _repository.GetTenantStatusAsync(tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(tenantStatus) ||
            !TenantAuthConstants.IsTenantLoginStatusAllowed(tenantStatus))
        {
            return TenantBlocked;
        }

        return null;
    }

    private static bool CanViewStock(TenantRequestContext context) =>
        context.HasPermission(TenantAdminStockPermissions.View) ||
        context.HasPermission(TenantAdminStockPermissions.LegacyInventoryView);

    private static bool CanStockIn(TenantRequestContext context) =>
        context.HasPermission(TenantAdminStockPermissions.StockIn);
}

public sealed class InventoryOperationException : Exception
{
    public InventoryOperationException(ApplicationError error)
        : base(error.Message)
    {
        Error = error;
    }

    public ApplicationError Error { get; }
}
