using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Dtos;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface IPriceListItemsRequestValidator
{
    ApplicationError? ValidateCreate(PriceListItemCreateRequest request);
    ApplicationError? ValidateUpdate(PriceListItemUpdateRequest request);
}
