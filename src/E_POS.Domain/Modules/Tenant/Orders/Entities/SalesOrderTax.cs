using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

public class SalesOrderTax : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
    public Guid? TaxJurisdictionId { get; protected set; }
    public Guid? TaxClassId { get; protected set; }
    public Guid? TaxRateId { get; protected set; }
    public string? TaxClassCodeSnapshot { get; protected set; }
    public string? TaxRateCodeSnapshot { get; protected set; }
    public string TaxNameSnapshot { get; protected set; } = string.Empty;
    public string? JurisdictionNameSnapshot { get; protected set; }
    public decimal TaxRatePercent { get; protected set; }
    public decimal TaxableAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public bool IsTaxIncluded { get; protected set; }
    public int CalculationSequence { get; protected set; }
}

