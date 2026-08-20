using System;
using System.Collections.Generic;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Services;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class ProductVariantClientKeyHelperTests
{
    [Fact]
    public void GenerateClientCombinationKey_ProducesCorrectFormat()
    {
        var optId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var valId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var pairs = new List<(Guid, Guid)>
        {
            (optId1, valId1)
        };

        var key = ProductVariantClientKeyHelper.GenerateClientCombinationKey(pairs);
        Assert.Equal($"{optId1:D}:{valId1:D}", key);
    }
}
