using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

/// <summary>
/// Phase A.1 Verification: ProductSetupCompatibilityHelper tests.
/// Validates 6-step normalization, tracking method mapping, and JSON serialization.
/// </summary>
public sealed class ProductSetupCompatibilityHelperTests
{
    #region Step Normalization Tests

    [Theory]
    [InlineData(1, 1, "Step 1 maps to Step 1")]
    [InlineData(2, 2, "Step 2 maps to Step 2")]
    [InlineData(3, 3, "Step 3 maps to Step 3")]
    [InlineData(4, 3, "Legacy Step 4 (Units) maps to Step 3")]
    [InlineData(5, 3, "Legacy Step 5 (old config) maps to Step 3")]
    [InlineData(6, 4, "Legacy Step 6 (Pricing) maps to Step 4")]
    [InlineData(7, 6, "Legacy Step 7 (Review) maps to Step 6")]
    public void NormalizeLegacySetupStep_MapsCorrectly(int legacyStep, int expectedTarget, string description)
    {
        var result = ProductSetupCompatibilityHelper.NormalizeLegacySetupStep(legacyStep);
        Assert.Equal(expectedTarget, result);
    }

    [Theory]
    [InlineData(0, "Step 0 is invalid")]
    [InlineData(-1, "Negative step is invalid")]
    [InlineData(8, "Step 8 (beyond legacy) is unknown")]
    [InlineData(100, "Step 100 is unknown")]
    public void NormalizeLegacySetupStep_HandlesInvalidValues_AsPassthrough(int invalidStep, string description)
    {
        var result = ProductSetupCompatibilityHelper.NormalizeLegacySetupStep(invalidStep);
        // Fallback: return as-is for unknown values
        Assert.Equal(invalidStep, result);
    }

    #endregion

    #region Tracking Method Mapping Tests

    [Fact]
    public void MapPolicyToTrackingMethod_NotStockTracked_ReturnsNull()
    {
        var result = ProductSetupCompatibilityHelper.MapPolicyToTrackingMethod(
            isStockTracked: false,
            batch: false,
            expiry: false);

        Assert.Null(result);
    }

    [Fact]
    public void MapPolicyToTrackingMethod_QuantityOnly_ReturnsQuantity()
    {
        var result = ProductSetupCompatibilityHelper.MapPolicyToTrackingMethod(
            isStockTracked: true,
            batch: false,
            expiry: false);

        Assert.Equal(TrackingMethodConstants.Quantity, result);
    }

    [Fact]
    public void MapPolicyToTrackingMethod_BatchOnly_ReturnsBatch()
    {
        var result = ProductSetupCompatibilityHelper.MapPolicyToTrackingMethod(
            isStockTracked: true,
            batch: true,
            expiry: false);

        Assert.Equal(TrackingMethodConstants.Batch, result);
    }

    [Fact]
    public void MapPolicyToTrackingMethod_BatchAndExpiry_ReturnsBatchExpiry()
    {
        var result = ProductSetupCompatibilityHelper.MapPolicyToTrackingMethod(
            isStockTracked: true,
            batch: true,
            expiry: true);

        Assert.Equal(TrackingMethodConstants.BatchExpiry, result);
    }

    #endregion

    #region Apply Tracking Method Tests

    [Fact]
    public void ApplyTrackingMethod_Quantity_SetsCorrectPolicyState()
    {
        bool isStockTracked = false, batch = false, expiry = false, serial = false;

        ProductSetupCompatibilityHelper.ApplyTrackingMethod(
            TrackingMethodConstants.Quantity,
            ref isStockTracked,
            ref batch,
            ref expiry,
            ref serial);

        Assert.True(isStockTracked);
        Assert.False(batch);
        Assert.False(expiry);
        Assert.False(serial); // Serial not affected by Quantity
    }

    [Fact]
    public void ApplyTrackingMethod_Batch_SetsCorrectPolicyState()
    {
        bool isStockTracked = false, batch = false, expiry = false, serial = false;

        ProductSetupCompatibilityHelper.ApplyTrackingMethod(
            TrackingMethodConstants.Batch,
            ref isStockTracked,
            ref batch,
            ref expiry,
            ref serial);

        Assert.True(isStockTracked);
        Assert.True(batch);
        Assert.False(expiry);
        Assert.False(serial);
    }

    [Fact]
    public void ApplyTrackingMethod_BatchExpiry_SetsCorrectPolicyState()
    {
        bool isStockTracked = false, batch = false, expiry = false, serial = false;

        ProductSetupCompatibilityHelper.ApplyTrackingMethod(
            TrackingMethodConstants.BatchExpiry,
            ref isStockTracked,
            ref batch,
            ref expiry,
            ref serial);

        Assert.True(isStockTracked);
        Assert.True(batch);
        Assert.True(expiry);
        Assert.False(serial);
    }

    [Fact]
    public void ApplyTrackingMethod_Null_SkipsTracking()
    {
        bool isStockTracked = true, batch = true, expiry = true, serial = true;

        ProductSetupCompatibilityHelper.ApplyTrackingMethod(
            null,
            ref isStockTracked,
            ref batch,
            ref expiry,
            ref serial);

        Assert.False(isStockTracked);
        Assert.False(batch);
        Assert.False(expiry);
        Assert.True(serial); // Serial preserved
    }

    [Fact]
    public void ApplyTrackingMethod_PreservesLegacySerial_WhenOmitted()
    {
        // Existing product with legacy serial tracking
        bool isStockTracked = true, batch = false, expiry = false, serial = true;

        // Apply Quantity tracking (serial not in target method)
        ProductSetupCompatibilityHelper.ApplyTrackingMethod(
            TrackingMethodConstants.Quantity,
            ref isStockTracked,
            ref batch,
            ref expiry,
            ref serial);

        // Serial should remain preserved (not cleared)
        Assert.True(serial);
    }

    [Fact]
    public void ApplyTrackingMethod_UnknownMethod_TreatsAsSkip()
    {
        bool isStockTracked = true, batch = true, expiry = true, serial = false;

        ProductSetupCompatibilityHelper.ApplyTrackingMethod(
            "UNKNOWN_METHOD",
            ref isStockTracked,
            ref batch,
            ref expiry,
            ref serial);

        // Unknown treated as Skip
        Assert.False(isStockTracked);
        Assert.False(batch);
        Assert.False(expiry);
    }

    #endregion

    #region Quantity Draft Serialization Tests

    [Fact]
    public void DeserializeQuantityDraft_NullOrEmpty_ReturnsNull()
    {
        var resultNull = ProductSetupCompatibilityHelper.DeserializeQuantityDraft(null);
        Assert.Null(resultNull);

        var resultEmpty = ProductSetupCompatibilityHelper.DeserializeQuantityDraft("   ");
        Assert.Null(resultEmpty);
    }

    [Fact]
    public void DeserializeQuantityDraft_ValidJson_ReturnsDto()
    {
        var json = @"{
            ""stockOwners"": [
                {
                    ""variantId"": null,
                    ""openingQuantity"": 100,
                    ""allocations"": [
                        {
                            ""outletId"": ""12345678-1234-1234-1234-123456789012"",
                            ""quantity"": 100
                        }
                    ]
                }
            ]
        }";

        var result = ProductSetupCompatibilityHelper.DeserializeQuantityDraft(json);

        Assert.NotNull(result);
        Assert.Single(result.StockOwners);
        Assert.Equal(100m, result.StockOwners[0].OpeningQuantity);
        Assert.Single(result.StockOwners[0].Allocations);
    }

    [Fact]
    public void DeserializeQuantityDraft_MalformedJson_ReturnsNull()
    {
        var malformed = "{ invalid json ]";

        var result = ProductSetupCompatibilityHelper.DeserializeQuantityDraft(malformed);

        // Malformed JSON handled gracefully
        Assert.Null(result);
    }

    [Fact]
    public void DeserializeQuantityDraft_EmptyArray_ReturnsNull()
    {
        var emptyJson = "{ \"stockOwners\": [] }";

        var result = ProductSetupCompatibilityHelper.DeserializeQuantityDraft(emptyJson);

        Assert.NotNull(result);
        Assert.Empty(result.StockOwners);
    }

    [Fact]
    public void SerializeQuantityDraft_Null_ReturnsNull()
    {
        var result = ProductSetupCompatibilityHelper.SerializeQuantityDraft(null);
        Assert.Null(result);
    }

    [Fact]
    public void SerializeQuantityDraft_EmptyStockOwners_ReturnsNull()
    {
        var dto = new OpeningStockDraftDto { StockOwners = new() };

        var result = ProductSetupCompatibilityHelper.SerializeQuantityDraft(dto);

        Assert.Null(result);
    }

    [Fact]
    public void SerializeQuantityDraft_ValidDto_ReturnsJsonString()
    {
        var variantId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var outletId = Guid.Parse("87654321-4321-4321-4321-210987654321");

        var dto = new OpeningStockDraftDto
        {
            StockOwners = new()
            {
                new OpeningStockOwnerDraftDto
                {
                    VariantId = variantId,
                    OpeningQuantity = 50m,
                    Allocations = new()
                    {
                        new OutletAllocationDraftDto
                        {
                            OutletId = outletId,
                            Quantity = 50m
                        }
                    }
                }
            }
        };

        var result = ProductSetupCompatibilityHelper.SerializeQuantityDraft(dto);

        Assert.NotNull(result);
        Assert.Contains("\"openingQuantity\"", result);
        Assert.Contains("50", result);
    }

    [Fact]
    public void SerializeAndDeserialize_RoundTrip_PreservesData()
    {
        var variantId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var outletId = Guid.Parse("87654321-4321-4321-4321-210987654321");

        var original = new OpeningStockDraftDto
        {
            StockOwners = new()
            {
                new OpeningStockOwnerDraftDto
                {
                    VariantId = variantId,
                    OpeningQuantity = 100m,
                    Allocations = new()
                    {
                        new OutletAllocationDraftDto
                        {
                            OutletId = outletId,
                            Quantity = 60m
                        },
                        new OutletAllocationDraftDto
                        {
                            OutletId = Guid.NewGuid(),
                            Quantity = 40m
                        }
                    }
                }
            }
        };

        var serialized = ProductSetupCompatibilityHelper.SerializeQuantityDraft(original);
        var deserialized = ProductSetupCompatibilityHelper.DeserializeQuantityDraft(serialized);

        Assert.NotNull(deserialized);
        Assert.Single(deserialized.StockOwners);
        Assert.Equal(original.StockOwners[0].OpeningQuantity, deserialized.StockOwners[0].OpeningQuantity);
        Assert.Equal(original.StockOwners[0].Allocations.Count, deserialized.StockOwners[0].Allocations.Count);
    }

    #endregion
}
