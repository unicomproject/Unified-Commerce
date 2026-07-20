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
    public string? BarcodeSnapshot { get; protected set; }
    public string ProductNameSnapshot { get; protected set; } = string.Empty;
    public string? VariantNameSnapshot { get; protected set; }
    public string? DepartmentNameSnapshot { get; protected set; }
    public string? CategoryNameSnapshot { get; protected set; }
    public string? SubcategoryNameSnapshot { get; protected set; }
    public string? BrandNameSnapshot { get; protected set; }
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
        bool isTaxInclusive,
        DateTimeOffset now) =>
        CreateForPosSale(
            id,
            tenantId,
            salesOrderId,
            lineNumber,
            productId,
            productVariantId,
            uomId,
            priceListItemId,
            skuSnapshot,
            null,
            productNameSnapshot,
            variantNameSnapshot,
            null,
            null,
            null,
            null,
            uomCodeSnapshot,
            uomNameSnapshot,
            productTypeSnapshot,
            productStructureSnapshot,
            quantity,
            unitPrice,
            lineSubtotalAmount,
            lineDiscountAmount,
            lineTaxAmount,
            isTaxInclusive,
            now);

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
        string? barcodeSnapshot,
        string productNameSnapshot,
        string? variantNameSnapshot,
        string? departmentNameSnapshot,
        string? categoryNameSnapshot,
        string? subcategoryNameSnapshot,
        string? brandNameSnapshot,
        string uomCodeSnapshot,
        string uomNameSnapshot,
        string productTypeSnapshot,
        string productStructureSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal lineSubtotalAmount,
        decimal lineDiscountAmount,
        decimal lineTaxAmount,
        bool isTaxInclusive,
        DateTimeOffset now)
    {
        // When tax is inclusive, the total price already contains the tax,
        // so we do NOT add lineTaxAmount on top. When exclusive, we do.
        var lineTotalAmount = isTaxInclusive
            ? lineSubtotalAmount - lineDiscountAmount
            : lineSubtotalAmount - lineDiscountAmount + lineTaxAmount;

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
            BarcodeSnapshot = barcodeSnapshot?.Trim(),
            ProductNameSnapshot = productNameSnapshot.Trim(),
            VariantNameSnapshot = variantNameSnapshot?.Trim(),
            DepartmentNameSnapshot = departmentNameSnapshot?.Trim(),
            CategoryNameSnapshot = categoryNameSnapshot?.Trim(),
            SubcategoryNameSnapshot = subcategoryNameSnapshot?.Trim(),
            BrandNameSnapshot = brandNameSnapshot?.Trim(),
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
            LineTotalAmount = lineTotalAmount,
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
        bool isTaxInclusive,
        DateTimeOffset now) =>
        CreateForHeldPosSale(
            id, tenantId, salesOrderId, lineNumber, productId, productVariantId, uomId,
            priceListItemId, skuSnapshot, null, productNameSnapshot, variantNameSnapshot,
            null, null, null, null, uomCodeSnapshot, uomNameSnapshot, productTypeSnapshot,
            productStructureSnapshot, quantity, unitPrice, lineSubtotalAmount, lineDiscountAmount,
            lineTaxAmount, isTaxInclusive, now);

    public static SalesOrderLine CreateForHeldPosSale(
        Guid id, Guid tenantId, Guid salesOrderId, int lineNumber,
        Guid productId, Guid productVariantId, Guid uomId, Guid? priceListItemId,
        string? skuSnapshot, string? barcodeSnapshot, string productNameSnapshot, string? variantNameSnapshot,
        string? departmentNameSnapshot, string? categoryNameSnapshot, string? subcategoryNameSnapshot,
        string? brandNameSnapshot, string uomCodeSnapshot, string uomNameSnapshot, string productTypeSnapshot,
        string productStructureSnapshot, decimal quantity, decimal unitPrice,
        decimal lineSubtotalAmount, decimal lineDiscountAmount, decimal lineTaxAmount,
        bool isTaxInclusive,
        DateTimeOffset now)
    {
        var line = CreateForPosSale(
            id, tenantId, salesOrderId, lineNumber, productId, productVariantId,
            uomId, priceListItemId, skuSnapshot, barcodeSnapshot, productNameSnapshot,
            variantNameSnapshot, departmentNameSnapshot, categoryNameSnapshot, subcategoryNameSnapshot,
            brandNameSnapshot, uomCodeSnapshot, uomNameSnapshot,
            productTypeSnapshot, productStructureSnapshot, quantity, unitPrice,
            lineSubtotalAmount, lineDiscountAmount, lineTaxAmount, isTaxInclusive, now);
        line.FulfilledQuantity = 0;
        line.LineStatus = "ACTIVE";
        return line;
    }

    public static SalesOrderLine CreateForClickAndCollect(
        Guid id,
        Guid tenantId,
        Guid salesOrderId,
        int lineNumber,
        Guid productId,
        Guid? productVariantId,
        Guid uomId,
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
        bool isTaxInclusive,
        DateTimeOffset now)
    {
        var lineTotalAmount = isTaxInclusive
            ? lineSubtotalAmount - lineDiscountAmount
            : lineSubtotalAmount - lineDiscountAmount + lineTaxAmount;

        return new SalesOrderLine
        {
            Id = id,
            TenantId = tenantId,
            SalesOrderId = salesOrderId,
            LineNumber = lineNumber,
            ProductId = productId,
            ProductVariantId = productVariantId,
            UomId = uomId,
            SkuSnapshot = string.IsNullOrWhiteSpace(skuSnapshot) ? null : skuSnapshot.Trim(),
            ProductNameSnapshot = productNameSnapshot.Trim(),
            VariantNameSnapshot = string.IsNullOrWhiteSpace(variantNameSnapshot) ? null : variantNameSnapshot.Trim(),
            UomCodeSnapshot = uomCodeSnapshot.Trim().ToUpperInvariant(),
            UomNameSnapshot = uomNameSnapshot.Trim(),
            ProductTypeSnapshot = productTypeSnapshot.Trim().ToUpperInvariant(),
            ProductStructureSnapshot = productStructureSnapshot.Trim().ToUpperInvariant(),
            Quantity = quantity,
            FulfilledQuantity = 0m,
            CancelledQuantity = 0m,
            ReturnedQuantity = 0m,
            OriginalUnitPrice = unitPrice,
            UnitPrice = unitPrice,
            LineSubtotalAmount = lineSubtotalAmount,
            LineDiscountAmount = lineDiscountAmount,
            LineTaxAmount = lineTaxAmount,
            LineTotalAmount = lineTotalAmount,
            LineStatus = "ACTIVE",
            CreatedAt = now,
            UpdatedAt = now
        };
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
