using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class TenantAdminProductDraftValidatorTests
{
    private readonly TenantAdminProductRequestValidator _validator = new();

    [Fact]
    public void ValidateSaveDraft_WithoutCategory_Succeeds()
    {
        var request = CreateValidRequest();
        request.CategoryId = null;

        Assert.Null(_validator.ValidateSaveDraft(request));
    }

    [Fact]
    public void ValidateSaveAndContinue_DraftPlaceholderName_Fails()
    {
        var request = CreateValidRequest();
        request.ProductName = ProductConstants.DraftProductNamePlaceholder;

        var error = _validator.ValidateSaveAndContinue(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field == "productName");
    }

    [Fact]
    public void ValidateSaveDraft_BlankProductName_Succeeds()
    {
        var request = CreateValidRequest();
        request.ProductName = " ";
        request.CategoryId = null;

        Assert.Null(_validator.ValidateSaveDraft(request));
    }

    [Fact]
    public void ValidateSaveAndContinue_MissingProductName_Fails()
    {
        var request = CreateValidRequest();
        request.ProductName = " ";

        var error = _validator.ValidateSaveAndContinue(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field == "productName");
    }

    [Fact]
    public void ValidateSaveAndContinue_MissingCategory_Fails()
    {
        var request = CreateValidRequest();
        request.CategoryId = null;

        var error = _validator.ValidateSaveAndContinue(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field == "categoryId");
    }

    [Fact]
    public void ValidateSaveAndContinue_BrandOptional_Succeeds()
    {
        var request = CreateValidRequest();
        request.BrandId = null;

        Assert.Null(_validator.ValidateSaveAndContinue(request));
    }

    [Fact]
    public void ValidateSaveDraft_ShortDescriptionTooLong_ReturnsFieldError()
    {
        var request = CreateValidRequest();
        request.ShortDescription = new string('s', ProductConstants.ShortDescriptionMaxLength + 1);

        var error = _validator.ValidateSaveDraft(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field == "shortDescription");
    }

    [Fact]
    public void ValidateSaveDraft_LongDescriptionTooLong_ReturnsFieldError()
    {
        var request = CreateValidRequest();
        request.LongDescription = new string('l', ProductConstants.LongDescriptionMaxLength + 1);

        var error = _validator.ValidateSaveDraft(request);

        Assert.NotNull(error);
        Assert.Contains(error!.FieldErrors!, e => e.Field == "longDescription");
    }

    [Fact]
    public void ValidateSaveAndContinue_MaxLengthBoundary_Succeeds()
    {
        var request = CreateValidRequest();
        request.ShortDescription = new string('s', ProductConstants.ShortDescriptionMaxLength);
        request.LongDescription = new string('l', ProductConstants.LongDescriptionMaxLength);

        Assert.Null(_validator.ValidateSaveAndContinue(request));
    }

    [Fact]
    public void ProductTypeTracking_RejectsInvalidStructure()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 2,
            AdvanceStep = true,
            ProductStructure = "INVALID",
            TrackInventory = true
        };

        var error = _validator.ValidateSaveAndContinue(request);

        Assert.NotNull(error);
        Assert.Equal("product.invalid_product_structure", error!.Code);
    }

    [Fact]
    public void ProductTypeTracking_RequiresExplicitStructure_WhenContinuing()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 2,
            AdvanceStep = true,
            ProductStructure = null,
            TrackInventory = true
        };

        var error = _validator.ValidateSaveAndContinue(request);

        Assert.NotNull(error);
        Assert.Equal("product.invalid_product_structure", error!.Code);
    }

    [Fact]
    public void ProductTypeTracking_AllowsOmittedStructure_WhenSavingDraft()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 2,
            AdvanceStep = false,
            ProductStructure = null,
            TrackInventory = true
        };

        var error = _validator.ValidateSaveDraft(request);

        Assert.Null(error);
    }

    [Fact]
    public void ProductTypeTracking_EvaluatesTrackingRulesForSimpleAndVariant()
    {
        var request = new SaveProductDraftRequest
        {
            CurrentSetupStep = 2,
            AdvanceStep = true,
            ProductStructure = "SIMPLE",
            TrackInventory = true,
            BatchTracking = false,
            ExpiryTracking = true // Invalid: expiry requires batch
        };

        var error = _validator.ValidateSaveAndContinue(request);

        Assert.NotNull(error);
        Assert.Equal("product.batch_required_for_expiry", error!.Code);
    }

    private static SaveProductDraftRequest CreateValidRequest() =>
        new()
        {
            ProductName = "Draft Product",
            CategoryId = Guid.NewGuid(),
            CurrentSetupStep = 1,
        };
}
