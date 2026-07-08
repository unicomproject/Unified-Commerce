using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class CashMovementType : AuditableEntity
{
    public Guid? TenantId { get; protected set; }
    public string MovementTypeCode { get; protected set; } = string.Empty;
    public string MovementTypeName { get; protected set; } = string.Empty;
    public string Direction { get; protected set; } = string.Empty;
    public bool AffectsExpectedCash { get; protected set; }
    public bool RequiresReason { get; protected set; }
    public bool IsSystemType { get; protected set; }
    public string Status { get; protected set; } = string.Empty;

    public static CashMovementType Create(
        Guid id,
        Guid? tenantId,
        string movementTypeCode,
        string movementTypeName,
        string direction,
        bool affectsExpectedCash,
        bool requiresReason,
        bool isSystemType,
        string status,
        DateTimeOffset now)
    {
        return new CashMovementType
        {
            Id = id,
            TenantId = tenantId,
            MovementTypeCode = movementTypeCode.Trim().ToUpperInvariant(),
            MovementTypeName = movementTypeName.Trim(),
            Direction = direction.Trim().ToUpperInvariant(),
            AffectsExpectedCash = affectsExpectedCash,
            RequiresReason = requiresReason,
            IsSystemType = isSystemType,
            Status = status.Trim().ToUpperInvariant(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string movementTypeName,
        bool affectsExpectedCash,
        bool requiresReason,
        string status,
        DateTimeOffset now)
    {
        MovementTypeName = movementTypeName.Trim();
        AffectsExpectedCash = affectsExpectedCash;
        RequiresReason = requiresReason;
        Status = status.Trim().ToUpperInvariant();
        UpdatedAt = now;
    }
}

