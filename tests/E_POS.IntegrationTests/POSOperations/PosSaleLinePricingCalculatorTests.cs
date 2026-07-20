using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Inventory.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.PricingTax.Entities;
using E_POS.Infrastructure.Modules.Tenant.POSOperations.Services;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.POSOperations;

public sealed class PosSaleLinePricingCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CalculateAsync_TaxExclusiveProduct_AppliesTaxOnSubtotal()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var lineKey = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedPricedSellableVariantAsync(
            db, tenantId, outletId, productId, variantId, listedPrice: 100m, available: 10m, priceIncludesTax: false);
        await SeedProductTaxAsync(db, tenantId, productId, variantId, ratePercent: 10m, isCompound: false);
        await db.SaveChangesAsync();

        var calculator = new PosSaleLinePricingCalculator(db);
        var result = await calculator.CalculateAsync(
            tenantId,
            outletId,
            [new PosSaleLinePricingRequest(lineKey, productId, variantId, 2m)],
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal(200m, result.Subtotal);
        Assert.Equal(20m, result.TaxTotal);
        Assert.Equal(0m, result.DiscountTotal);
        Assert.Equal(220m, result.GrandTotal);
        Assert.Equal(100m, result.Lines[0].UnitPrice);
        Assert.Equal(20m, result.Lines[0].LineTax);
        Assert.Equal(220m, result.Lines[0].LineTotal);
    }

    [Fact]
    public async Task CalculateAsync_TaxInclusiveProduct_PeelsTaxFromListedPrice()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var lineKey = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedPricedSellableVariantAsync(
            db, tenantId, outletId, productId, variantId, listedPrice: 110m, available: 5m, priceIncludesTax: true);
        await SeedProductTaxAsync(db, tenantId, productId, variantId, ratePercent: 10m, isCompound: false);
        await db.SaveChangesAsync();

        var calculator = new PosSaleLinePricingCalculator(db);
        var result = await calculator.CalculateAsync(
            tenantId,
            outletId,
            [new PosSaleLinePricingRequest(lineKey, productId, variantId, 1m)],
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal(100m, result.Subtotal);
        Assert.Equal(10m, result.TaxTotal);
        Assert.Equal(110m, result.GrandTotal);
        Assert.Equal(100m, result.Lines[0].UnitPrice);
    }

    [Fact]
    public async Task CalculateAsync_QuantityTier_UsesHighestEligibleMinQuantity()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var lineKey = Guid.NewGuid();

        await using var db = CreateDbContext();
        var priceListId = await SeedPricedSellableVariantAsync(
            db, tenantId, outletId, productId, variantId, listedPrice: 100m, available: 20m, priceIncludesTax: false);
        await db.SaveChangesAsync();
        var uomId = await db.ProductVariants.AsNoTracking()
            .Where(x => x.Id == variantId)
            .Select(x => x.SalesUomId)
            .SingleAsync();
        db.PriceListItems.Add(PriceListItem.Create(
            Guid.NewGuid(), tenantId, priceListId, productId, variantId, uomId,
            90m, null, 3m, null, null, "ACTIVE", null, Now));
        await db.SaveChangesAsync();

        var calculator = new PosSaleLinePricingCalculator(db);
        var result = await calculator.CalculateAsync(
            tenantId,
            outletId,
            [new PosSaleLinePricingRequest(lineKey, productId, variantId, 3m)],
            Now,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.ErrorCode);
        Assert.Equal(270m, result.Subtotal);
        Assert.Equal(90m, result.Lines[0].ListedUnitPrice);
    }

    [Fact]
    public async Task CalculateAsync_InsufficientOutletStock_Fails()
    {
        var tenantId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await using var db = CreateDbContext();
        await SeedPricedSellableVariantAsync(
            db, tenantId, outletId, productId, variantId, listedPrice: 50m, available: 1m, priceIncludesTax: false);
        await db.SaveChangesAsync();

        var calculator = new PosSaleLinePricingCalculator(db);
        var result = await calculator.CalculateAsync(
            tenantId,
            outletId,
            [new PosSaleLinePricingRequest(Guid.NewGuid(), productId, variantId, 2m)],
            Now,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("insufficient_outlet_stock", result.ErrorCode);
    }

    private static async Task<Guid> SeedPricedSellableVariantAsync(
        EPosDbContext db,
        Guid tenantId,
        Guid outletId,
        Guid productId,
        Guid variantId,
        decimal listedPrice,
        decimal available,
        bool priceIncludesTax)
    {
        var uomId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        db.Outlets.Add(Outlet.Create(
            outletId, tenantId, "Main Outlet", "MAIN-01", "ACTIVE", "STORE", "UTC",
            true, null, null, null, Now));
        db.UnitOfMeasures.Add(UnitOfMeasure.Create(
            uomId, tenantId, "EA", "Each", "COUNT", "ea", null, 1m,
            ProductConstants.ActiveStatus, Now));
        db.Products.Add(Product.Create(
            productId, tenantId, "P-1", "Exchange Product", "exchange-product",
            "STANDARD", "SIMPLE", null, null, null, null, null,
            true, true, ProductConstants.ActiveStatus, null, Now));
        db.ProductVariants.Add(ProductVariant.Create(
            variantId, tenantId, productId, "DEFAULT", "Exchange Product", "SKU-1",
            uomId, uomId, true, true, false, ProductConstants.ActiveStatus, null, Now));
        db.PriceLists.Add(PriceList.Create(
            priceListId, tenantId, "DEFAULT", "Default", "STANDARD", "LKR",
            priceIncludesTax, true, 1, null, null, "ACTIVE", null, Now));
        db.PriceListItems.Add(PriceListItem.Create(
            Guid.NewGuid(), tenantId, priceListId, productId, variantId, uomId,
            listedPrice, null, 1m, null, null, "ACTIVE", null, Now));
        db.InventoryLocations.Add(InventoryLocation.Create(
            locationId, tenantId, outletId, null, "POS-FLOOR", "POS Floor", "STORE",
            true, false, false, false, "ACTIVE", null, Now));
        var balance = InventoryBalance.Create(
            Guid.NewGuid(), tenantId, locationId, productId, variantId, null, Now);
        balance.AdjustQuantities(available, 0, 0, 0, Now);
        db.InventoryBalances.Add(balance);
        await Task.CompletedTask;
        return priceListId;
    }

    private static async Task SeedProductTaxAsync(
        EPosDbContext db,
        Guid tenantId,
        Guid productId,
        Guid variantId,
        decimal ratePercent,
        bool isCompound)
    {
        var jurisdiction = TaxJurisdiction.Create(
            tenantId, "LK", "Sri Lanka", "COUNTRY", "LK", null, null, null, null, Now);
        db.TaxJurisdictions.Add(jurisdiction);
        await db.SaveChangesAsync();

        var taxClass = TaxClass.Create(tenantId, "STD", "Standard", null, true, null, Now);
        db.TaxClasses.Add(taxClass);
        await db.SaveChangesAsync();

        var taxRate = TaxRate.Create(
            tenantId, jurisdiction.Id, "VAT10", "VAT 10%", ratePercent, isCompound,
            null, null, null, Now);
        db.TaxRates.Add(taxRate);
        await db.SaveChangesAsync();

        db.TaxClassRates.Add(TaxClassRate.Create(tenantId, taxClass.Id, taxRate.Id, 1, null, Now));
        db.ProductTaxAssignments.Add(ProductTaxAssignment.Create(
            tenantId, productId, variantId, taxClass.Id, null, null, null, Now));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new EPosDbContext(options);
    }
}
