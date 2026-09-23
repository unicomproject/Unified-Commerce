using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.Inventory.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed partial class TenantAdminProductServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task GetSummaryAsync_WithoutViewPermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetSummaryAsync(CreateContext([]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetSummaryAsync_WithTenantProductsView_ReturnsSummary()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            Summary = new TenantAdminProductSummaryResponse(12, 10, 2, 4),
        };
        var service = CreateService(repository);

        var result = await service.GetSummaryAsync(
            CreateContext([TenantAdminProductPermissions.View]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(12, result.Value.TotalProducts);
        Assert.Equal(10, result.Value.ActiveProducts);
        Assert.Equal(2, result.Value.InactiveProducts);
        Assert.Equal(4, result.Value.CategoryCount);
    }

    [Fact]
    public async Task GetSummaryAsync_WithCatalogProductsView_ReturnsSummary()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            Summary = new TenantAdminProductSummaryResponse(0, 0, 0, 0),
        };
        var service = CreateService(repository);

        var result = await service.GetSummaryAsync(
            CreateContext([ProductConstants.ViewPermission]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalProducts);
        Assert.Equal(0, result.Value.CategoryCount);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.ResolveBarcodeAsync(
            CreateContext([ProductConstants.ViewPermission]),
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "SCAN" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_WithoutProductCatalogEntitlement_ReturnsEntitlementDenied()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, entitlementEvaluator: new FakeEntitlementEvaluator { IsEntitled = false });

        var result = await service.ResolveBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "SCAN" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.entitlement_denied", result.Error.Code);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_InvalidChecksum_ReturnsInvalid_WithoutLookup()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.ResolveBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ResolveProductBarcodeRequest { Barcode = "4006381333930", InputMode = "SCAN" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("INVALID", result.Value!.Outcome);
        Assert.Equal("CHECKSUM_FAILED", result.Value.InvalidReason);
        Assert.Null(result.Value.NormalizedBarcode);
        Assert.Null(result.Value.LocalMatch);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_UnsupportedLength_ReturnsInvalid_WithoutLookup()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.ResolveBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ResolveProductBarcodeRequest { Barcode = "1234567", InputMode = "MANUAL" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("INVALID", result.Value!.Outcome);
        Assert.Equal("LENGTH_NOT_SUPPORTED", result.Value.InvalidReason);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Theory]
    [InlineData("96385074", "GTIN8")]
    [InlineData("012345678905", "GTIN12")]
    [InlineData("4006381333931", "GTIN13")]
    [InlineData("10012345678902", "GTIN14")]
    public async Task ResolveBarcodeAsync_ValidGtin_NoMatch_PreservesStandardAndDoesNotSetGtin14AsBarcodeType(
        string barcode,
        string expectedStandard)
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.ResolveBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ResolveProductBarcodeRequest { Barcode = barcode, InputMode = "SCAN", ReportedSymbology = "UNKNOWN" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("VALID_NO_LOCAL_MATCH", result.Value!.Outcome);
        Assert.Equal(barcode, result.Value.NormalizedBarcode);
        Assert.Equal(expectedStandard, result.Value.IdentifierStandard);
        Assert.Equal("UNKNOWN", result.Value.BarcodeType);
        Assert.NotEqual("GTIN14", result.Value.BarcodeType);
        Assert.Null(result.Value.LocalMatch);
        Assert.Equal(1, repository.ResolveLookupCallCount);
        Assert.Equal(barcode, repository.LastResolvedBarcode);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_PreservesLeadingZeros_IntoRepositoryLookup()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.ResolveBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ResolveProductBarcodeRequest { Barcode = " 012345678905 ", InputMode = "MANUAL" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("VALID_NO_LOCAL_MATCH", result.Value!.Outcome);
        Assert.Equal("012345678905", repository.LastResolvedBarcode);
        Assert.Equal("012345678905", result.Value.NormalizedBarcode);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_ScanAndManual_UseSameLookupPath()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);
        var context = CreateContext([ProductConstants.CreatePermission]);

        var scan = await service.ResolveBarcodeAsync(
            context,
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "SCAN" },
            CancellationToken.None);
        var manual = await service.ResolveBarcodeAsync(
            context,
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "MANUAL" },
            CancellationToken.None);

        Assert.Equal(scan.Value!.Outcome, manual.Value!.Outcome);
        Assert.Equal(scan.Value.NormalizedBarcode, manual.Value.NormalizedBarcode);
        Assert.Equal(2, repository.ResolveLookupCallCount);
        Assert.Equal("4006381333931", repository.LastResolvedBarcode);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_LocalMatch_CreateWithoutView_ReturnsConflictProjectionWithCanViewFalse()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ResolveMatch = new ProductBarcodeResolveMatchProjection(
                productId,
                null,
                "PRODUCT",
                "Cola 330ml",
                null,
                "BrandX",
                "Drinks",
                "SKU-COLA",
                ProductConstants.ActiveStatus,
                null,
                1),
        };
        var service = CreateService(repository);

        var result = await service.ResolveBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "SCAN" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("VALID_LOCAL_MATCH", result.Value!.Outcome);
        Assert.NotNull(result.Value.LocalMatch);
        Assert.Equal(productId, result.Value.LocalMatch!.ProductId);
        Assert.Equal("Cola 330ml", result.Value.LocalMatch.ProductName);
        Assert.Equal("SKU-COLA", result.Value.LocalMatch.Sku);
        Assert.False(result.Value.LocalMatch.CanViewProduct);
        Assert.False(result.Value.LocalMatch.CanEditProduct);
        Assert.Null(result.Value.LocalMatch.SellingPrice);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_LocalMatch_WithViewAndUpdate_SetsCapabilityFlags()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ResolveMatch = new ProductBarcodeResolveMatchProjection(
                productId,
                variantId,
                "VARIANT",
                "Shirt",
                "Red / M",
                null,
                null,
                "SKU-RED-M",
                ProductConstants.InactiveStatus,
                "https://cdn.example/p.png",
                1),
        };
        var service = CreateService(repository);

        var result = await service.ResolveBarcodeAsync(
            CreateContext([
                ProductConstants.CreatePermission,
                ProductConstants.ViewPermission,
                ProductConstants.UpdatePermission,
                ProductConstants.ProductCostViewPermission]),
            new ResolveProductBarcodeRequest { Barcode = "012345678905", InputMode = "SCAN", ReportedSymbology = "UPCA" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("VALID_LOCAL_MATCH", result.Value!.Outcome);
        Assert.Equal("UPCA", result.Value.BarcodeType);
        Assert.Equal("GTIN12", result.Value.IdentifierStandard);
        Assert.Equal(variantId, result.Value.LocalMatch!.VariantId);
        Assert.Equal("VARIANT", result.Value.LocalMatch.MatchedAt);
        Assert.Equal("Red / M", result.Value.LocalMatch.VariantLabel);
        Assert.True(result.Value.LocalMatch.CanViewProduct);
        Assert.True(result.Value.LocalMatch.CanEditProduct);
        Assert.Null(result.Value.LocalMatch.SellingPrice);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_CorruptMultipleMatches_ReturnsIntegrityViolation()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            ResolveMatch = new ProductBarcodeResolveMatchProjection(
                Guid.NewGuid(),
                null,
                "PRODUCT",
                "A",
                null,
                null,
                null,
                "SKU",
                ProductConstants.ActiveStatus,
                null,
                2),
        };
        var service = CreateService(repository);

        var result = await service.ResolveBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "SCAN" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.barcode_integrity_violation", result.Error.Code);
    }

    [Fact]
    public async Task GenerateSkuCandidateAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.GenerateSkuCandidateAsync(
            CreateContext([ProductConstants.ViewPermission]),
            new GenerateSkuCandidateRequest { Purpose = "NO_BARCODE_PRODUCT" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.SkuExistsCallCount);
    }

    [Fact]
    public async Task GenerateSkuCandidateAsync_WithoutProductCatalog_ReturnsEntitlementDenied()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, entitlementEvaluator: new FakeEntitlementEvaluator { IsEntitled = false });

        var result = await service.GenerateSkuCandidateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new GenerateSkuCandidateRequest { Purpose = "NO_BARCODE_PRODUCT" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.entitlement_denied", result.Error.Code);
    }

    [Fact]
    public async Task GenerateSkuCandidateAsync_UnsupportedPurpose_ReturnsValidationFailed()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.GenerateSkuCandidateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new GenerateSkuCandidateRequest { Purpose = "VARIANT" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Equal(0, repository.SkuExistsCallCount);
    }

    [Fact]
    public async Task GenerateSkuCandidateAsync_WithoutCategory_ReturnsValidationError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.GenerateSkuCandidateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new GenerateSkuCandidateRequest { Purpose = "NO_BARCODE_PRODUCT" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "categoryId");
    }

    [Fact]
    public async Task GenerateSkuCandidateAsync_WithCategory_UsesCategoryCodeAndTenantSequence()
    {
        var repository = new FakeTenantAdminProductRepository();
        var categoryId = Guid.NewGuid();
        var service = CreateService(repository);

        var result = await service.GenerateSkuCandidateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new GenerateSkuCandidateRequest
            {
                Purpose = "NO_BARCODE_PRODUCT",
                CategoryId = categoryId,
                Mode = "AUTO",
                ProductName = " House Lemon Juice! ",
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("TSH-000125", result.Value!.Candidate);
        Assert.True(result.Value.Reserved);
        Assert.True(result.Value.Candidate.Length <= ProductSkuCandidateGenerator.MaxLength);
        Assert.Equal(0, repository.CreateProductCallCount);
        Assert.Equal(0, repository.SaveDraftCallCount);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task GenerateSkuCandidateAsync_Collision_ConsumesNextTenantSequence()
    {
        var repository = new FakeTenantAdminProductRepository();
        repository.ExistingSkus.Add("TSH-000125");
        var service = CreateService(repository);

        var result = await service.GenerateSkuCandidateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new GenerateSkuCandidateRequest
            {
                Purpose = "NO_BARCODE_PRODUCT",
                CategoryId = Guid.NewGuid(),
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("TSH-000126", result.Value!.Candidate);
        Assert.True(result.Value.Reserved);
        Assert.Equal(2, repository.SkuExistsCallCount);
    }

    [Fact]
    public async Task GenerateSkuCandidateAsync_ExistingDraft_ReplacesStaleBase()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            ActiveCategoryCode = "BEV",
            GeneratedSkuBase = "TSH-000124",
            NextProductSkuSequence = 128,
        };
        var service = CreateService(repository);

        var result = await service.GenerateSkuCandidateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new GenerateSkuCandidateRequest
            {
                Purpose = "NO_BARCODE_PRODUCT",
                CategoryId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ExpectedRowVersion = 3,
                Mode = "AUTO",
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("BEV-000128", result.Value!.Candidate);
        Assert.Equal("BEV-000128", repository.GeneratedSkuBase);
    }

    [Fact]
    public async Task GenerateSkuCandidateAsync_DifferentTenants_AreRepositoryScoped()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.GenerateSkuCandidateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new GenerateSkuCandidateRequest
            {
                Purpose = "NO_BARCODE_PRODUCT",
                CategoryId = Guid.NewGuid(),
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("TSH-000125", result.Value!.Candidate);
    }

    [Fact]
    public async Task GenerateSkuCandidateAsync_ExhaustedAttempts_ReturnsBusinessError()
    {
        var repository = new FakeTenantAdminProductRepository();
        for (var i = 0; i < ProductSkuCandidateGenerator.MaxAttempts; i++)
        {
            repository.ExistingSkus.Add(
                ProductSkuCandidateGenerator.BuildProductBase("TSH", 125 + i));
        }

        var service = CreateService(repository);
        var result = await service.GenerateSkuCandidateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new GenerateSkuCandidateRequest
            {
                Purpose = "NO_BARCODE_PRODUCT",
                CategoryId = Guid.NewGuid(),
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.sku_candidate_exhausted", result.Error.Code);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator();
        var service = CreateService(repository, coordinator);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.ViewPermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
        Assert.Equal(0, coordinator.CallCount);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_WithoutProductCatalog_ReturnsEntitlementDenied()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator();
        var service = CreateService(
            repository,
            coordinator,
            entitlementEvaluator: new FakeEntitlementEvaluator { IsEntitled = false });

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.entitlement_denied", result.Error.Code);
        Assert.Equal(0, coordinator.CallCount);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_InvalidBarcode_NeverCallsCoordinatorOrLocalLookup()
    {
        var repository = new FakeTenantAdminProductRepository { ResolveMatch = new ProductBarcodeResolveMatchProjection(
            Guid.NewGuid(), null, "PRODUCT", "X", null, null, null, null, "ACTIVE", null, 1) };
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(ExternalProductLookupStatuses.Found, null, null, false),
        };
        var service = CreateService(repository, coordinator);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "123" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Equal(0, coordinator.CallCount);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_Found_ReturnsNormalizedSuggestion_WithoutLocalLookup()
    {
        var repository = new FakeTenantAdminProductRepository();
        var suggestion = new ExternalProductSuggestion(
            "Cola", "C", "BrandText", "CategoryText", "330ml", "US",
            "short", "long", "https://cdn.example/c.png", "4006381333931", "GTIN13");
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found, suggestion, "src-1", false),
        };
        var service = CreateService(repository, coordinator);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExternalProductLookupStatuses.Found, result.Value!.Status);
        Assert.Equal("Cola", result.Value.Suggestion!.ProductName);
        Assert.Equal("BrandText", result.Value.Suggestion.BrandText);
        Assert.Equal("https://cdn.example/c.png", result.Value.Suggestion.ImageCandidate);
        Assert.Equal("src-1", result.Value.SourceReference);
        Assert.False(result.Value.RetryAllowed);
        Assert.Equal(1, coordinator.CallCount);
        Assert.Equal(0, repository.ResolveLookupCallCount);
        Assert.Equal(0, repository.CreateProductCallCount);
        Assert.Equal(0, repository.SaveDraftCallCount);
        Assert.Null(result.Value.Suggestion.GetType().GetProperty("BrandId"));
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_NoMatch_IncludingZeroProviderPath()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.NoMatch, null, null, false),
        };
        var service = CreateService(repository, coordinator);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Value!.Status);
        Assert.False(result.Value.RetryAllowed);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_TemporaryFailure_ReturnsRetryAllowed()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, true),
        };
        var service = CreateService(repository, coordinator);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Value!.Status);
        Assert.True(result.Value.RetryAllowed);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_PreservesLeadingZeros_AndNeverCallsLocalLookup()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.NoMatch, null, null, false),
        };
        var service = CreateService(repository, coordinator);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "04006381333931" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("04006381333931", coordinator.LastRequest!.Identifier);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_PropagatesCancellation()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator { ThrowOnCancel = true };
        var service = CreateService(repository, coordinator);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ExternalLookupBarcodeAsync(
                CreateContext([ProductConstants.CreatePermission]),
                new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
                cts.Token));
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_FoundWithCategoryKey_InvokesResolverAndPopulatesResolution()
    {
        var repository = new FakeTenantAdminProductRepository();
        var categoryCandidateId = Guid.NewGuid();
        var suggestion = new ExternalProductSuggestion(
            "Coca Cola 330ml", "Cola", "Coca-Cola", "Beverages, Colas", "330ml", "FR",
            "short", "long", "https://cdn.example/c.png", "5449000000996", "GTIN13",
            ExternalCategoryKey: "en:colas",
            ExternalCategoryName: "Colas",
            ExternalCategoryHierarchy: new[] { "en:beverages", "en:carbonated-drinks", "en:colas" });

        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found, suggestion, "OpenFoodFacts", false),
        };

        var resolver = new FakeTenantExternalCategoryResolver
        {
            Result = new TenantCategoryResolutionResult(
                "openfoodfacts",
                "en:colas",
                "Colas",
                new TenantCategoryCandidate(categoryCandidateId, "Soft Drinks", "CAT-SOFT-DRINKS"),
                Array.Empty<TenantCategorySuggestionItem>()),
        };

        var service = CreateService(repository, coordinator, tenantExternalCategoryResolver: resolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExternalProductLookupStatuses.Found, result.Value!.Status);
        Assert.Equal(1, coordinator.CallCount);
        Assert.Equal(1, resolver.CallCount);
        Assert.NotNull(resolver.LastRequest);
        Assert.Equal(TenantId, resolver.LastRequest!.TenantId);
        Assert.Equal("openfoodfacts", resolver.LastRequest.Provider);
        Assert.Equal("en:colas", resolver.LastRequest.ExternalCategoryKey);
        Assert.Equal("Colas", resolver.LastRequest.ExternalCategoryName);
        Assert.Equal(3, resolver.LastRequest.ExternalCategoryHierarchy!.Count);

        Assert.NotNull(result.Value.CategoryResolution);
        Assert.Equal("openfoodfacts", result.Value.CategoryResolution!.Provider);
        Assert.Equal("en:colas", result.Value.CategoryResolution.ExternalCategoryKey);
        Assert.Equal("Colas", result.Value.CategoryResolution.ExternalCategoryName);
        Assert.NotNull(result.Value.CategoryResolution.MappedCategory);
        Assert.Equal(categoryCandidateId, result.Value.CategoryResolution.MappedCategory!.Id);
        Assert.Equal("Soft Drinks", result.Value.CategoryResolution.MappedCategory.Name);
        Assert.Equal("CAT-SOFT-DRINKS", result.Value.CategoryResolution.MappedCategory.Code);
    }

    // --- External Brand Mapping: brand resolution runs alongside category resolution whenever the
    // winning provider result carries a meaningful BrandText, using the same SourceProvider. ---

    [Fact]
    public async Task ExternalLookupBarcodeAsync_FoundWithBrandText_InvokesBrandResolverAndPopulatesResolution()
    {
        var repository = new FakeTenantAdminProductRepository();
        var brandCandidateId = Guid.NewGuid();
        var suggestion = new ExternalProductSuggestion(
            "Coca Cola 330ml", "Cola", "Coca-Cola", "Beverages, Colas", "330ml", "FR",
            "short", "long", "https://cdn.example/c.png", "5449000000996", "GTIN13");

        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found, suggestion, "openfoodfacts", false,
                SourceProvider: "openfoodfacts", RetrievalSource: ExternalProductLookupRetrievalSources.Provider),
        };

        var brandResolver = new FakeTenantExternalBrandResolver
        {
            Result = new TenantBrandResolutionResult(
                "openfoodfacts",
                "coca cola",
                "Coca-Cola",
                new TenantBrandCandidate(brandCandidateId, "Coca Cola", "COCA_COLA"),
                Array.Empty<TenantBrandSuggestionItem>()),
        };

        var service = CreateService(repository, coordinator, tenantExternalBrandResolver: brandResolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, brandResolver.CallCount);
        Assert.NotNull(brandResolver.LastRequest);
        Assert.Equal(TenantId, brandResolver.LastRequest!.TenantId);
        Assert.Equal("openfoodfacts", brandResolver.LastRequest.Provider);
        Assert.Equal("coca cola", brandResolver.LastRequest.ExternalBrandKey);
        Assert.Equal("Coca-Cola", brandResolver.LastRequest.ExternalBrandName);

        Assert.NotNull(result.Value!.BrandResolution);
        Assert.Equal("openfoodfacts", result.Value.BrandResolution!.Provider);
        Assert.NotNull(result.Value.BrandResolution.MappedBrand);
        Assert.Equal(brandCandidateId, result.Value.BrandResolution.MappedBrand!.Id);
        Assert.Equal("Coca Cola", result.Value.BrandResolution.MappedBrand.Name);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_MultiValueBrandText_ExtractsFirstSegmentForResolution()
    {
        var repository = new FakeTenantAdminProductRepository();
        var suggestion = new ExternalProductSuggestion(
            "Coca Cola 330ml", "Cola", "Coca-Cola, The Coca-Cola Company", "Beverages", "330ml", "FR",
            "short", "long", null, "5449000000996", "GTIN13");

        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found, suggestion, "openfoodfacts", false,
                SourceProvider: "openfoodfacts", RetrievalSource: ExternalProductLookupRetrievalSources.Provider),
        };

        var brandResolver = new FakeTenantExternalBrandResolver();

        var service = CreateService(repository, coordinator, tenantExternalBrandResolver: brandResolver);

        await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.Equal(1, brandResolver.CallCount);
        Assert.Equal("coca cola", brandResolver.LastRequest!.ExternalBrandKey);
        Assert.Equal("Coca-Cola", brandResolver.LastRequest.ExternalBrandName);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_FoundWithoutBrandText_DoesNotInvokeBrandResolver_BrandResolutionNull()
    {
        var repository = new FakeTenantAdminProductRepository();
        var suggestion = new ExternalProductSuggestion(
            "Generic Product", null, null, null, null, null, null, null, null, "5449000000996", "GTIN13");

        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found, suggestion, "openfoodfacts", false,
                SourceProvider: "openfoodfacts", RetrievalSource: ExternalProductLookupRetrievalSources.Provider),
        };

        var brandResolver = new FakeTenantExternalBrandResolver();

        var service = CreateService(repository, coordinator, tenantExternalBrandResolver: brandResolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, brandResolver.CallCount);
        Assert.Null(result.Value!.BrandResolution);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_NoMatch_DoesNotInvokeBrandResolver_BrandResolutionNull()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.NoMatch, null, null, false),
        };

        var brandResolver = new FakeTenantExternalBrandResolver();

        var service = CreateService(repository, coordinator, tenantExternalBrandResolver: brandResolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, brandResolver.CallCount);
        Assert.Null(result.Value!.BrandResolution);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_CacheHit_BrandResolutionUsesSourceProvider_NotCacheLiteral()
    {
        var repository = new FakeTenantAdminProductRepository();
        var suggestion = new ExternalProductSuggestion(
            "Coca Cola 330ml", "Cola", "Coca-Cola", "Beverages", "330ml", "FR",
            "short", "long", null, "5449000000996", "GTIN13");

        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found,
                suggestion,
                SourceReference: "openfoodfacts",
                RetryAllowed: false,
                SourceProvider: "openfoodfacts",
                RetrievalSource: ExternalProductLookupRetrievalSources.Cache),
        };

        var brandResolver = new FakeTenantExternalBrandResolver();

        var service = CreateService(repository, coordinator, tenantExternalBrandResolver: brandResolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("openfoodfacts", brandResolver.LastRequest!.Provider);
        Assert.NotEqual("cache", brandResolver.LastRequest.Provider);
    }

    // --- Phase A regression: category resolution must use SourceProvider (correct on cache hits),
    // never RetrievalSource or the literal "cache". This proves the original defect is fixed at
    // the TenantAdminProductService call site, independent of the coordinator's own cache logic. ---

    [Fact]
    public async Task ExternalLookupBarcodeAsync_CacheHit_UsesSourceProvider_NotRetrievalSourceOrCacheLiteral()
    {
        var repository = new FakeTenantAdminProductRepository();
        var suggestion = new ExternalProductSuggestion(
            "Coca Cola 330ml", "Cola", "Coca-Cola", "Beverages, Colas", "330ml", "FR",
            "short", "long", "https://cdn.example/c.png", "5449000000996", "GTIN13",
            ExternalCategoryKey: "en:colas",
            ExternalCategoryName: "Colas");

        // Simulate exactly what a fixed coordinator returns on a CACHE HIT: SourceProvider is the
        // real provider, RetrievalSource is "CACHE", and SourceReference (legacy) also carries the
        // real provider — never the literal "cache".
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found,
                suggestion,
                SourceReference: "openfoodfacts",
                RetryAllowed: false,
                SourceProvider: "openfoodfacts",
                RetrievalSource: ExternalProductLookupRetrievalSources.Cache),
        };

        var resolver = new FakeTenantExternalCategoryResolver
        {
            Result = new TenantCategoryResolutionResult(
                "openfoodfacts", "en:colas", "Colas", null, Array.Empty<TenantCategorySuggestionItem>()),
        };

        var service = CreateService(repository, coordinator, tenantExternalCategoryResolver: resolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(resolver.LastRequest);
        Assert.Equal("openfoodfacts", resolver.LastRequest!.Provider);
        Assert.NotEqual("cache", resolver.LastRequest.Provider);
        Assert.NotEqual(ExternalProductLookupRetrievalSources.Cache, resolver.LastRequest.Provider);

        Assert.Equal(ExternalProductLookupRetrievalSources.Cache, result.Value!.RetrievalSource);
        Assert.Equal("openfoodfacts", result.Value.SourceProvider);
        Assert.NotEqual("cache", result.Value.SourceProvider);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_SourceProviderPreferredOverStaleSourceReference()
    {
        var repository = new FakeTenantAdminProductRepository();
        var suggestion = new ExternalProductSuggestion(
            "Coca Cola 330ml", null, null, null, null, null, null, null, null, "5449000000996", "GTIN13",
            ExternalCategoryKey: "en:colas",
            ExternalCategoryName: "Colas");

        // A caller that only populated the legacy field with a mismatched/incorrect value must
        // still resolve correctly once SourceProvider is authoritative.
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found,
                suggestion,
                SourceReference: "stale-or-wrong",
                RetryAllowed: false,
                SourceProvider: "openfoodfacts",
                RetrievalSource: ExternalProductLookupRetrievalSources.Provider),
        };

        var resolver = new FakeTenantExternalCategoryResolver
        {
            Result = new TenantCategoryResolutionResult(
                "openfoodfacts", "en:colas", "Colas", null, Array.Empty<TenantCategorySuggestionItem>()),
        };

        var service = CreateService(repository, coordinator, tenantExternalCategoryResolver: resolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("openfoodfacts", resolver.LastRequest!.Provider);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_FoundWithoutCategoryKey_DoesNotInvokeResolver_CategoryResolutionNull()
    {
        var repository = new FakeTenantAdminProductRepository();
        var suggestion = new ExternalProductSuggestion(
            "Generic Product", null, null, null, null, null,
            null, null, null, "5449000000996", "GTIN13",
            ExternalCategoryKey: null,
            ExternalCategoryName: null,
            ExternalCategoryHierarchy: null);

        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found, suggestion, "openfoodfacts", false),
        };

        var resolver = new FakeTenantExternalCategoryResolver();
        var service = CreateService(repository, coordinator, tenantExternalCategoryResolver: resolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExternalProductLookupStatuses.Found, result.Value!.Status);
        Assert.Equal(1, coordinator.CallCount);
        Assert.Equal(0, resolver.CallCount);
        Assert.Null(result.Value.CategoryResolution);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_NoMatch_DoesNotInvokeResolver_CategoryResolutionNull()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.NoMatch, null, null, false),
        };

        var resolver = new FakeTenantExternalCategoryResolver();
        var service = CreateService(repository, coordinator, tenantExternalCategoryResolver: resolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Value!.Status);
        Assert.Equal(1, coordinator.CallCount);
        Assert.Equal(0, resolver.CallCount);
        Assert.Null(result.Value.CategoryResolution);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_TemporaryFailure_DoesNotInvokeResolver_CategoryResolutionNull()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, true),
        };

        var resolver = new FakeTenantExternalCategoryResolver();
        var service = CreateService(repository, coordinator, tenantExternalCategoryResolver: resolver);

        var result = await service.ExternalLookupBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "4006381333931" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Value!.Status);
        Assert.Equal(1, coordinator.CallCount);
        Assert.Equal(0, resolver.CallCount);
        Assert.Null(result.Value.CategoryResolution);
    }

    [Fact]
    public async Task ExternalLookupBarcodeAsync_MultiTenantResolution_EachTenantReceivesItsOwnResolution()
    {
        var repository = new FakeTenantAdminProductRepository();
        var suggestion = new ExternalProductSuggestion(
            "Cola", "C", "Coca-Cola", "Colas", "330ml", "US",
            null, null, null, "5449000000996", "GTIN13",
            ExternalCategoryKey: "en:colas",
            ExternalCategoryName: "Colas");

        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found, suggestion, "openfoodfacts", false),
        };

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var catA = Guid.NewGuid();
        var catB = Guid.NewGuid();

        // Custom resolver routing per tenant
        var resolver = new DelegatingTenantExternalCategoryResolver(req =>
        {
            if (req.TenantId == tenantA)
            {
                return new TenantCategoryResolutionResult(
                    req.Provider, req.ExternalCategoryKey, req.ExternalCategoryName,
                    new TenantCategoryCandidate(catA, "Soft Drinks", "CAT-SD"),
                    Array.Empty<TenantCategorySuggestionItem>());
            }
            if (req.TenantId == tenantB)
            {
                return new TenantCategoryResolutionResult(
                    req.Provider, req.ExternalCategoryKey, req.ExternalCategoryName,
                    new TenantCategoryCandidate(catB, "Beverages", "CAT-BEV"),
                    Array.Empty<TenantCategorySuggestionItem>());
            }
            return new TenantCategoryResolutionResult(req.Provider, req.ExternalCategoryKey, req.ExternalCategoryName, null, Array.Empty<TenantCategorySuggestionItem>());
        });

        var service = CreateService(repository, coordinator, tenantExternalCategoryResolver: resolver);

        // Tenant A request
        var resultA = await service.ExternalLookupBarcodeAsync(
            new TenantRequestContext(tenantA, Guid.NewGuid(), [ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        // Tenant B request with identical barcode/metadata
        var resultB = await service.ExternalLookupBarcodeAsync(
            new TenantRequestContext(tenantB, Guid.NewGuid(), [ProductConstants.CreatePermission]),
            new ExternalLookupProductBarcodeRequest { Barcode = "5449000000996" },
            CancellationToken.None);

        Assert.True(resultA.IsSuccess);
        Assert.True(resultB.IsSuccess);
        Assert.Equal("Soft Drinks", resultA.Value!.CategoryResolution!.MappedCategory!.Name);
        Assert.Equal(catA, resultA.Value.CategoryResolution.MappedCategory.Id);

        Assert.Equal("Beverages", resultB.Value!.CategoryResolution!.MappedCategory!.Name);
        Assert.Equal(catB, resultB.Value.CategoryResolution.MappedCategory.Id);
    }

    [Fact]
    public async Task ResolveBarcodeAsync_NeverInvokesExternalCoordinator()
    {
        var repository = new FakeTenantAdminProductRepository();
        var coordinator = new FakeExternalProductLookupCoordinator
        {
            Result = new ExternalProductLookupResult(
                ExternalProductLookupStatuses.Found,
                new ExternalProductSuggestion("X", null, null, null, null, null, null, null, null, null, null),
                "should-not-leak",
                false),
        };
        var service = CreateService(repository, coordinator);

        var result = await service.ResolveBarcodeAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new ResolveProductBarcodeRequest { Barcode = "4006381333931", InputMode = "SCAN" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("VALID_NO_LOCAL_MATCH", result.Value!.Outcome);
        Assert.Equal(0, coordinator.CallCount);
        Assert.Equal(1, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task ListAsync_IncludesResolvedPrimaryImageUrl()
    {
        var productId = Guid.NewGuid();
        const string imageUrl = "https://cdn.example.test/product.png";
        var productRepository = new FakeProductRepository
        {
            ListResponse = new ProductListResponse(
                [
                    new ProductSummaryResponse(
                        productId,
                        "PROD-001",
                        "Image Product",
                        ProductConstants.ActiveStatus,
                        "SKU-001",
                        null,
                        10m,
                        DateTimeOffset.UtcNow,
                        null),
                ],
                1,
                20,
                1),
        };
        // Setup test values
        var listResponse = new TenantAdminProductListResponse(
            [
                new TenantAdminProductListItemResponse(
                    productId,
                    "PROD-001",
                    "Image Product",
                    imageUrl,
                    "SKU-001",
                    null,
                    Guid.NewGuid(),
                    "CategoryName",
                    Guid.NewGuid(),
                    "BrandName",
                    1,
                    10m,
                    12m,
                    "LKR",
                    5m,
                    "ACTIVE",
                    "IN_STOCK",
                    1,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow)
            ],
            PageNumber: 1,
            PageSize: 10,
            TotalCount: 1,
            TotalPages: 1,
            HasPreviousPage: false,
            HasNextPage: false,
            CatalogTotalCount: 1);

        var tenantAdminRepository = new FakeTenantAdminProductRepository
        {
            ListResponse = listResponse,
            PrimaryImageUrls = new Dictionary<Guid, string>
            {
                [productId] = imageUrl,
            },
        };
        var service = CreateService(tenantAdminRepository, productRepository);

        var result = await service.ListAsync(
            CreateContext([TenantAdminProductPermissions.View]),
            search: null,
            categoryId: null,
            brandId: null,
            productStatus: null,
            stockStatus: null,
            pageNumber: 1,
            pageSize: 10,
            sortBy: null,
            sortDirection: null,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(productId, item.Id);
        Assert.Equal(imageUrl, item.ImageUrl);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetCreateOptionsAsync(CreateContext([]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_WithLegacyTenantProductsCreate_MapsToCatalogCreate_AndReturnsOptions()
    {
        var createOptions = new TenantAdminProductCreateOptionsResponse(
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
        var repository = new FakeTenantAdminProductRepository
        {
            CreateOptions = createOptions,
        };
        var service = CreateService(repository);

        var result = await service.GetCreateOptionsAsync(
            CreateContext([TenantAdminProductPermissions.Create]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_WithCatalogProductsCreate_ReturnsOptions()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.GetCreateOptionsAsync(
            CreateContext([ProductConstants.CreatePermission]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!.Categories);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_DoesNotRequireCategoryViewPermission()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            CreateOptions = new TenantAdminProductCreateOptionsResponse(
                [new TenantAdminProductCategoryOptionResponse(Guid.NewGuid(), "FOOD", "Food", null, 1, "Food", true, 1)],
                [],
                [],
                [],
                [],
                [],
                [],
                [])
        };

        var result = await CreateService(repository).GetCreateOptionsAsync(
            CreateContext([ProductConstants.CreatePermission]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("FOOD", Assert.Single(result.Value!.Categories).CategoryCode);
    }

    [Fact]
    public async Task UpdateDraftAsync_UnchangedInactiveCategoryMapping_RemainsValid()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ActiveCategoryExists = false,
            ExistingMappingCategoryExists = true,
            SetupDto = CreateSetup(productId, categoryId)
        };

        var result = await CreateService(repository).UpdateDraftAsync(
            CreateContext([ProductConstants.UpdatePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = ProductWizardStage.BasicDetails,
                ProductName = "Draft Product",
                CategoryId = categoryId
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateDraftAsync_NewInactiveCategorySelection_IsRejected()
    {
        var productId = Guid.NewGuid();
        var existingCategoryId = Guid.NewGuid();
        var replacementCategoryId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ActiveCategoryExists = false,
            ExistingMappingCategoryExists = true,
            SetupDto = CreateSetup(productId, existingCategoryId)
        };

        var result = await CreateService(repository).UpdateDraftAsync(
            CreateContext([ProductConstants.UpdatePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = ProductWizardStage.BasicDetails,
                ProductName = "Draft Product",
                CategoryId = replacementCategoryId
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithoutCreatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.CreateAsync(
            CreateContext([]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutViewPermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetByIdAsync(
            CreateContext([]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ReturnsNotFound()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetByIdAsync(
            CreateContext([TenantAdminProductPermissions.View]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WithTenantProductsDetailsView_ReturnsDetail()
    {
        var productId = Guid.NewGuid();
        var detail = CreateDetailResponse(productId);
        var repository = new FakeTenantAdminProductRepository { Detail = detail };
        var service = CreateService(repository);

        var result = await service.GetByIdAsync(
            CreateContext([TenantAdminProductPermissions.DetailsView]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(detail, result.Value);
        Assert.Equal(TenantId, repository.LastDetailTenantId);
        Assert.Equal(productId, repository.LastDetailProductId);
    }

    [Fact]
    public async Task GetByIdAsync_WithTenantProductsView_ReturnsDetail()
    {
        var productId = Guid.NewGuid();
        var detail = CreateDetailResponse(productId);
        var repository = new FakeTenantAdminProductRepository { Detail = detail };
        var service = CreateService(repository);

        var result = await service.GetByIdAsync(
            CreateContext([TenantAdminProductPermissions.View]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(detail, result.Value);
    }

    [Fact]
    public async Task UpdateAsync_WithoutUpdatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateAsync(
            CreateContext([]),
            Guid.NewGuid(),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ReturnsNotFound()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            Guid.NewGuid(),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.not_found", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithDiscountAboveSellingPrice_ReturnsValidationFailed()
    {
        var productId = Guid.NewGuid();
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(new FakeTenantAdminProductRepository(), productRepository);

        var request = CreateValidRequest();
        request.SellingPrice = 10m;
        request.DiscountPrice = 15m;

        var result = await service.UpdateAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "discountPrice");
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateSkuOnOtherProduct_ReturnsDuplicateSku()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository { SkuExistsOnOtherProduct = true };
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(repository, productRepository);

        var result = await service.UpdateAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.duplicate_sku", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ReturnsUpdatedDetail()
    {
        var productId = Guid.NewGuid();
        var detail = CreateDetailResponse(productId);
        var repository = new FakeTenantAdminProductRepository { UpdateDetail = detail };
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(repository, productRepository);

        var request = CreateValidRequest();
        var result = await service.UpdateAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(detail, result.Value);
        Assert.Equal(productId, repository.LastUpdateProductId);
        Assert.Same(request, repository.LastUpdateRequest);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithoutUpdatePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateStatusAsync(
            CreateContext([]),
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidStatus_ReturnsValidationFailed()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Deleted" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "status");
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenProductNotFound_ReturnsNotFound()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            Guid.NewGuid(),
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.not_found", result.Error.Code);
    }

    [Fact]
    public async Task UpdateStatusAsync_ActivateWithMissingFields_ReturnsValidationFailed()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ActivationSnapshot = new TenantAdminProductActivationSnapshot(
                string.Empty,
                null,
                false,
                0m,
                string.Empty),
        };
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(repository, productRepository);

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            new TenantAdminProductStatusUpdateRequest { Status = "Active" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "productName");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidInactiveStatus_ReturnsUpdatedStatus()
    {
        var productId = Guid.NewGuid();
        var response = new TenantAdminProductStatusUpdateResponse(productId, ProductConstants.InactiveStatus);
        var repository = new FakeTenantAdminProductRepository { StatusUpdateResponse = response };
        var productRepository = new FakeProductRepository { ExistingProductIds = [productId] };
        var service = CreateService(repository, productRepository);

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            new TenantAdminProductStatusUpdateRequest { Status = "Inactive" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(productId, result.Value!.ProductId);
        Assert.Equal(ProductConstants.InactiveStatus, result.Value.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_ActivateWithCompleteProduct_ReturnsActiveStatus()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ActivationSnapshot = new TenantAdminProductActivationSnapshot(
                "Sample Product",
                "SKU-001",
                true,
                10m,
                "PIECE"),
            StatusUpdateResponse = new TenantAdminProductStatusUpdateResponse(
                productId,
                ProductConstants.ActiveStatus),
        };
        var service = CreateService(repository);

        var result = await service.UpdateStatusAsync(
            CreateContext([TenantAdminProductPermissions.Update]),
            productId,
            new TenantAdminProductStatusUpdateRequest { Status = "Active" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductConstants.ActiveStatus, result.Value!.Status);
    }

    [Fact]
    public async Task DeleteAsync_WithoutDeletePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.DeleteAsync(
            CreateContext([]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ReturnsNotFound()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(null, "product.not_found"),
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.not_found", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenAlreadyDeleted_ReturnsDeleteBlocked()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(null, "product.delete_blocked"),
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.delete_blocked", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WithHistory_ArchivesProduct()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(
                new TenantAdminProductDeleteResponse(
                    productId,
                    "Archived",
                    ProductConstants.ArchivedStatus),
                null),
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Archived", result.Value!.Outcome);
        Assert.Equal(ProductConstants.ArchivedStatus, result.Value.Status);
    }

    [Fact]
    public async Task DeleteAsync_WithoutHistory_SoftDeletesProduct()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(
                new TenantAdminProductDeleteResponse(
                    productId,
                    "Deleted",
                    ProductConstants.ArchivedStatus),
                null),
        };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Deleted", result.Value!.Outcome);
        Assert.Equal(ProductConstants.ArchivedStatus, result.Value.Status);
    }

    [Fact]
    public async Task DeleteAsync_WithSuccess_LogsProductDeletedAudit()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            DeleteOperationResult = new TenantAdminProductDeleteOperationResult(
                new TenantAdminProductDeleteResponse(
                    productId,
                    "Deleted",
                    ProductConstants.ArchivedStatus),
                null),
        };
        var auditLogger = new FakeTenantAdminProductAuditLogger();
        var service = CreateService(repository, auditLogger: auditLogger);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminProductPermissions.Delete]),
            productId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(auditLogger.ProductDeletedLogged);
        Assert.Equal(productId, auditLogger.LastProductId);
        Assert.Equal("Deleted", auditLogger.LastOutcome);
        Assert.Equal(ProductConstants.ArchivedStatus, auditLogger.LastStatus);
    }

    [Fact]
    public async Task GetDashboardAsync_WithoutDashboardPermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.GetDashboardAsync(
            CreateContext([TenantAdminProductPermissions.View]),
            new TenantAdminProductDashboardQuery(
                null,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.permission_denied", result.Error?.Code);
    }

    [Fact]
    public async Task GetDashboardAsync_WithDashboardPermission_OmitsUnauthorizedSections()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            DashboardRaw = SampleDashboardRaw(),
        };
        var service = CreateService(repository);

        var result = await service.GetDashboardAsync(
            CreateContext([
                TenantAdminProductPermissions.DashboardView,
                TenantAdminProductPermissions.View,
            ]),
            new TenantAdminProductDashboardQuery(
                null,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value?.Summary.TotalProducts);
        Assert.Null(result.Value?.Summary.LowStock);
        Assert.Null(result.Value?.StockValue);
        Assert.Null(result.Value?.StockMovement);
    }

    [Fact]
    public async Task GetDashboardAsync_WithStockPermissions_ReturnsStockSections()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            DashboardRaw = SampleDashboardRaw(),
        };
        var service = CreateService(repository);

        var result = await service.GetDashboardAsync(
            CreateContext([
                TenantAdminProductPermissions.DashboardView,
                StockPermissions.View,
                StockPermissions.ValueView,
                StockPermissions.MovementsView,
            ]),
            new TenantAdminProductDashboardQuery(
                null,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value?.Summary.LowStock);
        Assert.NotNull(result.Value?.StockValue);
        Assert.NotNull(result.Value?.StockMovement);
    }

    [Fact]
    public async Task GetDashboardAsync_WithInvalidOutlet_ReturnsValidationFailed()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            OutletsBelong = false,
            DashboardRaw = SampleDashboardRaw(),
        };
        var service = CreateService(repository);

        var result = await service.GetDashboardAsync(
            CreateContext([TenantAdminProductPermissions.DashboardView]),
            new TenantAdminProductDashboardQuery(
                Guid.NewGuid(),
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.dashboard.invalid_outlet", result.Error?.Code);
    }

    private static TenantAdminProductDashboardRawData SampleDashboardRaw() =>
        new(
            "USD",
            new TenantAdminProductDashboardRawMetric(10, 8),
            new TenantAdminProductDashboardRawMetric(2, 1),
            new TenantAdminProductDashboardRawMetric(1, 0),
            new TenantAdminProductDashboardRawMetric(3, 2),
            new TenantAdminProductDashboardRawMetric(15, 12),
            new TenantAdminProductDashboardRawMetric(4, 3),
            1000m,
            900m,
            [new TenantAdminProductDashboardRawStockValuePoint(DateOnly.FromDateTime(DateTime.UtcNow), 1000m)],
            [new("stock_in", 5), new("stock_out", 2), new("adjustment", 1), new("transfer", 0)]);

    [Fact]
    public async Task CreateAsync_WithMissingRequiredFields_ReturnsValidationFailed()
    {
        var service = CreateService(new FakeTenantAdminProductRepository());

        var result = await service.CreateAsync(
            CreateContext([TenantAdminProductPermissions.Create]),
            new TenantAdminProductCreateRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "productName");
        Assert.Contains(result.Error.FieldErrors!, error => error.Field == "sku");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSku_ReturnsDuplicateSku()
    {
        var productRepository = new FakeProductRepository { ExistingSkus = ["SKU-001"] };
        var service = CreateService(new FakeTenantAdminProductRepository(), productRepository);

        var result = await service.CreateAsync(
            CreateContext([TenantAdminProductPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.duplicate_sku", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsCreatedProduct()
    {
        var categoryId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            ResolvedUnitId = unitId,
            CreateResponse = new TenantAdminProductCreateResponse(
                productId,
                "Sample Product",
                "SKU-001",
                ProductConstants.ActiveStatus),
        };
        var service = CreateService(repository);

        var request = CreateValidRequest(categoryId);
        var result = await service.CreateAsync(
            CreateContext([TenantAdminProductPermissions.Create]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(productId, result.Value!.ProductId);
        Assert.Equal(TenantId, repository.LastCreateTenantId);
        Assert.Equal(UserId, repository.LastCreateUserId);
        Assert.Equal(unitId, repository.LastCreateUnitId);
        Assert.Same(request, repository.LastCreateRequest);
    }

    private static TenantAdminProductCreateRequest CreateValidRequest(Guid? categoryId = null) =>
        new()
        {
            ProductName = "Sample Product",
            Sku = "SKU-001",
            CategoryId = categoryId ?? Guid.NewGuid(),
            UnitType = "PIECE",
            SellingPrice = 10m,
            Status = ProductConstants.ActiveStatus,
        };

    [Fact]
    public async Task UpdateDraftAsync_BarcodeSkuContinue_FailsWhenClientOmitsSellableTargets()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        var v3 = Guid.NewGuid();
        var v4 = Guid.NewGuid();
        var v5 = Guid.NewGuid();
        var v6 = Guid.NewGuid();

        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, categoryId) with { ProductStructure = ProductStructureConstants.Variant },
            Step5Targets =
            [
                new(v1, "V1", null, "k1"),
                new(v2, "V2", null, "k2"),
                new(v3, "V3", null, "k3"),
                new(v4, "V4", null, "k4"),
                new(v5, "V5", null, "k5"),
                new(v6, "V6", null, "k6"),
            ],
        };

        var result = await CreateService(repository).UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission,
                ProductConstants.VariantsManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = ProductWizardStage.BarcodeSku,
                WizardAction = "SAVE_AND_CONTINUE",
                AdvanceStep = true,
                ProductStructure = ProductStructureConstants.Variant,
                ExpectedRowVersion = 1,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [
                        new BarcodeSkuAssignmentDto(v1, "V1", "SKU-1", null, null, "k1"),
                        new BarcodeSkuAssignmentDto(v2, "V2", "SKU-2", null, null, "k2"),
                    ]),
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.barcode_sku_validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, e =>
            e.Field == "barcodeSkuConfiguration.assignments" &&
            e.Message.Contains("SKU is required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UpdateDraftAsync_BarcodeSku_RejectsForeignProductVariantId()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var owned = Guid.NewGuid();
        var foreign = Guid.NewGuid();

        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, categoryId) with { ProductStructure = ProductStructureConstants.Variant },
            Step5Targets = [new(owned, "Owned", null, "k1")],
        };

        var result = await CreateService(repository).UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission,
                ProductConstants.VariantsManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = ProductWizardStage.BarcodeSku,
                WizardAction = "SAVE_DRAFT",
                ProductStructure = ProductStructureConstants.Variant,
                ExpectedRowVersion = 1,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [
                        new BarcodeSkuAssignmentDto(foreign, "Injected", "SKU-X", null, null, "kx"),
                    ]),
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.barcode_sku_validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!, e =>
            e.Field.EndsWith(".productVariantId", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateDraftAsync_BarcodeSku_DetectsBulkSkuConflicts_CaseSensitive()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, categoryId) with { ProductStructure = ProductStructureConstants.Variant },
            Step5Targets = [new(variantId, "Owned", null, "k1")],
            SkuConflicts = new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                ["SKU-Exact"] = Guid.NewGuid(),
            },
        };

        var conflictResult = await CreateService(repository).UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission,
                ProductConstants.VariantsManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = ProductWizardStage.BarcodeSku,
                WizardAction = "SAVE_DRAFT",
                ProductStructure = ProductStructureConstants.Variant,
                ExpectedRowVersion = 1,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [
                        new BarcodeSkuAssignmentDto(variantId, "Owned", "SKU-Exact", null, null, "k1"),
                    ]),
            },
            CancellationToken.None);

        Assert.True(conflictResult.IsFailure);
        Assert.Contains(conflictResult.Error.FieldErrors!, e => e.Field.EndsWith(".sku", StringComparison.Ordinal));

        // Different case must not collide under Ordinal.
        repository.SkuConflicts = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["SKU-Exact"] = Guid.NewGuid(),
        };
        var okResult = await CreateService(repository).UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission,
                ProductConstants.VariantsManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = ProductWizardStage.BarcodeSku,
                WizardAction = "SAVE_DRAFT",
                ProductStructure = ProductStructureConstants.Variant,
                ExpectedRowVersion = 1,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [
                        new BarcodeSkuAssignmentDto(variantId, "Owned", "sku-exact", null, null, "k1"),
                    ]),
            },
            CancellationToken.None);

        Assert.True(okResult.IsSuccess);
    }

    private static ProductSetupWizardDto CreateSetup(Guid productId, Guid categoryId) =>
        new(
            productId,
            "Draft Product",
            "DRF-001",
            ProductConstants.DraftStatus,
            ProductConstants.ActiveStatus,
            1,
            DateTimeOffset.UtcNow,
            1,
            categoryId,
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
            []);

    private static TenantAdminProductDetailResponse CreateDetailResponse(Guid productId) =>
        new(
            productId,
            "Sample Product",
            "SKU-001",
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
            ProductConstants.ActiveStatus,
            false,
            null,
            [],
            [],
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private class FakeEntitlementEvaluator : ITenantFeatureEntitlementEvaluator
    {
        public bool IsEntitled { get; set; } = true;

        public Task<TenantFeatureEntitlementEvaluation> EvaluateAsync(Guid tenantId, string featureCode, DateTimeOffset evaluationTime, CancellationToken cancellationToken = default) =>
            Task.FromResult(IsEntitled
                ? TenantFeatureEntitlementEvaluation.Allowed(featureCode, featureCode, false, true, false)
                : TenantFeatureEntitlementEvaluation.Denied(
                    TenantFeatureEntitlementDecision.Disabled,
                    featureCode,
                    featureCode,
                    false,
                    true,
                    false,
                    "Feature disabled"));

        public Task<bool> IsEnabledAsync(Guid tenantId, string featureCode, DateTimeOffset evaluationTime, CancellationToken cancellationToken = default) =>
            Task.FromResult(IsEntitled);
    }

    private static TenantAdminProductService CreateService(
        ITenantAdminProductRepository tenantAdminProductRepository,
        FakeProductRepository? productRepository = null,
        ITenantAdminProductAuditLogger? auditLogger = null,
        FakeEntitlementEvaluator? entitlementEvaluator = null,
        IExternalProductLookupCoordinator? externalProductLookupCoordinator = null,
        ITenantExternalCategoryResolver? tenantExternalCategoryResolver = null,
        ITenantExternalBrandResolver? tenantExternalBrandResolver = null,
        Microsoft.Extensions.Options.IOptions<E_POS.Application.Modules.Tenant.CatalogProduct.Options.ExternalProductLookupOptions>? externalProductLookupOptions = null)
    {
        var clock = new FakeDateTimeProvider();
        var accessPolicy = new ProductWizardAccessPolicy(
            entitlementEvaluator ?? new FakeEntitlementEvaluator(),
            tenantAdminProductRepository,
            clock);
        // Default: no configured providers -> allowlist enforcement is a no-op (existing tests are
        // unaffected). Tests exercising the allowlist explicitly pass a populated options instance.
        var lookupOptions = externalProductLookupOptions
            ?? Microsoft.Extensions.Options.Options.Create(new E_POS.Application.Modules.Tenant.CatalogProduct.Options.ExternalProductLookupOptions());
        return new TenantAdminProductService(
            productRepository ?? new FakeProductRepository(),
            tenantAdminProductRepository,
            new TenantAdminProductRequestValidator(lookupOptions),
            clock,
            auditLogger ?? new FakeTenantAdminProductAuditLogger(),
            accessPolicy,
            new ProductVariantGenerationService(),
            externalProductLookupCoordinator ?? new FakeExternalProductLookupCoordinator(),
            tenantExternalCategoryResolver ?? new FakeTenantExternalCategoryResolver(),
            tenantExternalBrandResolver ?? new FakeTenantExternalBrandResolver(),
            lookupOptions);
    }

    private static TenantAdminProductService CreateService(
        ITenantAdminProductRepository tenantAdminProductRepository,
        FakeExternalProductLookupCoordinator externalProductLookupCoordinator,
        FakeEntitlementEvaluator? entitlementEvaluator = null,
        ITenantExternalCategoryResolver? tenantExternalCategoryResolver = null,
        ITenantExternalBrandResolver? tenantExternalBrandResolver = null) =>
        CreateService(
            tenantAdminProductRepository,
            productRepository: null,
            auditLogger: null,
            entitlementEvaluator: entitlementEvaluator,
            externalProductLookupCoordinator: externalProductLookupCoordinator,
            tenantExternalCategoryResolver: tenantExternalCategoryResolver,
            tenantExternalBrandResolver: tenantExternalBrandResolver);

    private static TenantRequestContext CreateContext(string[] permissions) =>
        new(TenantId, UserId, permissions);

    private sealed class FakeTenantAdminProductRepository : ITenantAdminProductRepository
    {
        public TenantAdminProductSummaryResponse Summary { get; init; } =
            new(0, 0, 0, 0);

        public TenantAdminProductCreateOptionsResponse CreateOptions { get; init; } =
            new TenantAdminProductCreateOptionsResponse([], [], [], [], [], [], [], []);

        public IReadOnlyDictionary<Guid, string> PrimaryImageUrls { get; init; } =
            new Dictionary<Guid, string>();

        public Task<TenantAdminProductSummaryResponse> GetSummaryAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Summary);

        public Task<TenantAdminProductCreateOptionsResponse> GetCreateOptionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateOptions);

        public TenantAdminProductListResponse ListResponse { get; init; } =
            new([], 1, 10, 0, 0, false, false, 0);

        public Task<TenantAdminProductListResponse> GetPagedListAsync(
            Guid tenantId,
            string? search,
            Guid? categoryId,
            Guid? brandId,
            string? productStatus,
            string? stockStatus,
            int pageNumber,
            int pageSize,
            string? sortBy,
            string? sortDirection,
            bool canViewStock,
            CancellationToken cancellationToken) =>
            Task.FromResult(ListResponse);

        public Task<TenantAdminProductFilterOptionsResponse> GetFilterOptionsAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new TenantAdminProductFilterOptionsResponse([], [], [], []));

        public Task<IReadOnlyDictionary<Guid, string>> GetPrimaryCategoryNamesAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyDictionary<Guid, string>> GetPrimaryImageUrlsAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(PrimaryImageUrls);

        public Guid? ResolvedUnitId { get; init; } = Guid.NewGuid();

        public bool CategoryBelongs { get; init; } = true;

        public bool BrandBelongs { get; init; } = true;

        public bool TaxClassBelongs { get; init; } = true;

        public bool OutletsBelong { get; init; } = true;

        public TenantAdminProductCreateResponse CreateResponse { get; init; } =
            new(Guid.NewGuid(), "Sample Product", "SKU-001", ProductConstants.ActiveStatus);

        public TenantAdminProductDetailResponse? Detail { get; init; }

        public TenantAdminProductDetailResponse? UpdateDetail { get; init; }

        public TenantAdminProductStatusUpdateResponse? StatusUpdateResponse { get; init; }

        public TenantAdminProductActivationSnapshot? ActivationSnapshot { get; init; }

        public TenantAdminProductDeleteOperationResult DeleteOperationResult { get; init; } =
            new(null, "product.not_found");

        public TenantAdminProductDashboardRawData DashboardRaw { get; init; } =
            new(
                "USD",
                new(0, 0),
                new(0, 0),
                new(0, 0),
                new(0, 0),
                new(0, 0),
                new(0, 0),
                0,
                0,
                [],
                []);

        public bool SkuExistsOnOtherProduct { get; init; }

        public bool BarcodeExistsOnOtherProduct { get; init; }

        public Guid? LastUpdateProductId { get; private set; }

        public TenantAdminProductCreateRequest? LastUpdateRequest { get; private set; }

        public Guid? LastCreateTenantId { get; private set; }

        public Guid? LastCreateUserId { get; private set; }

        public Guid? LastCreateUnitId { get; private set; }

        public TenantAdminProductCreateRequest? LastCreateRequest { get; private set; }

        public Guid? LastDetailTenantId { get; private set; }

        public Guid? LastDetailProductId { get; private set; }

        public Task<Guid?> ResolveUnitIdAsync(
            Guid tenantId,
            string unitType,
            CancellationToken cancellationToken) =>
            Task.FromResult(ResolvedUnitId);

        public Task<bool> CategoryBelongsToTenantAsync(
            Guid tenantId,
            Guid categoryId,
            Guid? parentCategoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(CategoryBelongs);

        public Task<bool> BrandBelongsToTenantAsync(
            Guid tenantId,
            Guid brandId,
            CancellationToken cancellationToken) =>
            Task.FromResult(BrandBelongs);

        public Task<bool> TaxClassBelongsToTenantAsync(
            Guid tenantId,
            Guid taxClassId,
            CancellationToken cancellationToken) =>
            Task.FromResult(TaxClassBelongs);

        public Task<bool> OutletsBelongToTenantAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> outletIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(OutletsBelong);

        public Task<TenantAdminProductCreateResponse> CreateProductAsync(
            Guid tenantId,
            Guid? userId,
            TenantAdminProductCreateRequest request,
            Guid unitId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            LastCreateTenantId = tenantId;
            LastCreateUserId = userId;
            LastCreateUnitId = unitId;
            LastCreateRequest = request;
            return Task.FromResult(CreateResponse);
        }

        public Task<TenantAdminProductDetailResponse?> GetDetailAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken)
        {
            LastDetailTenantId = tenantId;
            LastDetailProductId = productId;
            return Task.FromResult(Detail);
        }

        public Task<bool> SkuExistsOnOtherProductAsync(
            Guid tenantId,
            string sku,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SkuExistsOnOtherProduct);

        public Task<bool> BarcodeExistsOnOtherProductAsync(
            Guid tenantId,
            string barcode,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(BarcodeExistsOnOtherProduct);

        public Task<TenantAdminProductDetailResponse?> UpdateProductAsync(
            Guid tenantId,
            Guid userId,
            Guid productId,
            TenantAdminProductCreateRequest request,
            Guid unitId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            LastUpdateProductId = productId;
            LastUpdateRequest = request;
            return Task.FromResult(UpdateDetail);
        }

        public Task<TenantAdminProductActivationSnapshot?> GetActivationSnapshotAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ActivationSnapshot);

        public Task<TenantAdminProductStatusUpdateResponse?> UpdateProductStatusAsync(
            Guid tenantId,
            Guid userId,
            Guid productId,
            string status,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(StatusUpdateResponse);

        public Task<TenantAdminProductDeleteOperationResult> DeleteProductAsync(
            Guid tenantId,
            Guid userId,
            Guid productId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(DeleteOperationResult);

        public Task<TenantAdminProductDeleteHistoryFlags?> GetDeleteHistoryFlagsAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<TenantAdminProductDeleteHistoryFlags?>(null);

        public Task<TenantAdminProductDashboardRawData> GetDashboardAsync(
            Guid tenantId,
            TenantAdminProductDashboardQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(DashboardRaw);

        public bool ActiveCategoryExists { get; init; } = true;
        public bool ExistingMappingCategoryExists { get; init; } = true;

        public bool ProductCodeExists { get; init; }

        public Guid? DefaultInventoryUomId { get; init; } = Guid.NewGuid();

        public SaveProductDraftResult DraftResult { get; init; } =
            SaveProductDraftResult.Success(new ProductDraftResponse(
                Guid.NewGuid(),
                "Draft Product",
                "DRF-001",
                ProductConstants.DraftStatus,
                ProductConstants.DesiredPublishActive,
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
                ProductStructureConstants.Simple,
                false,
                []));

        public ProductSetupWizardDto? SetupDto { get; init; }

        public HashSet<string> ExistingSkus { get; } = new(StringComparer.Ordinal);
        public int SkuExistsCallCount { get; private set; }
        public string? LastSkuExistsQuery { get; private set; }

        public Task<bool> SkuExistsAsync(Guid tenantId, string sku, Guid? excludeProductVariantId = null, CancellationToken cancellationToken = default)
        {
            SkuExistsCallCount++;
            LastSkuExistsQuery = sku;
            return Task.FromResult(ExistingSkus.Contains(sku));
        }

        public Task<bool> BarcodeExistsAsync(Guid tenantId, string barcode, Guid? excludeProductId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public IReadOnlyList<BarcodeSkuVariantTargetProjection> Step5Targets { get; set; } =
            Array.Empty<BarcodeSkuVariantTargetProjection>();

        public IReadOnlyDictionary<string, Guid> SkuConflicts { get; set; } =
            new Dictionary<string, Guid>();

        public IReadOnlyDictionary<string, Guid> BarcodeConflicts { get; set; } =
            new Dictionary<string, Guid>();

        public Task<IReadOnlyDictionary<string, Guid>> FindSkuConflictsAsync(
            Guid tenantId,
            IReadOnlyCollection<string> skus,
            IReadOnlyCollection<Guid> excludeVariantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(SkuConflicts);

        public Task<IReadOnlyDictionary<string, Guid>> FindBarcodeConflictsAsync(
            Guid tenantId,
            IReadOnlyCollection<string> barcodes,
            IReadOnlyCollection<Guid> excludeVariantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(BarcodeConflicts);

        public Task<IReadOnlyList<BarcodeSkuVariantTargetProjection>> GetStep5SellableVariantTargetsAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Step5Targets);

        public Task<bool> ProductSlugExistsAsync(string slug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> IsValidBrandAsync(Guid brandId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<string?> GetTenantStatusAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("ACTIVE");

        public Task<bool> IsInitialCreationDraftAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public bool HasScanContext { get; set; }

        public Task<bool> HasScanContextAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult(HasScanContext);

        public Task<bool> IsCategoryEffectivelySelectableAsync(
            Guid tenantId,
            Guid categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ActiveCategoryExists);

        public Task<bool> ActiveCategoryExistsAsync(
            Guid tenantId,
            Guid categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ActiveCategoryExists);

        public string? ActiveCategoryCode { get; set; } = "TSH";
        public long NextProductSkuSequence { get; set; } = 125;
        public string? GeneratedSkuBase { get; set; }

        public Task<string?> GetActiveCategoryCodeAsync(
            Guid tenantId,
            Guid categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ActiveCategoryCode);

        public Task<long> AllocateNextProductSkuSequenceAsync(
            Guid tenantId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(NextProductSkuSequence++);

        public Task<string?> GetGeneratedSkuBaseAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(GeneratedSkuBase);

        public Task<ApplicationError?> ReplaceGeneratedSkuBaseAsync(
            Guid tenantId,
            Guid userId,
            Guid productId,
            long expectedRowVersion,
            string generatedSkuBase,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            GeneratedSkuBase = generatedSkuBase;
            return Task.FromResult<ApplicationError?>(null);
        }

        public Task<bool> CategoryExistsForExistingMappingAsync(
            Guid tenantId,
            Guid categoryId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExistingMappingCategoryExists);

        public Task<bool> ProductCodeExistsAsync(
            Guid tenantId,
            string productCode,
            Guid? excludeProductId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ProductCodeExists);

        public Task<Guid?> GetDefaultInventoryUomIdAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(DefaultInventoryUomId);

        public Task<bool> HasOperationalHistoryAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task SaveVariantsAsync(
            Guid tenantId,
            Guid productId,
            VariantConfigurationDto variantConfiguration,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ApplicationFieldError>> ValidateVariantConfigurationCatalogAsync(
            Guid tenantId,
            Guid? productId,
            VariantConfigurationDto configuration,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ApplicationFieldError>>([]);

        public int CreateProductCallCount { get; private set; }
        public int SaveDraftCallCount { get; private set; }
        public TenantAdminWizardProductCreateRequest? LastWizardRequest { get; private set; }
        public SaveProductDraftResult WizardCreateResult { get; set; } =
            SaveProductDraftResult.Failure(new ApplicationError("not_implemented", "Fake repository"));

        public Task<SaveProductDraftResult> CreateProductFromWizardAsync(
            Guid tenantId,
            Guid userId,
            TenantAdminWizardProductCreateRequest request,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CreateProductCallCount++;
            LastWizardRequest = request;
            return Task.FromResult(WizardCreateResult);
        }

        public Task<SaveProductDraftResult> SaveProductDraftAsync(
            Guid tenantId,
            Guid userId,
            SaveProductDraftCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            SaveDraftCallCount++;
            LastSaveDraftCommand = command;
            if (DraftResult.IsSuccess && DraftResult.Response is not null && command.ScanBootstrap is not null)
            {
                var baseline = DraftResult.Response;
                return Task.FromResult(SaveProductDraftResult.Success(baseline with
                {
                    CurrentSetupStep = command.TargetSetupStep,
                    Status = ProductConstants.DraftStatus,
                    ProductName = command.ProductName,
                    ShortDescription = command.ShortDescription,
                    LongDescription = command.LongDescription
                }));
            }

            return Task.FromResult(DraftResult);
        }

        public SaveProductDraftCommand? LastSaveDraftCommand { get; private set; }

        public Task<ProductSetupWizardDto?> GetSetupAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SetupDto);
        public Task UpdateVariantAsync(Guid tenantId, Guid userId, Guid productId, Guid variantId, TenantAdminProductVariantUpdateRequest request, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task AddBarcodeAsync(Guid tenantId, Guid userId, Guid productId, Guid variantId, TenantAdminProductBarcodeAddRequest request, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteBarcodeAsync(Guid tenantId, Guid userId, Guid productId, Guid variantId, Guid barcodeId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RestoreAsync(Guid tenantId, Guid userId, Guid productId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<TenantAdminProductCreateResponse> DuplicateAsync(Guid tenantId, Guid userId, Guid productId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(new TenantAdminProductCreateResponse(Guid.NewGuid(), "Duplicate", "DUP-001", "DRAFT"));

        public ProductBarcodeResolveMatchProjection? ResolveMatch { get; set; }
        public string? LastResolvedBarcode { get; private set; }
        public int ResolveLookupCallCount { get; private set; }

        public Task<ProductBarcodeResolveMatchProjection?> FindBarcodeResolveMatchAsync(
            Guid tenantId,
            string normalizedBarcode,
            CancellationToken cancellationToken)
        {
            ResolveLookupCallCount++;
            LastResolvedBarcode = normalizedBarcode;
            return Task.FromResult(ResolveMatch);
        }

        public Task<IReadOnlyList<BundleValidationProductProjection>> GetProductsForBundleValidationAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BundleValidationProductProjection>>([]);

        public Task<IReadOnlyList<BundleValidationVariantProjection>> GetVariantsForBundleValidationAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> variantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BundleValidationVariantProjection>>([]);

        public Task<IReadOnlyList<BundleValidationUomProjection>> GetComponentUomValidationDataAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> componentProductIds,
            IReadOnlyCollection<Guid> componentVariantIds,
            IReadOnlyCollection<Guid> componentUomIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BundleValidationUomProjection>>([]);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; init; } = DateTimeOffset.UtcNow;
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public IReadOnlyCollection<string> ExistingSkus { get; init; } = [];

        public IReadOnlyCollection<string> ExistingBarcodes { get; init; } = [];

        public IReadOnlyCollection<Guid> ExistingProductIds { get; init; } = [];

        public ProductListResponse? ListResponse { get; init; }

        public Task<bool> ProductCodeExistsAsync(
            Guid tenantId,
            string productCode,
            Guid? excludeProductId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> SkuExistsAsync(
            Guid tenantId,
            string sku,
            Guid? excludeProductId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExistingSkus.Contains(sku, StringComparer.OrdinalIgnoreCase));

        public Task<bool> BarcodeExistsAsync(
            Guid tenantId,
            string barcodeValue,
            Guid? excludeProductId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExistingBarcodes.Contains(barcodeValue, StringComparer.OrdinalIgnoreCase));

        public Task<ProductListResponse> ListAsync(
            Guid tenantId,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken cancellationToken) =>
            Task.FromResult(ListResponse ?? new ProductListResponse([], pageNumber, pageSize, 0));

        public Task<ProductResponse?> GetByIdAsync(
            Guid tenantId,
            Guid productId,
            bool includeDeleted,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProductResponse?>(null);

        public Task<Product?> GetEditableAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Product?>(null);

        public Task AddAsync(Product product, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddBarcodeAsync(ProductBarcode barcode, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddCategoryLinksAsync(
            IEnumerable<ProductCategory> links,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddCollectionLinksAsync(
            IEnumerable<ProductCollection> links,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddImagesAsync(IEnumerable<ProductImage> images, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddMediaAssetsAsync(IEnumerable<E_POS.Domain.Modules.Shared.Media.Entities.MediaAsset> mediaAssets, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddChannelVisibilitiesAsync(
            IEnumerable<ProductChannelVisibility> visibilities,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task AddPriceListItemAsync(PriceListItem priceListItem, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Guid>> GetProductImageMediaAssetIdsAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());

        public Task ClearProductMappingsAsync(Guid tenantId, Guid productId, bool clearImages, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task MarkMediaAssetsInactiveAsync(Guid tenantId, IReadOnlyCollection<Guid> mediaAssetIds, Guid? updatedByTenantUserId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> GetDefaultPriceListIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<Guid?>(null);

        public Task<ProductVariant?> GetDefaultVariantAsync(
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProductVariant?>(null);

        public Task<PriceListItem?> GetPriceListItemAsync(
            Guid priceListId,
            Guid variantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PriceListItem?>(null);

        public Task<ProductBarcode?> GetBarcodeAsync(Guid variantId, CancellationToken cancellationToken) =>
            Task.FromResult<ProductBarcode?>(null);

        public Task<bool> ProductExistsAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExistingProductIds.Contains(productId));

        public Task<bool> ProductVariantExistsAsync(
            Guid tenantId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> ProductIsPriceableAsync(
            Guid tenantId,
            Guid productId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExistingProductIds.Contains(productId));

        public Task<bool> ProductVariantIsPriceableAsync(
            Guid tenantId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class FakeTenantAdminProductAuditLogger : ITenantAdminProductAuditLogger
    {
        public bool ProductDeletedLogged { get; private set; }

        public Guid LastProductId { get; private set; }

        public string LastOutcome { get; private set; } = string.Empty;

        public string LastStatus { get; private set; } = string.Empty;

        public void LogProductDeleted(
            Guid tenantId,
            Guid userId,
            Guid productId,
            string outcome,
            string status)
        {
            ProductDeletedLogged = true;
            LastProductId = productId;
            LastOutcome = outcome;
            LastStatus = status;
        }

        public void LogStep2DraftUpdated(
            Guid tenantId, Guid userId, Guid productId, string oldStructure, string newStructure,
            bool oldTrackInventory, bool newTrackInventory, bool oldBatchTracking, bool newBatchTracking,
            bool oldExpiryTracking, bool newExpiryTracking, bool oldSerialTracking, bool newSerialTracking,
            long rowVersion)
        {
        }
    }

    private sealed class FakeExternalProductLookupCoordinator : IExternalProductLookupCoordinator
    {
        public int CallCount { get; private set; }
        public ExternalProductLookupRequest? LastRequest { get; private set; }
        public ExternalProductLookupResult Result { get; init; } =
            new(ExternalProductLookupStatuses.NoMatch, null, null, false);
        public bool ThrowOnCancel { get; init; }

        public Task<ExternalProductLookupResult> LookupAsync(
            ExternalProductLookupRequest request,
            CancellationToken cancellationToken)
        {
            if (ThrowOnCancel)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            CallCount++;
            LastRequest = request;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeTenantExternalCategoryResolver : ITenantExternalCategoryResolver
    {
        public int CallCount { get; private set; }
        public TenantCategoryResolutionRequest? LastRequest { get; private set; }
        public TenantCategoryResolutionResult Result { get; set; } =
            new("openfoodfacts", null, null, null, Array.Empty<TenantCategorySuggestionItem>());

        public Task<TenantCategoryResolutionResult> ResolveAsync(
            TenantCategoryResolutionRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeTenantExternalBrandResolver : ITenantExternalBrandResolver
    {
        public int CallCount { get; private set; }
        public TenantBrandResolutionRequest? LastRequest { get; private set; }
        public TenantBrandResolutionResult Result { get; set; } =
            new("openfoodfacts", null, null, null, Array.Empty<TenantBrandSuggestionItem>());

        public Task<TenantBrandResolutionResult> ResolveAsync(
            TenantBrandResolutionRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(Result);
        }
    }

    private sealed class DelegatingTenantExternalCategoryResolver : ITenantExternalCategoryResolver
    {
        private readonly Func<TenantCategoryResolutionRequest, TenantCategoryResolutionResult> _handler;

        public DelegatingTenantExternalCategoryResolver(Func<TenantCategoryResolutionRequest, TenantCategoryResolutionResult> handler)
        {
            _handler = handler;
        }

        public Task<TenantCategoryResolutionResult> ResolveAsync(
            TenantCategoryResolutionRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
