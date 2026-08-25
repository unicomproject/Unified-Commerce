using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ProductSetupInitialTrackingRulesTests
{
    [Fact]
    public void NormalizeBatch_Trims_And_EmptyBecomesNull()
    {
        Assert.Equal("BAT-1", ProductSetupInitialTrackingRules.NormalizeBatch("  BAT-1  "));
        Assert.Null(ProductSetupInitialTrackingRules.NormalizeBatch("   "));
    }

    [Fact]
    public void ValidateLengths_Rejects_BatchOver100_And_SerialOver150()
    {
        var batchError = ProductSetupInitialTrackingRules.ValidateLengths(new string('B', 101), null);
        Assert.NotNull(batchError);
        Assert.Equal("product.validation_failed", batchError!.Code);

        var serialError = ProductSetupInitialTrackingRules.ValidateLengths(null, new string('S', 151));
        Assert.NotNull(serialError);
    }

    [Fact]
    public void ValidateLengths_Allows_OptionalFields()
    {
        Assert.Null(ProductSetupInitialTrackingRules.ValidateLengths(null, null));
        Assert.Null(ProductSetupInitialTrackingRules.ValidateLengths("", "  "));
    }

    [Fact]
    public void EvaluateClear_QuantityOnly_RequiresConfirmation()
    {
        var plan = ProductSetupInitialTrackingRules.EvaluateClear(
            "SIMPLE",
            trackInventory: false,
            batchTracking: false,
            expiryTracking: false,
            serialTracking: false,
            "BAT-1",
            new DateOnly(2027, 6, 30),
            null);

        Assert.True(plan.RequiresConfirmation);
        Assert.Null(plan.BatchNumber);
        Assert.Null(plan.ExpiryDate);
        Assert.Null(plan.SerialNumber);
    }

    [Fact]
    public void EvaluateClear_BatchOnly_PreservesBatch_ClearsExpirySerial()
    {
        var plan = ProductSetupInitialTrackingRules.EvaluateClear(
            "SIMPLE",
            true,
            batchTracking: true,
            expiryTracking: false,
            serialTracking: false,
            "BAT-1",
            new DateOnly(2027, 6, 30),
            "SN-1");

        Assert.True(plan.RequiresConfirmation);
        Assert.Equal("BAT-1", plan.BatchNumber);
        Assert.Null(plan.ExpiryDate);
        Assert.Null(plan.SerialNumber);
    }

    [Fact]
    public void EvaluateClear_BatchAndExpiry_ClearsSerialOnly()
    {
        var plan = ProductSetupInitialTrackingRules.EvaluateClear(
            "SIMPLE",
            true,
            true,
            true,
            false,
            "BAT-1",
            new DateOnly(2027, 6, 30),
            "SN-1");

        Assert.True(plan.RequiresConfirmation);
        Assert.Equal("BAT-1", plan.BatchNumber);
        Assert.Equal(new DateOnly(2027, 6, 30), plan.ExpiryDate);
        Assert.Null(plan.SerialNumber);
    }

    [Fact]
    public void EvaluateClear_Serial_PreservesSerial()
    {
        var plan = ProductSetupInitialTrackingRules.EvaluateClear(
            "SIMPLE",
            true,
            false,
            false,
            true,
            "BAT-1",
            new DateOnly(2027, 6, 30),
            "SN-1");

        Assert.True(plan.RequiresConfirmation);
        Assert.Null(plan.BatchNumber);
        Assert.Null(plan.ExpiryDate);
        Assert.Equal("SN-1", plan.SerialNumber);
    }

    [Fact]
    public void EvaluateClear_Bundle_RequiresConfirmation()
    {
        var plan = ProductSetupInitialTrackingRules.EvaluateClear(
            "BUNDLE",
            true,
            true,
            false,
            false,
            "BAT-1",
            null,
            null);

        Assert.True(plan.RequiresConfirmation);
        Assert.Null(plan.BatchNumber);
    }
}
