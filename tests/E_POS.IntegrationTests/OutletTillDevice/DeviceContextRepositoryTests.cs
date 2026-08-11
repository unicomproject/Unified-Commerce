using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.IntegrationTests.OutletTillDevice;

public sealed class DeviceContextRepositoryTests
{
    [Fact]
    public async Task ResolveCurrentDeviceAsync_WhenFingerprintMissing_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        SeedOutletTillDevice(
            dbContext,
            tenantId,
            outletId,
            tillId,
            deviceId,
            now);

        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var snapshot = await repository.ResolveCurrentDeviceAsync(
            tenantId,
            "pos-web-test-fingerprint",
            CancellationToken.None);

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task ResolveCurrentDeviceAsync_WhenFingerprintMatchesTrustedDevice_ReturnsAssignedDevice()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        const string fingerprint = "pos-android-device-001";
        var fingerprintHash = DeviceFingerprintHasher.Hash(fingerprint);

        SeedOutletTillDevice(
            dbContext,
            tenantId,
            outletId,
            tillId,
            deviceId,
            now,
            fingerprintHash,
            isTrusted: true);

        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var snapshot = await repository.ResolveCurrentDeviceAsync(
            tenantId,
            fingerprint,
            CancellationToken.None);

        Assert.NotNull(snapshot);
        Assert.Equal(deviceId, snapshot!.DeviceId);
        Assert.Equal("POS-01", snapshot.DeviceCode);
        Assert.True(snapshot.IsTrusted);
    }

    [Fact]
    public async Task ActivateDeviceAsync_WhenActivationCodeValid_PairsDeviceAndReturnsSnapshot()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        const string activationCode = "TILL-8K92-POS";
        const string fingerprint = "pos-web-test-fingerprint";

        SeedOutletTillDevice(
            dbContext,
            tenantId,
            outletId,
            tillId,
            deviceId,
            now,
            isTrusted: false);

        dbContext.TillActivationCodes.Add(TillActivationCode.Create(
            Guid.NewGuid(),
            tenantId,
            outletId,
            tillId,
            DeviceFingerprintHasher.Hash(activationCode),
            tenantUserId,
            now.AddDays(30),
            now));

        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var result = await repository.ActivateDeviceAsync(
            tenantId,
            tenantUserId,
            new DeviceActivationCommand(
                activationCode,
                fingerprint,
                "Front POS Tablet",
                "fixed_pos_tablet",
                "web",
                "1.0.0"),
            now,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(deviceId, result.Snapshot!.DeviceId);
        Assert.Equal(tillId, result.Snapshot.TillId);
        Assert.True(result.Snapshot.IsTrusted);

        var activationRow = await dbContext.TillActivationCodes.SingleAsync();
        Assert.Equal("USED", activationRow.Status);
        Assert.Equal(deviceId, activationRow.UsedByPosDeviceId);

        var device = await dbContext.PosDevices.SingleAsync(x => x.Id == deviceId);
        Assert.True(device.IsTrusted);
        Assert.Equal(DeviceFingerprintHasher.Hash(fingerprint), device.DeviceFingerprintHash);
    }

    [Fact]
    public async Task ActivateDeviceAsync_WhenActivationCodeAlreadyUsedBySameDevice_RejectsChangedFingerprintWithoutMutation()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        const string activationCode = "TILL-8K92-POS";
        const string originalFingerprint = "pos-web-localhost-60057";
        const string newFingerprint = "pos-web-installation-abc";

        SeedOutletTillDevice(
            dbContext,
            tenantId,
            outletId,
            tillId,
            deviceId,
            now,
            DeviceFingerprintHasher.Hash(originalFingerprint),
            isTrusted: true);

        var activationRow = TillActivationCode.Create(
            Guid.NewGuid(),
            tenantId,
            outletId,
            tillId,
            DeviceFingerprintHasher.Hash(activationCode),
            tenantUserId,
            now.AddDays(30),
            now);
        activationRow.MarkUsed(deviceId, now);
        dbContext.TillActivationCodes.Add(activationRow);

        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var result = await repository.ActivateDeviceAsync(
            tenantId,
            tenantUserId,
            new DeviceActivationCommand(
                activationCode,
                newFingerprint,
                "Front POS Tablet",
                "fixed_pos_tablet",
                "web",
                "1.0.0"),
            now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("device_context.activation_code_used", result.ErrorCode);

        var device = await dbContext.PosDevices.SingleAsync(x => x.Id == deviceId);
        Assert.Equal(DeviceFingerprintHasher.Hash(originalFingerprint), device.DeviceFingerprintHash);
        Assert.True(device.IsTrusted);
    }

    [Fact]
    public async Task ActivateDeviceAsync_WhenActivationCodeUsedAndFingerprintUnchanged_RejectsCodeReuse()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        const string activationCode = "TILL-USED-POS";
        const string fingerprint = "pos-trusted-fingerprint";

        SeedOutletTillDevice(
            dbContext,
            tenantId,
            outletId,
            tillId,
            deviceId,
            now,
            DeviceFingerprintHasher.Hash(fingerprint),
            isTrusted: true);

        var activationRow = TillActivationCode.Create(
            Guid.NewGuid(), tenantId, outletId, tillId,
            DeviceFingerprintHasher.Hash(activationCode), tenantUserId,
            now.AddDays(30), now);
        activationRow.MarkUsed(deviceId, now);
        dbContext.TillActivationCodes.Add(activationRow);
        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var result = await repository.ActivateDeviceAsync(
            tenantId,
            tenantUserId,
            new DeviceActivationCommand(
                activationCode, fingerprint, "Front POS Tablet", "fixed_pos_tablet", "web", "1.0.0"),
            now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("device_context.activation_code_used", result.ErrorCode);
    }

    [Fact]
    public async Task ActivateDeviceAsync_WhenActivationCodeExpired_RejectsWithoutMutation()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        const string activationCode = "TILL-EXPIRED-POS";

        SeedOutletTillDevice(dbContext, tenantId, outletId, tillId, deviceId, now, isTrusted: false);
        dbContext.TillActivationCodes.Add(TillActivationCode.Create(
            Guid.NewGuid(), tenantId, outletId, tillId,
            DeviceFingerprintHasher.Hash(activationCode), tenantUserId,
            now.AddMinutes(-1), now.AddDays(-1)));
        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var result = await repository.ActivateDeviceAsync(
            tenantId, tenantUserId,
            new DeviceActivationCommand(
                activationCode, "expired-fingerprint", "Front POS Tablet", "fixed_pos_tablet", "web", "1.0.0"),
            now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("device_context.activation_code_expired", result.ErrorCode);
        Assert.Equal("ACTIVE", (await dbContext.TillActivationCodes.SingleAsync()).Status);
        Assert.False((await dbContext.PosDevices.SingleAsync()).IsTrusted);
    }

    [Fact]
    public async Task ActivateDeviceAsync_WhenActivationCodeRevoked_RejectsWithoutMutation()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        const string activationCode = "TILL-REVOKED-POS";

        SeedOutletTillDevice(dbContext, tenantId, outletId, tillId, deviceId, now, isTrusted: false);
        var activationRow = TillActivationCode.Create(
            Guid.NewGuid(), tenantId, outletId, tillId,
            DeviceFingerprintHasher.Hash(activationCode), tenantUserId,
            now.AddDays(30), now);
        typeof(TillActivationCode).GetProperty(nameof(TillActivationCode.Status))!
            .SetValue(activationRow, "REVOKED");
        dbContext.TillActivationCodes.Add(activationRow);
        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var result = await repository.ActivateDeviceAsync(
            tenantId, tenantUserId,
            new DeviceActivationCommand(
                activationCode, "revoked-fingerprint", "Front POS Tablet", "fixed_pos_tablet", "web", "1.0.0"),
            now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("device_context.activation_code_revoked", result.ErrorCode);
        Assert.Equal("REVOKED", (await dbContext.TillActivationCodes.SingleAsync()).Status);
        Assert.False((await dbContext.PosDevices.SingleAsync()).IsTrusted);
    }

    [Fact]
    public async Task ActivateDeviceAsync_WhenActivationCodeInvalid_ReturnsFailure()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);

        SeedOutletTillDevice(
            dbContext,
            tenantId,
            outletId,
            tillId,
            deviceId,
            now,
            isTrusted: false);

        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var result = await repository.ActivateDeviceAsync(
            tenantId,
            tenantUserId,
            new DeviceActivationCommand(
                "INVALID-CODE",
                "pos-web-test-fingerprint",
                "Front POS Tablet",
                "fixed_pos_tablet",
                "web",
                "1.0.0"),
            now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("device_context.invalid_activation_code", result.ErrorCode);
    }

    [Fact]
    public async Task ActivateDeviceAsync_WhenCodeBelongsToAnotherTenant_ReturnsSafeFailureWithoutMutation()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        const string activationCode = "OTHER-TENANT-CODE";

        SeedOutletTillDevice(dbContext, tenantId, outletId, tillId, deviceId, now, isTrusted: false);
        dbContext.TillActivationCodes.Add(TillActivationCode.Create(
            Guid.NewGuid(), otherTenantId, Guid.NewGuid(), Guid.NewGuid(),
            DeviceFingerprintHasher.Hash(activationCode), Guid.NewGuid(),
            now.AddDays(30), now));
        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var result = await repository.ActivateDeviceAsync(
            tenantId, tenantUserId,
            new DeviceActivationCommand(
                activationCode, "tenant-a-fingerprint", "Front POS Tablet", "fixed_pos_tablet", "web", "1.0.0"),
            now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("device_context.invalid_activation_code", result.ErrorCode);
        var activationRow = await dbContext.TillActivationCodes.SingleAsync();
        Assert.Equal("ACTIVE", activationRow.Status);
        Assert.Null(activationRow.UsedAt);
        Assert.Null(activationRow.UsedByPosDeviceId);
        Assert.False((await dbContext.PosDevices.SingleAsync()).IsTrusted);
    }

    [Fact]
    public async Task ActivateDeviceAsync_WhenFingerprintBelongsToAnotherDevice_RejectsAtomically()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var assignedDeviceId = Guid.NewGuid();
        var conflictingDeviceId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.Zero);
        const string activationCode = "TILL-CONFLICT-POS";
        const string fingerprint = "already-paired-fingerprint";

        SeedOutletTillDevice(
            dbContext, tenantId, outletId, tillId, assignedDeviceId, now, isTrusted: false);

        var conflictingDevice = PosDevice.Create(
            conflictingDeviceId,
            tenantId,
            outletId,
            "POS-02",
            "Other POS Device",
            "TABLET",
            "ACTIVE",
            null,
            now);
        typeof(PosDevice).GetProperty(nameof(PosDevice.DeviceFingerprintHash))!
            .SetValue(conflictingDevice, DeviceFingerprintHasher.Hash(fingerprint));
        typeof(PosDevice).GetProperty(nameof(PosDevice.IsTrusted))!
            .SetValue(conflictingDevice, true);
        dbContext.PosDevices.Add(conflictingDevice);

        dbContext.TillActivationCodes.Add(TillActivationCode.Create(
            Guid.NewGuid(), tenantId, outletId, tillId,
            DeviceFingerprintHasher.Hash(activationCode), tenantUserId,
            now.AddDays(30), now));
        await dbContext.SaveChangesAsync();

        var repository = new DeviceContextRepository(dbContext, NullLogger<DeviceContextRepository>.Instance);
        var result = await repository.ActivateDeviceAsync(
            tenantId, tenantUserId,
            new DeviceActivationCommand(
                activationCode, fingerprint, "Front POS Tablet", "fixed_pos_tablet", "web", "1.0.0"),
            now, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("device_context.fingerprint_already_paired", result.ErrorCode);

        var activationRow = await dbContext.TillActivationCodes.SingleAsync();
        Assert.Equal("ACTIVE", activationRow.Status);
        Assert.Null(activationRow.UsedAt);
        Assert.Null(activationRow.UsedByPosDeviceId);

        var assignedDevice = await dbContext.PosDevices.SingleAsync(x => x.Id == assignedDeviceId);
        Assert.False(assignedDevice.IsTrusted);
        Assert.Null(assignedDevice.DeviceFingerprintHash);
    }

    private static void SeedOutletTillDevice(
        EPosDbContext dbContext,
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        Guid deviceId,
        DateTimeOffset now,
        string? fingerprintHash = null,
        bool isTrusted = false)
    {
        dbContext.Tenants.Add(Tenant.Create(
            tenantId,
            "DEV-001",
            "dev-001",
            "Test Tenant",
            "active",
            "LKR",
            "UTC",
            null,
            null,
            now));

        dbContext.Outlets.Add(Outlet.Create(
            outletId,
            tenantId,
            "Main Outlet",
            "MAIN-01",
            "ACTIVE",
            "STORE",
            "UTC",
            true,
            null,
            null,
            null,
            now));

        dbContext.Tills.Add(Till.Create(
            tillId,
            tenantId,
            outletId,
            "Front Till 01",
            "Front",
            1,
            "FRONT-01",
            "STANDARD",
            0m,
            "LKR",
            true,
            "ACTIVE",
            null,
            now));

        var device = PosDevice.Create(
            deviceId,
            tenantId,
            outletId,
            "POS-01",
            "Front POS Device",
            "TABLET",
            "ACTIVE",
            null,
            now);

        if (!string.IsNullOrWhiteSpace(fingerprintHash))
        {
            typeof(PosDevice).GetProperty(nameof(PosDevice.DeviceFingerprintHash))!
                .SetValue(device, fingerprintHash);
        }

        if (isTrusted)
        {
            typeof(PosDevice).GetProperty(nameof(PosDevice.IsTrusted))!
                .SetValue(device, true);
        }

        dbContext.PosDevices.Add(device);

        dbContext.TillDeviceAssignments.Add(
            TillDeviceAssignment.Create(Guid.NewGuid(), tenantId, outletId, tillId, deviceId, null, now));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}
