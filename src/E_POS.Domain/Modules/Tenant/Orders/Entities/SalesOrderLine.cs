using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.Orders.Entities;

public class SalesOrderLine : AuditableEntity
{
    public int LineNumber { get; protected set; }
    public decimal LineTotalAmount { get; protected set; }
    public decimal Quantity { get; protected set; }
    public Guid? SalesOrderId { get; protected set; }
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public Guid? ProductVariantId { get; protected set; }
    public Guid UomId { get; protected set; }
    public Guid? PriceListItemId { get; protected set; }
    public string? SkuSnapshot { get; protected set; }
    public string ProductNameSnapshot { get; protected set; } = string.Empty;
    public string? VariantNameSnapshot { get; protected set; }
    public string UomCodeSnapshot { get; protected set; } = string.Empty;
    public string UomNameSnapshot { get; protected set; } = string.Empty;
    public string ProductTypeSnapshot { get; protected set; } = string.Empty;
    public string ProductStructureSnapshot { get; protected set; } = string.Empty;
    public decimal FulfilledQuantity { get; protected set; }
    public decimal CancelledQuantity { get; protected set; }
    public decimal ReturnedQuantity { get; protected set; }
    public decimal OriginalUnitPrice { get; protected set; }
    public decimal UnitPrice { get; protected set; }
    public decimal LineSubtotalAmount { get; protected set; }
    public decimal LineDiscountAmount { get; protected set; }
    public decimal LineTaxAmount { get; protected set; }
    public string LineStatus { get; protected set; } = string.Empty;

    public static SalesOrderLine CreateForPosSale(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        int lineNumber,
        Guid productId,
        Guid productVariantId,
        Guid uomId,
        Guid? priceListItemId,
        string? skuSnapshot,
        string productNameSnapshot,
        string? variantNameSnapshot,
        string uomCodeSnapshot,
        string uomNameSnapshot,
        string productTypeSnapshot,
        string productStructureSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal lineSubtotalAmount,
        decimal lineDiscountAmount,
        decimal lineTaxAmount,
        DateTimeOffset now)
    {
        return new SalesOrderLine
        {
            Id = id,
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            LineNumber = lineNumber,
            ProductId = productId,
            ProductVariantId = productVariantId,
            UomId = uomId,
            PriceListItemId = priceListItemId,
            SkuSnapshot = skuSnapshot?.Trim(),
            ProductNameSnapshot = productNameSnapshot.Trim(),
            VariantNameSnapshot = variantNameSnapshot?.Trim(),
            UomCodeSnapshot = uomCodeSnapshot.Trim(),
            UomNameSnapshot = uomNameSnapshot.Trim(),
            ProductTypeSnapshot = productTypeSnapshot.Trim(),
            ProductStructureSnapshot = productStructureSnapshot.Trim(),
            Quantity = quantity,
            FulfilledQuantity = quantity,
            CancelledQuantity = 0,
            ReturnedQuantity = 0,
            OriginalUnitPrice = unitPrice,
            UnitPrice = unitPrice,
            LineSubtotalAmount = lineSubtotalAmount,
            LineDiscountAmount = lineDiscountAmount,
            LineTaxAmount = lineTaxAmount,
            LineTotalAmount = lineSubtotalAmount - lineDiscountAmount + lineTaxAmount,
            LineStatus = "FULFILLED",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static SalesOrderLine CreateForHeldPosSale(
        Guid id, Guid tenantId, Guid salesOrderId, int lineNumber,
        Guid productId, Guid productVariantId, Guid uomId, Guid? priceListItemId,
        string? skuSnapshot, string productNameSnapshot, string? variantNameSnapshot,
        string uomCodeSnapshot, string uomNameSnapshot, string productTypeSnapshot,
        string productStructureSnapshot, decimal quantity, decimal unitPrice,
        decimal lineSubtotalAmount, decimal lineDiscountAmount, decimal lineTaxAmount,
        DateTimeOffset now)
    {
        var line = CreateForPosSale(
            id, tenantId, salesOrderId, lineNumber, productId, productVariantId,
            uomId, priceListItemId, skuSnapshot, productNameSnapshot,
            variantNameSnapshot, uomCodeSnapshot, uomNameSnapshot,
            productTypeSnapshot, productStructureSnapshot, quantity, unitPrice,
            lineSubtotalAmount, lineDiscountAmount, lineTaxAmount, now);
        line.FulfilledQuantity = 0;
        line.LineStatus = "PENDING";
        return line;
    }

    public void RecordReturn(decimal quantity, DateTimeOffset now)
    {
        if (quantity <= 0 || ReturnedQuantity + quantity > FulfilledQuantity)
        {
            throw new InvalidOperationException("Return quantity exceeds the fulfilled quantity.");
        }

        ReturnedQuantity += quantity;
        UpdatedAt = now;
    }
}

