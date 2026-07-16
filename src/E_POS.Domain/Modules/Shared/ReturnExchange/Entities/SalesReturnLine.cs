using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class SalesReturnLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesReturnId { get; protected set; }
    public Guid SalesOrderLineId { get; protected set; }
    public Guid? ReturnReasonId { get; protected set; }
    public string? ReturnReasonCodeSnapshot { get; protected set; }
    public string? ReturnReasonNameSnapshot { get; protected set; }
    public decimal QuantityRequested { get; protected set; }
    public decimal? QuantityReceived { get; protected set; }
    public decimal? QuantityApproved { get; protected set; }
    public decimal UnitPriceSnapshot { get; protected set; }
    public decimal UnitTaxAmountSnapshot { get; protected set; }
    public decimal LineSubtotalAmount { get; protected set; }
    public decimal LineTaxAmount { get; protected set; }
    public string? DispositionStatus { get; protected set; }
    public string? Notes { get; protected set; }

    public static SalesReturnLine CreateReceived(
        Guid id,
        Guid tenantId,
        Guid salesReturnId,
        Guid salesOrderLineId,
        Guid returnReasonId,
        decimal quantity,
        decimal unitPrice,
        decimal unitTax,
        decimal subtotal,
        decimal tax,
        bool requiresInspection,
        string? notes,
        DateTimeOffset now) =>
        CreateReceived(
            id,
            tenantId,
            salesReturnId,
            salesOrderLineId,
            returnReasonId,
            null,
            null,
            quantity,
            unitPrice,
            unitTax,
            subtotal,
            tax,
            requiresInspection,
            notes,
            now);

    public static SalesReturnLine CreateReceived(
        Guid id,
        Guid tenantId,
        Guid salesReturnId,
        Guid salesOrderLineId,
        Guid returnReasonId,
        string? returnReasonCodeSnapshot,
        string? returnReasonNameSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal unitTax,
        decimal subtotal,
        decimal tax,
        bool requiresInspection,
        string? notes,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesReturnId = salesReturnId,
            SalesOrderLineId = salesOrderLineId,
            ReturnReasonId = returnReasonId,
            ReturnReasonCodeSnapshot = returnReasonCodeSnapshot?.Trim(),
            ReturnReasonNameSnapshot = returnReasonNameSnapshot?.Trim(),
            QuantityRequested = quantity,
            QuantityReceived = quantity,
            QuantityApproved = quantity,
            UnitPriceSnapshot = unitPrice,
            UnitTaxAmountSnapshot = unitTax,
            LineSubtotalAmount = subtotal,
            LineTaxAmount = tax,
            DispositionStatus = requiresInspection ? "QUARANTINE" : "RESTOCKED",
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
}

