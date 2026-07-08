using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public sealed class TillDeviceAssignmentService : ITillDeviceAssignmentService
{
    private static readonly ApplicationError PermissionDenied = new("till_device_assignment.permission_denied", "Permission denied for till device assignment.");
    private static readonly ApplicationError TillNotFound = new("till_device_assignment.till_not_found", "Active till was not found for this tenant.");
    private static readonly ApplicationError DeviceNotFound = new("till_device_assignment.device_not_found", "Active POS device was not found for this tenant.");
    private static readonly ApplicationError NotFound = new("till_device_assignment.not_found", "Till device assignment was not found.");
    private readonly ITillDeviceAssignmentRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TillDeviceAssignmentService(ITillDeviceAssignmentRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<TillDeviceAssignmentResponse>> AssignAsync(TenantRequestContext context, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult<TillDeviceAssignmentResponse>.Failure(accessError);
        if (tillId == Guid.Empty || posDeviceId == Guid.Empty)
        {
            return ApplicationResult<TillDeviceAssignmentResponse>.Failure(new ApplicationError("till_device_assignment.validation_failed", "Till and POS device are required."));
        }

        var tillContext = await _repository.GetTillContextAsync(context.TenantId, tillId, cancellationToken);
        if (tillContext is null)
        {
            return ApplicationResult<TillDeviceAssignmentResponse>.Failure(TillNotFound);
        }

        var deviceContext = await _repository.GetDeviceContextAsync(context.TenantId, posDeviceId, cancellationToken);
        if (deviceContext is null)
        {
            return ApplicationResult<TillDeviceAssignmentResponse>.Failure(DeviceNotFound);
        }

        if (tillContext.OutletId != deviceContext.OutletId)
        {
            return ApplicationResult<TillDeviceAssignmentResponse>.Failure(new ApplicationError("till_device_assignment.outlet_mismatch", "Till and POS device must belong to the same outlet."));
        }

        if (await _repository.GetActiveByTillAndDeviceAsync(context.TenantId, tillId, posDeviceId, cancellationToken) is not null)
        {
            return ApplicationResult<TillDeviceAssignmentResponse>.Failure(new ApplicationError("till_device_assignment.duplicate", "POS device is already assigned to this till."));
        }

        if (await _repository.DeviceAssignedToAnyTillAsync(context.TenantId, posDeviceId, tillId, cancellationToken))
        {
            return ApplicationResult<TillDeviceAssignmentResponse>.Failure(new ApplicationError("till_device_assignment.device_already_assigned", "POS device is already assigned to another till."));
        }

        if (await _repository.TillAssignedToAnyDeviceAsync(context.TenantId, tillId, posDeviceId, cancellationToken))
        {
            return ApplicationResult<TillDeviceAssignmentResponse>.Failure(new ApplicationError("till_device_assignment.till_already_assigned", "Till is already assigned to another POS device."));
        }

        var now = _dateTimeProvider.UtcNow;
        var assignment = TillDeviceAssignment.Create(
            Guid.NewGuid(),
            context.TenantId,
            tillContext.OutletId,
            tillId,
            posDeviceId,
            context.UserId,
            now);
        await _repository.AddAsync(assignment, cancellationToken);
        var response = await _repository.GetActiveByTillAndDeviceAsync(context.TenantId, tillId, posDeviceId, cancellationToken);
        return ApplicationResult<TillDeviceAssignmentResponse>.Success(response!);
    }

    public async Task<ApplicationResult<TillDeviceAssignmentListResponse>> ListByTillAsync(TenantRequestContext context, Guid tillId, CancellationToken cancellationToken)
    {
        var accessError = ValidateViewAccess(context);
        if (accessError is not null) return ApplicationResult<TillDeviceAssignmentListResponse>.Failure(accessError);
        if (tillId == Guid.Empty)
        {
            return ApplicationResult<TillDeviceAssignmentListResponse>.Failure(new ApplicationError("till_device_assignment.validation_failed", "Till is required."));
        }

        var response = await _repository.ListByTillAsync(context.TenantId, tillId, cancellationToken);
        return response is null ? ApplicationResult<TillDeviceAssignmentListResponse>.Failure(TillNotFound) : ApplicationResult<TillDeviceAssignmentListResponse>.Success(response);
    }

    public async Task<ApplicationResult> RemoveAsync(TenantRequestContext context, Guid tillId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        var accessError = ValidateManageAccess(context);
        if (accessError is not null) return ApplicationResult.Failure(accessError);
        if (tillId == Guid.Empty || posDeviceId == Guid.Empty)
        {
            return ApplicationResult.Failure(new ApplicationError("till_device_assignment.validation_failed", "Till and POS device are required."));
        }

        var assignment = await _repository.GetEditableAsync(context.TenantId, tillId, posDeviceId, cancellationToken);
        if (assignment is null) return ApplicationResult.Failure(NotFound);

        await _repository.ReleaseAsync(assignment, context.UserId, null, _dateTimeProvider.UtcNow, cancellationToken);
        return ApplicationResult.Success();
    }

    private static ApplicationError? ValidateViewAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("till_device_assignment.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TillConstants.ViewPermission) ||
               context.HasPermission(TillConstants.ManagePermission) ||
               context.HasPermission(PosDeviceConstants.ViewPermission) ||
               context.HasPermission(PosDeviceConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }

    private static ApplicationError? ValidateManageAccess(TenantRequestContext context)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("till_device_assignment.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(TillConstants.ManagePermission) || context.HasPermission(PosDeviceConstants.ManagePermission)
            ? null
            : PermissionDenied;
    }
}
