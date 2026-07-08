using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public sealed class TenantAdminTillService : ITenantAdminTillService
{
    private static readonly ApplicationError PermissionDenied = new(
        "till.permission_denied",
        "Permission denied for till management.");
    private static readonly ApplicationError NotFound = new("till.not_found", "Till was not found.");
    private static readonly ApplicationError OutletNotFound = new(
        "till.outlet_not_found",
        "Outlet was not found for this tenant.");

    private readonly ITenantAdminTillRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TenantAdminTillService(
        ITenantAdminTillRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<TenantAdminTillListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        string? status,
        Guid? outletId,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.View,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillListResponse>.Failure(accessError);
        }

        if (outletId.HasValue &&
            !await _repository.OutletBelongsToTenantAsync(context.TenantId, outletId.Value, cancellationToken))
        {
            return ApplicationResult<TenantAdminTillListResponse>.Failure(OutletNotFound);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(
            context.TenantId,
            search,
            status,
            outletId,
            safePage,
            safePageSize,
            sortBy ?? "name",
            sortDirection ?? "asc",
            cancellationToken);

        return ApplicationResult<TenantAdminTillListResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminTillSummaryResponse>> GetSummaryAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.View,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillSummaryResponse>.Failure(accessError);
        }

        var response = await _repository.GetSummaryAsync(context.TenantId, cancellationToken);
        return ApplicationResult<TenantAdminTillSummaryResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminTillDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminTillCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.Create,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(accessError);
        }

        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(validationError);
        }

        if (!await _repository.OutletBelongsToTenantAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(OutletNotFound);
        }

        var normalizedTillCode = TillConstants.NormalizeTillCode(request.TillCode);
        if (await _repository.TillCodeExistsForTenantAsync(
                context.TenantId,
                normalizedTillCode,
                null,
                cancellationToken))
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(new ApplicationError(
                "till.duplicate_code",
                "Till code already exists for this tenant."));
        }

        var now = _dateTimeProvider.UtcNow;
        var tillId = Guid.NewGuid();
        var till = Till.Create(
            tillId,
            context.TenantId,
            request.OutletId,
            request.TillName,
            normalizedTillCode,
            request.Status,
            now,
            request.DeviceName,
            request.PrinterName,
            request.ScannerName,
            request.CashDrawerName,
            request.CardReaderName,
            request.InternalNote);

        await _repository.AddAsync(till, cancellationToken);
        var response = await _repository.GetDetailAsync(context.TenantId, tillId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminTillDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminTillDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminTillDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminTillPermissions.DetailsView,
            TenantAdminTillPermissions.View,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(accessError);
        }

        var response = await _repository.GetDetailAsync(context.TenantId, tillId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminTillDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminTillDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminTillDetailResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid tillId,
        TenantAdminTillUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.Update,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(accessError);
        }

        var validationError = ValidateUpdateRequest(request);
        if (validationError is not null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(validationError);
        }

        var till = await _repository.GetEditableAsync(context.TenantId, tillId, cancellationToken);
        if (till is null)
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(NotFound);
        }

        if (!await _repository.OutletBelongsToTenantAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(OutletNotFound);
        }

        var normalizedTillCode = TillConstants.NormalizeTillCode(request.TillCode);
        if (await _repository.TillCodeExistsForTenantAsync(
                context.TenantId,
                normalizedTillCode,
                tillId,
                cancellationToken))
        {
            return ApplicationResult<TenantAdminTillDetailResponse>.Failure(new ApplicationError(
                "till.duplicate_code",
                "Till code already exists for this tenant."));
        }

        till.UpdateProfile(
            request.OutletId,
            request.TillName,
            normalizedTillCode,
            request.Status,
            _dateTimeProvider.UtcNow,
            request.DeviceName,
            request.PrinterName,
            request.ScannerName,
            request.CashDrawerName,
            request.CardReaderName,
            request.InternalNote);

        await _repository.SaveChangesAsync(cancellationToken);
        var response = await _repository.GetDetailAsync(context.TenantId, tillId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminTillDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminTillDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(
            context,
            TenantAdminTillPermissions.Delete,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult.Failure(accessError);
        }

        var till = await _repository.GetEditableAsync(context.TenantId, tillId, cancellationToken);
        if (till is null)
        {
            return ApplicationResult.Failure(NotFound);
        }

        if (await _repository.HasActiveSessionAsync(context.TenantId, tillId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "till.delete_conflict",
                "Till cannot be deleted while an active session is open."));
        }

        if (await _repository.HasSalesAsync(context.TenantId, tillId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "till.delete_conflict",
                "Till cannot be deleted while sales records exist."));
        }

        if (await _repository.HasCashMovementsAsync(context.TenantId, tillId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "till.delete_conflict",
                "Till cannot be deleted while cash movements exist."));
        }

        if (await _repository.HasActiveDeviceAssignmentAsync(context.TenantId, tillId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError(
                "till.delete_conflict",
                "Till cannot be deleted while a trusted device is assigned."));
        }

        till.SoftDelete(_dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>> GetOutletOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminTillPermissions.OutletsView,
            TenantAdminTillPermissions.Create,
            TenantAdminTillPermissions.AssignOutlet,
            TenantAdminTillPermissions.Manage);
        if (accessError is not null)
        {
            return ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>.Failure(accessError);
        }

        var options = await _repository.GetOutletOptionsAsync(context.TenantId, cancellationToken);
        return ApplicationResult<IReadOnlyList<TenantAdminOutletOptionResponse>>.Success(options);
    }

    private static ApplicationError? ValidateCreateRequest(TenantAdminTillCreateRequest request)
    {
        return ValidateWriteRequest(
            request.OutletId,
            request.TillName,
            request.TillCode,
            request.Status,
            request.DeviceName,
            request.PrinterName,
            request.ScannerName,
            request.CashDrawerName,
            request.CardReaderName,
            request.InternalNote);
    }

    private static ApplicationError? ValidateUpdateRequest(TenantAdminTillUpdateRequest request)
    {
        return ValidateWriteRequest(
            request.OutletId,
            request.TillName,
            request.TillCode,
            request.Status,
            request.DeviceName,
            request.PrinterName,
            request.ScannerName,
            request.CashDrawerName,
            request.CardReaderName,
            request.InternalNote);
    }

    private static ApplicationError? ValidateWriteRequest(
        Guid outletId,
        string tillName,
        string tillCode,
        string status,
        string? deviceName,
        string? printerName,
        string? scannerName,
        string? cashDrawerName,
        string? cardReaderName,
        string? internalNote)
    {
        if (outletId == Guid.Empty)
        {
            return ValidationFailed("Outlet is required.");
        }

        if (string.IsNullOrWhiteSpace(tillName) || tillName.Trim().Length > 120)
        {
            return ValidationFailed("Till name is required and must be 120 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(tillCode) || tillCode.Trim().Length > 40)
        {
            return ValidationFailed("Till code is required and must be 40 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(status) || !TillConstants.IsValidWriteStatus(status))
        {
            return ValidationFailed("Till status must be Active, Inactive, or Maintenance.");
        }

        if (IsTooLong(deviceName, 120) ||
            IsTooLong(printerName, 120) ||
            IsTooLong(scannerName, 120) ||
            IsTooLong(cashDrawerName, 120) ||
            IsTooLong(cardReaderName, 120) ||
            IsTooLong(internalNote, 500))
        {
            return ValidationFailed("One or more hardware fields exceed the maximum length.");
        }

        return null;
    }

    private static bool IsTooLong(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength;

    private static ApplicationError ValidationFailed(string message) =>
        new("till.validation_failed", message);

    private static ApplicationError? ValidateAccess(
        TenantRequestContext context,
        string requiredPermission,
        string managePermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(managePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateAccessAny(
        TenantRequestContext context,
        params string[] permissions)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.");
        }

        return permissions.Any(context.HasPermission) ? null : PermissionDenied;
    }
}
