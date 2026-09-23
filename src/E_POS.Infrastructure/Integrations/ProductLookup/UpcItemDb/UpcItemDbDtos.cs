using System.Text.Json.Serialization;

namespace E_POS.Infrastructure.Integrations.ProductLookup.UpcItemDb;

/// <summary>
/// Raw UPCitemdb "lookup" response envelope. Internal to this adapter — never exposed outside
/// the provider. Deliberately does NOT model offers[]/pricing/asin/elid fields: marketplace and
/// pricing data must never enter OneVerz (tenant pricing is tenant-authoritative), so those
/// fields are simply never deserialized rather than mapped-then-dropped.
/// </summary>
internal sealed class UpcItemDbResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    [JsonPropertyName("items")]
    public List<UpcItemDbItem>? Items { get; set; }
}

internal sealed class UpcItemDbItem
{
    [JsonPropertyName("ean")]
    public string? Ean { get; set; }

    [JsonPropertyName("upc")]
    public string? Upc { get; set; }

    [JsonPropertyName("gtin")]
    public string? Gtin { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("dimension")]
    public string? Dimension { get; set; }

    [JsonPropertyName("weight")]
    public string? Weight { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }
}
