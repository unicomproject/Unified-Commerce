using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public sealed class PosDeviceService : IPosDeviceService
{
    private static readonly ApplicationError PermissionDenied = new("pos_device.permission_denied", "Permission denied for POS device management.");
    private static readonly ApplicationError NotFound = new("pos_device.not_found", "POS device was not found.");
    private static readonly ApplicationError OutletNotFound = new("pos_device.outlet_not_found", "Active outlet was not found for this tenant.");
    private readonly IPosDeviceRepository _repository;
    private readonly IPosDeviceRequestValidator _requestValidator;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PosDeviceService(
        IPosDeviceRepository repository,
        IPosDeviceRequestValidator requestValidator,
        ICodeSequenceRepository codeSequenceRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _requestValidator = requestValidator;
        _codeSequenceRepository = codeSequenceRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PosDeviceResponse>> CreateAsync(TenantRequestContext context, PosDeviceCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PosDeviceConstants.CreatePermission);
        if (accessError is not null) return ApplicationResult<PosDeviceResponse>.Failure(accessError);

        var validationError = _requestValidator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<PosDeviceResponse>.Failure(validationError);

        if (!await _repository.ActiveOutletExistsAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<PosDeviceResponse>.Failure(OutletNotFound);
        }

        var now = _dateTimeProvider.UtcNow;
        var deviceCode = await _codeSequenceRepository.GetNextCodeAsync(
            context.TenantId,
            PosDeviceConstants.CodeSequenceKey,
            PosDeviceConstants.CodePrefix,
            PosDeviceConstants.CodePaddingLength,
            now,
            cancellationToken);

        var posDeviceId = Guid.NewGuid();
        var posDevice = PosDevice.Create(
            posDeviceId,
            context.TenantId,
            request.OutletId,
            deviceCode,
            request.DeviceName,
            request.DeviceType,
            request.Status,
            context.UserId,
            now);
        await _repository.AddAsync(posDevice, cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, posDeviceId, false, cancellationToken);
        return ApplicationResult<PosDeviceResponse>.Success(response!);
    }

    public async Task<ApplicationResult<PosDeviceListResponse>> ListAsync(TenantRequestContext context, Guid? outletId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PosDeviceConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<PosDeviceListResponse>.Failure(accessError);

        if (outletId.HasValue && !await _repository.ActiveOutletExistsAsync(context.TenantId, outletId.Value, cancellationToken))
        {
            return ApplicationResult<PosDeviceListResponse>.Failure(OutletNotFound);
        }

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(context.TenantId, outletId, safePageNumber, safePageSize, search, cancellationToken);
        return ApplicationResult<PosDeviceListResponse>.Success(response);
    }

    public async Task<ApplicationResult<PosDeviceResponse>> GetByIdAsync(TenantRequestContext context, Guid posDeviceId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PosDeviceConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<PosDeviceResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, posDeviceId, false, cancellationToken);
        return response is null ? ApplicationResult<PosDeviceResponse>.Failure(NotFound) : ApplicationResult<PosDeviceResponse>.Success(response);
    }

    public async Task<ApplicationResult<PosDeviceResponse>> UpdateAsync(TenantRequestContext context, Guid posDeviceId, PosDeviceUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PosDeviceConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<PosDeviceResponse>.Failure(accessError);

        var validationError = _requestValidator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<PosDeviceResponse>.Failure(validationError);

        var posDevice = await _repository.GetEditableAsync(context.TenantId, posDeviceId, cancellationToken);
        if (posDevice is null) return ApplicationResult<PosDeviceResponse>.Failure(NotFound);

        if (!await _repository.ActiveOutletExistsAsync(context.TenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<PosDeviceResponse>.Failure(OutletNotFound);
        }

        posDevice.UpdateProfile(request.OutletId, request.DeviceName, request.DeviceType, request.Status, context.UserId, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, posDeviceId, false, cancellationToken);
        return response is null ? ApplicationResult<PosDeviceResponse>.Failure(NotFound) : ApplicationResult<PosDeviceResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid posDeviceId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, PosDeviceConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        var posDevice = await _repository.GetEditableAsync(context.TenantId, posDeviceId, cancellationToken);
        if (posDevice is null) return ApplicationResult.Failure(NotFound);

        if (await _repository.HasTillAssignmentAsync(context.TenantId, posDeviceId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError("pos_device.delete_conflict", "POS device cannot be deleted while assigned to a till."));
        }

        posDevice.SoftDelete(context.UserId, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("pos_device.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(PosDeviceConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }
}
