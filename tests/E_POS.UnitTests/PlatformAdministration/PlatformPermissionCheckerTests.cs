using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Services;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformPermissionCheckerTests
{
    [Fact]
    public async Task HasPermissionAsync_WhenUserHasPermission_ReturnsTrue()
    {
        var repository = new FakePlatformPermissionRepository(new HashSet<string>(StringComparer.Ordinal)
        {
            PlatformPermissionCodes.TenantsView
        });
        var checker = new PlatformPermissionChecker(repository);

        var result = await checker.HasPermissionAsync(
            Guid.NewGuid(),
            PlatformPermissionCodes.TenantsView,
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserDoesNotHavePermission_ReturnsFalse()
    {
        var repository = new FakePlatformPermissionRepository(new HashSet<string>(StringComparer.Ordinal)
        {
            PlatformPermissionCodes.TenantsView
        });
        var checker = new PlatformPermissionChecker(repository);

        var result = await checker.HasPermissionAsync(
            Guid.NewGuid(),
            PlatformPermissionCodes.TenantsCreate,
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HasPermissionAsync_WithBlankPermissionCode_ReturnsFalse()
    {
        var repository = new FakePlatformPermissionRepository(new HashSet<string>(StringComparer.Ordinal)
        {
            PlatformPermissionCodes.TenantsView
        });
        var checker = new PlatformPermissionChecker(repository);

        var result = await checker.HasPermissionAsync(Guid.NewGuid(), "   ", CancellationToken.None);

        Assert.False(result);
        Assert.Equal(Guid.Empty, repository.LastPlatformUserId);
    }

    private sealed class FakePlatformPermissionRepository : IPlatformPermissionRepository
    {
        private readonly IReadOnlySet<string> _permissionCodes;

        public FakePlatformPermissionRepository(IReadOnlySet<string> permissionCodes)
        {
            _permissionCodes = permissionCodes;
        }

        public Guid LastPlatformUserId { get; private set; }

        public Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            LastPlatformUserId = platformUserId;
            return Task.FromResult(_permissionCodes);
        }
    }
}
