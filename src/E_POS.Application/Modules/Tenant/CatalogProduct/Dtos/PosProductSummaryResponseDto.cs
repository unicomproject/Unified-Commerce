namespace E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

public sealed record PosProductSummaryResponseDto(
    Guid Id,
    Guid? VariantId,
    string Name,
    string? Description,
    string? ImageStorageKey,
    Guid? CategoryId,
    string CategoryName,
    int BasePrice,
    bool HasVariants,
    string StockStatus,
    decimal? AvailableQuantity,
    string? Sku = null,
    string? Barcode = null,
    string? VariantName = null,
    bool HasOffer = false,
    string? OfferType = null,
    Guid? OfferPolicyId = null,
    string? OfferName = null,
    int? OriginalPrice = null,
    int? SellingPrice = null,
    int? OfferPrice = null,
    string? DiscountLabel = null,
    bool RequiresCartValidation = false,
    bool RequiresManagerApproval = false)
{
    public string? ImageUrl => ImageStorageKey;
}
