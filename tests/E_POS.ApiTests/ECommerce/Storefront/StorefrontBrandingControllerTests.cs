using E_POS.Api.Controllers.V1.ECommerce.Storefront;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Application.Modules.ECommerce.Storefront.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.ECommerce.Storefront;

public sealed class StorefrontBrandingControllerTests
{
    [Fact]
    public async Task GetBranding_WithMissingTenantHeader_ReturnsBadRequest()
    {
        var service = new FakeBrandingService();
        var controller = new StorefrontBrandingController(service);

        var result = await controller.GetBranding(Guid.Empty, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(service.RequestedTenantId);
    }

    [Fact]
    public async Task GetBranding_WhenTenantIsUnavailable_ReturnsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeBrandingService();
        var controller = new StorefrontBrandingController(service);

        var result = await controller.GetBranding(tenantId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(tenantId, service.RequestedTenantId);
    }

    [Fact]
    public async Task GetBranding_WithActiveTenant_ReturnsPublicBrandingContract()
    {
        var tenantId = Guid.NewGuid();
        var branding = new StorefrontBrandingReadModel
        {
            TenantId = tenantId,
            StoreName = "Arena Store",
            StoreDescription = "Performance apparel",
            LogoImageUrl = "/uploads/logo.png",
            FaviconImageUrl = "/uploads/favicon.png",
            PrimaryColor = "#FF6A00",
            SecondaryColor = "#0D1B3D"
        };
        var service = new FakeBrandingService { Branding = branding };
        var controller = new StorefrontBrandingController(service);

        var result = await controller.GetBranding(tenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(branding, ok.Value);
        Assert.Equal(tenantId, service.RequestedTenantId);
    }

    private sealed class FakeBrandingService : IStorefrontBrandingService
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
