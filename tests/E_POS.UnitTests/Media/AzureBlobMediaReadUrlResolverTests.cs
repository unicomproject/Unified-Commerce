using E_POS.Application.Modules.Shared.Storage.Contracts;
using E_POS.Infrastructure.Modules.Shared.Media.Options;
using E_POS.Infrastructure.Modules.Shared.Media.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.Media;

public sealed class AzureBlobMediaReadUrlResolverTests
{
    [Fact]
    public void ResolveReadUrl_ReturnsNull_WhenNoPublicUrlOrStorageKeyExists()
    {
        var sasTokenProvider = new RecordingSasTokenProvider();
        var resolver = CreateResolver(sasTokenProvider);

        var result = resolver.ResolveReadUrl("images", null, null);

        Assert.Null(result);
        Assert.Empty(sasTokenProvider.Inputs);
    }

    [Fact]
    public void ResolveReadUrl_UsesExistingPublicUrlBeforeStorageKey()
    {
        var sasTokenProvider = new RecordingSasTokenProvider();
        var resolver = CreateResolver(sasTokenProvider);

        var result = resolver.ResolveReadUrl(
            "images",
            "products/ignored.png",
            "https://oneverzdevstorage01.blob.core.windows.net/images/products/current.png");

        Assert.Equal("signed:https://oneverzdevstorage01.blob.core.windows.net/images/products/current.png", result);
        Assert.Equal("https://oneverzdevstorage01.blob.core.windows.net/images/products/current.png", Assert.Single(sasTokenProvider.Inputs));
    }

    [Fact]
    public void ResolveReadUrl_BuildsBlobUrlWithContainerFromStorageKey()
    {
        var sasTokenProvider = new RecordingSasTokenProvider();
        var resolver = CreateResolver(sasTokenProvider);

        var result = resolver.ResolveReadUrl("images", "storefront/banner.png", null);

        Assert.Equal("signed:https://oneverzdevstorage01.blob.core.windows.net/images/storefront/banner.png", result);
        Assert.Equal("https://oneverzdevstorage01.blob.core.windows.net/images/storefront/banner.png", Assert.Single(sasTokenProvider.Inputs));
    }

    [Fact]
    public void ResolveReadUrl_DoesNotDuplicateContainerWhenStorageKeyAlreadyContainsContainer()
    {
        var sasTokenProvider = new RecordingSasTokenProvider();
        var resolver = CreateResolver(sasTokenProvider);

        var result = resolver.ResolveReadUrl("images", "images/storefront/banner.png", null);

        Assert.Equal("signed:https://oneverzdevstorage01.blob.core.windows.net/images/storefront/banner.png", result);
        Assert.Equal("https://oneverzdevstorage01.blob.core.windows.net/images/storefront/banner.png", Assert.Single(sasTokenProvider.Inputs));
    }

    private static AzureBlobMediaReadUrlResolver CreateResolver(RecordingSasTokenProvider sasTokenProvider)
    {
        return new AzureBlobMediaReadUrlResolver(
            sasTokenProvider,
            Options.Create(new AzureBlobStorageOptions
            {
                PublicBaseUrl = "https://oneverzdevstorage01.blob.core.windows.net/"
            }));
    }

    private sealed class RecordingSasTokenProvider : IAzureSasTokenProvider
    {
        public List<string> Inputs { get; } = [];

        public string AppendReadSasToken(string blobUrl)
        {
            Inputs.Add(blobUrl);
            return $"signed:{blobUrl}";
        }
    }
}
