using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductSetupInitialTracking : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public string? InitialBatchNumber { get; protected set; }
    public DateOnly? InitialExpiryDate { get; protected set; }
    public string? InitialSerialNumber { get; protected set; }
    public Guid? AssignedProductVariantId { get; protected set; }
    public DateTimeOffset? IncompatibleClearConfirmedAt { get; protected set; }
    public DateTimeOffset? ConsumedAt { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
    public long RowVersion { get; protected set; } = 1;

    protected ProductSetupInitialTracking() { }

    public static ProductSetupInitialTracking Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        string? initialBatchNumber,
        DateOnly? initialExpiryDate,
        string? initialSerialNumber,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductSetupInitialTracking
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            InitialBatchNumber = Normalize(initialBatchNumber),
            InitialExpiryDate = initialExpiryDate,
            InitialSerialNumber = Normalize(initialSerialNumber),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1
        };
    }

    public void UpdateValues(
        string? initialBatchNumber,
        DateOnly? initialExpiryDate,
        string? initialSerialNumber,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        InitialBatchNumber = Normalize(initialBatchNumber);
        InitialExpiryDate = initialExpiryDate;
        InitialSerialNumber = Normalize(initialSerialNumber);
        Touch(updatedByTenantUserId, now);
    }

    public void ConfirmIncompatibleClear(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        IncompatibleClearConfirmedAt = now;
        Touch(updatedByTenantUserId, now);
    }

    public void AssignVariant(Guid? variantId, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        AssignedProductVariantId = variantId;
        Touch(updatedByTenantUserId, now);
    }

    public void MarkConsumed(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        ConsumedAt = now;
        Touch(updatedByTenantUserId, now);
    }

    public bool HasAnyIdentityValues =>
        !string.IsNullOrWhiteSpace(InitialBatchNumber) ||
        InitialExpiryDate.HasValue ||
        !string.IsNullOrWhiteSpace(InitialSerialNumber);

    private void Touch(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
        RowVersion += 1;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
