using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.POSOperations.Entities;

public class Receipt : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? DocumentNumberSequenceId { get; protected set; }
    public string ReceiptNumber { get; protected set; } = string.Empty;
    public Guid SalesOrderId { get; protected set; }
    public string ReceiptType { get; protected set; } = string.Empty;
    public string ReceiptStatus { get; protected set; } = string.Empty;
    public Guid OutletId { get; protected set; }
    public Guid TillId { get; protected set; }
    public Guid TillSessionId { get; protected set; }
    public DateOnly BusinessDate { get; protected set; }
    public DateTimeOffset IssuedAt { get; protected set; }
    public Guid IssuedByTenantUserId { get; protected set; }
    public Guid? ReceiptTemplateVersionId { get; protected set; }
    public string CurrencyCode { get; protected set; } = string.Empty;
    public decimal SubtotalAmount { get; protected set; }
    public decimal DiscountAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public decimal ChargeAmount { get; protected set; }
    public decimal RoundingAmount { get; protected set; }
    public decimal TotalAmount { get; protected set; }
    public decimal PaidAmount { get; protected set; }
    public decimal ChangeAmount { get; protected set; }
    public string ReceiptDataJson { get; protected set; } = string.Empty;
}

