using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Services;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Moq;
using Xunit;

namespace E_POS.UnitTests.TenantFoundation;

public sealed class PosThemeServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly TenantRequestContext Context = new(TenantId, UserId, Array.Empty<string>());

    private readonly Mock<IPosLoginBrandingRepository> _repository = new(MockBehavior.Strict);

    [Fact]
    public async Task GetAsync_WhenTenantNotFound_ReturnsFailure()
    {
        _repository
            .Setup(r => r.FindTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PosLoginBrandingTenantSnapshot?)null);

        var service = new PosThemeService(_repository.Object);
        var result = await service.GetAsync(Context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_theme.tenant_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task GetAsync_WhenSettingsMissing_ReturnsProjectDefaults()
    {
        var tenant = new PosLoginBrandingTenantSnapshot(
            TenantId, "acme", "Acme", null, null, DateTimeOffset.UtcNow);

        _repository
            .Setup(r => r.FindTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _repository
            .Setup(r => r.GetSettingValuesAsync(
                TenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var service = new PosThemeService(_repository.Object);
        var result = await service.GetAsync(Context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("#FF6A00", result.Value.PrimaryColor);
        Assert.Equal("#000000", result.Value.SecondaryColor);
    }

    [Fact]
    public async Task GetAsync_WhenValidSettings_ReturnsConfiguredColors()
    {
        var tenant = new PosLoginBrandingTenantSnapshot(
            TenantId, "acme", "Acme", null, null, DateTimeOffset.UtcNow);

        _repository
            .Setup(r => r.FindTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _repository
            .Setup(r => r.GetSettingValuesAsync(
                TenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                [TenantSettingKeys.PosThemePrimaryColor] = "\"#112233\"",
                [TenantSettingKeys.PosThemeSecondaryColor] = "\"#445566\""
            });

        var service = new PosThemeService(_repository.Object);
        var result = await service.GetAsync(Context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("#112233", result.Value.PrimaryColor);
        Assert.Equal("#445566", result.Value.SecondaryColor);
    }
}
