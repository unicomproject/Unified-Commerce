using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesReturnLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesReturnId { get; protected set; }
    public Guid SalesOrderLineId { get; protected set; }
    public Guid? ReturnReasonId { get; protected set; }
    public decimal QuantityRequested { get; protected set; }
    public decimal? QuantityReceived { get; protected set; }
    public decimal UnitPriceSnapshot { get; protected set; }
    public decimal UnitTaxAmountSnapshot { get; protected set; }
    public decimal LineSubtotalAmount { get; protected set; }
    public decimal LineTaxAmount { get; protected set; }
    public string? DispositionStatus { get; protected set; }
    public string? Notes { get; protected set; }
}
