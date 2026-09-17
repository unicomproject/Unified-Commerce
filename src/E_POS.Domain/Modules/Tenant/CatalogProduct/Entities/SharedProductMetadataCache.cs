using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

/// <summary>
/// Global shared product metadata cache table.
/// Stores normalized external product suggestions to prevent redundant third-party API calls.
/// Completely independent of tenant products and contains no tenant master or inventory data.
/// </summary>
public class SharedProductMetadataCache : AuditableEntity
{
    public string NormalizedBarcode { get; protected set; } = string.Empty;
    public string? IdentifierStandard { get; protected set; }
    public string Provider { get; protected set; } = string.Empty;
    public string NormalizedMetadataJson { get; protected set; } = string.Empty;
    public string? RawResponseJson { get; protected set; }
    public DateTimeOffset CachedAt { get; protected set; }
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? LastVerifiedAt { get; protected set; }

    protected SharedProductMetadataCache() { }

    public static SharedProductMetadataCache Create(
        Guid id,
        string normalizedBarcode,
        string? identifierStandard,
        string provider,
        string normalizedMetadataJson,
        string? rawResponseJson,
        DateTimeOffset cachedAt,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedBarcode);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedMetadataJson);

        return new SharedProductMetadataCache
        {
            Id = id,
            NormalizedBarcode = normalizedBarcode.Trim(),
            IdentifierStandard = string.IsNullOrWhiteSpace(identifierStandard) ? null : identifierStandard.Trim().ToUpperInvariant(),
            Provider = provider.Trim().ToLowerInvariant(),
            NormalizedMetadataJson = normalizedMetadataJson.Trim(),
            RawResponseJson = string.IsNullOrWhiteSpace(rawResponseJson) ? null : rawResponseJson.Trim(),
            CachedAt = cachedAt,
            ExpiresAt = expiresAt,
            LastVerifiedAt = cachedAt,
            CreatedAt = cachedAt,
            UpdatedAt = cachedAt,
        };
    }

    public void Update(
        string? identifierStandard,
        string normalizedMetadataJson,
        string? rawResponseJson,
        DateTimeOffset cachedAt,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedMetadataJson);

        IdentifierStandard = string.IsNullOrWhiteSpace(identifierStandard) ? null : identifierStandard.Trim().ToUpperInvariant();
        NormalizedMetadataJson = normalizedMetadataJson.Trim();
        RawResponseJson = string.IsNullOrWhiteSpace(rawResponseJson) ? null : rawResponseJson.Trim();
        CachedAt = cachedAt;
        ExpiresAt = expiresAt;
        LastVerifiedAt = cachedAt;
        UpdatedAt = cachedAt;
    }

    public bool IsExpired(DateTimeOffset utcNow) => utcNow >= ExpiresAt;
}
