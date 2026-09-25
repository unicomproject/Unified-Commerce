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
    /// Explicit semantic map (D11).
    /// </summary>
    public static int MapLegacyPersistedStepToPublic(int legacyPersistedStep) =>
        legacyPersistedStep switch
        {
            1 => 2,
            2 => 3,
            3 => 3,
            4 => 3,
            5 => 3,
            6 => 4,
            7 => 6,
            _ => Math.Clamp(legacyPersistedStep, 1, 6)
        };

    /// <summary>
    /// Map public wizard steps to internal steps.
    /// </summary>
    public static int ResolveTargetSetupStep(string? productStructure, int publicCurrentSetupStep)
    {
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
            publicCurrent = Math.Clamp(setup.CurrentSetupStep, 1, 6);
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
