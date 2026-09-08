using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using E_POS.Application.Modules.ECommerce.Storefront.Services;
using Xunit;

namespace E_POS.UnitTests.ECommerce.Storefront;

public sealed class StorefrontBrandingServiceTests
{
    [Fact]
    public async Task GetBrandingAsync_ReturnsTenantBrandingFromRepository()
    {
        var tenantId = Guid.NewGuid();
        var expected = new StorefrontBrandingReadModel
        {
            TenantId = tenantId,
            StoreName = "Arena Store",
            LogoImageUrl = "/uploads/logo.png",
            FaviconImageUrl = "/uploads/favicon.png",
            PrimaryColor = "#FF6A00",
            SecondaryColor = "#0D1B3D"
        };
        var repository = new FakeBrandingRepository { Branding = expected };
        var service = new StorefrontBrandingService(repository);

        var result = await service.GetBrandingAsync(tenantId, CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(tenantId, repository.RequestedTenantId);
    }

    private sealed class FakeBrandingRepository : IStorefrontBrandingRepository
    {
        public StorefrontBrandingReadModel? Branding { get; init; }
        public Guid? RequestedTenantId { get; private set; }

        public Task<StorefrontBrandingReadModel?> GetBrandingAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            RequestedTenantId = tenantId;
            return Task.FromResult(Branding);
        }
    }
}
