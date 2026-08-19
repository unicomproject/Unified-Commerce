using System.Reflection;
using E_POS.Api.Controllers.V1.Tenant.ECommerce;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace E_POS.ApiTests.Tenant;

public sealed class TenantAdminOnlineStoreControllerSurfaceTests
{
    [Fact]
    public void Controller_UsesCanonicalTenantAdminOnlineStoreRoute()
    {
        var route = typeof(TenantAdminOnlineStoreController)
            .GetCustomAttribute<RouteAttribute>()?
            .Template;

        Assert.Equal("api/v1/tenant-admin/online-store", route);
    }

    [Theory]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetOverview), "overview")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetReadiness), "readiness")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetActivation), "activation")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateActivation), "activation")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetIdentity), "identity")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateIdentity), "identity")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetUrlDomain), "url-domain")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateUrl), "url")]
    [InlineData(nameof(TenantAdminOnlineStoreController.ListDomains), "domains")]
    [InlineData(nameof(TenantAdminOnlineStoreController.CreateDomain), "domains")]
    [InlineData(nameof(TenantAdminOnlineStoreController.VerifyDomain), "domains/{id:guid}/verify")]
    [InlineData(nameof(TenantAdminOnlineStoreController.RotateDomainToken), "domains/{id:guid}/verification-token/rotate")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetDomainStatus), "domains/{id:guid}/status")]
    [InlineData(nameof(TenantAdminOnlineStoreController.ProvisionSsl), "domains/{id:guid}/ssl/provision")]
    [InlineData(nameof(TenantAdminOnlineStoreController.SetPrimaryDomain), "domains/{id:guid}/set-primary")]
    [InlineData(nameof(TenantAdminOnlineStoreController.DeleteDomain), "domains/{id:guid}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetBranding), "branding")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateBranding), "branding")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UploadMedia), "media/{purpose}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.DeleteMedia), "media/{id:guid}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.ListBanners), "banners")]
    [InlineData(nameof(TenantAdminOnlineStoreController.CreateBanner), "banners")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetBanner), "banners/{id:guid}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateBanner), "banners/{id:guid}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateBannerStatus), "banners/{id:guid}/status")]
    [InlineData(nameof(TenantAdminOnlineStoreController.ReorderBanners), "banners/order")]
    [InlineData(nameof(TenantAdminOnlineStoreController.DeleteBanner), "banners/{id:guid}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetSupport), "support")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateSupport), "support")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetClickCollect), "click-collect")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateClickCollect), "click-collect")]
    [InlineData(nameof(TenantAdminOnlineStoreController.ListClickCollectOutlets), "click-collect/outlets")]
    [InlineData(nameof(TenantAdminOnlineStoreController.AddClickCollectOutlet), "click-collect/outlets")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpsertClickCollectOutlet), "click-collect/outlets/{outletId:guid}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.DeleteClickCollectOutlet), "click-collect/outlets/{outletId:guid}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.BulkApplyClickCollect), "click-collect/outlets/bulk-apply")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetCatalogSummary), "catalog/summary")]
    [InlineData(nameof(TenantAdminOnlineStoreController.ListCatalogProducts), "catalog/products")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateProductVisibility), "catalog/products/{id:guid}/visibility")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpdateVariantVisibility), "catalog/products/{id:guid}/variants/{variantId:guid}/visibility")]
    [InlineData(nameof(TenantAdminOnlineStoreController.BulkVisibility), "catalog/products/bulk-visibility")]
    [InlineData(nameof(TenantAdminOnlineStoreController.ListPolicies), "policies")]
    [InlineData(nameof(TenantAdminOnlineStoreController.GetPolicy), "policies/{type}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.UpsertPolicy), "policies/{type}")]
    [InlineData(nameof(TenantAdminOnlineStoreController.PublishPolicy), "policies/{type}/publish")]
    [InlineData(nameof(TenantAdminOnlineStoreController.PolicyVersions), "policies/{type}/versions")]
    [InlineData(nameof(TenantAdminOnlineStoreController.ArchivePolicy), "policies/{type}/archive")]
    [InlineData(nameof(TenantAdminOnlineStoreController.Publish), "publish")]
    public void Controller_ExposesExpectedScreenEndpointRoute(string methodName, string expectedTemplate)
    {
        var method = typeof(TenantAdminOnlineStoreController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(methodInfo => methodInfo.Name == methodName);

        var template = method
            .GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single()
            .Template;

        Assert.Equal(expectedTemplate, template);
    }

    [Fact]
    public void Publish_ReadsIdempotencyKeyHeader()
    {
        var source = File.ReadAllText(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "E_POS.Api",
                "Controllers",
                "V1",
                "Tenant",
                "ECommerce",
                "TenantAdminOnlineStoreController.cs"));

        Assert.Contains("Request.Headers[\"Idempotency-Key\"]", source);
        Assert.Contains("PublishAsync(context, idempotencyKey", source);
    }

    [Fact]
    public void Readiness_TreatsHostedUrlAsSufficientWhenNoCustomPrimaryDomainExists()
    {
        var source = ReadOnlineStoreServiceSource();

        Assert.Contains("var storeSlugConfigured = !string.IsNullOrWhiteSpace(ReadString(state.Settings, \"storeSlug\"));", source);
        Assert.Contains("primaryCustomDomain is null ||", source);
        Assert.Contains("primaryCustomDomain is null", source);
        Assert.DoesNotContain("domains.Any(x => x.DomainType == \"CUSTOM\"", source);
    }

    [Fact]
    public void Readiness_BlocksOnlyPrimaryCustomDomainUntilVerifiedWithActiveSsl()
    {
        var source = ReadOnlineStoreServiceSource();

        Assert.Contains("x.IsPrimary", source);
        Assert.Contains("string.Equals(x.DomainType, \"CUSTOM\", StringComparison.OrdinalIgnoreCase)", source);
        Assert.Contains("primaryCustomDomain.VerificationStatus == \"VERIFIED\" && primaryCustomDomain.SslStatus == \"ACTIVE\"", source);
        Assert.Contains("Primary custom domain verification or SSL is incomplete.", source);
    }

    private static string ReadOnlineStoreServiceSource() => File.ReadAllText(
        Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "E_POS.Infrastructure",
            "Modules",
            "Tenant",
            "OnlineStoreSetup",
            "Services",
            "TenantAdminOnlineStoreService.cs"));
}
