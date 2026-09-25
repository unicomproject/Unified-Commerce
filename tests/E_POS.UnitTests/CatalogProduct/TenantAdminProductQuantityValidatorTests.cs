using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Validators;
using System.Collections.Generic;
using System.Linq;
using System;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class TenantAdminProductQuantityValidatorTests
{
    private SaveProductDraftRequest CreateBaseRequest()
    {
        return new SaveProductDraftRequest
        {
            ProductStructure = "SIMPLE",
            TrackInventory = true,
            BatchTracking = false,
            ExpiryTracking = false,
            SerialTracking = false,
            QuantityDraft = new OpeningStockDraftDto
            {
                StockOwners = new List<OpeningStockOwnerDraftDto>
                {
                    new OpeningStockOwnerDraftDto
                    {
                        OpeningQuantity = 0,
                        Allocations = new List<OutletAllocationDraftDto>()
                    }
                }
            }
        };
    }

    [Fact]
    public void Validate_OpeningQuantityZero_Succeeds()
    {
        var request = CreateBaseRequest();
        var draftError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingSaveDraft(request);
        var continueError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingContinue(request);

        Assert.Null(draftError);
        Assert.Null(continueError);
    }

    [Fact]
    public void Validate_OpeningQuantityNegative_Fails()
    {
        var request = CreateBaseRequest();
        request.QuantityDraft!.StockOwners!.First().OpeningQuantity = -10;

        var draftError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingSaveDraft(request);
        
        Assert.NotNull(draftError);
        Assert.Contains(draftError!.FieldErrors!, e => e.Field == "quantityDraft.openingQuantity");
    }

    [Fact]
    public void Validate_PositiveExactAllocation_Succeeds()
    {
        var request = CreateBaseRequest();
        var outlet1 = Guid.NewGuid();
        var outlet2 = Guid.NewGuid();

        request.QuantityDraft!.StockOwners!.First().OpeningQuantity = 100;
        request.QuantityDraft!.StockOwners!.First().Allocations = new List<OutletAllocationDraftDto>
        {
            new OutletAllocationDraftDto { OutletId = outlet1, Quantity = 60 },
            new OutletAllocationDraftDto { OutletId = outlet2, Quantity = 40 }
        };

        var continueError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingContinue(request);
        Assert.Null(continueError);
    }

    [Fact]
    public void Validate_Continue_UnderAllocation_Fails()
    {
        var request = CreateBaseRequest();
        var outlet1 = Guid.NewGuid();

        request.QuantityDraft!.StockOwners!.First().OpeningQuantity = 100;
        request.QuantityDraft!.StockOwners!.First().Allocations = new List<OutletAllocationDraftDto>
        {
            new OutletAllocationDraftDto { OutletId = outlet1, Quantity = 60 }
        };

        var continueError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingContinue(request);
        
        Assert.NotNull(continueError);
        Assert.Contains(continueError!.FieldErrors!, e => e.Message.Contains("Sum of allocated quantities must exactly equal"));
    }

    [Fact]
    public void Validate_OverAllocation_Fails()
    {
        var request = CreateBaseRequest();
        var outlet1 = Guid.NewGuid();
        var outlet2 = Guid.NewGuid();

        request.QuantityDraft!.StockOwners!.First().OpeningQuantity = 100;
        request.QuantityDraft!.StockOwners!.First().Allocations = new List<OutletAllocationDraftDto>
        {
            new OutletAllocationDraftDto { OutletId = outlet1, Quantity = 60 },
            new OutletAllocationDraftDto { OutletId = outlet2, Quantity = 60 }
        };

        var draftError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingSaveDraft(request);
        
        Assert.NotNull(draftError);
        Assert.Contains(draftError!.FieldErrors!, e => e.Message.Contains("exceeds Opening Quantity"));
    }

    [Fact]
    public void Validate_DuplicateOutlet_Fails()
    {
        var request = CreateBaseRequest();
        var outlet1 = Guid.NewGuid();

        request.QuantityDraft!.StockOwners!.First().OpeningQuantity = 100;
        request.QuantityDraft!.StockOwners!.First().Allocations = new List<OutletAllocationDraftDto>
        {
            new OutletAllocationDraftDto { OutletId = outlet1, Quantity = 50 },
            new OutletAllocationDraftDto { OutletId = outlet1, Quantity = 50 }
        };

        var draftError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingSaveDraft(request);
        
        Assert.NotNull(draftError);
        Assert.Contains(draftError!.FieldErrors!, e => e.Message.Contains("Duplicate outlet IDs"));
    }

    [Fact]
    public void Validate_Variant_Reconciliation_Succeeds()
    {
        var request = CreateBaseRequest();
        request.ProductStructure = "VARIANT";
        var outlet1 = Guid.NewGuid();
        var outlet2 = Guid.NewGuid();

        request.QuantityDraft!.StockOwners = new List<OpeningStockOwnerDraftDto>
        {
            new OpeningStockOwnerDraftDto
            {
                VariantId = Guid.NewGuid(),
                OpeningQuantity = 50,
                Allocations = new List<OutletAllocationDraftDto>
                {
                    new OutletAllocationDraftDto { OutletId = outlet1, Quantity = 30 },
                    new OutletAllocationDraftDto { OutletId = outlet2, Quantity = 20 }
                }
            },
            new OpeningStockOwnerDraftDto
            {
                VariantId = Guid.NewGuid(),
                OpeningQuantity = 70,
                Allocations = new List<OutletAllocationDraftDto>
                {
                    new OutletAllocationDraftDto { OutletId = outlet1, Quantity = 40 },
                    new OutletAllocationDraftDto { OutletId = outlet2, Quantity = 30 }
                }
            }
        };

        var continueError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingContinue(request);
        Assert.Null(continueError);
    }

    [Fact]
    public void Validate_Variant_Reconciliation_Fails_WhenTotalMatchesButPerVariantFails()
    {
        var request = CreateBaseRequest();
        request.ProductStructure = "VARIANT";
        var outlet1 = Guid.NewGuid();
        var outlet2 = Guid.NewGuid();

        request.QuantityDraft!.StockOwners = new List<OpeningStockOwnerDraftDto>
        {
            new OpeningStockOwnerDraftDto
            {
                VariantId = Guid.NewGuid(),
                OpeningQuantity = 50,
                Allocations = new List<OutletAllocationDraftDto>
                {
                    new OutletAllocationDraftDto { OutletId = outlet1, Quantity = 70 }
                }
            },
            new OpeningStockOwnerDraftDto
            {
                VariantId = Guid.NewGuid(),
                OpeningQuantity = 70,
                Allocations = new List<OutletAllocationDraftDto>
                {
                    new OutletAllocationDraftDto { OutletId = outlet2, Quantity = 50 }
                }
            }
        };

        var draftError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingSaveDraft(request);
        var continueError = TenantAdminProductRequestValidator.ValidateProductTypeTrackingContinue(request);

        Assert.NotNull(draftError);
        Assert.Contains(draftError!.FieldErrors!, e => e.Message.Contains("exceeds Opening Quantity"));

        Assert.NotNull(continueError);
        Assert.Contains(continueError!.FieldErrors!, e => e.Message.Contains("Sum of allocated quantities must exactly equal"));
    }
}
