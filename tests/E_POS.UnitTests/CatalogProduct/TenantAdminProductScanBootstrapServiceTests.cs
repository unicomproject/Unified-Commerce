using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed partial class TenantAdminProductServiceTests
{
    [Fact]
    public async Task SaveDraftAsync_ScanBootstrap_ContinueWithBarcode_RoutesBasicDetailsAndPersistsStep2()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.SaveDraftAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "SCAN",
                    CreationAction = "CONTINUE_WITH_BARCODE",
                    CandidateIdentifier = "012345678905",
                    ExternalLookupStatus = "NOT_STARTED"
                }
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.CurrentSetupStep);
        Assert.Equal(ProductConstants.DraftStatus, result.Value.Status);
        Assert.NotNull(repository.LastSaveDraftCommand);
        Assert.Equal(ProductWizardStage.BasicDetails, repository.LastSaveDraftCommand!.CurrentStage);
        Assert.Equal(2, repository.LastSaveDraftCommand.TargetSetupStep);
        Assert.Equal("012345678905", repository.LastSaveDraftCommand.ScanBootstrap!.CandidateIdentifier);
        Assert.Equal("GTIN12", repository.LastSaveDraftCommand.ScanBootstrap.IdentifierStandard);
        Assert.Equal(1, repository.ResolveLookupCallCount);
        Assert.Equal("012345678905", repository.LastResolvedBarcode);
    }

    [Fact]
    public async Task SaveDraftAsync_ScanBootstrap_Manual_Succeeds()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.SaveDraftAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "MANUAL",
                    CreationAction = "CONTINUE_WITH_BARCODE",
                    CandidateIdentifier = "4006381333931"
                }
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("MANUAL", repository.LastSaveDraftCommand!.ScanBootstrap!.AcquisitionMode);
    }

    [Theory]
    [InlineData("OWN_MADE")]
    [InlineData("SERVICE_FEE")]
    [InlineData("UNLABELLED")]
    public async Task SaveDraftAsync_ScanBootstrap_NoBarcode_PersistsReasonAndSkuPreview(string reason)
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.SaveDraftAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "NO_BARCODE",
                    CreationAction = "CONTINUE_TO_BASIC_DETAILS",
                    NoBarcodeReason = reason,
                    GeneratedSkuCandidate = "SKU-NB-TEST"
                }
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.CurrentSetupStep);
        Assert.Equal(0, repository.ResolveLookupCallCount);
        Assert.Equal(reason, repository.LastSaveDraftCommand!.ScanBootstrap!.NoBarcodeReason);
        Assert.Equal("SKU-NB-TEST", repository.LastSaveDraftCommand.ScanBootstrap.GeneratedSkuCandidate);
        Assert.Null(repository.LastSaveDraftCommand.ScanBootstrap.CandidateIdentifier);
    }

    [Fact]
    public async Task SaveDraftAsync_UseThisProduct_AppliesPrefillName()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.SaveDraftAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "SCAN",
                    CreationAction = "USE_THIS_PRODUCT",
                    CandidateIdentifier = "4006381333931",
                    ExternalLookupStatus = "FOUND",
                    ExternalSourceReference = "ext-1",
                    NormalizedPrefill = new ExternalProductSuggestion(
                        "Prefill Product", null, "BrandText", null, null, null, "Short desc", null, "https://img", null, "GTIN13")
                }
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Prefill Product", repository.LastSaveDraftCommand!.ProductName);
        Assert.Equal("Short desc", repository.LastSaveDraftCommand.ShortDescription);
        Assert.True(repository.LastSaveDraftCommand.ScanBootstrap!.ApplyPrefillToDraft);
        Assert.Contains("Prefill Product", repository.LastSaveDraftCommand.ScanBootstrap.NormalizedPrefillJson);
        Assert.DoesNotContain("BrandId", repository.LastSaveDraftCommand.ScanBootstrap.NormalizedPrefillJson ?? "");
    }

    [Fact]
    public async Task SaveDraftAsync_CreateManually_RetainsBarcode_DiscardsPrefill()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.SaveDraftAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ProductName = "Manual Name",
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "SCAN",
                    CreationAction = "CREATE_MANUALLY",
                    CandidateIdentifier = "4006381333931",
                    ExternalLookupStatus = "FOUND",
                    NormalizedPrefill = new ExternalProductSuggestion(
                        "Should Ignore", null, null, null, null, null, null, null, null, null, null)
                }
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Manual Name", repository.LastSaveDraftCommand!.ProductName);
        Assert.Equal("4006381333931", repository.LastSaveDraftCommand.ScanBootstrap!.CandidateIdentifier);
        Assert.Null(repository.LastSaveDraftCommand.ScanBootstrap.NormalizedPrefillJson);
        Assert.False(repository.LastSaveDraftCommand.ScanBootstrap.ApplyPrefillToDraft);
    }

    [Fact]
    public async Task SaveDraftAsync_DuplicateCandidate_Returns409Code_DoesNotSave()
    {
        var repository = new FakeTenantAdminProductRepository
        {
            ResolveMatch = new ProductBarcodeResolveMatchProjection(
                Guid.NewGuid(),
                null,
                "PRODUCT",
                "Existing",
                null,
                null,
                null,
                "SKU-1",
                ProductConstants.ActiveStatus,
                null,
                1)
        };
        var service = CreateService(repository);

        var result = await service.SaveDraftAsync(
            CreateContext([ProductConstants.CreatePermission]),
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

        Assert.True(result.IsFailure);
        Assert.Equal("product.duplicate_barcode", result.Error!.Code);
        Assert.Equal(0, repository.SaveDraftCallCount);
    }

    [Fact]
    public async Task SaveDraftAsync_ScanBootstrap_WrongStep_Rejected()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.SaveDraftAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 1,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "NO_BARCODE",
                    CreationAction = "CONTINUE_TO_BASIC_DETAILS",
                    NoBarcodeReason = "OWN_MADE"
                }
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(0, repository.SaveDraftCallCount);
    }

    [Fact]
    public async Task SaveDraftAsync_ScanBootstrap_MissingCreatePermission_Denied()
    {
        var repository = new FakeTenantAdminProductRepository();
        var service = CreateService(repository);

        var result = await service.SaveDraftAsync(
            CreateContext([]),
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "NO_BARCODE",
                    CreationAction = "CONTINUE_TO_BASIC_DETAILS",
                    NoBarcodeReason = "OWN_MADE"
                }
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.permission_denied", result.Error!.Code);
    }

    [Fact]
    public async Task SaveDraftAsync_ScanBootstrap_MissingEntitlement_Denied()
    {
        var repository = new FakeTenantAdminProductRepository();
        var entitlement = new FakeEntitlementEvaluator { IsEntitled = false };
        var service = CreateService(repository, entitlementEvaluator: entitlement);

        var result = await service.SaveDraftAsync(
            CreateContext([ProductConstants.CreatePermission]),
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "NO_BARCODE",
                    CreationAction = "CONTINUE_TO_BASIC_DETAILS",
                    NoBarcodeReason = "OWN_MADE"
                }
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("product.entitlement_denied", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateDraftAsync_ScannerFirstStep2_RoutesBasicDetails_PersistsPublic2()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            HasScanContext = true,
            SetupDto = CreateSetup(productId, Guid.NewGuid())
        };
        var service = CreateService(repository);

        var result = await service.UpdateDraftAsync(
            CreateContext([ProductConstants.UpdatePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ExpectedRowVersion = 1,
                ProductName = "Updated Basic"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductWizardStage.BasicDetails, repository.LastSaveDraftCommand!.CurrentStage);
        Assert.Equal(2, repository.LastSaveDraftCommand.TargetSetupStep);
        Assert.Null(repository.LastSaveDraftCommand.ScanBootstrap);
    }

    [Fact]
    public async Task UpdateDraftAsync_LegacyWithoutScanContext_Step1StillBasicDetails()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            HasScanContext = false,
            SetupDto = CreateSetup(productId, Guid.NewGuid())
        };
        var service = CreateService(repository);

        var result = await service.UpdateDraftAsync(
            CreateContext([ProductConstants.UpdatePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 1,
                ExpectedRowVersion = 1,
                ProductName = "Legacy Basic"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductWizardStage.BasicDetails, repository.LastSaveDraftCommand!.CurrentStage);
        Assert.Equal(1, repository.LastSaveDraftCommand.TargetSetupStep);
    }

    [Fact]
    public async Task UpdateDraftAsync_WithScanBootstrap_Rejected()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            HasScanContext = true,
            SetupDto = CreateSetup(productId, Guid.NewGuid())
        };
        var service = CreateService(repository);

        var result = await service.UpdateDraftAsync(
            CreateContext([ProductConstants.UpdatePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 2,
                ExpectedRowVersion = 1,
                ScanBootstrap = new ProductSetupScanBootstrapRequest
                {
                    AcquisitionMode = "SCAN",
                    CreationAction = "CONTINUE_WITH_BARCODE",
                    CandidateIdentifier = "4006381333931"
                }
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(0, repository.SaveDraftCallCount);
    }
}
