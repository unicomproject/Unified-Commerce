using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Infrastructure.Modules.Shared.Media.Options;
using E_POS.Infrastructure.Modules.Shared.Media.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.Media;

public sealed class AzureBlobMediaObjectStorageTests
{
    [Fact]
    public async Task UploadAsync_UsesLocalStorage_WhenDevelopmentFallbackIsEnabled()
    {
        var container = $"test-images-{Guid.NewGuid():N}";
        var storageKey = $"outlets/{Guid.NewGuid():N}.png";
        var storage = new AzureBlobMediaObjectStorage(
            Options.Create(new AzureBlobStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = container,
                AllowLocalFallback = true
            }));

        await using var content = new MemoryStream([1, 2, 3, 4]);
        var result = await storage.UploadAsync(
            new MediaObjectUploadRequest(
                storageKey,
                content,
                "image/png",
                new Dictionary<string, string>()),
            CancellationToken.None);

        Assert.True(storage.IsConfigured);
        Assert.Equal(container, result.ContainerName);
        Assert.Equal(storageKey, result.StorageKey);
        Assert.Equal($"/uploads/{container}/{storageKey}", result.PublicUrl);

        var localPath = Path.Combine(
            AppContext.BaseDirectory,
            "App_Data",
            "media-storage",
            container,
            storageKey.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(localPath));

        await storage.DeleteIfExistsAsync(container, storageKey, CancellationToken.None);
        Assert.False(File.Exists(localPath));
    }
}
