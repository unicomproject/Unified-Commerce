using System;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Services;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public class ProductVariantCodeGeneratorTests
{
    [Fact]
    public void GenerateVariantCode_UsesProductCodeAnd8CharHash()
    {
        var productId = Guid.NewGuid();
        var hash = new string('A', 64);
        
        var code = ProductVariantCodeGenerator.GenerateVariantCode(
            "TEST-CODE",
            productId,
            hash,
            _ => false); // no collision

        Assert.Equal("VAR-TESTCODE-AAAAAAAA", code);
    }

    [Fact]
    public void GenerateVariantCode_ResolvesCollision12Chars()
    {
        var productId = Guid.NewGuid();
        var hash = new string('B', 64);
        
        var code = ProductVariantCodeGenerator.GenerateVariantCode(
            "TESTCODE",
            productId,
            hash,
            c => c.Length == 21); // collide on 8 char suffix (VAR-TESTCODE-8chars)

        Assert.Equal("VAR-TESTCODE-BBBBBBBBBBBB", code);
    }

    [Fact]
    public void GenerateVariantCode_ThrowsOnMaxCollision()
    {
        var productId = Guid.NewGuid();
        var hash = new string('C', 64);
        
        Assert.Throws<InvalidOperationException>(() => 
            ProductVariantCodeGenerator.GenerateVariantCode(
                "TESTCODE",
                productId,
                hash,
                _ => true)); // always collides
    }
}
