namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosSaleLinePricingCalculator
{
    Task<PosSaleLinePricingResult> CalculateAsync(
        Guid tenantId,
        Guid outletId,
        IReadOnlyList<PosSaleLinePricingRequest> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public sealed record PosSaleLinePricingRequest(
    Guid LineKey,
    Guid ProductId,
    Guid VariantId,
    decimal Quantity);

public sealed record PosSaleLinePricingResult(
    string? ErrorCode,
    Guid? PriceListId,
    string? CurrencyCode,
    bool PriceIncludesTax,
    IReadOnlyList<PosSaleLinePricingLineResult> Lines,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal)
{
    public bool IsSuccess => ErrorCode is null;
}

public sealed record PosSaleLinePricingLineResult(
    Guid LineKey,
    Guid ProductId,
    Guid VariantId,
    decimal Quantity,
    Guid PriceListItemId,
    decimal ListedUnitPrice,
    decimal UnitPrice,
    decimal LineSubtotal,
    decimal LineDiscount,
    decimal LineTax,
    decimal LineTotal,
    decimal TaxPercent,
    decimal AvailableQuantity);
