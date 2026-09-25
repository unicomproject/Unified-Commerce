using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using Xunit;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class TenantAdminProductQuantityDraftServiceTests
{
    [Fact]
    public void SerializeQuantityDraft_RoundTripsCorrectly()
    {
        var draft = new OpeningStockDraftDto
        {
            StockOwners = new List<OpeningStockOwnerDraftDto>
            {
                new OpeningStockOwnerDraftDto
                {
                    OpeningQuantity = 250,
                    Allocations = new List<OutletAllocationDraftDto>
                    {
                        new OutletAllocationDraftDto { OutletId = Guid.NewGuid(), Quantity = 150 },
                        new OutletAllocationDraftDto { OutletId = Guid.NewGuid(), Quantity = 100 }
                    }
                }
            }
        };

        var json = ProductSetupCompatibilityHelper.SerializeQuantityDraft(draft);
        var restored = ProductSetupCompatibilityHelper.DeserializeQuantityDraft(json);

        Assert.NotNull(restored);
        Assert.Single(restored!.StockOwners);
        Assert.Equal(250, restored.StockOwners[0].OpeningQuantity);
        Assert.Equal(2, restored.StockOwners[0].Allocations!.Count);
        Assert.Equal(draft.StockOwners[0].Allocations![0].Quantity, restored.StockOwners[0].Allocations![0].Quantity);
    }

    [Fact]
    public void SerializeQuantityDraft_ZeroQuantity_RoundTripsCorrectly()
    {
        var draft = new OpeningStockDraftDto
        {
            StockOwners = new List<OpeningStockOwnerDraftDto>
            {
                new OpeningStockOwnerDraftDto
                {
                    OpeningQuantity = 0,
                    Allocations = new List<OutletAllocationDraftDto>()
                }
            }
        };

        var json = ProductSetupCompatibilityHelper.SerializeQuantityDraft(draft);
        var restored = ProductSetupCompatibilityHelper.DeserializeQuantityDraft(json);

        Assert.NotNull(restored);
        Assert.Single(restored!.StockOwners);
        Assert.Equal(0, restored.StockOwners[0].OpeningQuantity);
        Assert.Empty(restored.StockOwners[0].Allocations!);
    }
}
