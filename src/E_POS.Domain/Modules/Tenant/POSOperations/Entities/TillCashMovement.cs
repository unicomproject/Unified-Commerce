using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class TillCashMovement : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid TillSessionId { get; protected set; }
    public Guid? PosDeviceId { get; protected set; }
    public Guid? RequestId { get; protected set; }
    public string MovementType { get; protected set; } = string.Empty;
    public decimal Amount { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string? Reason { get; protected set; }
    public string? ReferenceNumber { get; protected set; }
    public Guid PerformedByTenantUserId { get; protected set; }
    public DateTimeOffset PerformedAt { get; protected set; }

    public static TillCashMovement CreateCashOut(
        Guid id,
        Guid tenantId,
        Guid tillSessionId,
        decimal amount,
        string currencyCode,
        string reason,
        string referenceNumber,
        Guid tenantUserId,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            TillSessionId = tillSessionId,
            MovementType = "CASH_OUT",
            Amount = amount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Reason = reason.Trim(),
            ReferenceNumber = referenceNumber.Trim().ToUpperInvariant(),
            PerformedByTenantUserId = tenantUserId,
            PerformedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

    public static TillCashMovement CreateCashIn(
        Guid id,
        Guid tenantId,
        Guid tillSessionId,
        decimal amount,
        string currencyCode,
        string reason,
        string referenceNumber,
        Guid tenantUserId,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            TillSessionId = tillSessionId,
            MovementType = "CASH_IN",
            Amount = amount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Reason = reason.Trim(),
            ReferenceNumber = referenceNumber.Trim().ToUpperInvariant(),
            PerformedByTenantUserId = tenantUserId,
            PerformedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

    public static TillCashMovement CreateManual(
        Guid id,
        Guid tenantId,
        Guid tillSessionId,
        Guid posDeviceId,
        Guid requestId,
        string movementType,
        decimal amount,
        string currencyCode,
        string reason,
        string? referenceNumber,
        Guid tenantUserId,
        DateTimeOffset now)
    {
        var normalizedType = movementType.Trim().ToUpperInvariant();
        if (normalizedType is not ("CASH_IN" or "CASH_OUT" or "CASH_DROP"))
            throw new ArgumentOutOfRangeException(nameof(movementType));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        return new TillCashMovement
        {
            Id = id,
            TenantId = tenantId,
            TillSessionId = tillSessionId,
            PosDeviceId = posDeviceId,
            RequestId = requestId,
            MovementType = normalizedType,
            Amount = amount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Reason = reason.Trim(),
            ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber)
                ? null
                : referenceNumber.Trim().ToUpperInvariant(),
            PerformedByTenantUserId = tenantUserId,
            PerformedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

