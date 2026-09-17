using System.Text.Json.Serialization;

namespace E_POS.Infrastructure.Integrations.ProductLookup.OpenFoodFacts;

/// <summary>
/// Root response model for Open Food Facts v2 product API:
/// https://world.openfoodfacts.org/api/v2/product/{barcode}.json
/// </summary>
public sealed class OpenFoodFactsResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("status_verbose")]
    public string? StatusVerbose { get; set; }

    [JsonPropertyName("product")]
    public OpenFoodFactsProduct? Product { get; set; }
}

public sealed class OpenFoodFactsProduct
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("product_name_en")]
    public string? ProductNameEn { get; set; }

    [JsonPropertyName("generic_name")]
    public string? GenericName { get; set; }

    [JsonPropertyName("generic_name_en")]
    public string? GenericNameEn { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("categories")]
    public string? Categories { get; set; }

    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }

    [JsonPropertyName("image_front_small_url")]
    public string? ImageFrontSmallUrl { get; set; }

    [JsonPropertyName("countries_tags")]
    public List<string>? CountriesTags { get; set; }
}
