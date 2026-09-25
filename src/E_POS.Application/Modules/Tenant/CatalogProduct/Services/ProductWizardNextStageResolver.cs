using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

/// <summary>
/// Canonical Save &amp; Continue next-stage resolution for Product Setup wizard processors.
/// Aligns with 7-Step Contract Unit &amp; Pack applicability matrix.
/// </summary>
public static class ProductWizardNextStageResolver
{
    /// <summary>
    /// Resolves the next legacy <see cref="ProductWizardStage"/> processor after a successful Continue.
    /// Scanner-first callers map the result via <see cref="ScannerFirstWizardStageMapper.MapProcessorToPublicStep"/>.
    /// </summary>
    public static int ResolveNextApplicableStage(string productStructure, bool trackInventory, int currentStage)
    {
        var normalizedStructure = ProductStructureConstants.Normalize(productStructure);

        if (currentStage == ProductWizardStage.ProductTypeTracking)
        {

            // SIMPLE / VARIANT: Units required only when Track Inventory ON.
            if (!trackInventory)
            {
                return string.Equals(normalizedStructure, ProductStructureConstants.Simple, StringComparison.OrdinalIgnoreCase)
                    ? ProductWizardStage.BarcodeSku
                    : ProductWizardStage.ProductConfiguration;
            }

            return ProductWizardStage.UnitsPackConversion;
        }

        if (currentStage == ProductWizardStage.UnitsPackConversion)
        {
            if (string.Equals(normalizedStructure, ProductStructureConstants.Simple, StringComparison.OrdinalIgnoreCase))
            {
                return ProductWizardStage.BarcodeSku;
            }

            return ProductWizardStage.ProductConfiguration;
        }

        if (currentStage == ProductWizardStage.ProductConfiguration &&
            string.Equals(normalizedStructure, ProductStructureConstants.Simple, StringComparison.OrdinalIgnoreCase))
        {
            return ProductWizardStage.BarcodeSku;
        }

        return Math.Min(currentStage + 1, ProductWizardStage.ReviewCreate);
    }
}
