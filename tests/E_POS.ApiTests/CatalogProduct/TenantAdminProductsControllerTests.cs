using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Api.Controllers.V1.Tenant.CatalogProduct;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.CatalogProduct;

public sealed class TenantAdminProductsControllerTests
{
    [Fact]
    public async Task UploadBrandLogo_WithCreatePermission_PassesCreationContextAndReturnsBrand()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var brand = new BrandResponse(brandId, "ACME", "Acme", "https://cdn/brand.png", Guid.NewGuid(), "ACTIVE", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var mediaService = new FakeCatalogMediaService
        {
            BrandLogoResult = ApplicationResult<MediaAssetUploadResponse>.Success(
                new MediaAssetUploadResponse(Guid.NewGuid(), null, null, null, null, brandId, "media", "key", brand.LogoUrl!, null, brand.LogoUrl!, "brand.png", "image/png", ".png", 68, 1, 1, "hash")),
        };
        var brandService = new FakeBrandService
        {
            DetailResult = ApplicationResult<BrandResponse>.Success(brand),
        };
        var controller = new CatalogMediaController(mediaService, brandService, new TenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        SetTenantClaims(controller, tenantId, userId, BrandConstants.CreatePermission);
        await using var stream = new MemoryStream(CreateOnePixelPng());
        var file = new FormFile(stream, 0, stream.Length, "file", "brand.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };

        var result = await controller.UploadBrandLogo(brandId, file, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, mediaService.BrandLogoContext?.TenantId);
        Assert.Equal(userId, mediaService.BrandLogoContext?.UserId);
        Assert.Contains(BrandConstants.CreatePermission, mediaService.BrandLogoContext!.Permissions);
        Assert.Equal(brandId, mediaService.BrandLogoId);
    }

    [Fact]
    public async Task UploadBrandLogo_WhenInitialCompletionUnauthorized_ReturnsForbiddenStableCode()
    {
        var mediaService = new FakeCatalogMediaService
        {
            BrandLogoResult = ApplicationResult<MediaAssetUploadResponse>.Failure(
                new ApplicationError("media.initial_brand_logo_not_authorized", "Not authorized.")),
        };
        var controller = new CatalogMediaController(mediaService, new FakeBrandService(), new TenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), BrandConstants.CreatePermission);
        await using var stream = new MemoryStream(CreateOnePixelPng());
        var file = new FormFile(stream, 0, stream.Length, "file", "brand.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };

        var result = await controller.UploadBrandLogo(Guid.NewGuid(), file, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Contains("media.initial_brand_logo_not_authorized", forbidden.Value!.ToString());
    }

    private static byte[] CreateOnePixelPng() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
    [Fact]
    public async Task GetSummary_WithTenantProductsView_ReturnsOk()
    {
        var summary = new TenantAdminProductSummaryCardsResponse(5, 4, 1, 2);
        var service = new FakeTenantAdminProductService
        {
            SummaryResult = ApplicationResult<TenantAdminProductSummaryCardsResponse>.Success(summary),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetSummary(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetSummary_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.GetSummary(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetSummary_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            SummaryResult = ApplicationResult<TenantAdminProductSummaryCardsResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.GetSummary(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetCreateOptions_WithTenantProductsCreate_ReturnsOk()
    {
        var options = new TenantAdminProductCreateOptionsResponse(
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var service = new FakeTenantAdminProductService
        {
            CreateOptionsResult = ApplicationResult<TenantAdminProductCreateOptionsResponse>.Success(options),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.GetCreateOptions(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetCreateOptions_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.GetCreateOptions(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetCreateOptions_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            CreateOptionsResult = ApplicationResult<TenantAdminProductCreateOptionsResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetCreateOptions(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public void ResolveBarcode_Route_IsPostBarcodesResolve()
    {
        var method = typeof(TenantAdminProductsController).GetMethod(
            nameof(TenantAdminProductsController.ResolveBarcode));
        Assert.NotNull(method);
        var httpPost = Assert.Single(method!.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal("barcodes/resolve", httpPost.Template);

        var route = Assert.Single(typeof(TenantAdminProductsController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/v1/tenant-admin/products", route.Template);
    }

    [Fact]
    public async Task ResolveBarcode_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.ResolveBarcode(
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "SCAN" },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.LastResolveRequest);
    }

    [Fact]
    public async Task ResolveBarcode_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            ResolveBarcodeResult = ApplicationResult<ResolveProductBarcodeResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.view");

        var result = await controller.ResolveBarcode(
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "SCAN" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task ResolveBarcode_WithEntitlementDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            ResolveBarcodeResult = ApplicationResult<ResolveProductBarcodeResponse>.Failure(
                new ApplicationError("product.entitlement_denied", "Product management feature is not included.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ResolveBarcode(
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "SCAN" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task ResolveBarcode_InvalidBusinessOutcome_ReturnsOkEnvelope()
    {
        var service = new FakeTenantAdminProductService
        {
            ResolveBarcodeResult = ApplicationResult<ResolveProductBarcodeResponse>.Success(
                new ResolveProductBarcodeResponse(
                    "INVALID",
                    null,
                    null,
                    "UNKNOWN",
                    "CHECKSUM_FAILED",
                    null)),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ResolveBarcode(
            new ResolveProductBarcodeRequest { Barcode = "4006381333930", InputMode = "MANUAL" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ResolveProductBarcodeResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));
        Assert.Equal("INVALID", body.Outcome);
        Assert.Equal("CHECKSUM_FAILED", body.InvalidReason);
        Assert.NotNull(service.LastResolveRequest);
        Assert.Equal("MANUAL", service.LastResolveRequest!.InputMode);
    }

    [Fact]
    public async Task ResolveBarcode_ValidNoLocalMatch_ReturnsOk()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ResolveBarcode(
            new ResolveProductBarcodeRequest
            {
                Barcode = "4006381333931",
                InputMode = "SCAN",
                ReportedSymbology = "EAN13",
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ResolveProductBarcodeResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));
        Assert.Equal("VALID_NO_LOCAL_MATCH", body.Outcome);
        Assert.Equal("4006381333931", service.LastResolveRequest!.Barcode);
        Assert.Equal("EAN13", service.LastResolveRequest.ReportedSymbology);
    }

    [Fact]
    public async Task ResolveBarcode_ValidLocalMatch_ReturnsOkWithProjection()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            ResolveBarcodeResult = ApplicationResult<ResolveProductBarcodeResponse>.Success(
                new ResolveProductBarcodeResponse(
                    "VALID_LOCAL_MATCH",
                    "012345678905",
                    "GTIN12",
                    "UPCA",
                    null,
                    new ResolveProductBarcodeLocalMatchDto(
                        "PRODUCT",
                        productId,
                        null,
                        "Existing Product",
                        null,
                        "Brand",
                        "Category",
                        "SKU-1",
                        null,
                        null,
                        "ACTIVE",
                        null,
                        true,
                        false))),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ResolveBarcode(
            new ResolveProductBarcodeRequest { Barcode = "012345678905", InputMode = "SCAN" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ResolveProductBarcodeResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));
        Assert.Equal("VALID_LOCAL_MATCH", body.Outcome);
        Assert.Equal(productId, body.LocalMatch!.ProductId);
        Assert.True(body.LocalMatch.CanViewProduct);
        Assert.False(body.LocalMatch.CanEditProduct);
    }

    [Fact]
    public void GenerateSkuCandidate_Route_IsPostSkuCandidatesGenerate()
    {
        var method = typeof(TenantAdminProductsController).GetMethod(
            nameof(TenantAdminProductsController.GenerateSkuCandidate));
        Assert.NotNull(method);
        var httpPost = Assert.Single(method!.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal("sku-candidates/generate", httpPost.Template);
    }

    [Fact]
    public async Task GenerateSkuCandidate_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.GenerateSkuCandidate(
            new GenerateSkuCandidateRequest { Purpose = "NO_BARCODE_PRODUCT" },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.LastGenerateSkuRequest);
    }

    [Fact]
    public async Task GenerateSkuCandidate_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            GenerateSkuCandidateResult = ApplicationResult<GenerateSkuCandidateResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.view");

        var result = await controller.GenerateSkuCandidate(
            new GenerateSkuCandidateRequest { Purpose = "NO_BARCODE_PRODUCT" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GenerateSkuCandidate_ValidRequest_ReturnsOkEnvelope()
    {
        var service = new FakeTenantAdminProductService
        {
            GenerateSkuCandidateResult = ApplicationResult<GenerateSkuCandidateResponse>.Success(
                new GenerateSkuCandidateResponse("TSH-000125", true)),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");
        var categoryId = Guid.NewGuid();

        var result = await controller.GenerateSkuCandidate(
            new GenerateSkuCandidateRequest
            {
                Purpose = "NO_BARCODE_PRODUCT",
                CategoryId = categoryId,
                Mode = "AUTO",
                ProductName = "House Lemon Juice",
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<GenerateSkuCandidateResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));
        Assert.Equal("TSH-000125", body.Candidate);
        Assert.True(body.Reserved);
        Assert.Equal("NO_BARCODE_PRODUCT", service.LastGenerateSkuRequest!.Purpose);
        Assert.Equal(categoryId, service.LastGenerateSkuRequest.CategoryId);
        Assert.Equal("AUTO", service.LastGenerateSkuRequest.Mode);
        Assert.Equal("House Lemon Juice", service.LastGenerateSkuRequest.ProductName);
    }

    [Fact]
    public async Task GenerateSkuCandidate_BadPurpose_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            GenerateSkuCandidateResult = ApplicationResult<GenerateSkuCandidateResponse>.Failure(
                new ApplicationError(
                    "product.validation_failed",
                    "purpose must be NO_BARCODE_PRODUCT.",
                    [new ApplicationFieldError("purpose", "Unsupported purpose.")])),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.GenerateSkuCandidate(
            new GenerateSkuCandidateRequest { Purpose = "IMPORT" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GenerateSkuCandidate_WithEntitlementDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            GenerateSkuCandidateResult = ApplicationResult<GenerateSkuCandidateResponse>.Failure(
                new ApplicationError("product.entitlement_denied", "Product management feature is not included.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.GenerateSkuCandidate(
            new GenerateSkuCandidateRequest { Purpose = "NO_BARCODE_PRODUCT" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public void SaveDraft_Route_IsPostDraft()
    {
        var method = typeof(TenantAdminProductsController).GetMethod(
            nameof(TenantAdminProductsController.SaveDraft));
        Assert.NotNull(method);
        var httpPost = Assert.Single(method!.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal("draft", httpPost.Template);
    }

    [Fact]
    public async Task SaveDraft_WithScanBootstrap_ReturnsCreatedEnvelope_Step2()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            DraftResult = ApplicationResult<ProductDraftResponse>.Success(
                new ProductDraftResponse(
                    productId,
                    "Scanner Product",
                    "DRF-1",
                    ProductConstants.DraftStatus,
                    ProductConstants.DesiredPublishActive,
                    2,
                    null,
                    1,
                    null,
                    null,
                    null,
                    null,
                    true,
                    false,
                    false,
                    false,
                    false,
                    "SIMPLE",
                    false,
                    [],
                    TargetSetupStep: 2))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.SaveDraft(
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "SCAN",
                    CreationAction = "CONTINUE_WITH_BARCODE",
                    CandidateIdentifier = "4006381333931"
                }
            },
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.NotNull(service.LastSaveDraftRequest);
        Assert.NotNull(service.LastSaveDraftRequest!.ScanBootstrap);
        Assert.Equal(2, service.LastSaveDraftRequest.CurrentSetupStep);
    }

    [Fact]
    public async Task SaveDraft_DuplicateBarcode_ReturnsConflict()
    {
        var service = new FakeTenantAdminProductService
        {
            DraftResult = ApplicationResult<ProductDraftResponse>.Failure(
                new ApplicationError("product.duplicate_barcode", "Barcode already exists."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.SaveDraft(
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "SCAN",
                    CreationAction = "CONTINUE_WITH_BARCODE",
                    CandidateIdentifier = "4006381333931"
                }
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task SaveDraft_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.SaveDraft(
            new SaveProductDraftRequest { CurrentSetupStep = 2 },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void ExternalLookupBarcode_Route_IsPostBarcodesExternalLookup()
    {
        var method = typeof(TenantAdminProductsController).GetMethod(
            nameof(TenantAdminProductsController.ExternalLookupBarcode));
        Assert.NotNull(method);
        var httpPost = Assert.Single(method!.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal("barcodes/external-lookup", httpPost.Template);
    }

    [Fact]
    public async Task ExternalLookupBarcode_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.LastExternalLookupRequest);
    }

    [Fact]
    public async Task ExternalLookupBarcode_PermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            ExternalLookupResult = ApplicationResult<ExternalLookupProductBarcodeResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.view");

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task ExternalLookupBarcode_EntitlementDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            ExternalLookupResult = ApplicationResult<ExternalLookupProductBarcodeResponse>.Failure(
                new ApplicationError("product.entitlement_denied", "Product management feature is not included.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task ExternalLookupBarcode_NoMatch_ReturnsOkEnvelope()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "04006381333931" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ExternalLookupProductBarcodeResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));
        Assert.Equal(ExternalProductLookupStatuses.NoMatch, body.Status);
        Assert.Equal("04006381333931", service.LastExternalLookupRequest!.Barcode);
    }

    [Fact]
    public async Task ExternalLookupBarcode_Found_SerializesSuggestion()
    {
        var suggestion = new ExternalProductSuggestion(
            "Cola", null, "Brand", "Cat", "1L", null, null, null,
            "https://cdn.example/x.png", "4006381333931", "GTIN13");
        var service = new FakeTenantAdminProductService
        {
            ExternalLookupResult = ApplicationResult<ExternalLookupProductBarcodeResponse>.Success(
                new ExternalLookupProductBarcodeResponse(
                    ExternalProductLookupStatuses.Found, suggestion, "ref", false)),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ExternalLookupProductBarcodeResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));
        Assert.Equal(ExternalProductLookupStatuses.Found, body.Status);
        Assert.Equal("Cola", body.Suggestion!.ProductName);
        Assert.Equal("Brand", body.Suggestion.BrandText);
        var json = System.Text.Json.JsonSerializer.Serialize(body);
        Assert.DoesNotContain("apiKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("upcitemdb", "PROVIDER")]
    [InlineData("upcitemdb", "CACHE")]
    [InlineData("openfoodfacts", "PROVIDER")]
    public async Task ExternalLookupBarcode_Found_SerializesSourceProviderAndRetrievalSource(
        string sourceProvider, string retrievalSource)
    {
        // Phase C: proves the controller relays the Phase A identity model end-to-end (e.g. a
        // result the service produced after OpenFoodFacts NO_MATCH -> UPCitemdb FOUND) without
        // dropping/renaming the new fields, and without leaking any provider request details.
        var suggestion = new ExternalProductSuggestion(
            "Widget", null, "BrandCo", "Category", "1 pc", null, null, null,
            "https://cdn.example/widget.png", "4006381333931", "GTIN13");
        var service = new FakeTenantAdminProductService
        {
            ExternalLookupResult = ApplicationResult<ExternalLookupProductBarcodeResponse>.Success(
                new ExternalLookupProductBarcodeResponse(
                    ExternalProductLookupStatuses.Found,
                    suggestion,
                    SourceReference: sourceProvider,
                    RetryAllowed: false,
                    CategoryResolution: null,
                    SourceProvider: sourceProvider,
                    RetrievalSource: retrievalSource)),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ExternalLookupProductBarcodeResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));

        Assert.Equal(sourceProvider, body.SourceProvider);
        Assert.Equal(retrievalSource, body.RetrievalSource);
        Assert.NotEqual("cache", body.SourceProvider);

        var json = System.Text.Json.JsonSerializer.Serialize(body);
        Assert.DoesNotContain("BaseUrl", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user_key", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api.upcitemdb.com", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExternalLookupBarcode_Found_SerializesCategoryResolution_WithExpectedJsonShape()
    {
        var categoryId = Guid.NewGuid();
        var suggestion = new ExternalProductSuggestion(
            "Coca-Cola Original Taste", null, "Coca-Cola", "Beverages, Colas", "330ml", "US",
            null, null, "https://cdn.example/coke.png", "5449000000996", "GTIN13",
            ExternalCategoryKey: "en:colas",
            ExternalCategoryName: "Colas",
            ExternalCategoryHierarchy: new[] { "en:beverages", "en:carbonated-drinks", "en:colas" });

        var categoryResolution = new TenantCategoryResolutionResult(
            Provider: "openfoodfacts",
            ExternalCategoryKey: "en:colas",
            ExternalCategoryName: "Colas",
            MappedCategory: new TenantCategoryCandidate(categoryId, "Soft Drinks", "CAT-SOFT-DRINKS"),
            Suggestions: new[]
            {
                new TenantCategorySuggestionItem(Guid.NewGuid(), "Cola Drinks", "CAT-COLA", "EXACT"),
            });

        var service = new FakeTenantAdminProductService
        {
            ExternalLookupResult = ApplicationResult<ExternalLookupProductBarcodeResponse>.Success(
                new ExternalLookupProductBarcodeResponse(
                    ExternalProductLookupStatuses.Found,
                    suggestion,
                    "openfoodfacts",
                    false,
                    CategoryResolution: categoryResolution)),
        };
        var controller = CreateController(service);
        var tenantId = Guid.NewGuid();
        SetTenantClaims(controller, tenantId, Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ExternalLookupProductBarcodeResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));

        Assert.Equal(ExternalProductLookupStatuses.Found, body.Status);
        Assert.NotNull(body.CategoryResolution);
        Assert.Equal("openfoodfacts", body.CategoryResolution!.Provider);
        Assert.Equal("en:colas", body.CategoryResolution.ExternalCategoryKey);
        Assert.Equal("Colas", body.CategoryResolution.ExternalCategoryName);
        Assert.NotNull(body.CategoryResolution.MappedCategory);
        Assert.Equal(categoryId, body.CategoryResolution.MappedCategory!.Id);
        Assert.Equal("Soft Drinks", body.CategoryResolution.MappedCategory.Name);
        Assert.Equal("CAT-SOFT-DRINKS", body.CategoryResolution.MappedCategory.Code);
        Assert.Single(body.CategoryResolution.Suggestions);

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        };
        var json = System.Text.Json.JsonSerializer.Serialize(body, jsonOptions);

        Assert.Contains("\"categoryResolution\"", json);
        Assert.Contains("\"mappedCategory\"", json);
        Assert.Contains("\"externalCategoryKey\":\"en:colas\"", json);
        Assert.Contains("\"name\":\"Soft Drinks\"", json);
        Assert.Contains("\"code\":\"CAT-SOFT-DRINKS\"", json);
        Assert.Contains("\"suggestions\"", json);
        Assert.Contains("\"matchType\":\"EXACT\"", json);
        // Ensure no internal DB/Tenant fields are exposed
        Assert.DoesNotContain("TenantId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CreatedAt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UpdatedAt", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExternalLookupBarcode_Found_SerializesBrandResolution_WithExpectedJsonShape()
    {
        var brandId = Guid.NewGuid();
        var suggestion = new ExternalProductSuggestion(
            "Coca-Cola Original Taste", null, "Coca-Cola", "Beverages, Colas", "330ml", "US",
            null, null, "https://cdn.example/coke.png", "5449000000996", "GTIN13");

        var brandResolution = new TenantBrandResolutionResult(
            Provider: "openfoodfacts",
            ExternalBrandKey: "coca cola",
            ExternalBrandName: "Coca-Cola",
            MappedBrand: new TenantBrandCandidate(brandId, "Coca Cola", "COCA_COLA"),
            Suggestions: new[]
            {
                new TenantBrandSuggestionItem(Guid.NewGuid(), "Coca Cola Zero", "COCA_COLA_ZERO", "SIMILARITY"),
            });

        var service = new FakeTenantAdminProductService
        {
            ExternalLookupResult = ApplicationResult<ExternalLookupProductBarcodeResponse>.Success(
                new ExternalLookupProductBarcodeResponse(
                    ExternalProductLookupStatuses.Found,
                    suggestion,
                    "openfoodfacts",
                    false,
                    BrandResolution: brandResolution)),
        };
        var controller = CreateController(service);
        var tenantId = Guid.NewGuid();
        SetTenantClaims(controller, tenantId, Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ExternalLookupProductBarcodeResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));

        Assert.Equal(ExternalProductLookupStatuses.Found, body.Status);
        Assert.NotNull(body.BrandResolution);
        Assert.Equal("openfoodfacts", body.BrandResolution!.Provider);
        Assert.Equal("coca cola", body.BrandResolution.ExternalBrandKey);
        Assert.Equal("Coca-Cola", body.BrandResolution.ExternalBrandName);
        Assert.NotNull(body.BrandResolution.MappedBrand);
        Assert.Equal(brandId, body.BrandResolution.MappedBrand!.Id);
        Assert.Equal("Coca Cola", body.BrandResolution.MappedBrand.Name);
        Assert.Equal("COCA_COLA", body.BrandResolution.MappedBrand.Code);
        Assert.Single(body.BrandResolution.Suggestions);

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        };
        var json = System.Text.Json.JsonSerializer.Serialize(body, jsonOptions);

        Assert.Contains("\"brandResolution\"", json);
        Assert.Contains("\"mappedBrand\"", json);
        Assert.Contains("\"externalBrandKey\":\"coca cola\"", json);
        Assert.Contains("\"name\":\"Coca Cola\"", json);
        Assert.Contains("\"code\":\"COCA_COLA\"", json);
        Assert.Contains("\"suggestions\"", json);
        Assert.Contains("\"matchType\":\"SIMILARITY\"", json);
        // Ensure no internal DB/Tenant fields are exposed
        Assert.DoesNotContain("TenantId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CreatedAt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UpdatedAt", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExternalLookupBarcode_TemporaryFailure_SerializesRetryAllowed()
    {
        var service = new FakeTenantAdminProductService
        {
            ExternalLookupResult = ApplicationResult<ExternalLookupProductBarcodeResponse>.Success(
                new ExternalLookupProductBarcodeResponse(
                    ExternalProductLookupStatuses.TemporaryFailure, null, null, true)),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ExternalLookupProductBarcodeResponse>(
            ok.Value!.GetType().GetProperty("data")!.GetValue(ok.Value));
        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, body.Status);
        Assert.True(body.RetryAllowed);
    }

    [Fact]
    public async Task ExternalLookupBarcode_InvalidBarcode_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            ExternalLookupResult = ApplicationResult<ExternalLookupProductBarcodeResponse>.Failure(
                new ApplicationError(
                    "product.validation_failed",
                    "barcode is not a valid product identifier.",
                    [new ApplicationFieldError("barcode", "CHECKSUM_FAILED")])),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.create");

        var result = await controller.ExternalLookupBarcode(
            new ExternalLookupProductBarcodeRequest { Barcode = "123" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(
            typeof(TenantAdminProductsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public async Task Create_WithTenantProductsCreate_ReturnsCreated()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            CreateResult = ApplicationResult<TenantAdminProductCreateResponse>.Success(
                new TenantAdminProductCreateResponse(
                    productId,
                    "Sample Product",
                    "SKU-001",
                    "ACTIVE")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal($"/api/v1/tenant-admin/products/{productId}", created.Location);
    }

    [Fact]
    public async Task Create_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            CreateResult = ApplicationResult<TenantAdminProductCreateResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidationFailed_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            CreateResult = ApplicationResult<TenantAdminProductCreateResponse>.Failure(
                new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [new ApplicationFieldError("productName", "Product name is required.")])),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateSku_ReturnsConflict()
    {
        var service = new FakeTenantAdminProductService
        {
            CreateResult = ApplicationResult<TenantAdminProductCreateResponse>.Failure(
                new ApplicationError("product.duplicate_sku", "SKU already exists.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.Create(CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetById_WithTenantProductsView_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var detail = CreateDetailResponse(productId);
        var service = new FakeTenantAdminProductService
        {
            DetailResult = ApplicationResult<TenantAdminProductDetailResponse>.Success(detail),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetById(productId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithTenantProductsDetailsView_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            DetailResult = ApplicationResult<TenantAdminProductDetailResponse>.Success(
                CreateDetailResponse(productId)),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.DetailsView);

        var result = await controller.GetById(productId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            DetailResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Create);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenProductNotFound_ReturnsNotFound()
    {
        var service = new FakeTenantAdminProductService
        {
            DetailResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError("product.not_found", "Product was not found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithTenantProductsUpdate_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            UpdateResult = ApplicationResult<TenantAdminProductDetailResponse>.Success(
                CreateDetailResponse(productId)),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.Update(productId, CreateRequest(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            UpdateResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidationFailed_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            UpdateResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [new ApplicationFieldError("productName", "Product name is required.")])),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenProductNotFound_ReturnsNotFound()
    {
        var service = new FakeTenantAdminProductService
        {
            UpdateResult = ApplicationResult<TenantAdminProductDetailResponse>.Failure(
                new ApplicationError("product.not_found", "Product was not found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.Update(Guid.NewGuid(), CreateRequest(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_WithTenantProductsUpdate_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            StatusUpdateResult = ApplicationResult<TenantAdminProductStatusUpdateResponse>.Success(
                new TenantAdminProductStatusUpdateResponse(productId, "INACTIVE")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.UpdateStatus(
            productId,
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            StatusUpdateResult = ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(
                new ApplicationError(
                    "product.validation_failed",
                    "Product validation failed.",
                    [new ApplicationFieldError("status", "Status must be Active or Inactive.")])),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Deleted" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateStatus_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            StatusUpdateResult = ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WhenProductNotFound_ReturnsNotFound()
    {
        var service = new FakeTenantAdminProductService
        {
            StatusUpdateResult = ApplicationResult<TenantAdminProductStatusUpdateResponse>.Failure(
                new ApplicationError("product.not_found", "Product was not found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Update);

        var result = await controller.UpdateStatus(
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task List_WithValidFilters_ReturnsOk()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.List(
            search: "Jersey",
            categoryId: categoryId,
            brandId: brandId,
            productStatus: "ACTIVE",
            stockStatus: "IN_STOCK",
            page: 2,
            pageSize: 10,
            sortBy: "productName",
            sortDirection: "asc",
            cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetFilterOptions_WithPermission_ReturnsOk()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetFilterOptions(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithTenantProductsDelete_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var service = new FakeTenantAdminProductService
        {
            DeleteResult = ApplicationResult<TenantAdminProductDeleteResponse>.Success(
                new TenantAdminProductDeleteResponse(productId, "Archived", "INACTIVE")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Delete);

        var result = await controller.Delete(productId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            DeleteResult = ApplicationResult<TenantAdminProductDeleteResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenAlreadyDeleted_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            DeleteResult = ApplicationResult<TenantAdminProductDeleteResponse>.Failure(
                new ApplicationError("product.delete_blocked", "Product is already deleted.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Delete);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenProductNotFound_ReturnsNotFound()
    {
        var service = new FakeTenantAdminProductService
        {
            DeleteResult = ApplicationResult<TenantAdminProductDeleteResponse>.Failure(
                new ApplicationError("product.not_found", "Product was not found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.Delete);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static TenantAdminProductDetailResponse CreateDetailResponse(Guid productId) =>
        new(
            productId,
            "Sample Product",
            "PROD-001",
            "SKU-001",
            null,
            Guid.NewGuid(),
            "Beverages",
            null,
            null,
            "PIECE",
            null,
            null,
            null,
            [],
            null,
            10m,
            null,
            null,
            null,
            "ACTIVE",
            false,
            null,
            [],
            [],
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static TenantAdminProductCreateRequest CreateRequest() =>
        new()
        {
            ProductName = "Sample Product",
            Sku = "SKU-001",
            CategoryId = Guid.NewGuid(),
            UnitType = "PIECE",
            SellingPrice = 10m,
            Status = "ACTIVE",
        };

    private static TenantAdminProductsController CreateController(FakeTenantAdminProductService service)
    {
        var controller = new TenantAdminProductsController(
            service,
            new FakeCatalogMediaService(),
            new FakeTenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static void SetTenantClaims(
        ControllerBase controller,
        Guid tenantId,
        Guid userId,
        string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission),
            ],
            "Test"));
    }

    [Fact]
    public async Task GetSetup_WithViewPermission_ReturnsOk()
    {
        var productId = Guid.NewGuid();
        var setup = new ProductSetupWizardDto(
            productId,
            "Draft Product",
            "DRF-001",
            "DRAFT",
            "ACTIVE",
            2,
            DateTimeOffset.UtcNow,
            1,
            Guid.NewGuid(),
            null,
            null,
            null,
            true,
            false,
            false,
            false,
            false,
            "SIMPLE",
            false,
            [],
            ScanContext: new ProductSetupScanContextDto("SCAN", "012345678905"));
        var service = new FakeTenantAdminProductService
        {
            SetupResult = ApplicationResult<ProductSetupWizardDto>.Success(setup),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), ProductConstants.ViewPermission);

        var result = await controller.GetSetup(productId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetSetup_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            SetupResult = ApplicationResult<ProductSetupWizardDto>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "catalog.products.delete");

        var result = await controller.GetSetup(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetSetup_WhenNotFound_ReturnsNotFound()
    {
        var service = new FakeTenantAdminProductService
        {
            SetupResult = ApplicationResult<ProductSetupWizardDto>.Failure(
                new ApplicationError("product.not_found", "Product was not found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), ProductConstants.ViewPermission);

        var result = await controller.GetSetup(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetSetup_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeTenantAdminProductService());

        var result = await controller.GetSetup(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithExternalCategoryMappingContext_PassesContextAndReturnsCreated()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetTenantClaims(controller, tenantId, userId, ProductConstants.CreatePermission);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Mapped Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-MAPPED-01",
            SellingPrice = 10m,
            UnitType = "PIECE",
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("openfoodfacts", "en:colas", "Colas"),
        };

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(created.Value);
        Assert.NotNull(service.LastCreateRequest);
        Assert.NotNull(service.LastCreateRequest!.ExternalCategoryMappingContext);
        Assert.Equal("openfoodfacts", service.LastCreateRequest.ExternalCategoryMappingContext!.Provider);
        Assert.Equal("en:colas", service.LastCreateRequest.ExternalCategoryMappingContext.ExternalCategoryKey);
    }

    [Fact]
    public async Task CreateFromWizard_WithExternalCategoryMappingContext_PassesContextAndReturnsCreated()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetTenantClaims(controller, tenantId, userId, ProductConstants.CreatePermission);

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Wizard Mapped Product",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "SIMPLE",
            DesiredPublishActive = true,
            ProductUnitId = Guid.NewGuid(),
            BaseUnitId = Guid.NewGuid(),
            UnitModel = "SINGLE_UNIT",
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("openfoodfacts", "en:colas", "Colas"),
        };

        var result = await controller.CreateFromWizard(request, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(created.Value);
        Assert.NotNull(service.LastWizardCreateRequest);
        Assert.NotNull(service.LastWizardCreateRequest!.ExternalCategoryMappingContext);
        Assert.Equal("openfoodfacts", service.LastWizardCreateRequest.ExternalCategoryMappingContext!.Provider);
        Assert.Equal("en:colas", service.LastWizardCreateRequest.ExternalCategoryMappingContext.ExternalCategoryKey);
    }

    [Fact]
    public async Task CreateFromWizard_WithoutContext_Succeeds()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetTenantClaims(controller, tenantId, userId, ProductConstants.CreatePermission);

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Normal Wizard Product",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "SIMPLE",
            DesiredPublishActive = true,
            ProductUnitId = Guid.NewGuid(),
            BaseUnitId = Guid.NewGuid(),
            UnitModel = "SINGLE_UNIT",
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            ExternalCategoryMappingContext = null,
        };

        var result = await controller.CreateFromWizard(request, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(created.Value);
        Assert.NotNull(service.LastWizardCreateRequest);
        Assert.Null(service.LastWizardCreateRequest!.ExternalCategoryMappingContext);
    }

    [Fact]
    public async Task CreateFromWizard_WithoutTenantClaims_ReturnsUnauthorized()
    {
        var service = new FakeTenantAdminProductService();
        var controller = CreateController(service);

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Unauthorized Wizard Product",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "SIMPLE",
        };

        var result = await controller.CreateFromWizard(request, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CreateFromWizard_WhenValidationFails_ReturnsBadRequest()
    {
        var service = new FakeTenantAdminProductService
        {
            CreateResult = ApplicationResult<TenantAdminProductCreateResponse>.Failure(
                new ApplicationError("product.validation_failed", "Validation failed.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), ProductConstants.CreatePermission);

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Invalid Wizard Product",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "SIMPLE",
        };

        var result = await controller.CreateFromWizard(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetDashboard_WithDashboardPermission_ReturnsOk()
    {
        var dashboard = new TenantAdminProductDashboardResponse(
            DateTimeOffset.UtcNow,
            "USD",
            new TenantAdminProductDashboardSummaryDto(
                new TenantAdminProductDashboardMetricDto(5, 0),
                null,
                null,
                null,
                null,
                null),
            null,
            null);
        var service = new FakeTenantAdminProductService
        {
            DashboardResult = ApplicationResult<TenantAdminProductDashboardResponse>.Success(dashboard),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.DashboardView);

        var result = await controller.GetDashboard(cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDashboard_WithPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeTenantAdminProductService
        {
            DashboardResult = ApplicationResult<TenantAdminProductDashboardResponse>.Failure(
                new ApplicationError("product.permission_denied", "Permission denied for product management.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), TenantAdminProductPermissions.View);

        var result = await controller.GetDashboard(cancellationToken: CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    private sealed class FakeTenantAdminProductService : ITenantAdminProductService
    {
        public TenantAdminProductCreateRequest? LastCreateRequest { get; private set; }
        public TenantAdminWizardProductCreateRequest? LastWizardCreateRequest { get; private set; }

        public ApplicationResult<TenantAdminProductSummaryCardsResponse> SummaryResult { get; init; } =
            ApplicationResult<TenantAdminProductSummaryCardsResponse>.Success(
                new TenantAdminProductSummaryCardsResponse(0, 0, 0, 0));

        public ApplicationResult<TenantAdminProductCreateOptionsResponse> CreateOptionsResult { get; init; } =
            ApplicationResult<TenantAdminProductCreateOptionsResponse>.Success(
                new TenantAdminProductCreateOptionsResponse([], [], [], [], [], [], [], []));

        public ApplicationResult<TenantAdminProductCreateResponse> CreateResult { get; init; } =
            ApplicationResult<TenantAdminProductCreateResponse>.Success(
                new TenantAdminProductCreateResponse(Guid.NewGuid(), "Sample Product", "SKU-001", "ACTIVE"));

        public ApplicationResult<TenantAdminProductDetailResponse> DetailResult { get; init; } =
            ApplicationResult<TenantAdminProductDetailResponse>.Success(
                new TenantAdminProductDetailResponse(
                    Guid.NewGuid(),
                    "Sample Product",
                    "PC-001",
                    "SKU-001",
                    null,
                    Guid.NewGuid(),
                    "Beverages",
                    null,
                    null,
                    "PIECE",
                    null,
                    null,
                    null,
                    [],
                    null,
                    10m,
                    null,
                    null,
                    null,
                    "ACTIVE",
                    false,
                    null,
                    [],
                    [],
                    null,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow));

        public ApplicationResult<TenantAdminProductDetailResponse> UpdateResult { get; init; } =
            ApplicationResult<TenantAdminProductDetailResponse>.Success(
                new TenantAdminProductDetailResponse(
                    Guid.NewGuid(),
                    "Updated Product",
                    "PC-001",
                    "SKU-001",
                    null,
                    Guid.NewGuid(),
                    "Beverages",
                    null,
                    null,
                    "PIECE",
                    null,
                    null,
                    null,
                    [],
                    null,
                    10m,
                    null,
                    null,
                    null,
                    "ACTIVE",
                    false,
                    null,
                    [],
                    [],
                    null,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow));

        public ApplicationResult<TenantAdminProductStatusUpdateResponse> StatusUpdateResult { get; init; } =
            ApplicationResult<TenantAdminProductStatusUpdateResponse>.Success(
                new TenantAdminProductStatusUpdateResponse(Guid.NewGuid(), "INACTIVE"));

        public ApplicationResult<TenantAdminProductDeleteResponse> DeleteResult { get; init; } =
            ApplicationResult<TenantAdminProductDeleteResponse>.Success(
                new TenantAdminProductDeleteResponse(Guid.NewGuid(), "Deleted", "DELETED"));

        public ApplicationResult<TenantAdminProductDashboardResponse> DashboardResult { get; init; } =
            ApplicationResult<TenantAdminProductDashboardResponse>.Success(
                new TenantAdminProductDashboardResponse(
                    DateTimeOffset.UtcNow,
                    "USD",
                    new TenantAdminProductDashboardSummaryDto(null, null, null, null, null, null),
                    null,
                    null));

        public Task<ApplicationResult<TenantAdminProductDashboardResponse>> GetDashboardAsync(
            TenantRequestContext context,
            TenantAdminProductDashboardQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(DashboardResult);

        public Task<ApplicationResult<TenantAdminProductSummaryCardsResponse>> GetSummaryAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(SummaryResult);

        public Task<ApplicationResult<TenantAdminProductCreateOptionsResponse>> GetCreateOptionsAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateOptionsResult);

        public Task<ApplicationResult<TenantAdminProductFilterOptionsResponse>> GetFilterOptionsAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<TenantAdminProductFilterOptionsResponse>.Success(
                new TenantAdminProductFilterOptionsResponse([], [], [], [])));

        public Task<ApplicationResult<TenantAdminProductCreateResponse>> CreateAsync(
            TenantRequestContext context,
            TenantAdminProductCreateRequest request,
            CancellationToken cancellationToken)
        {
            LastCreateRequest = request;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<TenantAdminProductCreateResponse>> CreateFromWizardAsync(
            TenantRequestContext context,
            TenantAdminWizardProductCreateRequest request,
            CancellationToken cancellationToken)
        {
            LastWizardCreateRequest = request;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<TenantAdminProductDetailResponse>> GetByIdAsync(
            TenantRequestContext context,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(DetailResult);

        public Task<ApplicationResult<TenantAdminProductDetailResponse>> UpdateAsync(
            TenantRequestContext context,
            Guid productId,
            TenantAdminProductCreateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(UpdateResult);

        public Task<ApplicationResult<TenantAdminProductStatusUpdateResponse>> UpdateStatusAsync(
            TenantRequestContext context,
            Guid productId,
            TenantAdminProductStatusUpdateRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(StatusUpdateResult);

        public Task<ApplicationResult<TenantAdminProductDeleteResponse>> DeleteAsync(
            TenantRequestContext context,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(DeleteResult);

        public Task<ApplicationResult<TenantAdminProductListResponse>> ListAsync(
            TenantRequestContext context,
            string? search,
            Guid? categoryId,
            Guid? brandId,
            string? productStatus,
            string? stockStatus,
            int pageNumber,
            int pageSize,
            string? sortBy,
            string? sortDirection,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                ApplicationResult<TenantAdminProductListResponse>.Success(
                    new TenantAdminProductListResponse(
                        [],
                        pageNumber,
                        pageSize,
                        0,
                        0,
                        false,
                        false,
                        0)));

        public ApplicationResult<ProductDraftResponse> DraftResult { get; init; } =
            ApplicationResult<ProductDraftResponse>.Success(
                new ProductDraftResponse(
                    Guid.NewGuid(),
                    "Draft Product",
                    "DRF-001",
                    "DRAFT",
                    "ACTIVE",
                    1,
                    DateTimeOffset.UtcNow,
                    1,
                    Guid.NewGuid(),
                    null,
                    null,
                    null,
                    true,
                    false,
                    false,
                    false,
                    false,
                    "SIMPLE",
                    false,
                    []));

        public ApplicationResult<ProductSetupWizardDto> SetupResult { get; init; } =
            ApplicationResult<ProductSetupWizardDto>.Success(
                new ProductSetupWizardDto(
                    Guid.NewGuid(),
                    "Draft Product",
                    "DRF-001",
                    "DRAFT",
                    "ACTIVE",
                    1,
                    DateTimeOffset.UtcNow,
                    1,
                    Guid.NewGuid(),
                    null,
                    null,
                    null,
                    true,
                    false,
                    false,
                    false,
                    false,
                    "SIMPLE",
                    false,
                    []));

        public SaveProductDraftRequest? LastSaveDraftRequest { get; private set; }

        public Task<ApplicationResult<ProductDraftResponse>> SaveDraftAsync(
            TenantRequestContext context,
            SaveProductDraftRequest request,
            CancellationToken cancellationToken)
        {
            LastSaveDraftRequest = request;
            return Task.FromResult(DraftResult);
        }

        public Task<ApplicationResult<ProductDraftResponse>> UpdateDraftAsync(
            TenantRequestContext context,
            Guid productId,
            SaveProductDraftRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(DraftResult);

        public Task<ApplicationResult<ProductSetupWizardDto>> GetSetupAsync(
            TenantRequestContext context,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SetupResult);

        public Task<ApplicationResult<ProductDraftResponse>> PublishAsync(
            TenantRequestContext context,
            Guid productId,
            PublishProductRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(DraftResult);
        public Task<ApplicationResult> UpdateVariantAsync(TenantRequestContext context, Guid productId, Guid variantId, TenantAdminProductVariantUpdateRequest request, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Success());
        public Task<ApplicationResult> AddBarcodeAsync(TenantRequestContext context, Guid productId, Guid variantId, TenantAdminProductBarcodeAddRequest request, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Success());
        public Task<ApplicationResult> DeleteBarcodeAsync(TenantRequestContext context, Guid productId, Guid variantId, Guid barcodeId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Success());
        public Task<ApplicationResult> RestoreAsync(TenantRequestContext context, Guid productId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Success());
        public Task<ApplicationResult<TenantAdminProductCreateResponse>> DuplicateAsync(TenantRequestContext context, Guid productId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<TenantAdminProductCreateResponse>.Success(new TenantAdminProductCreateResponse(Guid.NewGuid(), "Sample", "SKU", "ACTIVE")));

        public ApplicationResult<ResolveProductBarcodeResponse> ResolveBarcodeResult { get; init; } =
            ApplicationResult<ResolveProductBarcodeResponse>.Success(
                new ResolveProductBarcodeResponse(
                    "VALID_NO_LOCAL_MATCH",
                    "4006381333931",
                    "GTIN13",
                    "UNKNOWN",
                    null,
                    null));

        public ResolveProductBarcodeRequest? LastResolveRequest { get; private set; }

        public Task<ApplicationResult<ResolveProductBarcodeResponse>> ResolveBarcodeAsync(
            TenantRequestContext context,
            ResolveProductBarcodeRequest request,
            CancellationToken cancellationToken)
        {
            LastResolveRequest = request;
            return Task.FromResult(ResolveBarcodeResult);
        }

        public ApplicationResult<GenerateSkuCandidateResponse> GenerateSkuCandidateResult { get; init; } =
            ApplicationResult<GenerateSkuCandidateResponse>.Success(
                new GenerateSkuCandidateResponse("SKU-NB", false));

        public GenerateSkuCandidateRequest? LastGenerateSkuRequest { get; private set; }

        public Task<ApplicationResult<GenerateSkuCandidateResponse>> GenerateSkuCandidateAsync(
            TenantRequestContext context,
            GenerateSkuCandidateRequest request,
            CancellationToken cancellationToken)
        {
            LastGenerateSkuRequest = request;
            return Task.FromResult(GenerateSkuCandidateResult);
        }

        public ApplicationResult<ExternalLookupProductBarcodeResponse> ExternalLookupResult { get; init; } =
            ApplicationResult<ExternalLookupProductBarcodeResponse>.Success(
                new ExternalLookupProductBarcodeResponse(
                    ExternalProductLookupStatuses.NoMatch, null, null, false));

        public ExternalLookupProductBarcodeRequest? LastExternalLookupRequest { get; private set; }

        public Task<ApplicationResult<ExternalLookupProductBarcodeResponse>> ExternalLookupBarcodeAsync(
            TenantRequestContext context,
            ExternalLookupProductBarcodeRequest request,
            CancellationToken cancellationToken)
        {
            LastExternalLookupRequest = request;
            return Task.FromResult(ExternalLookupResult);
        }
    }



    private sealed class FakeCatalogMediaService : ICatalogMediaService
    {
        public TenantRequestContext? BrandLogoContext { get; private set; }
        public Guid? BrandLogoId { get; private set; }
        public ApplicationResult<MediaAssetUploadResponse> BrandLogoResult { get; init; } =
            ApplicationResult<MediaAssetUploadResponse>.Failure(new ApplicationError("media.permission_denied", "Permission denied for media upload."));
        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadProductImageAsync(
            TenantRequestContext context,
            Guid productId,
            ProductImageUploadRequest request,
            MediaUploadFile file,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<MediaAssetUploadResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<StagedProductImageResponse>> StageProductImageAsync(
            TenantRequestContext context,
            MediaUploadFile file,
            Guid? uploadSessionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<StagedProductImageResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<StagedProductImageResponse>> StageProductImageFromUrlAsync(
            TenantRequestContext context,
            string imageUrl,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<StagedProductImageResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<ProductImagesMutationResponse>> ReorderProductImagesAsync(
            TenantRequestContext context,
            Guid productId,
            ReorderProductImagesRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ProductImagesMutationResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<ProductImagesMutationResponse>> DeleteProductImageAsync(
            TenantRequestContext context,
            Guid productId,
            Guid productImageId,
            long? expectedRowVersion,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ProductImagesMutationResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<ProductImagesMutationResponse>> ReplaceProductImagesAsync(
            TenantRequestContext context,
            Guid productId,
            long expectedRowVersion,
            IReadOnlyList<MediaUploadFile>? files,
            IReadOnlyList<Guid>? stagedMediaAssetIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<ProductImagesMutationResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadCategoryImageAsync(
            TenantRequestContext context,
            Guid categoryId,
            MediaUploadFile file,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<MediaAssetUploadResponse>.Failure(
                new ApplicationError("media.permission_denied", "Permission denied for media upload.")));

        public Task<ApplicationResult<MediaAssetUploadResponse>> UploadBrandLogoAsync(
            TenantRequestContext context,
            Guid brandId,
            MediaUploadFile file,
            CancellationToken cancellationToken)
        {
            BrandLogoContext = context;
            BrandLogoId = brandId;
            return Task.FromResult(BrandLogoResult);
        }
        public Task<ApplicationResult> RemoveCategoryImageAsync(TenantRequestContext context, Guid categoryId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Failure(new ApplicationError("media.permission_denied", "Permission denied for media upload.")));
    }

    private sealed class FakeBrandService : IBrandService
    {
        public ApplicationResult<BrandResponse> DetailResult { get; init; } =
            ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.not_found", "Brand was not found."));

        public Task<ApplicationResult<BrandResponse>> CreateAsync(TenantRequestContext context, BrandCreateRequest request, CancellationToken cancellationToken) => Task.FromResult(DetailResult);
        public Task<ApplicationResult<BrandListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<BrandListResponse>.Success(new BrandListResponse([], pageNumber, pageSize, 0)));
        public Task<ApplicationResult<BrandResponse>> GetByIdAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken) => Task.FromResult(DetailResult);
        public Task<ApplicationResult<BrandResponse>> GetByIdAfterMutationAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken) => Task.FromResult(DetailResult);
        public Task<ApplicationResult<BrandResponse>> UpdateAsync(TenantRequestContext context, Guid brandId, BrandUpdateRequest request, CancellationToken cancellationToken) => Task.FromResult(DetailResult);
        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Success());

    }

    private sealed class FakeTenantRequestContextFactory : ITenantRequestContextFactory
    {
        public bool TryCreate(ClaimsPrincipal user, out TenantRequestContext context)
        {
            var tenantUserIdValue = user.FindFirstValue("sub");
            var tenantIdValue = user.FindFirstValue("tenant_id");
            var hasTenantUserId = Guid.TryParse(tenantUserIdValue, out var tenantUserId);
            var hasTenantId = Guid.TryParse(tenantIdValue, out var tenantId);

            if (!hasTenantUserId || !hasTenantId)
            {
                context = new TenantRequestContext(Guid.Empty, Guid.Empty, []);
                return false;
            }

            var permissions = user.FindAll("permissions")
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            context = new TenantRequestContext(tenantId, tenantUserId, permissions);
            return true;
        }
    }
}
