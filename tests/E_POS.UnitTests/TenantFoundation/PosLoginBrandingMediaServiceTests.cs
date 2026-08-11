using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Services;
using E_POS.Domain.Modules.Shared.Media.Constants;
using E_POS.Domain.Modules.Shared.Media.Entities;
using Xunit;

namespace E_POS.UnitTests.TenantFoundation;

public sealed class PosLoginBrandingMediaServiceTests
{
    [Fact]
    public async Task UploadAsync_ValidPng_PersistsTenantScopedPurpose()
    {
        var repository = new Repository();
        var storage = new Storage();
        var service = new PosLoginBrandingMediaService(repository, storage, new Clock());
        var bytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        var result = await service.UploadAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [PosLoginBrandingService.ManagePermission]),
            MediaAssetPurposes.PosLoginHero,
            new MediaUploadFile(new MemoryStream(bytes), "hero.png", "image/png", bytes.Length),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.Asset);
        Assert.Equal(MediaAssetPurposes.PosLoginHero, repository.Asset!.AssetPurpose);
        Assert.Equal("IMAGE", repository.Asset.AssetType);
        Assert.True(repository.Saved);
    }

    [Fact]
    public async Task UploadAsync_RejectsUnsupportedPurposeBeforeStorage()
    {
        var storage = new Storage();
        var service = new PosLoginBrandingMediaService(new Repository(), storage, new Clock());

        var result = await service.UploadAsync(
            new TenantRequestContext(Guid.NewGuid(), Guid.NewGuid(), [PosLoginBrandingService.ManagePermission]),
            "TENANT_LOGO",
            new MediaUploadFile(new MemoryStream([1]), "logo.png", "image/png", 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("pos_login_branding.media_invalid", result.Error.Code);
        Assert.False(storage.Uploaded);
    }

    private sealed class Repository : IPosLoginBrandingMediaRepository
    {
        public MediaAsset? Asset { get; private set; }
        public bool Saved { get; private set; }
        public Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
        {
            Asset = mediaAsset;
            return Task.CompletedTask;
        }
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class Storage : IMediaObjectStorage
    {
        public bool IsConfigured => true;
        public bool Uploaded { get; private set; }
        public Task<MediaObjectUploadResult> UploadAsync(MediaObjectUploadRequest request, CancellationToken cancellationToken)
        {
            Uploaded = true;
            return Task.FromResult(new MediaObjectUploadResult("media", request.StorageKey, "https://cdn.example.test/branding.png"));
        }
        public Task DeleteIfExistsAsync(string containerName, string storageKey, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    }
}
