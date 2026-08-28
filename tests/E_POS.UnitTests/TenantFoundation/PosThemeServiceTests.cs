using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Services;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Xunit;

namespace E_POS.UnitTests.TenantFoundation;

public sealed class PosThemeServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("31000000-0000-4000-8000-000000000001");

    [Fact]
    public async Task GetAsync_NoOverrides_ReturnsCanonicalDefaults()
    {
        var result = await new PosThemeService(new FakeRepository())
            .GetAsync(new(TenantId, Guid.NewGuid(), []), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("#FF6A00", result.Value!.PrimaryColor);
        Assert.Equal("#000000", result.Value.SecondaryColor);
    }

    [Fact]
    public async Task GetAsync_ValidOverrides_NormalizesHexColours()
    {
        var repository = new FakeRepository();
        repository.Settings[TenantSettingKeys.PosThemePrimaryColor] = "\"#a1b2c3\"";
        repository.Settings[TenantSettingKeys.PosThemeSecondaryColor] = "\"#112233\"";

        var result = await new PosThemeService(repository)
            .GetAsync(new(TenantId, Guid.NewGuid(), []), CancellationToken.None);

        Assert.Equal("#A1B2C3", result.Value!.PrimaryColor);
        Assert.Equal("#112233", result.Value.SecondaryColor);
    }

    [Fact]
    public async Task GetAsync_InvalidOverrides_FallsBackSafely()
    {
        var repository = new FakeRepository();
        repository.Settings[TenantSettingKeys.PosThemePrimaryColor] = "\"orange\"";
        repository.Settings[TenantSettingKeys.PosThemeSecondaryColor] = "null";

        var result = await new PosThemeService(repository)
            .GetAsync(new(TenantId, Guid.NewGuid(), []), CancellationToken.None);

        Assert.Equal(PosThemeService.DefaultPrimaryColor, result.Value!.PrimaryColor);
        Assert.Equal(PosThemeService.DefaultSecondaryColor, result.Value.SecondaryColor);
    }

    private sealed class FakeRepository : IPosLoginBrandingRepository
    {
        public Dictionary<string, string> Settings { get; } = new(StringComparer.Ordinal);

        public Task<PosLoginBrandingTenantSnapshot?> FindActiveTenantBySlugAsync(string tenantSlug, CancellationToken cancellationToken) =>
            Task.FromResult<PosLoginBrandingTenantSnapshot?>(Tenant());

        public Task<PosLoginBrandingTenantSnapshot?> FindTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<PosLoginBrandingTenantSnapshot?>(Tenant());

        public Task<IReadOnlyDictionary<string, string>> GetSettingValuesAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(Settings);

        public Task<IReadOnlyDictionary<string, string>> GetResolvedSettingValuesAsync(Guid tenantId, IReadOnlyCollection<string> settingKeys, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(Settings);

        public Task<PosLoginBrandingMediaSnapshot?> FindMediaAsync(Guid mediaAssetId, CancellationToken cancellationToken) =>
            Task.FromResult<PosLoginBrandingMediaSnapshot?>(null);

        public Task SaveSettingsAsync(Guid tenantId, IReadOnlyDictionary<string, string?> values, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        private static PosLoginBrandingTenantSnapshot Tenant() =>
            new(TenantId, "development", "Development", null, null, DateTimeOffset.UtcNow);
    }
}
