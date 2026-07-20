using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class ReturnInspectionDraft : AuditableEntity
{
    public const int DefaultLifetimeHours = 24;

    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid SaleId { get; protected set; }
    public string Status { get; protected set; } = "DRAFT";
    public int Version { get; protected set; } = 1;
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? ValidatedAt { get; protected set; }
    public Guid? ValidatedByTenantUserId { get; protected set; }
    public bool RequiresInspection { get; protected set; }
    public bool RequiresManagerApproval { get; protected set; }
    public string? ResolutionType { get; protected set; }
    public DateTimeOffset? ResolutionSelectedAt { get; protected set; }
    public Guid? ResolutionSelectedByTenantUserId { get; protected set; }
    public string? RefundMethodCode { get; protected set; }
    public DateTimeOffset? RefundMethodSelectedAt { get; protected set; }
    public Guid? RefundMethodSelectedByTenantUserId { get; protected set; }
    public Guid CreatedByTenantUserId { get; protected set; }

    public static ReturnInspectionDraft Create(
        Guid id, Guid tenantId, Guid outletId, Guid saleId, Guid createdByTenantUserId, DateTimeOffset now) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            SaleId = saleId,
            CreatedByTenantUserId = createdByTenantUserId,
            Status = "DRAFT",
            Version = 1,
            CreatedAt = now,
            ExpiresAt = now.AddHours(DefaultLifetimeHours)
        };

    public bool IsExpired(DateTimeOffset now) =>
        now >= ExpiresAt ||
        string.Equals(Status, "CANCELLED", StringComparison.OrdinalIgnoreCase);

    public void MarkDraft(DateTimeOffset now)
    {
        Status = "DRAFT";
        ValidatedAt = null;
        ValidatedByTenantUserId = null;
        Version += 1;
        ExpiresAt = now.AddHours(DefaultLifetimeHours);
        ClearResolution();
    }

    public void MarkValidated(
        Guid userId,
        DateTimeOffset now,
        bool requiresInspection = false,
        bool requiresManagerApproval = false)
    {
        Status = "VALIDATED";
        ValidatedByTenantUserId = userId;
        ValidatedAt = now;
        RequiresInspection |= requiresInspection;
        RequiresManagerApproval |= requiresManagerApproval;
        Version += 1;
        ExpiresAt = now.AddHours(DefaultLifetimeHours);
    }

    public void MarkConsumed() => Status = "CONSUMED";

    public void Cancel() => Status = "CANCELLED";

    public bool SetResolution(string resolutionType, Guid userId, DateTimeOffset now)
    {
        var normalized = resolutionType.Trim().ToUpperInvariant();
        if (string.Equals(ResolutionType, normalized, StringComparison.Ordinal))
        {
            return false;
        }

        ClearRefundMethod();
        ResolutionType = normalized;
        ResolutionSelectedAt = now;
        ResolutionSelectedByTenantUserId = userId;
        Version += 1;
        return true;
    }

    public void ClearResolution()
    {
        ResolutionType = null;
        ResolutionSelectedAt = null;
        ResolutionSelectedByTenantUserId = null;
        ClearRefundMethod();
    }

    public void SetRefundMethod(string methodCode, Guid userId, DateTimeOffset now)
    {
        RefundMethodCode = methodCode;
        RefundMethodSelectedAt = now;
        RefundMethodSelectedByTenantUserId = userId;
        Version += 1;
    }

    public void ClearRefundMethod()
    {
        RefundMethodCode = null;
        RefundMethodSelectedAt = null;
        RefundMethodSelectedByTenantUserId = null;
    }

    public void MarkExchangeReplacementChanged() => Version += 1;
}
