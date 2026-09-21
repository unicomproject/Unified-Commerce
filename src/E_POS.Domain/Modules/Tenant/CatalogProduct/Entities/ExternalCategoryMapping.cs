using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

/// <summary>
/// Tenant-specific mapping from an external provider category key (e.g. OpenFoodFacts)
/// to an authoritative tenant category.
/// </summary>
public class ExternalCategoryMapping : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Provider { get; protected set; } = string.Empty;
    public string ExternalCategoryKey { get; protected set; } = string.Empty;
    public string ExternalCategoryName { get; protected set; } = string.Empty;
    public Guid TenantCategoryId { get; protected set; }
    public string MappingSource { get; protected set; } = ExternalCategoryMappingConstants.DefaultMappingSource;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected ExternalCategoryMapping() { }

    public static ExternalCategoryMapping Create(
        Guid id,
        Guid tenantId,
        string provider,
        string externalCategoryKey,
        string externalCategoryName,
        Guid tenantCategoryId,
        string mappingSource,
        Guid? userId,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalCategoryKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalCategoryName);
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (tenantCategoryId == Guid.Empty) throw new ArgumentException("TenantCategoryId cannot be empty.", nameof(tenantCategoryId));

        var trimmedProvider = provider.Trim().ToLowerInvariant();
        var trimmedKey = externalCategoryKey.Trim().ToLowerInvariant();
        var trimmedName = externalCategoryName.Trim();

        if (trimmedProvider.Length > ExternalCategoryMappingConstants.ProviderMaxLength)
            throw new ArgumentException($"Provider cannot exceed {ExternalCategoryMappingConstants.ProviderMaxLength} characters.", nameof(provider));
        if (trimmedKey.Length > ExternalCategoryMappingConstants.ExternalCategoryKeyMaxLength)
            throw new ArgumentException($"ExternalCategoryKey cannot exceed {ExternalCategoryMappingConstants.ExternalCategoryKeyMaxLength} characters.", nameof(externalCategoryKey));
        if (trimmedName.Length > ExternalCategoryMappingConstants.ExternalCategoryNameMaxLength)
            throw new ArgumentException($"ExternalCategoryName cannot exceed {ExternalCategoryMappingConstants.ExternalCategoryNameMaxLength} characters.", nameof(externalCategoryName));

        return new ExternalCategoryMapping
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id,
            TenantId = tenantId,
            Provider = trimmedProvider,
            ExternalCategoryKey = trimmedKey,
            ExternalCategoryName = trimmedName,
            TenantCategoryId = tenantCategoryId,
            MappingSource = string.IsNullOrWhiteSpace(mappingSource) ? ExternalCategoryMappingConstants.DefaultMappingSource : mappingSource.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = userId,
            UpdatedByTenantUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(
        string externalCategoryName,
        Guid tenantCategoryId,
        string mappingSource,
        Guid? userId,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalCategoryName);
        if (tenantCategoryId == Guid.Empty) throw new ArgumentException("TenantCategoryId cannot be empty.", nameof(tenantCategoryId));

        var trimmedName = externalCategoryName.Trim();
        if (trimmedName.Length > ExternalCategoryMappingConstants.ExternalCategoryNameMaxLength)
            throw new ArgumentException($"ExternalCategoryName cannot exceed {ExternalCategoryMappingConstants.ExternalCategoryNameMaxLength} characters.", nameof(externalCategoryName));

        ExternalCategoryName = trimmedName;
        TenantCategoryId = tenantCategoryId;
        MappingSource = string.IsNullOrWhiteSpace(mappingSource) ? ExternalCategoryMappingConstants.DefaultMappingSource : mappingSource.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = userId;
        UpdatedAt = now;
    }
}
