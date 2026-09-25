using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed partial class TenantAdminProductServiceTests
{
    private static TenantRequestContext CreateFullAdminContext() =>
        new(TenantId, UserId, [
            ProductConstants.CreatePermission,
            ProductConstants.PublishPermission,
            ProductConstants.ViewPermission,
            ProductConstants.UpdatePermission,
            ProductConstants.BarcodesManagePermission,
            ProductConstants.ProductPricingManagePermission,
            ProductConstants.ProductCostViewPermission,
            ProductConstants.ManagePermission,
        ]);

    [Fact]
    public async Task CreateFromWizardAsync_WithInvalidMappingContext_ReturnsValidationErrorWithoutCallingRepository()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "SIMPLE",
            DesiredPublishActive = true,
            ProductUnitId = Guid.NewGuid(),
            BaseUnitId = Guid.NewGuid(),
            UnitModel = "SINGLE_UNIT",
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("", "en:colas", "Colas"), // Empty provider!
        };

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Equal(0, repository.CreateProductCallCount);
    }

    [Fact]
    public async Task CreateFromWizardAsync_WithValidMappingContext_NormalizesContextAndCallsRepository()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            WizardCreateResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                Guid.NewGuid(),
                "Test Product",
                "PC-001",
                ProductConstants.ActiveStatus,
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
                [])),
        };
        var service = CreateService(repository);

        var categoryId = Guid.NewGuid();
        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = categoryId,
            ProductStructure = "SIMPLE",
            DesiredPublishActive = true,
            ProductUnitId = Guid.NewGuid(),
            BaseUnitId = Guid.NewGuid(),
            UnitModel = "SINGLE_UNIT",
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                [
                    new BarcodeSkuAssignmentDto(
                        null,
                        "Simple",
                        "SKU-SIMPLE-001",
                        "4006381333931",
                        null,
                        "SIMPLE_DEFAULT",
                        "EAN13")
                ]),
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("  OPENFOODFACTS  ", "  EN:COLAS  ", "  Colas  "),
        };

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess, $"{result.Error?.Code}: {result.Error?.Message} ({string.Join(", ", result.Error?.FieldErrors?.Select(f => f.Message) ?? [])})");
        Assert.Equal(1, repository.CreateProductCallCount);
        Assert.NotNull(repository.LastWizardRequest);
        Assert.NotNull(repository.LastWizardRequest!.ExternalCategoryMappingContext);
        Assert.Equal("openfoodfacts", repository.LastWizardRequest.ExternalCategoryMappingContext!.Provider);
        Assert.Equal("en:colas", repository.LastWizardRequest.ExternalCategoryMappingContext.ExternalCategoryKey);
        Assert.Equal("Colas", repository.LastWizardRequest.ExternalCategoryMappingContext.ExternalCategoryName);
    }

    [Fact]
    public async Task CreateFromWizardAsync_WithoutMappingContext_SucceedsNormally()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            WizardCreateResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                Guid.NewGuid(),
                "Normal Product",
                "PC-001",
                ProductConstants.ActiveStatus,
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
                [])),
        };
        var service = CreateService(repository);

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Normal Product",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "SIMPLE",
            DesiredPublishActive = true,
            ProductUnitId = Guid.NewGuid(),
            BaseUnitId = Guid.NewGuid(),
            UnitModel = "SINGLE_UNIT",
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                Array.Empty<Step5IdentifierTargetDto>(),
                [
                    new BarcodeSkuAssignmentDto(
                        null,
                        "Simple",
                        "SKU-SIMPLE-002",
                        "4006381333932",
                        null,
                        "SIMPLE_DEFAULT",
                        "EAN13")
                ]),
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            ExternalCategoryMappingContext = null,
        };

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess, $"{result.Error?.Code}: {result.Error?.Message} ({string.Join(", ", result.Error?.FieldErrors?.Select(f => f.Message) ?? [])})");
        Assert.Equal(1, repository.CreateProductCallCount);
        Assert.Null(repository.LastWizardRequest!.ExternalCategoryMappingContext);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidMappingContext_ReturnsValidationError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("", "en:colas", "Colas"), // Empty provider!
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Null(repository.LastCreateRequest);
    }

    [Fact]
    public async Task CreateAsync_WithValidMappingContext_NormalizesContextAndCallsRepository()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            ResolvedUnitId = Guid.NewGuid(),
        };
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("  OPENFOODFACTS  ", "  EN:COLAS  ", "  Colas  "),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastCreateRequest);
        Assert.NotNull(repository.LastCreateRequest!.ExternalCategoryMappingContext);
        Assert.Equal("openfoodfacts", repository.LastCreateRequest.ExternalCategoryMappingContext!.Provider);
        Assert.Equal("en:colas", repository.LastCreateRequest.ExternalCategoryMappingContext.ExternalCategoryKey);
        Assert.Equal("Colas", repository.LastCreateRequest.ExternalCategoryMappingContext.ExternalCategoryName);
    }

    [Fact]
    public async Task CreateAsync_ProviderLength51_ReturnsValidationError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext(new string('p', 51), "en:colas", "Colas"),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors, e => e.Message.Contains("cannot exceed 50 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_KeyLength151_ReturnsValidationError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("openfoodfacts", new string('k', 151), "Colas"),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors, e => e.Message.Contains("cannot exceed 150 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_NameLength151_ReturnsValidationError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("openfoodfacts", "en:colas", new string('n', 151)),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors, e => e.Message.Contains("cannot exceed 150 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateAsync_ExactBoundaries50And150_Succeeds()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            ResolvedUnitId = Guid.NewGuid(),
        };
        var service = CreateService(repository);

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext(new string('p', 50), new string('k', 150), new string('n', 150)),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastCreateRequest);
        Assert.Equal(50, repository.LastCreateRequest!.ExternalCategoryMappingContext!.Provider.Length);
        Assert.Equal(150, repository.LastCreateRequest.ExternalCategoryMappingContext.ExternalCategoryKey.Length);
        Assert.Equal(150, repository.LastCreateRequest.ExternalCategoryMappingContext.ExternalCategoryName!.Length);
    }

    // --- Phase A regression: provider allowlist rejects any identity that never came from a
    // real, configured provider (e.g. the literal "cache"), closing the persistence path a
    // client could otherwise use to write a corrupted mapping row. ---

    private static IOptions<ExternalProductLookupOptions> OpenFoodFactsOnlyLookupOptions() =>
        Options.Create(new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "openfoodfacts", Enabled = true, Priority = 1 },
            ],
        });

    [Fact]
    public async Task CreateAsync_ProviderIsCacheLiteral_RejectedByAllowlist_NeverPersisted()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsOnlyLookupOptions());

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("cache", "en:colas", "Colas"),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors, e => e.Message.Contains("not a recognized external product-lookup provider", StringComparison.OrdinalIgnoreCase));
        Assert.Null(repository.LastCreateRequest);
    }

    [Theory]
    [InlineData("random")]
    [InlineData("foo")]
    [InlineData("upcitemdb")] // not yet configured -> not yet allowed
    public async Task CreateAsync_UnconfiguredProvider_RejectedByAllowlist(string provider)
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsOnlyLookupOptions());

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext(provider, "en:colas", "Colas"),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Null(repository.LastCreateRequest);
    }

    [Fact]
    public async Task CreateAsync_ConfiguredProvider_AllowlistPermitsPersistence()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            ResolvedUnitId = Guid.NewGuid(),
        };
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsOnlyLookupOptions());

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("openfoodfacts", "en:colas", "Colas"),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastCreateRequest);
        Assert.Equal("openfoodfacts", repository.LastCreateRequest!.ExternalCategoryMappingContext!.Provider);
    }

    // --- Config-hardening regression: the allowlist is keyed off "configured" (present in
    // Providers, regardless of Enabled), not "enabled". Temporarily disabling upcitemdb (e.g. to
    // avoid burning its free-tier quota in production) must not invalidate mappings a tenant
    // already saved for it while it was enabled. ---

    private static IOptions<ExternalProductLookupOptions> OpenFoodFactsEnabledUpcItemDbDisabledLookupOptions() =>
        Options.Create(new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "openfoodfacts", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "upcitemdb", Enabled = false, Priority = 2 },
            ],
        });

    [Fact]
    public async Task CreateAsync_DisabledButConfiguredProvider_AllowlistStillPermitsPersistence()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            ResolvedUnitId = Guid.NewGuid(),
        };
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsEnabledUpcItemDbDisabledLookupOptions());

        var request = new TenantAdminProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            Sku = "SKU-001",
            SellingPrice = 10m,
            UnitType = "PIECE",
            Status = ProductConstants.ActiveStatus,
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("upcitemdb", "some-category-text", "Some Category"),
        };

        var result = await service.CreateAsync(
            CreateContext([ProductConstants.CreatePermission]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastCreateRequest);
        Assert.Equal("upcitemdb", repository.LastCreateRequest!.ExternalCategoryMappingContext!.Provider);
    }

    [Fact]
    public async Task CreateFromWizardAsync_ProviderIsCacheLiteral_RejectedByAllowlist_NeverPersisted()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsOnlyLookupOptions());

        var request = new TenantAdminWizardProductCreateRequest
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            ProductStructure = "SIMPLE",
            DesiredPublishActive = true,
            ProductUnitId = Guid.NewGuid(),
            BaseUnitId = Guid.NewGuid(),
            UnitModel = "SINGLE_UNIT",
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            ExternalCategoryMappingContext = new ExternalCategoryMappingContext("cache", "en:colas", "Colas"),
        };

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Equal(0, repository.CreateProductCallCount);
    }

    // --- External Brand Mapping: wizard create request wiring. Mirrors the Category mapping
    // context tests above — same allowlist, same "configured not enabled" semantics, same
    // normalize-then-pass-to-repository flow, but scoped to the wizard-only request per spec. ---

    private static TenantAdminWizardProductCreateRequest ValidWizardRequest(
        Guid? brandId = null,
        ExternalBrandMappingContext? brandMappingContext = null,
        ExternalCategoryMappingContext? categoryMappingContext = null) =>
        new()
        {
            ProductName = "Test Product",
            CategoryId = Guid.NewGuid(),
            BrandId = brandId,
            ProductStructure = "SIMPLE",
            DesiredPublishActive = true,
            ProductUnitId = Guid.NewGuid(),
            BaseUnitId = Guid.NewGuid(),
            UnitModel = "SINGLE_UNIT",
            PricingTax = new PricingTaxConfigurationDto(10, 15, 12, Guid.NewGuid(), true),
            BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                IdentifierTargets: null,
                Assignments: [new BarcodeSkuAssignmentDto(null, null, "SKU-001", null, null)]),
            ExternalCategoryMappingContext = categoryMappingContext,
            ExternalBrandMappingContext = brandMappingContext,
        };

    [Fact]
    public async Task CreateFromWizardAsync_BrandMappingContext_ProviderIsCacheLiteral_RejectedByAllowlist_NeverPersisted()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsOnlyLookupOptions());

        var request = ValidWizardRequest(
            brandId: Guid.NewGuid(),
            brandMappingContext: new ExternalBrandMappingContext("cache", "coca cola", "Coca-Cola"));

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Equal(0, repository.CreateProductCallCount);
    }

    [Theory]
    [InlineData("random")]
    [InlineData("foo")]
    [InlineData("upcitemdb")] // not configured in OpenFoodFactsOnlyLookupOptions -> rejected
    public async Task CreateFromWizardAsync_BrandMappingContext_UnconfiguredProvider_RejectedByAllowlist(string provider)
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsOnlyLookupOptions());

        var request = ValidWizardRequest(
            brandId: Guid.NewGuid(),
            brandMappingContext: new ExternalBrandMappingContext(provider, "coca cola", "Coca-Cola"));

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.validation_failed", result.Error.Code);
        Assert.Equal(0, repository.CreateProductCallCount);
    }

    [Fact]
    public async Task CreateFromWizardAsync_BrandMappingContext_DisabledButConfiguredProvider_StillPermitted()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsEnabledUpcItemDbDisabledLookupOptions());

        var request = ValidWizardRequest(
            brandId: Guid.NewGuid(),
            brandMappingContext: new ExternalBrandMappingContext("upcitemdb", "coca cola", "Coca-Cola"));

        var result = await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        // Not rejected by the allowlist — reaches the repository (result may still fail there
        // since FakeTenantAdminProductRepository.WizardCreateResult defaults to failure; the
        // point here is proving validation did not block it).
        Assert.Equal(1, repository.CreateProductCallCount);
        Assert.Equal("upcitemdb", repository.LastWizardRequest!.ExternalBrandMappingContext!.Provider);
    }

    [Fact]
    public async Task CreateFromWizardAsync_BrandMappingContext_ConfiguredProvider_NormalizedBeforeReachingRepository()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsOnlyLookupOptions());

        var request = ValidWizardRequest(
            brandId: Guid.NewGuid(),
            brandMappingContext: new ExternalBrandMappingContext("  OpenFoodFacts  ", "  Coca-Cola  ", "  Coca-Cola  "));

        await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.Equal(1, repository.CreateProductCallCount);
        var persistedContext = repository.LastWizardRequest!.ExternalBrandMappingContext!;
        Assert.Equal("openfoodfacts", persistedContext.Provider);
        Assert.Equal("coca cola", persistedContext.ExternalBrandKey);
    }

    [Fact]
    public async Task CreateFromWizardAsync_CategoryAndBrandMappingContext_BothAcceptedTogether()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsOnlyLookupOptions());

        var request = ValidWizardRequest(
            brandId: Guid.NewGuid(),
            brandMappingContext: new ExternalBrandMappingContext("openfoodfacts", "coca cola", "Coca-Cola"),
            categoryMappingContext: new ExternalCategoryMappingContext("openfoodfacts", "en:colas", "Colas"));

        await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.Equal(1, repository.CreateProductCallCount);
        Assert.NotNull(repository.LastWizardRequest!.ExternalCategoryMappingContext);
        Assert.NotNull(repository.LastWizardRequest.ExternalBrandMappingContext);
        Assert.Equal("en:colas", repository.LastWizardRequest.ExternalCategoryMappingContext!.ExternalCategoryKey);
        Assert.Equal("coca cola", repository.LastWizardRequest.ExternalBrandMappingContext!.ExternalBrandKey);
    }

    [Fact]
    public async Task CreateFromWizardAsync_NoBrandMappingContext_SucceedsValidationWithoutError()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository, externalProductLookupOptions: OpenFoodFactsOnlyLookupOptions());

        var request = ValidWizardRequest(brandId: null, brandMappingContext: null);

        await service.CreateFromWizardAsync(
            CreateFullAdminContext(),
            request,
            CancellationToken.None);

        Assert.Equal(1, repository.CreateProductCallCount);
        Assert.Null(repository.LastWizardRequest!.ExternalBrandMappingContext);
    }
}
