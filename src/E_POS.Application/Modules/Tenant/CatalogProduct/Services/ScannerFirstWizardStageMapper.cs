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
    public const int PublicPricingTax = 4;
    public const int PublicProductTracking = 5;
    public const int PublicReviewCreate = 6;

    /// <summary>
    /// Maps a scanner-first public/API step to the legacy processor stage that owns the write.
    /// Step 3 is SPECIAL/COMPOSITE — routes to Product Configuration processor for existing handlers.
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
                isSpecialComposite = true;
                // Semantic (not arithmetic): Step 3 absorbs legacy config (4), units (3), barcode/SKU (5).
                processorStage = ProductWizardStage.ProductConfiguration;
                return true;
            case PublicPricingTax:
                processorStage = ProductWizardStage.PricingTax;
                return true;
            case PublicProductTracking:
                processorStage = 0; // Handled in future Phase E
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
    /// for persistence.
    /// </summary>
    public static int MapProcessorToPublicStep(int processorStage) =>
        processorStage switch
        {
            ProductWizardStage.BasicDetails => PublicBasicDetails,
            ProductWizardStage.ProductTypeTracking => PublicProductTypeTracking,
            ProductWizardStage.UnitsPackConversion => PublicProductTypeTracking,
            ProductWizardStage.ProductConfiguration => PublicProductTypeTracking,
            ProductWizardStage.BarcodeSku => PublicProductTypeTracking,
            ProductWizardStage.PricingTax => PublicPricingTax,
            ProductWizardStage.ReviewCreate => PublicReviewCreate,
            _ => processorStage
        };

    /// <summary>
    /// True when <paramref name="scannerApiStep"/> is the SPECIAL/COMPOSITE Step 3.
    /// </summary>
    public static bool IsSpecialCompositeStep(int scannerApiStep) =>
        scannerApiStep == PublicProductTypeTracking;
}
