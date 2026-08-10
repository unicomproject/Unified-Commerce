using E_POS.Infrastructure.Modules.Shared.Media.Options;
using E_POS.Infrastructure.Modules.Shared.Storage.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.Media;

public sealed class AzureBlobSasTokenProviderTests
{
    [Fact]
    public void AppendReadSasToken_ReturnsOriginalUrl_WhenConnectionStringIsMissing()
    {
        var provider = new AzureBlobSasTokenProvider(Options.Create(new AzureBlobStorageOptions()));
        const string blobUrl = "https://oneverzdevstorage01.blob.core.windows.net/images/banner.png";

        var result = provider.AppendReadSasToken(blobUrl);

        Assert.Equal(blobUrl, result);
    }

    [Fact]
    public void AppendReadSasToken_ReturnsTrimmedOriginalUrl_WhenUrlIsNotAbsolute()
    {
        var provider = new AzureBlobSasTokenProvider(Options.Create(new AzureBlobStorageOptions()));

        var result = provider.AppendReadSasToken(" /images/banner.png ");

        Assert.Equal("/images/banner.png", result);
    }
}
