using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Contracts;
using E_POS.Application.Modules.PricingTax.Dtos;
using E_POS.Domain.Modules.PricingTax.Constants;

namespace E_POS.Application.Modules.PricingTax.Validators;

public sealed class PriceListItemsRequestValidator : IPriceListItemsRequestValidator
{
    public ApplicationError? ValidateCreate(PriceListItemCreateRequest request)
    {
        if (request.PriceListId == Guid.Empty) return ValidationFailed("Price list ID is required.");
        if (request.ProductId == Guid.Empty) return ValidationFailed("Product ID is required.");

        return Validate(
            request.SellingPrice,
            request.CompareAtPrice,
            request.MinQuantity,
            request.ValidFrom,
            request.ValidUntil,
            request.Status);
    }

    public ApplicationError? ValidateUpdate(PriceListItemUpdateRequest request)
    {
        return Validate(
            request.SellingPrice,
            request.CompareAtPrice,
            request.MinQuantity,
            request.ValidFrom,
            request.ValidUntil,
            request.Status);
    }

    private static ApplicationError? Validate(
        decimal sellingPrice,
        decimal? compareAtPrice,
        decimal minQuantity,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        string status)
    {
        if (sellingPrice < 0) return ValidationFailed("Selling price cannot be negative.");

        if (compareAtPrice.HasValue && compareAtPrice.Value < sellingPrice)
            return ValidationFailed("Compare at price must be greater than or equal to selling price.");

        if (minQuantity <= 0) return ValidationFailed("Minimum quantity must be greater than 0.");

        if (validFrom.HasValue && validUntil.HasValue && validUntil.Value < validFrom.Value)
            return ValidationFailed("Valid until date must be greater than or equal to valid from date.");

        if (string.IsNullOrWhiteSpace(status) || !PricingTaxConstants.IsValidWriteStatus(status))
            return ValidationFailed("Status must be ACTIVE or INACTIVE.");

        return null;
    }

    private static ApplicationError ValidationFailed(string message)
    {
        return new ApplicationError("pricing.price_list_item.validation_failed", message);
    }
}
