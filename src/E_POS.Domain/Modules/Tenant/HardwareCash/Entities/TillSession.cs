using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class TillSession : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid TillId { get; protected set; }
    public string SessionNumber { get; protected set; } = string.Empty;
    public DateOnly BusinessDate { get; protected set; }
    public Guid OpenedByTenantUserId { get; protected set; }
    public Guid? ClosedByTenantUserId { get; protected set; }
    public Guid OpenedFromPosDeviceId { get; protected set; }
    public Guid? ClosedFromPosDeviceId { get; protected set; }
    public decimal OpeningFloatAmount { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public DateTimeOffset OpenedAt { get; protected set; }
    public DateTimeOffset? ClosedAt { get; protected set; }
    public string? OpeningNote { get; protected set; }
    public string? ClosingNote { get; protected set; }
    // No explicit CreatedBy/UpdatedBy per ERD since it uses OpenedBy/ClosedBy? Wait, ERD says:
    // | created_at | timestamptz |  | NOT NULL | Creation timestamp. |
    // | updated_at | timestamptz |  | NOT NULL | Last update timestamp. |
    // No created_by_tenant_user_id. So we will just ignore CreatedBy/UpdatedBy from AuditableEntity in configuration.

    public static TillSession Open(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        string sessionNumber,
        DateOnly businessDate,
        Guid openedByTenantUserId,
        Guid openedFromPosDeviceId,
        decimal openingFloatAmount,
        string currencyCode,
        string? openingNote,
        DateTimeOffset now)
    {
        return new TillSession
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            TillId = tillId,
            SessionNumber = sessionNumber.Trim().ToUpperInvariant(),
            BusinessDate = businessDate,
            OpenedByTenantUserId = openedByTenantUserId,
            OpenedFromPosDeviceId = openedFromPosDeviceId,
            OpeningFloatAmount = openingFloatAmount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Status = "OPEN",
            OpenedAt = now,
            OpeningNote = openingNote?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Close(
        Guid closedByTenantUserId,
        Guid closedFromPosDeviceId,
        string? closingNote,
        DateTimeOffset now)
    {
        Status = "CLOSED";
        ClosedByTenantUserId = closedByTenantUserId;
        ClosedFromPosDeviceId = closedFromPosDeviceId;
        ClosingNote = closingNote?.Trim();
        ClosedAt = now;
        UpdatedAt = now;
    }
}
