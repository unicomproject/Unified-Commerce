using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.CatalogProduct;

public sealed class CatalogMediaCategoryControllerTests
{
    [Fact]
    public async Task RemoveCategoryImage_WhenSuccess_ReturnsNoContent()
    {
        var service = new FakeCatalogMediaService { RemoveResult = ApplicationResult.Success() };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), CategoryConstants.UpdatePermission);

        var result = await controller.RemoveCategoryImage(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveCategoryImage_WhenNotFound_Returns404()
    {
        var service = new FakeCatalogMediaService
        {
            RemoveResult = ApplicationResult.Failure(new ApplicationError("category.not_found", "Category was not found."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), CategoryConstants.UpdatePermission);

        var result = await controller.RemoveCategoryImage(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task RemoveCategoryImage_WhenEntitlementDenied_Returns403()
    {
        var service = new FakeCatalogMediaService
        {
            RemoveResult = ApplicationResult.Failure(new ApplicationError("category.entitlement_denied", "Product catalog feature is not included in the tenant subscription."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), CategoryConstants.UpdatePermission);

        var result = await controller.RemoveCategoryImage(Guid.NewGuid(), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task UploadCategoryImage_WhenSaveFailed_ReturnsSafe500()
    {
        var service = new FakeCatalogMediaService
        {
            UploadResult = ApplicationResult<MediaAssetUploadResponse>.Failure(
                new ApplicationError("media.save_failed", "Category image could not be saved."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), CategoryConstants.UpdatePermission);

        var result = await controller.UploadCategoryImage(Guid.NewGuid(), new FakeFormFile(), CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
        var json = System.Text.Json.JsonSerializer.Serialize(serverError.Value);
        Assert.Contains("media.save_failed", json);
        Assert.DoesNotContain("Exception", json);
        Assert.DoesNotContain("stack", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadCategoryImage_WhenUnexpectedFailure_Returns500()
    {
        var service = new FakeCatalogMediaService
        {
            UploadResult = ApplicationResult<MediaAssetUploadResponse>.Failure(
                new ApplicationError("media.unexpected_failure", "Category image storage failed."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), CategoryConstants.UpdatePermission);

        var result = await controller.UploadCategoryImage(Guid.NewGuid(), new FakeFormFile(), CancellationToken.None);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, serverError.StatusCode);
    }

    [Fact]
    public void CatalogMediaController_HasCategoryImageDeleteRoute()
    {
        var method = typeof(CatalogMediaController).GetMethod(nameof(CatalogMediaController.RemoveCategoryImage));
        Assert.NotNull(method);
        var httpDelete = Assert.Single(method!.GetCustomAttributes(typeof(HttpDeleteAttribute), inherit: true));
        Assert.Equal("categories/{categoryId:guid}/image", ((HttpDeleteAttribute)httpDelete).Template);
    }

    private static CatalogMediaController CreateController(FakeCatalogMediaService service)
    {
        var controller = new CatalogMediaController(service, new FakeBrandService(), new TenantRequestContextFactory());
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

    private sealed class FakeFormFile : IFormFile
    {
        public string ContentType => "image/png";
        public string ContentDisposition => "form-data; name=file; filename=category.png";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length => 32;
        public string Name => "file";
        public string FileName => "category.png";
        public void CopyTo(Stream target) => target.Write(new byte[32]);
        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            await target.WriteAsync(new byte[32], cancellationToken);
        }
        public Stream OpenReadStream() => new MemoryStream(new byte[32]);
    }

    private sealed class FakeCatalogMediaService : ICatalogMediaService
    {
        public ApplicationResult<MediaAssetUploadResponse> UploadResult { get; init; } =
            ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError("media.permission_denied", "denied"));

        public ApplicationResult RemoveResult { get; init; } =
            ApplicationResult.Failure(new ApplicationError("media.permission_denied", "denied"));

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadProductImageAsync(
            TenantRequestContext context,
            Guid productId,
            ProductImageUploadRequest request,
            MediaUploadFile file,
            CancellationToken cancellationToken) =>
            Task.FromResult(UploadResult);

        public Task<ApplicationResult<StagedProductImageResponse>> StageProductImageAsync(
            TenantRequestContext context,
            MediaUploadFile file,
            Guid? uploadSessionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<StagedProductImageResponse>.Failure(new ApplicationError("media.permission_denied", "denied")));

        public Task<ApplicationResult<ProductImagesMutationResponse>> ReorderProductImagesAsync(
            TenantRequestContext context,
            Guid productId,
            ReorderProductImagesRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError("media.permission_denied", "denied")));

        public Task<ApplicationResult<ProductImagesMutationResponse>> DeleteProductImageAsync(
            TenantRequestContext context,
            Guid productId,
            Guid productImageId,
            long? expectedRowVersion,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError("media.permission_denied", "denied")));

        public Task<ApplicationResult<ProductImagesMutationResponse>> ReplaceProductImagesAsync(
            TenantRequestContext context,
            Guid productId,
            long expectedRowVersion,
            IReadOnlyList<MediaUploadFile>? files,
            IReadOnlyList<Guid>? stagedMediaAssetIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ProductImagesMutationResponse>.Failure(new ApplicationError("media.permission_denied", "denied")));

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadCategoryImageAsync(
            TenantRequestContext context,
            Guid categoryId,
            MediaUploadFile file,
            CancellationToken cancellationToken) =>
            Task.FromResult(UploadResult);

        public Task<ApplicationResult> RemoveCategoryImageAsync(
            TenantRequestContext context,
            Guid categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(RemoveResult);

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadBrandLogoAsync(
            TenantRequestContext context,
            Guid brandId,
            MediaUploadFile file,
            CancellationToken cancellationToken) =>
            Task.FromResult(UploadResult);
    }

    private sealed class FakeBrandService : IBrandService
    {
        public Task<ApplicationResult<BrandResponse>> CreateAsync(TenantRequestContext context, BrandCreateRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.not_found", "unused")));

        public Task<ApplicationResult<BrandListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<BrandListResponse>.Failure(new ApplicationError("brand.not_found", "unused")));

        public Task<ApplicationResult<BrandResponse>> GetByIdAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.not_found", "unused")));

        public Task<ApplicationResult<BrandResponse>> UpdateAsync(TenantRequestContext context, Guid brandId, BrandUpdateRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.not_found", "unused")));

        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult.Failure(new ApplicationError("brand.not_found", "unused")));
    }
}
