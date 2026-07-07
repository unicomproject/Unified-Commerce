using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;

namespace E_POS.Application.Modules.Tenant.PricingTax.Validators;

public sealed class PriceListRequestValidator : IPriceListRequestValidator
{
    public ApplicationError? ValidateCreate(PriceListCreateRequest request)
    {
        return Validate(
            request.PriceListCode,
            request.PriceListName,
            request.PriceListType,
            request.CurrencyCode,
            request.Priority,
            request.ValidFrom,
            request.ValidUntil,
            request.Status);
    }

    public ApplicationError? ValidateUpdate(PriceListUpdateRequest request)
    {
        return Validate(
            request.PriceListCode,
            request.PriceListName,
            request.PriceListType,
            request.CurrencyCode,
            request.Priority,
            request.ValidFrom,
            request.ValidUntil,
            request.Status);
    }

    private static ApplicationError? Validate(
        string code,
        string name,
        string type,
        string currencyCode,
        int priority,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        string status)
    {
        if (string.IsNullOrWhiteSpace(code)) return ValidationFailed("Price list code is required.");
        if (code.Trim().Length > 80) return ValidationFailed("Price list code cannot exceed 80 characters.");

        if (string.IsNullOrWhiteSpace(name)) return ValidationFailed("Price list name is required.");
        if (name.Trim().Length > 150) return ValidationFailed("Price list name cannot exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(type) || !PricingTaxConstants.IsValidPriceListType(type))
            return ValidationFailed("Price list type must be POS, STOREFRONT, or CUSTOM.");

        if (string.IsNullOrWhiteSpace(currencyCode)) return ValidationFailed("Currency code is required.");
        if (currencyCode.Trim().Length != 3) return ValidationFailed("Currency code must be exactly 3 characters.");

        if (priority < 0) return ValidationFailed("Priority must be greater than or equal to 0.");

        if (validFrom.HasValue && validUntil.HasValue && validUntil.Value < validFrom.Value)
            return ValidationFailed("Valid until date must be greater than or equal to valid from date.");

        if (string.IsNullOrWhiteSpace(status) || !PricingTaxConstants.IsValidWriteStatus(status))
            return ValidationFailed("Price list status must be ACTIVE or INACTIVE.");

        return null;
    }

    private static ApplicationError ValidationFailed(string message)
    {
        return new ApplicationError("pricing.price_list.validation_failed", message);
    }
}


