using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

public class SalesOrderTax : AuditableEntity
{
    protected SalesOrderTax() { }

    public Guid TenantId { get; protected set; }
    public Guid SalesOrderId { get; protected set; }
    public Guid? SalesOrderLineId { get; protected set; }
    public Guid? TaxJurisdictionId { get; protected set; }
    public Guid? TaxClassId { get; protected set; }
    public Guid? TaxRateId { get; protected set; }
    public string? TaxClassCodeSnapshot { get; protected set; }
    public string? TaxRateCodeSnapshot { get; protected set; }
    public string TaxNameSnapshot { get; protected set; } = string.Empty;
    public string? TaxTreatmentSnapshot { get; protected set; }
    public string? JurisdictionNameSnapshot { get; protected set; }
    public decimal TaxRatePercent { get; protected set; }
    public decimal TaxableAmount { get; protected set; }
    public decimal TaxAmount { get; protected set; }
    public bool IsTaxIncluded { get; protected set; }
    public int CalculationSequence { get; protected set; }

    public static SalesOrderTax Create(
        Guid tenantId,
        Guid salesOrderId,
        Guid? salesOrderLineId,
        Guid? taxJurisdictionId,
        Guid? taxClassId,
        Guid? taxRateId,
        string? taxClassCodeSnapshot,
        string? taxRateCodeSnapshot,
        string taxNameSnapshot,
        string? taxTreatmentSnapshot,
        string? jurisdictionNameSnapshot,
        decimal taxRatePercent,
        decimal taxableAmount,
        decimal taxAmount,
        bool isTaxIncluded,
        int calculationSequence,
        DateTimeOffset now)
    {
        return new SalesOrderTax
        {
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            SalesOrderLineId = salesOrderLineId,
            TaxJurisdictionId = taxJurisdictionId,
            TaxClassId = taxClassId,
            TaxRateId = taxRateId,
            TaxClassCodeSnapshot = taxClassCodeSnapshot,
            TaxRateCodeSnapshot = taxRateCodeSnapshot,
            TaxNameSnapshot = taxNameSnapshot,
            TaxTreatmentSnapshot = taxTreatmentSnapshot,
            JurisdictionNameSnapshot = jurisdictionNameSnapshot,
            TaxRatePercent = taxRatePercent,
            TaxableAmount = taxableAmount,
            TaxAmount = taxAmount,
            IsTaxIncluded = isTaxIncluded,
            CalculationSequence = calculationSequence,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
