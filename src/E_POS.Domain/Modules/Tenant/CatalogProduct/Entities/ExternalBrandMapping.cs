using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

/// <summary>
/// Tenant-specific mapping from an external provider brand key (e.g. OpenFoodFacts, UPCitemdb)
/// to an authoritative tenant brand. External brand data is enrichment only — the final brand
/// selected by the user during product create is authoritative.
/// </summary>
public class ExternalBrandMapping : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Provider { get; protected set; } = string.Empty;
    public string ExternalBrandKey { get; protected set; } = string.Empty;
    public string ExternalBrandName { get; protected set; } = string.Empty;
    public Guid TenantBrandId { get; protected set; }
    public string MappingSource { get; protected set; } = ExternalBrandMappingConstants.DefaultMappingSource;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected ExternalBrandMapping() { }

    public static ExternalBrandMapping Create(
        Guid id,
        Guid tenantId,
        string provider,
        string externalBrandKey,
        string externalBrandName,
        Guid tenantBrandId,
        string mappingSource,
        Guid? userId,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalBrandKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalBrandName);
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (tenantBrandId == Guid.Empty) throw new ArgumentException("TenantBrandId cannot be empty.", nameof(tenantBrandId));

        var trimmedProvider = provider.Trim().ToLowerInvariant();
        var trimmedKey = externalBrandKey.Trim().ToLowerInvariant();
        var trimmedName = externalBrandName.Trim();

        if (trimmedProvider.Length > ExternalBrandMappingConstants.ProviderMaxLength)
            throw new ArgumentException($"Provider cannot exceed {ExternalBrandMappingConstants.ProviderMaxLength} characters.", nameof(provider));
        if (trimmedKey.Length > ExternalBrandMappingConstants.ExternalBrandKeyMaxLength)
            throw new ArgumentException($"ExternalBrandKey cannot exceed {ExternalBrandMappingConstants.ExternalBrandKeyMaxLength} characters.", nameof(externalBrandKey));
        if (trimmedName.Length > ExternalBrandMappingConstants.ExternalBrandNameMaxLength)
            throw new ArgumentException($"ExternalBrandName cannot exceed {ExternalBrandMappingConstants.ExternalBrandNameMaxLength} characters.", nameof(externalBrandName));

        return new ExternalBrandMapping
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id,
            TenantId = tenantId,
            Provider = trimmedProvider,
            ExternalBrandKey = trimmedKey,
            ExternalBrandName = trimmedName,
            TenantBrandId = tenantBrandId,
            MappingSource = string.IsNullOrWhiteSpace(mappingSource) ? ExternalBrandMappingConstants.DefaultMappingSource : mappingSource.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = userId,
            UpdatedByTenantUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(
        string externalBrandName,
        Guid tenantBrandId,
        string mappingSource,
        Guid? userId,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalBrandName);
        if (tenantBrandId == Guid.Empty) throw new ArgumentException("TenantBrandId cannot be empty.", nameof(tenantBrandId));

        var trimmedName = externalBrandName.Trim();
        if (trimmedName.Length > ExternalBrandMappingConstants.ExternalBrandNameMaxLength)
            throw new ArgumentException($"ExternalBrandName cannot exceed {ExternalBrandMappingConstants.ExternalBrandNameMaxLength} characters.", nameof(externalBrandName));

        ExternalBrandName = trimmedName;
        TenantBrandId = tenantBrandId;
        MappingSource = string.IsNullOrWhiteSpace(mappingSource) ? ExternalBrandMappingConstants.DefaultMappingSource : mappingSource.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = userId;
        UpdatedAt = now;
    }
}
