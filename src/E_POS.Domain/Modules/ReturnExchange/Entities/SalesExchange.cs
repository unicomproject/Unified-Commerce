using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesExchange : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public Guid? SalesReturnId { get; protected set; }
    public Guid? ReplacementSalesOrderId { get; protected set; }
    public string ExchangeNumber { get; protected set; } = string.Empty;
    public string ExchangeStatus { get; protected set; } = string.Empty;
    public string ExchangeMode { get; protected set; } = string.Empty;
    public decimal PriceDifferenceAmount { get; protected set; }
    public decimal AdditionalPaymentAmount { get; protected set; }
    public decimal RefundBackAmount { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }
    public DateTimeOffset? CancelledAt { get; protected set; }
    public string? Notes { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
}
