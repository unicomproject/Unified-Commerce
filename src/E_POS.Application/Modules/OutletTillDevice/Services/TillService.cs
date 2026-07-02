using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.OutletTillDevice.Contracts;
using E_POS.Application.Modules.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.OutletTillDevice.Constants;
using E_POS.Domain.Modules.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.OutletTillDevice.Services;

public sealed class TillService : ITillService
{
    private static readonly ApplicationError PermissionDenied = new("till.permission_denied", "Permission denied for till management.");
    private static readonly ApplicationError NotFound = new("till.not_found", "Till was not found.");
    private static readonly ApplicationError OutletNotFound = new("till.outlet_not_found", "Active outlet was not found for this tenant.");
    private readonly ITillRepository _repository;
    private readonly ITillRequestValidator _requestValidator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TillService(ITillRepository repository, ITillRequestValidator requestValidator, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _requestValidator = requestValidator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<TillResponse>> CreateAsync(TenantRequestContext context, TillCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, TillConstants.CreatePermission);
        if (accessError is not null) return ApplicationResult<TillResponse>.Failure(accessError);

        var validationError = _requestValidator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<TillResponse>.Failure(validationError);

        if (!await _repository.ActiveOutletExistsAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<TillResponse>.Failure(OutletNotFound);
        }

        var normalizedTillCode = TillConstants.NormalizeTillCode(request.TillCode);
        if (await _repository.TillCodeExistsAsync(context.TenantId, request.OutletId, normalizedTillCode, null, cancellationToken))
        {
            return ApplicationResult<TillResponse>.Failure(new ApplicationError("till.duplicate_code", "Till code already exists for this outlet."));
        }

        var now = _dateTimeProvider.UtcNow;
        var tillId = Guid.NewGuid();
        var till = Till.Create(tillId, context.TenantId, request.OutletId, request.Name, normalizedTillCode, request.Status, now);
        await _repository.AddAsync(till, cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, tillId, false, cancellationToken);
        return ApplicationResult<TillResponse>.Success(response!);
    }

    public async Task<ApplicationResult<TillListResponse>> ListAsync(TenantRequestContext context, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, TillConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<TillListResponse>.Failure(accessError);

        if (outletId.HasValue && !await _repository.ActiveOutletExistsAsync(context.TenantId, outletId.Value, cancellationToken))
        {
            return ApplicationResult<TillListResponse>.Failure(OutletNotFound);
        }

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(context.TenantId, outletId, safePageNumber, safePageSize, search, cancellationToken);
        return ApplicationResult<TillListResponse>.Success(response);
    }

    public async Task<ApplicationResult<TillResponse>> GetByIdAsync(TenantRequestContext context, Guid tillId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, TillConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<TillResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, tillId, false, cancellationToken);
        return response is null ? ApplicationResult<TillResponse>.Failure(NotFound) : ApplicationResult<TillResponse>.Success(response);
    }

    public async Task<ApplicationResult<TillResponse>> UpdateAsync(TenantRequestContext context, Guid tillId, TillUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, TillConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<TillResponse>.Failure(accessError);

        var validationError = _requestValidator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<TillResponse>.Failure(validationError);

        var till = await _repository.GetEditableAsync(context.TenantId, tillId, cancellationToken);
        if (till is null) return ApplicationResult<TillResponse>.Failure(NotFound);

        if (!await _repository.ActiveOutletExistsAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<TillResponse>.Failure(OutletNotFound);
        }

        var normalizedTillCode = TillConstants.NormalizeTillCode(request.TillCode);
        if (await _repository.TillCodeExistsAsync(context.TenantId, request.OutletId, normalizedTillCode, tillId, cancellationToken))
        {
            return ApplicationResult<TillResponse>.Failure(new ApplicationError("till.duplicate_code", "Till code already exists for this outlet."));
        }

        till.UpdateProfile(request.OutletId, request.Name, normalizedTillCode, request.Status, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, tillId, false, cancellationToken);
        return response is null ? ApplicationResult<TillResponse>.Failure(NotFound) : ApplicationResult<TillResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid tillId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, TillConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        var till = await _repository.GetEditableAsync(context.TenantId, tillId, cancellationToken);
        if (till is null) return ApplicationResult.Failure(NotFound);

        if (await _repository.HasDeviceAssignmentAsync(context.TenantId, tillId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError("till.delete_conflict", "Till cannot be deleted while POS devices are assigned."));
        }

        till.SoftDelete(_dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("till.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(TillConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }
}