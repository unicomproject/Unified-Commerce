using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

/// <summary>
/// Centralized scanner-first API/persisted step → legacy <see cref="ProductWizardStage"/> processor mapping
/// (WSM decision 2026-09-13 — OPTION 1 <c>persist_2_plus_write_map</c>).
/// Does not change persisted <c>current_setup_step</c>; never uses arithmetic ±1.
/// </summary>
public static class ScannerFirstWizardStageMapper
{
    public const int PublicBasicDetails = 2;
    public const int PublicProductTypeTracking = 3;
    public const int PublicUnitsPackConversion = 4;
    public const int PublicProductConfiguration = 5;
    public const int PublicPricingTax = 6;
    public const int PublicReviewCreate = 7;

    /// <summary>
    /// Maps a scanner-first public/API step to the legacy processor stage that owns the write.
    /// Step 5 is SPECIAL/COMPOSITE — routes to Product Configuration processor for existing handlers;
    /// final identifier section behavior remains B10.
    /// </summary>
    public static bool TryMapToProcessorStage(
        int scannerApiStep,
        out int processorStage,
        out bool isSpecialComposite)
    {
        isSpecialComposite = false;
        switch (scannerApiStep)
        {
            case PublicBasicDetails:
                processorStage = ProductWizardStage.BasicDetails;
                return true;
            case PublicProductTypeTracking:
                processorStage = ProductWizardStage.ProductTypeTracking;
                return true;
            case PublicUnitsPackConversion:
                processorStage = ProductWizardStage.UnitsPackConversion;
                return true;
            case PublicProductConfiguration:
                isSpecialComposite = true;
                // Semantic (not arithmetic): Step 5 absorbs legacy config (4) + barcode/SKU (5).
                // B10: service validates + repository persists both in one atomic save.
                processorStage = ProductWizardStage.ProductConfiguration;
                return true;
            case PublicPricingTax:
                processorStage = ProductWizardStage.PricingTax;
                return true;
            case PublicReviewCreate:
                processorStage = ProductWizardStage.ReviewCreate;
                return true;
            default:
                processorStage = 0;
                return false;
        }
    }

    /// <summary>
    /// Maps a legacy processor stage (including ResolveNext results) back to scanner-first public step
    /// for persistence. Legacy BarcodeSku (5) and ProductConfiguration (4) both map to public Step 5.
    /// </summary>
    public static int MapProcessorToPublicStep(int processorStage) =>
        processorStage switch
        {
            ProductWizardStage.BasicDetails => PublicBasicDetails,
            ProductWizardStage.ProductTypeTracking => PublicProductTypeTracking,
            ProductWizardStage.UnitsPackConversion => PublicUnitsPackConversion,
            ProductWizardStage.ProductConfiguration => PublicProductConfiguration,
            ProductWizardStage.BarcodeSku => PublicProductConfiguration,
            ProductWizardStage.PricingTax => PublicPricingTax,
            ProductWizardStage.ReviewCreate => PublicReviewCreate,
            _ => processorStage
        };

    /// <summary>
    /// True when <paramref name="scannerApiStep"/> is the SPECIAL/COMPOSITE Step 5.
    /// </summary>
    public static bool IsSpecialCompositeStep(int scannerApiStep) =>
        scannerApiStep == PublicProductConfiguration;
}
