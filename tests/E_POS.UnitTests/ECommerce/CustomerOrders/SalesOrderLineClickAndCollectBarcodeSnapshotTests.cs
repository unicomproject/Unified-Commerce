using E_POS.Domain.Modules.Tenant.Orders.Entities;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class SalesOrderLineClickAndCollectBarcodeSnapshotTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateForClickAndCollect_CapturesAuthoritativeBarcodeSnapshot()
    {
        var line = SalesOrderLine.CreateForClickAndCollect(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "MER-012-SKU",
            "2000000001210",
            "Training Basketball",
            null,
            "PCS",
            "Pieces",
            "STANDARD",
            "SIMPLE",
            1m,
            1800m,
            1800m,
            0m,
            0m,
            false,
            Now);

        Assert.Equal("2000000001210", line.BarcodeSnapshot);
        Assert.Equal("MER-012-SKU", line.SkuSnapshot);
    }

    [Fact]
    public void CreateForClickAndCollect_DoesNotReuseSiblingLineBarcode()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var first = SalesOrderLine.CreateForClickAndCollect(
            Guid.NewGuid(), tenantId, orderId, 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SKU-A", "BARCODE-A", "Product A", null, "PCS", "Pieces", "STANDARD", "SIMPLE",
            1m, 10m, 10m, 0m, 0m, false, Now);
        var second = SalesOrderLine.CreateForClickAndCollect(
            Guid.NewGuid(), tenantId, orderId, 2, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SKU-B", "BARCODE-B", "Product B", null, "PCS", "Pieces", "STANDARD", "SIMPLE",
            1m, 20m, 20m, 0m, 0m, false, Now);

        Assert.Equal("BARCODE-A", first.BarcodeSnapshot);
        Assert.Equal("BARCODE-B", second.BarcodeSnapshot);
        Assert.NotEqual(first.BarcodeSnapshot, second.BarcodeSnapshot);
    }

    [Fact]
    public void CreateForClickAndCollect_WhitespaceBarcodeBecomesNull()
    {
        var line = SalesOrderLine.CreateForClickAndCollect(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "SKU-1", "   ", "Product", null, "PCS", "Pieces", "STANDARD", "SIMPLE",
            1m, 10m, 10m, 0m, 0m, false, Now);

        Assert.Null(line.BarcodeSnapshot);
    }
}
