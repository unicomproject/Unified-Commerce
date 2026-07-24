using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Shared.ReturnExchange.Constants;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class SalesExchangeLine : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesExchangeId { get; protected set; }
    public Guid? SalesReturnLineId { get; protected set; }
    public Guid? ReplacementProductId { get; protected set; }
    public Guid? ReplacementProductVariantId { get; protected set; }
    public Guid? ReplacementSalesOrderLineId { get; protected set; }
    public decimal Quantity { get; protected set; }
    public decimal OriginalLineAmount { get; protected set; }
    public decimal ReplacementLineAmount { get; protected set; }
    public decimal NetDifferenceAmount { get; protected set; }
    public string ExchangeActionType { get; protected set; } = string.Empty;

    public static SalesExchangeLine Create(
        Guid id,
        Guid tenantId,
        Guid salesExchangeId,
        Guid salesReturnLineId,
        Guid replacementProductId,
        Guid replacementProductVariantId,
        Guid replacementSalesOrderLineId,
        decimal quantity,
        decimal originalLineAmount,
        decimal replacementLineAmount,
        DateTimeOffset now) => new()
        {
            Id = id,
            TenantId = tenantId,
            SalesExchangeId = salesExchangeId,
            SalesReturnLineId = salesReturnLineId,
            ReplacementProductId = replacementProductId,
            ReplacementProductVariantId = replacementProductVariantId,
            ReplacementSalesOrderLineId = replacementSalesOrderLineId,
            Quantity = quantity,
            OriginalLineAmount = originalLineAmount,
            ReplacementLineAmount = replacementLineAmount,
            NetDifferenceAmount = Math.Abs(replacementLineAmount - originalLineAmount),
            ExchangeActionType = SalesExchangeConstants.ActionType.Replace,
            CreatedAt = now,
            UpdatedAt = now,
        };
}

