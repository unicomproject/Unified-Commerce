using E_POS.Application.Common.Models;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public static class ProductSetupInitialTrackingRules
{
    public const string IncompatibleConfirmationRequired =
        "product.initial_tracking.incompatible_values_require_confirmation";

    public const string BatchRequiredForExpiry =
        "product.initial_tracking.batch_required_for_expiry";

    public const string InvalidExpiryDate =
        "product.initial_tracking.invalid_expiry_date";

    public const string DuplicateBatch = "product.initial_tracking.duplicate_batch";
    public const string DuplicateSerial = "product.initial_tracking.duplicate_serial";
    public const string VariantAssignmentRequired =
        "product.initial_tracking.variant_assignment_required";
    public const string InvalidVariantAssignment =
        "product.initial_tracking.invalid_variant_assignment";
    public const string BundleParentNotSupported =
        "product.initial_tracking.bundle_parent_not_supported";

    public static string? NormalizeBatch(string? value) =>
        Normalize(value, ProductConstants.InitialBatchNumberMaxLength);

    public static string? NormalizeSerial(string? value) =>
        Normalize(value, ProductConstants.InitialSerialNumberMaxLength);

    public static ApplicationError? ValidateLengths(string? batchNumber, string? serialNumber)
    {
        var fieldErrors = new List<ApplicationFieldError>();
        if (batchNumber is { Length: > 0 } &&
            batchNumber.Trim().Length > ProductConstants.InitialBatchNumberMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "initialBatchNumber",
                $"Batch number cannot exceed {ProductConstants.InitialBatchNumberMaxLength} characters."));
        }

        if (serialNumber is { Length: > 0 } &&
            serialNumber.Trim().Length > ProductConstants.InitialSerialNumberMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "initialSerialNumber",
                $"Serial number cannot exceed {ProductConstants.InitialSerialNumberMaxLength} characters."));
        }

        return fieldErrors.Count == 0
            ? null
            : new ApplicationError("product.validation_failed", "Product validation failed.", fieldErrors);
    }

    public static bool HasAnyValues(string? batch, DateOnly? expiry, string? serial) =>
        !string.IsNullOrWhiteSpace(batch) || expiry.HasValue || !string.IsNullOrWhiteSpace(serial);

    public static TrackingClearPlan EvaluateClear(
        string productStructure,
        bool trackInventory,
        bool batchTracking,
        bool expiryTracking,
        bool serialTracking,
        string? batch,
        DateOnly? expiry,
        string? serial)
    {
        var structure = (productStructure ?? "SIMPLE").Trim().ToUpperInvariant();
        var keepBatch = NormalizeBatch(batch);
        var keepExpiry = expiry;
        var keepSerial = NormalizeSerial(serial);

        var isBundle = structure == "BUNDLE";
        var quantityOnly = !trackInventory || isBundle;

        if (quantityOnly)
        {
            if (!HasAnyValues(keepBatch, keepExpiry, keepSerial))
            {
                return TrackingClearPlan.Unchanged(keepBatch, keepExpiry, keepSerial);
            }

            return TrackingClearPlan.NeedsConfirmation(null, null, null);
        }

        if (serialTracking)
        {
            var clearBatch = keepBatch is not null;
            var clearExpiry = keepExpiry.HasValue;
            if (clearBatch || clearExpiry)
            {
                return TrackingClearPlan.NeedsConfirmation(null, null, keepSerial);
            }

            return TrackingClearPlan.Unchanged(null, null, keepSerial);
        }

        if (batchTracking && expiryTracking)
        {
            if (keepSerial is not null)
            {
                return TrackingClearPlan.NeedsConfirmation(keepBatch, keepExpiry, null);
            }

            return TrackingClearPlan.Unchanged(keepBatch, keepExpiry, null);
        }

        if (batchTracking)
        {
            var needsClear = keepExpiry.HasValue || keepSerial is not null;
            if (needsClear)
            {
                return TrackingClearPlan.NeedsConfirmation(keepBatch, null, null);
            }

            return TrackingClearPlan.Unchanged(keepBatch, null, null);
        }

        if (HasAnyValues(keepBatch, keepExpiry, keepSerial))
        {
            return TrackingClearPlan.NeedsConfirmation(null, null, null);
        }

        return TrackingClearPlan.Unchanged(keepBatch, keepExpiry, keepSerial);
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}

public sealed record TrackingClearPlan(
    string? BatchNumber,
    DateOnly? ExpiryDate,
    string? SerialNumber,
    bool RequiresConfirmation)
{
    public static TrackingClearPlan Unchanged(string? batch, DateOnly? expiry, string? serial) =>
        new(batch, expiry, serial, false);

    public static TrackingClearPlan NeedsConfirmation(string? batch, DateOnly? expiry, string? serial) =>
        new(batch, expiry, serial, true);
}
