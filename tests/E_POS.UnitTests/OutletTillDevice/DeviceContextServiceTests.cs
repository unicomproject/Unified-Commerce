using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Xunit;

namespace E_POS.UnitTests.OutletTillDevice;

public sealed class DeviceContextServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task ActivateDeviceAsync_WithoutTenantTillManage_ReturnsPermissionDeniedBeforeRepository()
    {
        var repository = new FakeRepository();
        var service = new DeviceContextService(repository, new FixedClock());

        var result = await service.ActivateDeviceAsync(
            new TenantRequestContext(TenantId, UserId, []),
            ValidRequest(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("device_context.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.ActivationCalls);
    }

    [Fact]
    public async Task ActivateDeviceAsync_WithTenantTillManage_ContinuesToBusinessValidation()
    {
        var repository = new FakeRepository
        {
            ActivationResult = new DeviceActivationRepositoryResult(
                false,
                "device_context.invalid_activation_code",
                "Activation code is invalid.",
                null),
        };
        var service = new DeviceContextService(repository, new FixedClock());

        var result = await service.ActivateDeviceAsync(
            new TenantRequestContext(TenantId, UserId, ["tenant.till.manage"]),
            ValidRequest(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("device_context.invalid_activation_code", result.Error.Code);
        Assert.Equal(1, repository.ActivationCalls);
    }

    [Fact]
    public async Task GetCurrentDeviceAsync_DoesNotRequireActivationPermission()
    {
        var snapshot = Snapshot();
        var repository = new FakeRepository { CurrentDevice = snapshot };
        var service = new DeviceContextService(repository, new FixedClock());

        var result = await service.GetCurrentDeviceAsync(
            new TenantRequestContext(TenantId, UserId, []),
            "trusted-fingerprint",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(snapshot.DeviceId, result.Value!.Device.Id);
        Assert.Equal("development", result.Value.TenantSlug);
    }

    private static ActivateDeviceRequest ValidRequest() => new(
        "TILL-ACTIVE-POS",
        "device-fingerprint",
        "Front POS Tablet",
        "fixed_pos_tablet",
        "web",
        "1.0.0");

    private static CurrentDeviceDbSnapshot Snapshot() => new(
        TenantId,
        "development",
        Guid.NewGuid(),
        "POS-01",
        "Front POS Tablet",
        "TABLET",
        "web",
        true,
        Guid.NewGuid(),
        "Development Main Store",
        Guid.NewGuid(),
        "TILL-01",
        "Front Till 01",
        0m,
        "LKR");

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } =
            new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeRepository : IDeviceContextRepository
    {
        public int ActivationCalls { get; private set; }
        public CurrentDeviceDbSnapshot? CurrentDevice { get; init; }
        public DeviceActivationRepositoryResult ActivationResult { get; init; } =
            new(false, "device_context.invalid_activation_code", "Activation code is invalid.", null);

        public Task<CurrentDeviceDbSnapshot?> ResolveCurrentDeviceAsync(
            Guid tenantId,
            string deviceFingerprint,
            CancellationToken cancellationToken) =>
            Task.FromResult(CurrentDevice);

        public Task<DeviceActivationRepositoryResult> ActivateDeviceAsync(
            Guid tenantId,
            Guid tenantUserId,
            DeviceActivationCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ActivationCalls++;
            return Task.FromResult(ActivationResult);
        }

        public Task<PosDevice?> GetEditableByFingerprintAsync(
            Guid tenantId,
            string deviceFingerprint,
            CancellationToken cancellationToken) => Task.FromResult<PosDevice?>(null);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
