using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ReturnExchange.Entities;

public class SalesExchange : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public decimal AdditionalAmount { get; protected set; }
    public string ExchangeNumber { get; protected set; } = string.Empty;
    public decimal RefundAmount { get; protected set; }
    public Guid ReplacementOrderId { get; protected set; }
    public Guid? SalesReturnId { get; protected set; }
}
