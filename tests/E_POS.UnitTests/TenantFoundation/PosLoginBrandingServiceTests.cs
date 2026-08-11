using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Services;
using E_POS.Domain.Modules.Shared.Media.Constants;
using Xunit;

namespace E_POS.UnitTests.TenantFoundation;

public sealed class PosLoginBrandingServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("31000000-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset UpdatedAt = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetPublicAsync_NoOverrides_UsesSafeDefaultsAndTenantName()
    {
        var repository = new FakeRepository { Tenant = Tenant("Acme Retail") };
        var result = await new PosLoginBrandingService(repository)
            .GetPublicAsync("acme", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Retail", result.Value!.BrandDisplayName);
        Assert.Equal("Smart Cashier System", result.Value.SystemName);
        Assert.Equal("Sign in to continue to Acme Retail", result.Value.LoginSubtitle);
        Assert.Equal("COLOR", result.Value.BackgroundMode);
        Assert.Null(result.Value.BackgroundImageUrl);
    }

    [Fact]
    public async Task UpdateAdminAsync_CrossTenantBackground_IsRejectedWithoutSaving()
    {
        var mediaId = Guid.NewGuid();
        var repository = new FakeRepository
        {
            Tenant = Tenant(),
            Media = ValidMedia(mediaId, Guid.NewGuid(), MediaAssetPurposes.PosLoginBackground)
        };
        var request = new UpdatePosLoginBrandingRequest(null, null, null, "IMAGE", "#112233", mediaId, null);

        var result = await new PosLoginBrandingService(repository).UpdateAdminAsync(
            new TenantRequestContext(TenantId, Guid.NewGuid(), [PosLoginBrandingService.ManagePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_login_branding.background_media_invalid", result.Error.Code);
        Assert.False(repository.SaveCalled);
    }

    [Fact]
    public async Task UpdateAdminAsync_ValidImageSettings_AreSavedAndResolved()
    {
        var backgroundId = Guid.NewGuid();
        var heroId = Guid.NewGuid();
        var repository = new FakeRepository
        {
            Tenant = Tenant(),
            MediaById =
            {
                [backgroundId] = ValidMedia(backgroundId, TenantId, MediaAssetPurposes.PosLoginBackground),
                [heroId] = ValidMedia(heroId, TenantId, MediaAssetPurposes.PosLoginHero)
            }
        };

        var result = await new PosLoginBrandingService(repository).UpdateAdminAsync(
            new TenantRequestContext(TenantId, Guid.NewGuid(), [PosLoginBrandingService.ManagePermission]),
            new UpdatePosLoginBrandingRequest("Till Pro", "Fast checkout", "Welcome to {tenantName}", "IMAGE", "#AABBCC", backgroundId, heroId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.SaveCalled);
        Assert.Equal("IMAGE", result.Value!.Effective.BackgroundMode);
        Assert.Equal("https://cdn.example.test/image.png", result.Value.Effective.BackgroundImageUrl);
        Assert.Equal("Welcome to Acme", result.Value.Effective.LoginSubtitle);
    }

    [Fact]
    public async Task UpdateAdminAsync_UnsupportedSubtitlePlaceholder_IsRejected()
    {
        var repository = new FakeRepository { Tenant = Tenant() };
        var result = await new PosLoginBrandingService(repository).UpdateAdminAsync(
            new TenantRequestContext(TenantId, Guid.NewGuid(), [PosLoginBrandingService.ManagePermission]),
            new UpdatePosLoginBrandingRequest(null, null, "Welcome {tenantId}", "COLOR", "#020B1F", null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("pos_login_branding.subtitle_invalid", result.Error.Code);
        Assert.False(repository.SaveCalled);
    }

    private static PosLoginBrandingTenantSnapshot Tenant(string displayName = "Acme") =>
        new(TenantId, "acme", displayName, null, null, UpdatedAt);

    private static PosLoginBrandingMediaSnapshot ValidMedia(Guid id, Guid tenantId, string purpose) =>
        new(id, tenantId, "https://cdn.example.test/image.png", "image/png", ".png", 1024, "IMAGE", purpose, "ACTIVE", UpdatedAt);

    private sealed class FakeRepository : IPosLoginBrandingRepository
    {
        public PosLoginBrandingTenantSnapshot? Tenant { get; init; }
        public PosLoginBrandingMediaSnapshot? Media { get; init; }
        public Dictionary<Guid, PosLoginBrandingMediaSnapshot> MediaById { get; } = [];
        public Dictionary<string, string> Settings { get; } = new(StringComparer.Ordinal);
        public bool SaveCalled { get; private set; }

        public Task<PosLoginBrandingTenantSnapshot?> FindActiveTenantBySlugAsync(string tenantSlug, CancellationToken cancellationToken) => Task.FromResult(Tenant);
        public Task<PosLoginBrandingTenantSnapshot?> FindTenantAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult(Tenant);
        public Task<IReadOnlyDictionary<string, string>> GetSettingValuesAsync(Guid tenantId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<string, string>>(Settings);
        public Task<PosLoginBrandingMediaSnapshot?> FindMediaAsync(Guid mediaAssetId, CancellationToken cancellationToken) =>
            Task.FromResult(MediaById.GetValueOrDefault(mediaAssetId) ?? Media);

        public Task SaveSettingsAsync(Guid tenantId, IReadOnlyDictionary<string, string?> values, DateTimeOffset now, CancellationToken cancellationToken)
        {
            SaveCalled = true;
            foreach (var pair in values)
            {
                if (pair.Value is null) Settings.Remove(pair.Key);
                else Settings[pair.Key] = pair.Value;
            }
            return Task.CompletedTask;
        }
    }
}
