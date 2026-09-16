using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed partial class TenantAdminProductServiceTests
{
    [Fact]
    public async Task PublishAsync_AlreadyPublished_ReturnsConflict()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, categoryId) with
            {
                Status = ProductConstants.ActiveStatus,
                CurrentSetupStep = 7
            }
        };
        var service = CreateService(repository);

        var result = await service.PublishAsync(
            CreateContext([ProductConstants.PublishPermission]),
            productId,
            new PublishProductRequest(1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.already_published", result.Error!.Code);
        Assert.Null(repository.LastSaveDraftCommand);
    }

    [Fact]
    public async Task PublishAsync_MissingSku_FailsBeforeSave()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, categoryId) with
            {
                CurrentSetupStep = 7,
                ProductStructure = ProductStructureConstants.Simple,
                RowVersion = 2,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [new BarcodeSkuAssignmentDto(variantId, "Default", null, null, null, variantId.ToString())])
            },
            Step5Targets = [new(variantId, "Default", null, variantId.ToString())]
        };
        var service = CreateService(repository);

        var result = await service.PublishAsync(
            CreateContext([
                ProductConstants.PublishPermission,
                ProductConstants.BarcodesManagePermission,
                ProductConstants.ProductPricingManagePermission]),
            productId,
            new PublishProductRequest(2),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.barcode_sku_validation_failed", result.Error!.Code);
        Assert.Null(repository.LastSaveDraftCommand);
    }

    [Fact]
    public async Task PublishAsync_ValidIdentifiers_CallsSaveWithPublishAction()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, categoryId) with
            {
                CurrentSetupStep = 7,
                ProductStructure = ProductStructureConstants.Simple,
                RowVersion = 5,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [
                        new BarcodeSkuAssignmentDto(
                            variantId,
                            "Default",
                            "SKU-PUB-1",
                            "012345678905",
                            null,
                            variantId.ToString(),
                            "UNKNOWN",
                            "GTIN12")
                    ])
            },
            Step5Targets = [new(variantId, "Default", "SKU-PUB-1", variantId.ToString())],
            DraftResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                productId,
                "Draft Product",
                "DRF-001",
                ProductConstants.ActiveStatus,
                ProductConstants.DesiredPublishActive,
                7,
                DateTimeOffset.UtcNow,
                6,
                categoryId,
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
                TargetSetupStep: 7,
                LastCompletedSetupStep: 7))
        };
        var service = CreateService(repository);

        var result = await service.PublishAsync(
            CreateContext([
                ProductConstants.PublishPermission,
                ProductConstants.BarcodesManagePermission,
                ProductConstants.ProductPricingManagePermission]),
            productId,
            new PublishProductRequest(5),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(ProductConstants.ActiveStatus, result.Value!.Status);
        Assert.NotNull(repository.LastSaveDraftCommand);
        Assert.Equal(ProductWizardStage.ReviewCreate, repository.LastSaveDraftCommand!.CurrentStage);
        Assert.Equal(5, repository.LastSaveDraftCommand.ExpectedRowVersion);
        Assert.False(repository.LastSaveDraftCommand.IsExplicitDraftSave);
    }

    [Fact]
    public async Task PublishAsync_DuplicateSkuConflict_Mapped()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, categoryId) with
            {
                CurrentSetupStep = 7,
                RowVersion = 1,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [new BarcodeSkuAssignmentDto(variantId, "Default", "TAKEN-SKU", null, null, variantId.ToString())])
            },
            Step5Targets = [new(variantId, "Default", "TAKEN-SKU", variantId.ToString())],
            SkuConflicts = new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                ["TAKEN-SKU"] = Guid.NewGuid()
            }
        };
        var service = CreateService(repository);

        var result = await service.PublishAsync(
            CreateContext([
                ProductConstants.PublishPermission,
                ProductConstants.BarcodesManagePermission,
                ProductConstants.ProductPricingManagePermission]),
            productId,
            new PublishProductRequest(1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("product.duplicate_sku", result.Error!.Code);
        Assert.Null(repository.LastSaveDraftCommand);
    }

    [Fact]
    public async Task PublishAsync_DoesNotCallExternalLookupOrSkuGenerator()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var coordinator = new FakeExternalProductLookupCoordinator();
        var repository = new FakeTenantAdminProductRepository
        {
            SetupDto = CreateSetup(productId, categoryId) with
            {
                CurrentSetupStep = 7,
                RowVersion = 1,
                BarcodeSkuConfiguration = new BarcodeSkuConfigurationDto(
                    null,
                    [new BarcodeSkuAssignmentDto(variantId, "Default", "SKU-OK", null, null, variantId.ToString())])
            },
            Step5Targets = [new(variantId, "Default", "SKU-OK", variantId.ToString())],
            DraftResult = SaveProductDraftResult.Success(new ProductDraftResponse(
                productId,
                "Draft Product",
                "DRF-001",
                ProductConstants.ActiveStatus,
                ProductConstants.DesiredPublishActive,
                7,
                DateTimeOffset.UtcNow,
                2,
                categoryId,
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
                []))
        };
        var service = CreateService(repository, coordinator);

        var result = await service.PublishAsync(
            CreateContext([
                ProductConstants.PublishPermission,
                ProductConstants.BarcodesManagePermission,
                ProductConstants.ProductPricingManagePermission]),
            productId,
            new PublishProductRequest(1),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(0, coordinator.CallCount);
    }
}
