using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class TenantAdminProductServicePhaseDTests
{
    [Fact]
    public void Step5_ScannerFirst_OmittedTracking_PreservesExisting()
    {
        var trackInventory = false;
        var batchTracking = false;
        var expiryTracking = false;
        var serialTracking = false;
        
        var isScannerFirst = true;
        var isSkip = false;
        string? requestTrackingMethod = null;
        
        var existingSetup = new ProductSetupWizardDto(
            Guid.NewGuid(), "Test", null, "DRAFT", null, 5, null, 1, null, null, null, null, true,
            TrackInventory: true,
            BatchTracking: true,
            ExpiryTracking: false,
            SerialTracking: false,
            ProductStructure: "SIMPLE",
            AllowOnlineSale: false,
            Images: [],
            HasLegacySerialTracking: true
        );

        if (isScannerFirst)
        {
            if (isSkip)
            {
                trackInventory = false;
                batchTracking = false;
                expiryTracking = false;
                serialTracking = existingSetup?.HasLegacySerialTracking ?? false;
            }
            else if (requestTrackingMethod != null)
            {
                ProductSetupCompatibilityHelper.ApplyTrackingMethod(
                    requestTrackingMethod, 
                    ref trackInventory, 
                    ref batchTracking, 
                    ref expiryTracking, 
                    ref serialTracking);
                serialTracking = serialTracking || (existingSetup?.HasLegacySerialTracking ?? false);
            }
            else
            {
                trackInventory = existingSetup?.TrackInventory ?? false;
                batchTracking = existingSetup?.BatchTracking ?? false;
                expiryTracking = existingSetup?.ExpiryTracking ?? false;
                serialTracking = existingSetup?.HasLegacySerialTracking ?? false;
            }
        }

        Assert.True(trackInventory);
        Assert.True(batchTracking);
        Assert.False(expiryTracking);
        Assert.True(serialTracking);
    }
    
    [Fact]
    public void Step5_ScannerFirst_ExplicitSkip_ClearsTrackingButPreservesLegacySerial()
    {
        var trackInventory = true;
        var batchTracking = true;
        var expiryTracking = false;
        var serialTracking = false;
        
        var isScannerFirst = true;
        var isSkip = true;
        string? requestTrackingMethod = null;
        
        var existingSetup = new ProductSetupWizardDto(
            Guid.NewGuid(), "Test", null, "DRAFT", null, 5, null, 1, null, null, null, null, true,
            TrackInventory: true,
            BatchTracking: true,
            ExpiryTracking: false,
            SerialTracking: false,
            ProductStructure: "SIMPLE",
            AllowOnlineSale: false,
            Images: [],
            HasLegacySerialTracking: true
        );

        if (isScannerFirst)
        {
            if (isSkip)
            {
                trackInventory = false;
                batchTracking = false;
                expiryTracking = false;
                serialTracking = existingSetup?.HasLegacySerialTracking ?? false;
            }
        }

        Assert.False(trackInventory);
        Assert.False(batchTracking);
        Assert.False(expiryTracking);
        Assert.True(serialTracking);
    }
}
