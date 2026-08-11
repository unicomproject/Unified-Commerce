using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

public sealed class DeviceContextRepository : IDeviceContextRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly ILogger<DeviceContextRepository> _logger;

    public DeviceContextRepository(
        EPosDbContext dbContext,
        ILogger<DeviceContextRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CurrentDeviceDbSnapshot?> ResolveCurrentDeviceAsync(
        Guid tenantId,
        string deviceFingerprint,
        CancellationToken cancellationToken)
    {
        var fingerprintHash = DeviceFingerprintHasher.Hash(deviceFingerprint);
        var deviceId = await ResolveDeviceIdAsync(tenantId, fingerprintHash, cancellationToken);

        if (deviceId is null || deviceId == Guid.Empty)
        {
            _logger.LogDebug(
                "Current device unresolved for tenant {TenantId}: no active POS device context.",
                tenantId);
            return null;
        }

        return await BuildSnapshotAsync(tenantId, deviceId.Value, cancellationToken);
    }

    public async Task<DeviceActivationRepositoryResult> ActivateDeviceAsync(
        Guid tenantId,
        Guid tenantUserId,
        DeviceActivationCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var fingerprintHash = DeviceFingerprintHasher.Hash(command.DeviceFingerprint);
        var activationCodeHash = DeviceFingerprintHasher.Hash(command.ActivationCode);

        var activationCode = await _dbContext.TillActivationCodes
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.ActivationCodeHash == activationCodeHash,
                cancellationToken);

        if (activationCode is null)
        {
            return Failure(
                "device_context.invalid_activation_code",
                "Activation code is invalid.");
        }

        if (string.Equals(
                activationCode.Status,
                TillActivationCodeConstants.UsedStatus,
                StringComparison.Ordinal))
        {
            return Failure(
                "device_context.activation_code_used",
                "Activation code has already been used.");
        }

        if (string.Equals(
                activationCode.Status,
                TillActivationCodeConstants.RevokedStatus,
                StringComparison.Ordinal))
        {
            return Failure(
                "device_context.activation_code_revoked",
                "Activation code has been revoked.");
        }

        if (activationCode.ExpiresAt <= now)
        {
            return Failure(
                "device_context.activation_code_expired",
                "Activation code has expired.");
        }

        if (!activationCode.IsUsable(now))
        {
            return Failure(
                "device_context.invalid_activation_code",
                "Activation code is not active.");
        }

        var conflictingDeviceId = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.DeviceFingerprintHash == fingerprintHash &&
                x.Status == PosDeviceConstants.ActiveStatus)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var assignedDeviceId = await (
                from assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
                where assignment.TenantId == tenantId &&
                      assignment.TillId == activationCode.TillId &&
                      assignment.ReleasedAt == null
                orderby assignment.AssignedAt descending
                select assignment.PosDeviceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignedDeviceId == Guid.Empty)
        {
            return Failure(
                "device_context.no_device_assigned",
                "No POS device is assigned to this till.");
        }

        if (conflictingDeviceId is not null &&
            conflictingDeviceId != Guid.Empty &&
            conflictingDeviceId != assignedDeviceId)
        {
            return Failure(
                "device_context.fingerprint_already_paired",
                "This device fingerprint is already paired to another POS device.");
        }

        var device = await _dbContext.PosDevices
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == assignedDeviceId,
                cancellationToken);

        if (device is null)
        {
            return Failure(
                "device_context.no_device_assigned",
                "Assigned POS device could not be found.");
        }

        if (!string.Equals(device.Status, PosDeviceConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                "device_context.device_not_active",
                "Assigned POS device is not active.");
        }

        device.PairForActivation(
            command.DeviceName,
            command.DeviceType,
            command.Platform,
            command.AppVersion,
            fingerprintHash,
            tenantUserId,
            now);

        activationCode.MarkUsed(device.Id, now);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Device activated for tenant {TenantId}: device {DeviceId}, till {TillId}.",
            tenantId,
            device.Id,
            activationCode.TillId);

        var snapshot = await BuildSnapshotAsync(tenantId, device.Id, cancellationToken);
        if (snapshot is null)
        {
            return Failure(
                "device_context.activation_failed",
                "POS device was activated but context could not be resolved.");
        }

        return Success(snapshot with { IsTrusted = true });
    }

    private async Task<CurrentDeviceDbSnapshot?> BuildSnapshotAsync(
        Guid tenantId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var snapshot = await (
                from device in _dbContext.PosDevices.AsNoTracking()
                join tenant in _dbContext.Tenants.AsNoTracking()
                    on device.TenantId equals tenant.Id
                join assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
                    on device.Id equals assignment.PosDeviceId
                join till in _dbContext.Tills.AsNoTracking()
                    on assignment.TillId equals till.Id
                join outlet in _dbContext.Outlets.AsNoTracking()
                    on till.OutletId equals outlet.Id
                where device.TenantId == tenantId &&
                      device.Id == deviceId &&
                      device.Status == PosDeviceConstants.ActiveStatus &&
                      device.IsTrusted &&
                      device.DeviceFingerprintHash != null &&
                      device.DeviceFingerprintHash != string.Empty &&
                      assignment.TenantId == tenantId &&
                      assignment.OutletId == till.OutletId &&
                      assignment.ReleasedAt == null &&
                      till.TenantId == tenantId &&
                      till.Status == TillConstants.ActiveStatus &&
                      outlet.TenantId == tenantId
                orderby assignment.AssignedAt descending
                select new
                {
                    device.TenantId,
                    tenant.TenantSlug,
                    DeviceId = device.Id,
                    device.DeviceCode,
                    device.DeviceName,
                    device.DeviceType,
                    device.Platform,
                    device.IsTrusted,
                    OutletId = outlet.Id,
                    outlet.OutletName,
                    TillId = till.Id,
                    till.TillCode,
                    till.TillName,
                    till.DefaultOpeningFloatAmount,
                    till.CurrencyCode,
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        return new CurrentDeviceDbSnapshot(
            TenantId: snapshot.TenantId,
            TenantSlug: snapshot.TenantSlug,
            DeviceId: snapshot.DeviceId,
            DeviceCode: snapshot.DeviceCode,
            DeviceName: snapshot.DeviceName,
            DeviceType: snapshot.DeviceType,
            Platform: snapshot.Platform,
            IsTrusted: true,
            OutletId: snapshot.OutletId,
            OutletName: snapshot.OutletName,
            TillId: snapshot.TillId,
            TillCode: snapshot.TillCode,
            TillName: snapshot.TillName,
            DefaultOpeningFloatAmount: snapshot.DefaultOpeningFloatAmount,
            CurrencyCode: snapshot.CurrencyCode);
    }

    private async Task<Guid?> ResolveDeviceIdAsync(
        Guid tenantId,
        string fingerprintHash,
        CancellationToken cancellationToken)
    {
        var deviceId = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.DeviceFingerprintHash == fingerprintHash &&
                x.Status == PosDeviceConstants.ActiveStatus &&
                x.IsTrusted &&
                x.DeviceFingerprintHash != null &&
                x.DeviceFingerprintHash != string.Empty)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (deviceId is not null && deviceId != Guid.Empty)
        {
            _logger.LogDebug(
                "Current device matched by fingerprint for tenant {TenantId}: device {DeviceId}.",
                tenantId,
                deviceId);
            return deviceId;
        }

        return null;
    }

    private static DeviceActivationRepositoryResult Success(CurrentDeviceDbSnapshot snapshot) =>
        new(true, null, null, snapshot);

    private static DeviceActivationRepositoryResult Failure(string errorCode, string message) =>
        new(false, errorCode, message, null);

    public async Task<PosDevice?> GetEditableByFingerprintAsync(
        Guid tenantId,
        string deviceFingerprint,
        CancellationToken cancellationToken)
    {
        var fingerprintHash = DeviceFingerprintHasher.Hash(deviceFingerprint);

        return await _dbContext.PosDevices
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.DeviceFingerprintHash == fingerprintHash &&
                x.Status == PosDeviceConstants.ActiveStatus &&
                x.IsTrusted,
                cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
