using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.HardwareCash.Entities;

public class CashMovement : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid OutletId { get; protected set; }
    public Guid TillId { get; protected set; }
    public Guid TillSessionId { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public Guid MovementTypeId { get; protected set; }
    public string MovementNumber { get; protected set; } = string.Empty;
    public decimal Amount { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string? Reason { get; protected set; }
    public Guid? OrderId { get; protected set; }
    public Guid? PaymentId { get; protected set; }
    public Guid? RefundId { get; protected set; }
    public Guid PerformedByTenantUserId { get; protected set; }
    public DateTimeOffset PerformedAt { get; protected set; }

    public static CashMovement Create(
        Guid id,
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        Guid tillSessionId,
        Guid? posDeviceId,
        Guid movementTypeId,
        string movementNumber,
        decimal amount,
        string currencyCode,
        string? reason,
        Guid? orderId,
        Guid? paymentId,
        Guid? refundId,
        Guid performedByTenantUserId,
        DateTimeOffset now)
    {
        return new CashMovement
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            TillId = tillId,
            TillSessionId = tillSessionId,
            PosDeviceId = posDeviceId,
            MovementTypeId = movementTypeId,
            MovementNumber = movementNumber.Trim().ToUpperInvariant(),
            Amount = amount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Reason = reason?.Trim(),
            OrderId = orderId,
            PaymentId = paymentId,
            RefundId = refundId,
            PerformedByTenantUserId = performedByTenantUserId,
            PerformedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

