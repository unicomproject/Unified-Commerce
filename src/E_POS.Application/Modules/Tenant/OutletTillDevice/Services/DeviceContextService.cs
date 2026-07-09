using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Services;

public sealed class DeviceContextService : IDeviceContextService
{
    private static readonly ApplicationError InvalidFingerprint = new(
        "device_context.invalid_fingerprint",
        "Device fingerprint is required.");

    private static readonly ApplicationError InvalidActivationCode = new(
        "device_context.invalid_activation_code",
        "Activation code is required.");

    private static readonly ApplicationError InvalidDeviceName = new(
        "device_context.invalid_device_name",
        "Device name is required.");

    private static readonly ApplicationError InvalidDeviceType = new(
        "device_context.invalid_device_type",
        "Device type is required.");

    private static readonly ApplicationError NotFound = new(
        "device_context.not_found",
        "No registered POS device was found for this client.");

    private readonly IDeviceContextRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeviceContextService(
        IDeviceContextRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<CurrentDeviceResponseDto>> GetCurrentDeviceAsync(
        TenantRequestContext context,
        string? deviceFingerprint,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceFingerprint))
        {
            return ApplicationResult<CurrentDeviceResponseDto>.Failure(InvalidFingerprint);
        }

        var snapshot = await _repository.ResolveCurrentDeviceAsync(
            context.TenantId,
            deviceFingerprint.Trim(),
            cancellationToken);

        if (snapshot is null)
        {
            return ApplicationResult<CurrentDeviceResponseDto>.Failure(NotFound);
        }

        return ApplicationResult<CurrentDeviceResponseDto>.Success(MapToResponse(snapshot));
    }

    public async Task<ApplicationResult<CurrentDeviceResponseDto>> ActivateDeviceAsync(
        TenantRequestContext context,
        ActivateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ActivationCode))
        {
            return ApplicationResult<CurrentDeviceResponseDto>.Failure(InvalidActivationCode);
        }

        if (string.IsNullOrWhiteSpace(request.DeviceFingerprint))
        {
            return ApplicationResult<CurrentDeviceResponseDto>.Failure(InvalidFingerprint);
        }

        if (string.IsNullOrWhiteSpace(request.DeviceName))
        {
            return ApplicationResult<CurrentDeviceResponseDto>.Failure(InvalidDeviceName);
        }

        if (string.IsNullOrWhiteSpace(request.DeviceType))
        {
            return ApplicationResult<CurrentDeviceResponseDto>.Failure(InvalidDeviceType);
        }

        var result = await _repository.ActivateDeviceAsync(
            context.TenantId,
            context.UserId,
            new DeviceActivationCommand(
                request.ActivationCode.Trim(),
                request.DeviceFingerprint.Trim(),
                request.DeviceName.Trim(),
                request.DeviceType.Trim(),
                string.IsNullOrWhiteSpace(request.Platform) ? null : request.Platform.Trim(),
                string.IsNullOrWhiteSpace(request.AppVersion) ? null : request.AppVersion.Trim()),
            _dateTimeProvider.UtcNow,
            cancellationToken);

        if (!result.IsSuccess || result.Snapshot is null)
        {
            return ApplicationResult<CurrentDeviceResponseDto>.Failure(
                new ApplicationError(
                    result.ErrorCode ?? "device_context.activation_failed",
                    result.Message ?? "POS device activation failed."));
        }

        return ApplicationResult<CurrentDeviceResponseDto>.Success(MapToResponse(result.Snapshot));
    }

    private static CurrentDeviceResponseDto MapToResponse(CurrentDeviceDbSnapshot snapshot) =>
        new(
            TenantId: snapshot.TenantId,
            Device: new CurrentDeviceDeviceDto(
                Id: snapshot.DeviceId,
                DeviceCode: snapshot.DeviceCode,
                DeviceName: snapshot.DeviceName,
                DeviceType: snapshot.DeviceType,
                Platform: snapshot.Platform,
                OutletId: snapshot.OutletId,
                TillId: snapshot.TillId,
                IsTrusted: snapshot.IsTrusted),
            Outlet: new CurrentDeviceOutletDto(
                Id: snapshot.OutletId,
                Name: snapshot.OutletName),
            Till: new CurrentDeviceTillDto(
                Id: snapshot.TillId,
                Code: snapshot.TillCode,
                Name: snapshot.TillName,
                DefaultOpeningFloatAmount: snapshot.DefaultOpeningFloatAmount,
                CurrencyCode: snapshot.CurrencyCode));
}
