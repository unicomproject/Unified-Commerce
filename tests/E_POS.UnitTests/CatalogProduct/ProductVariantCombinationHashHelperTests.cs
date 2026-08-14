using System;
using System.Collections.Generic;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Services;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class ProductVariantCombinationHashHelperTests
{
    [Fact]
    public void GenerateCanonicalHash_ProducesCorrectSha256()
    {
        var optId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var valId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var pairs = new List<(Guid, Guid)>
        {
            (optId1, valId1)
        };

        // Format is: opt:{Id}|val:{Id} without trailing semicolon for a single pair
        var expectedString = $"opt:{optId1:D}|val:{valId1:D}";
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expectedString);
        var expectedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(expectedBytes)).ToLowerInvariant();
        
        var hash = ProductVariantCombinationHashHelper.GenerateCanonicalHash(pairs);
        Assert.NotNull(hash);
        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public void GenerateCanonicalHash_OrderIndependent()
    {
        var optId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var valId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var optId2 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var valId2 = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var pairsOrder1 = new List<(Guid, Guid)> { (optId1, valId1), (optId2, valId2) };
        var pairsOrder2 = new List<(Guid, Guid)> { (optId2, valId2), (optId1, valId1) };

        var hash1 = ProductVariantCombinationHashHelper.GenerateCanonicalHash(pairsOrder1);
        var hash2 = ProductVariantCombinationHashHelper.GenerateCanonicalHash(pairsOrder2);

        Assert.Equal(hash1, hash2);
    }
}
