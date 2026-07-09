namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface IDeviceContextRepository
{
    Task<CurrentDeviceDbSnapshot?> ResolveCurrentDeviceAsync(
        Guid tenantId,
        string deviceFingerprint,
        CancellationToken cancellationToken);

    Task<DeviceActivationRepositoryResult> ActivateDeviceAsync(
        Guid tenantId,
        Guid tenantUserId,
        DeviceActivationCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record DeviceActivationCommand(
    string ActivationCode,
    string DeviceFingerprint,
    string DeviceName,
    string DeviceType,
    string? Platform,
    string? AppVersion);

public sealed record DeviceActivationRepositoryResult(
    bool IsSuccess,
    string? ErrorCode,
    string? Message,
    CurrentDeviceDbSnapshot? Snapshot);

public sealed record CurrentDeviceDbSnapshot(
    Guid TenantId,
    Guid DeviceId,
    string DeviceCode,
    string DeviceName,
    string DeviceType,
    string? Platform,
    bool IsTrusted,
    Guid OutletId,
    string OutletName,
    Guid TillId,
    string TillCode,
    string TillName,
    decimal DefaultOpeningFloatAmount,
    string CurrencyCode);
