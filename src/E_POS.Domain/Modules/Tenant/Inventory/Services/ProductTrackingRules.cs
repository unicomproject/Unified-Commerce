namespace E_POS.Domain.Modules.Tenant.Inventory.Services;

/// <summary>
/// Domain policy holding the authoritative tracking rule matrix for ProductInventorySetting.
/// </summary>
public static class ProductTrackingRules
{
    public static (bool IsValid, string? ErrorCode, string? ErrorMessage) ValidateTrackingCombination(
        bool isStockTracked,
        bool requiresBatchTracking,
        bool requiresExpiryTracking,
        bool requiresSerialTracking)
    {
        if (!isStockTracked)
        {
            if (requiresBatchTracking)
            {
                return (false, "product.track_inventory_required_for_batch", "Track Inventory must be ON to enable Batch / Lot Tracking.");
            }
            if (requiresExpiryTracking)
            {
                return (false, "product.track_inventory_required_for_expiry", "Track Inventory must be ON to enable Expiry Tracking.");
            }
            if (requiresSerialTracking)
            {
                return (false, "product.track_inventory_required_for_serial", "Track Inventory must be ON to enable Serial Number Tracking.");
            }
        }
        else
        {
            if (requiresSerialTracking)
            {
                if (requiresBatchTracking)
                {
                    return (false, "product.serial_incompatible_with_batch", "Serial Number Tracking cannot coexist with Batch / Lot Tracking.");
                }
                if (requiresExpiryTracking)
                {
                    return (false, "product.serial_incompatible_with_expiry", "Serial Number Tracking cannot coexist with Expiry Tracking.");
                }
            }

            if (requiresExpiryTracking && !requiresBatchTracking)
            {
                return (false, "product.batch_required_for_expiry", "Batch / Lot Tracking must be ON to enable Expiry Tracking.");
            }
        }

        return (true, null, null);
    }

    public static (bool IsStockTracked, bool RequiresBatchTracking, bool RequiresExpiryTracking, bool RequiresSerialTracking) NormalizeProfile(
        bool isStockTracked,
        bool requiresBatchTracking,
        bool requiresExpiryTracking,
        bool requiresSerialTracking)
    {
        if (!isStockTracked)
        {
            return (false, false, false, false);
        }

        if (requiresSerialTracking)
        {
            return (true, false, false, true);
        }

        var normalizedExpiry = requiresBatchTracking && requiresExpiryTracking;
        return (true, requiresBatchTracking, normalizedExpiry, false);
    }
}
