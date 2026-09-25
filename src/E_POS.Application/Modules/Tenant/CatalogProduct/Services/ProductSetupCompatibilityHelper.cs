using System.Text.Json;
using E_POS.Application.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public static class ProductSetupCompatibilityHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static int NormalizeLegacySetupStep(int legacyStep)
    {
        return legacyStep switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 3,
            5 => 3,
            6 => 4,
            7 => 6,
            _ => legacyStep // Fallback for target steps or unknown
        };
    }

    public static string? MapPolicyToTrackingMethod(bool isStockTracked, bool batch, bool expiry)
    {
        if (!isStockTracked) return null;
        if (!batch) return TrackingMethodConstants.Quantity;
        if (!expiry) return TrackingMethodConstants.Batch;
        return TrackingMethodConstants.BatchExpiry;
    }

    public static void ApplyTrackingMethod(string? trackingMethod, ref bool isStockTracked, ref bool batch, ref bool expiry, ref bool serial)
    {
        // Preserve legacy serial unless it's explicitly part of the target method logic (it's not).
        // The instructions say: omission means preserve legacy serial state.
        
        switch (trackingMethod)
        {
            case TrackingMethodConstants.Quantity:
                isStockTracked = true;
                batch = false;
                expiry = false;
                break;
            case TrackingMethodConstants.Batch:
                isStockTracked = true;
                batch = true;
                expiry = false;
                break;
            case TrackingMethodConstants.BatchExpiry:
                isStockTracked = true;
                batch = true;
                expiry = true;
                break;
            case null:
            default: // Treat unknown as Skip
                isStockTracked = false;
                batch = false;
                expiry = false;
                break;
        }
    }

    public static OpeningStockDraftDto? DeserializeQuantityDraft(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;

        try
        {
            return JsonSerializer.Deserialize<OpeningStockDraftDto>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            // Log malformed payload here ideally
            return null;
        }
    }

    public static string? SerializeQuantityDraft(OpeningStockDraftDto? draft)
    {
        if (draft == null || draft.StockOwners == null || !draft.StockOwners.Any())
            return null;

        return JsonSerializer.Serialize(draft, JsonOptions);
    }
}
