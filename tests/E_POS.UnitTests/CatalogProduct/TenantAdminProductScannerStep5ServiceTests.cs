using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed partial class TenantAdminProductServiceTests
{
    [Fact]
    public async Task UpdateDraftAsync_ScannerCompositeStep5_SaveAndContinue_PersistsIdentifiersAndAdvancesTo6()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            HasScanContext = true,
            GeneratedSkuBase = "TSH-000125",
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with
            {
                CurrentSetupStep = 5,
                ProductStructure = ProductStructureConstants.Simple,
                RowVersion = 3
            },
            Step5Targets = [new(variantId, "Default", null, variantId.ToString())],
            DraftResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                productId,
                "Scanner Product",
                "SCN-1",
                ProductConstants.DraftStatus,
                ProductConstants.DesiredPublishActive,
                6,
                DateTimeOffset.UtcNow,
                4,
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
                [],
                TargetSetupStep: 6,
                LastCompletedSetupStep: 6))
        };
        var service = CreateService(repository);

        var result = await service.UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 5,
                WizardAction = "SAVE_AND_CONTINUE",
                ExpectedRowVersion = 3,
                ProductStructure = ProductStructureConstants.Simple,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [
                        new BarcodeSkuAssignmentDto(
                            variantId,
                            "Default",
                            "SKU-FINAL-1",
                            "012345678905",
                            null,
                            variantId.ToString(),
                            "UNKNOWN",
                            "GTIN12")
                    ],
                    "MANUAL")
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(6, result.Value!.CurrentSetupStep);
        Assert.Equal(ProductConstants.DraftStatus, result.Value.Status);
        Assert.NotNull(repository.LastSaveDraftCommand);
        Assert.True(repository.LastSaveDraftCommand!.ApplyCompositeStep5Identifiers);
        Assert.Equal(ProductWizardStage.ProductConfiguration, repository.LastSaveDraftCommand.CurrentStage);
        Assert.Equal(6, repository.LastSaveDraftCommand.TargetSetupStep);
        Assert.Equal("SKU-FINAL-1", repository.LastSaveDraftCommand.BarcodeSkuConfiguration!.Assignments![0].Sku);
        Assert.Null(repository.LastSaveDraftCommand.AutoSkuBase);
        Assert.Equal("012345678905", repository.LastSaveDraftCommand.BarcodeSkuConfiguration.Assignments[0].Barcode);
        Assert.Equal(0, repository.ResolveLookupCallCount);
    }

    [Fact]
    public async Task UpdateDraftAsync_AutoSkuSimple_AllowsNoClientSkuAndCarriesStableBase()
    {
        var productId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            HasScanContext = true,
            GeneratedSkuBase = "BEV-000128",
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with
            {
                CurrentSetupStep = 5,
                ProductStructure = ProductStructureConstants.Simple,
                RowVersion = 3
            },
            DraftResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                productId,
                "Beverage",
                "BEV-P",
                ProductConstants.DraftStatus,
                ProductConstants.DesiredPublishActive,
                6,
                DateTimeOffset.UtcNow,
                4,
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
                [],
                TargetSetupStep: 6,
                LastCompletedSetupStep: 6))
        };
        var service = CreateService(repository);

        var result = await service.UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 5,
                WizardAction = "SAVE_AND_CONTINUE",
                ExpectedRowVersion = 3,
                ProductStructure = ProductStructureConstants.Simple,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(null, [], "AUTO")
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("BEV-000128", repository.LastSaveDraftCommand!.AutoSkuBase);
        Assert.True(repository.LastSaveDraftCommand.ApplyCompositeStep5Identifiers);
    }

    [Fact]
    public async Task UpdateDraftAsync_ScannerCompositeStep5_SaveDraft_StaysAt5()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            HasScanContext = true,
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with
            {
                CurrentSetupStep = 5,
                ProductStructure = ProductStructureConstants.Simple,
                RowVersion = 2
            },
            Step5Targets = [new(variantId, "Default", null, variantId.ToString())],
            DraftResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                productId,
                "Scanner Product",
                "SCN-1",
                ProductConstants.DraftStatus,
                ProductConstants.DesiredPublishActive,
                5,
                DateTimeOffset.UtcNow,
                3,
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
                [],
                TargetSetupStep: 5,
                LastCompletedSetupStep: 5))
        };
        var service = CreateService(repository);

        var result = await service.UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 5,
                WizardAction = "SAVE_DRAFT",
                ExpectedRowVersion = 2,
                ProductStructure = ProductStructureConstants.Simple,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [new BarcodeSkuAssignmentDto(variantId, "Default", "SKU-DRAFT", null, null, variantId.ToString())])
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(5, repository.LastSaveDraftCommand!.TargetSetupStep);
        Assert.True(repository.LastSaveDraftCommand.ApplyCompositeStep5Identifiers);
    }

    [Fact]
    public async Task UpdateDraftAsync_LegacyWithoutScanContext_Step5StillBarcodeSkuOnly()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            HasScanContext = false,
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with
            {
                CurrentSetupStep = 5,
                ProductStructure = ProductStructureConstants.Simple,
                RowVersion = 2
            },
            Step5Targets = [new(variantId, "Default", null, variantId.ToString())],
            DraftResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                productId,
                "Legacy Product",
                "LEG-1",
                ProductConstants.DraftStatus,
                ProductConstants.DesiredPublishActive,
                5,
                DateTimeOffset.UtcNow,
                3,
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
                [],
                TargetSetupStep: 5,
                LastCompletedSetupStep: 5))
        };
        var service = CreateService(repository);

        var result = await service.UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = ProductWizardStage.BarcodeSku,
                WizardAction = "SAVE_DRAFT",
                ExpectedRowVersion = 2,
                ProductStructure = ProductStructureConstants.Simple,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [new BarcodeSkuAssignmentDto(variantId, "Default", "SKU-LEG", null, null, variantId.ToString())])
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.False(repository.LastSaveDraftCommand!.ApplyCompositeStep5Identifiers);
        Assert.Equal(ProductWizardStage.BarcodeSku, repository.LastSaveDraftCommand.CurrentStage);
    }

    [Fact]
    public async Task UpdateDraftAsync_ScannerCompositeStep5_RejectsDuplicateSkuInRequest()
    {
        var productId = Guid.NewGuid();
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            HasScanContext = true,
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with
            {
                ProductStructure = ProductStructureConstants.Variant,
                RowVersion = 1
            },
            Step5Targets =
            [
                new(v1, "A", null, "k1"),
                new(v2, "B", null, "k2")
            ]
        };
        var service = CreateService(repository);

        var result = await service.UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 5,
                WizardAction = "SAVE_AND_CONTINUE",
                ExpectedRowVersion = 1,
                ProductStructure = ProductStructureConstants.Variant,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [
                        new BarcodeSkuAssignmentDto(v1, "A", "SAME-SKU", null, null, "k1"),
                        new BarcodeSkuAssignmentDto(v2, "B", "SAME-SKU", null, null, "k2")
                    ])
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, repository.SaveDraftCallCount);
    }

    [Fact]
    public async Task UpdateDraftAsync_ScannerCompositeStep5_DoesNotCallExternalLookupOrResolve()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var coordinator = new FakeExternalProductLookupCoordinator();
        var repository = new FakeTenantAdminProductRepository
        {
            HasScanContext = true,
            SetupDto = CreateSetup(productId, Guid.NewGuid()) with { RowVersion = 1 },
            Step5Targets = [new(variantId, "Default", null, variantId.ToString())],
            DraftResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                productId,
                "P",
                "P1",
                ProductConstants.DraftStatus,
                ProductConstants.DesiredPublishActive,
                5,
                DateTimeOffset.UtcNow,
                2,
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
                [],
                TargetSetupStep: 5,
                LastCompletedSetupStep: 5))
        };
        var service = CreateService(repository, coordinator);

        var result = await service.UpdateDraftAsync(
            CreateContext([
                ProductConstants.UpdatePermission,
                ProductConstants.BarcodesManagePermission]),
            productId,
            new SaveProductDraftRequest
            {
                CurrentSetupStep = 5,
                WizardAction = "SAVE_DRAFT",
                ExpectedRowVersion = 1,
                ProductStructure = ProductStructureConstants.Simple,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [new BarcodeSkuAssignmentDto(variantId, "Default", "SKU-X", null, null, variantId.ToString())])
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, coordinator.CallCount);
        Assert.Equal(0, repository.ResolveLookupCallCount);
        Assert.Equal(0, repository.SkuExistsCallCount);
    }
}
