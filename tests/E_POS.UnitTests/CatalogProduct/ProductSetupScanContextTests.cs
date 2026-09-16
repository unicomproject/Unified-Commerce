using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ProductSetupScanContextTests
{
    [Fact]
    public void Create_NormalizesCodesAndPreservesIdentifierLeadingZeros()
    {
        var now = DateTimeOffset.UtcNow;

        var context = ProductSetupScanContext.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " scan ",
            " 012345678905 ",
            " gtin12 ",
            " upca ",
            null,
            " not_started ",
            " provider-ref ",
            """{"name":"Example"}""",
            " sku-001 ",
            Guid.NewGuid(),
            now);

        Assert.Equal("SCAN", context.AcquisitionMode);
        Assert.Equal("012345678905", context.CandidateIdentifier);
        Assert.Equal("GTIN12", context.IdentifierStandard);
        Assert.Equal("UPCA", context.SymbologyHint);
        Assert.Equal("NOT_STARTED", context.ExternalLookupStatus);
        Assert.Equal(1, context.RowVersion);
    }

    [Fact]
    public void Update_IncrementsRowVersion()
    {
        var context = ProductSetupScanContext.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "NO_BARCODE",
            null,
            null,
            null,
            "OWN_MADE",
            null,
            null,
            null,
            "SKU-001",
            null,
            DateTimeOffset.UtcNow);

        context.Update(
            "MANUAL",
            "ABC-1",
            "OTHER",
            "CODE128",
            null,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);

        Assert.Equal(2, context.RowVersion);
        Assert.Equal("MANUAL", context.AcquisitionMode);
    }
}
