using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Inventory.Entities;

public class InventoryLocation : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid? ParentInventoryLocationId { get; protected set; }
    public string LocationCode { get; protected set; } = string.Empty;
    public string LocationName { get; protected set; } = string.Empty;
    public string LocationType { get; protected set; } = string.Empty;
    public bool IsSellableLocation { get; protected set; }
    public bool IsReturnLocation { get; protected set; }
    public bool IsReceivingLocation { get; protected set; }
    public bool IsQuarantineLocation { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    protected InventoryLocation() { }

    public static InventoryLocation Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid? parentInventoryLocationId,
        string locationCode,
        string locationName,
        string locationType,
        bool isSellableLocation,
        bool isReturnLocation,
        bool isReceivingLocation,
        bool isQuarantineLocation,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new InventoryLocation
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            ParentInventoryLocationId = parentInventoryLocationId,
            LocationCode = locationCode.Trim(),
            LocationName = locationName.Trim(),
            LocationType = locationType.Trim(),
            IsSellableLocation = isSellableLocation,
            IsReturnLocation = isReturnLocation,
            IsReceivingLocation = isReceivingLocation,
            IsQuarantineLocation = isQuarantineLocation,
            Status = status.Trim(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        Guid? parentInventoryLocationId,
        string locationName,
        string locationType,
        bool isSellableLocation,
        bool isReturnLocation,
        bool isReceivingLocation,
        bool isQuarantineLocation,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        ParentInventoryLocationId = parentInventoryLocationId;
        LocationName = locationName.Trim();
        LocationType = locationType.Trim();
        IsSellableLocation = isSellableLocation;
        IsReturnLocation = isReturnLocation;
        IsReceivingLocation = isReceivingLocation;
        IsQuarantineLocation = isQuarantineLocation;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void UpdateStatus(string status, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = status.Trim();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }
}
