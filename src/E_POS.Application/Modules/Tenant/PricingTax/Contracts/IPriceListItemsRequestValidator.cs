using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;

namespace E_POS.Application.Modules.Tenant.PricingTax.Contracts;

public interface IPriceListItemsRequestValidator
{
    ApplicationError? ValidateCreate(PriceListItemCreateRequest request);
    ApplicationError? ValidateUpdate(PriceListItemUpdateRequest request);
}

