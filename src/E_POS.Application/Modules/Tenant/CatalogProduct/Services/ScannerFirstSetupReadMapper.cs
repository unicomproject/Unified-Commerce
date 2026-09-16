using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

/// <summary>
/// B9 read-time compatibility: legacy persisted <c>current_setup_step</c> → scanner-first public setup DTO.
/// Separate from <see cref="ScannerFirstWizardStageMapper"/> (write path). Never mutates the database.
/// </summary>
public static class ScannerFirstSetupReadMapper
{
    public const string AcquisitionLegacy = "LEGACY";

    /// <summary>
    /// Explicit semantic map (D11). Not arithmetic — old 4 and old 5 both become public 5.
    /// </summary>
    public static int MapLegacyPersistedStepToPublic(int legacyPersistedStep) =>
        legacyPersistedStep switch
        {
            1 => 2,
            2 => 3,
            3 => 4,
            4 => 5,
            5 => 5,
            6 => 6,
            7 => 7,
            _ => Math.Clamp(legacyPersistedStep, 1, 7)
        };

    /// <summary>
    /// Preserve documented BUNDLE Units skip: public Units (4) → navigate to Product Configuration (5).
    /// </summary>
    public static int ResolveTargetSetupStep(string? productStructure, int publicCurrentSetupStep)
    {
        var structure = ProductStructureConstants.Normalize(productStructure ?? ProductStructureConstants.Simple);
        if (string.Equals(structure, ProductStructureConstants.Bundle, StringComparison.OrdinalIgnoreCase) &&
            publicCurrentSetupStep == ScannerFirstWizardStageMapper.PublicUnitsPackConversion)
        {
            return ScannerFirstWizardStageMapper.PublicProductConfiguration;
        }

        return publicCurrentSetupStep;
    }

    /// <summary>
    /// Applies B9 resume projection. <paramref name="hasScanContextRow"/> is presence of
    /// <c>product_setup_scan_context</c> (scanner-first drafts are not legacy-remapped).
    /// </summary>
    public static ProductSetupWizardDto ApplyReadCompatibility(
        ProductSetupWizardDto setup,
        bool hasScanContextRow)
    {
        int publicCurrent;
        ProductSetupScanContextDto? scanContext;

        if (hasScanContextRow)
        {
            publicCurrent = Math.Clamp(setup.CurrentSetupStep, 1, 7);
            scanContext = setup.ScanContext;
        }
        else
        {
            publicCurrent = MapLegacyPersistedStepToPublic(setup.CurrentSetupStep);
            scanContext = ProductSetupScanContextDto.CreateLegacy();
        }

        var target = ResolveTargetSetupStep(setup.ProductStructure, publicCurrent);

        return setup with
        {
            CurrentSetupStep = publicCurrent,
            TargetSetupStep = target,
            LastCompletedSetupStep = publicCurrent,
            ScanContext = scanContext
        };
    }
}
