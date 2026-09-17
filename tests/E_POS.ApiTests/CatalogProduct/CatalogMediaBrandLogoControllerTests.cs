using System.Reflection;
using System.Security.Claims;
using System.Text;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.CatalogProduct;

public sealed class CatalogMediaBrandLogoControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 29, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid BrandId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001");

    [Fact]
    public void CatalogMediaController_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(CatalogMediaController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public async Task UploadBrandLogo_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeCatalogMediaService(), new FakeBrandService());

        var result = await controller.UploadBrandLogo(BrandId, CreatePngFormFile(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task UploadBrandLogo_WhenSuccessful_ReturnsBrandContractEnvelope()
    {
        var brand = new BrandResponse(
            BrandId,
            "NIKE",
            "Nike",
            "https://cdn.example.test/nike.png",
            Guid.NewGuid(),
            BrandConstants.ActiveStatus,
            Now,
            Now);

        var mediaService = new FakeCatalogMediaService
        {
            UploadBrandLogoResult = ApplicationResult<MediaAssetUploadResponse>.Success(
                new MediaAssetUploadResponse(
                    Guid.NewGuid(),
                    null,
                    null,
                    null,
                    null,
                    BrandId,
                    "media",
                    "tenants/x/brands/y/logo/z.png",
                    "https://cdn.example.test/nike.png",
                    null,
                    "https://cdn.example.test/nike.png",
                    "nike.png",
                    "image/png",
                    ".png",
                    128,
                    1,
                    1,
                    "checksum"))
        };
        var brandService = new FakeBrandService
        {
            GetByIdResult = ApplicationResult<BrandResponse>.Success(brand)
        };
        var controller = CreateController(mediaService, brandService);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), BrandConstants.UpdatePermission);

        var result = await controller.UploadBrandLogo(BrandId, CreatePngFormFile(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var dataProperty = ok.Value!.GetType().GetProperty("data");
        Assert.NotNull(dataProperty);
        var payload = Assert.IsType<BrandResponse>(dataProperty!.GetValue(ok.Value));
        Assert.Equal(BrandId, payload.Id);
        Assert.Equal("NIKE", payload.BrandCode);
        Assert.Equal("https://cdn.example.test/nike.png", payload.LogoUrl);
        Assert.Equal(BrandId, mediaService.LastBrandId);
        Assert.NotNull(brandService.Context);
    }

    [Fact]
    public async Task UploadBrandLogo_WhenMissingFile_ReturnsBadRequest()
    {
        var controller = CreateController(new FakeCatalogMediaService(), new FakeBrandService());
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), BrandConstants.UpdatePermission);

        var result = await controller.UploadBrandLogo(BrandId, file: null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static CatalogMediaController CreateController(
        ICatalogMediaService mediaService,
        IBrandService brandService)
    {
        var controller = new CatalogMediaController(
            mediaService,
            brandService,
            new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetTenantClaims(ControllerBase controller, Guid tenantId, Guid userId, string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission)
            ],
            "Test"));
    }

    private static IFormFile CreatePngFormFile()
    {
        var bytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "brand.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    private sealed class FakeCatalogMediaService : ICatalogMediaService
    {
        public Guid? LastBrandId { get; private set; }

        public ApplicationResult<MediaAssetUploadResponse> UploadBrandLogoResult { get; init; } =
            ApplicationResult<MediaAssetUploadResponse>.Failure(
                new ApplicationError("media.not_configured", "Not configured."));

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadProductImageAsync(
            TenantRequestContext context,
            Guid productId,
            ProductImageUploadRequest request,
            MediaUploadFile file,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<StagedProductImageResponse>> StageProductImageAsync(
            TenantRequestContext context,
            MediaUploadFile file,
            Guid? trackingId,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<StagedProductImageResponse>> StageProductImageFromUrlAsync(
            TenantRequestContext context,
            string imageUrl,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<ProductImagesMutationResponse>> ReorderProductImagesAsync(
            TenantRequestContext context,
            Guid productId,
            ReorderProductImagesRequest request,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<ProductImagesMutationResponse>> DeleteProductImageAsync(
            TenantRequestContext context,
            Guid productId,
            Guid mediaAssetId,
            long? expectedVersion,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<ProductImagesMutationResponse>> ReplaceProductImagesAsync(
            TenantRequestContext context,
            Guid productId,
            long expectedVersion,
            IReadOnlyList<MediaUploadFile>? newFiles,
            IReadOnlyList<Guid>? existingAssetIds,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadCategoryImageAsync(
            TenantRequestContext context,
            Guid categoryId,
            MediaUploadFile file,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult> RemoveCategoryImageAsync(
            TenantRequestContext context,
            Guid categoryId,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadBrandLogoAsync(
            TenantRequestContext context,
            Guid brandId,
            MediaUploadFile file,
            CancellationToken cancellationToken)
        {
            LastBrandId = brandId;
            return Task.FromResult(UploadBrandLogoResult);
        }
    }

    private sealed class FakeBrandService : IBrandService
    {
        public TenantRequestContext? Context { get; private set; }

        public ApplicationResult<BrandResponse> GetByIdResult { get; init; } =
            ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.not_configured", "Not configured."));

        public Task<ApplicationResult<BrandResponse>> CreateAsync(
            TenantRequestContext context,
            BrandCreateRequest request,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<BrandListResponse>> ListAsync(
            TenantRequestContext context,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult<BrandResponse>> GetByIdAsync(
            TenantRequestContext context,
            Guid brandId,
            CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(GetByIdResult);
        }

        public Task<ApplicationResult<BrandResponse>> UpdateAsync(
            TenantRequestContext context,
            Guid brandId,
            BrandUpdateRequest request,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApplicationResult> DeleteAsync(
            TenantRequestContext context,
            Guid brandId,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
