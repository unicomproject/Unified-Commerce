using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Dtos;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface IPriceListRequestValidator
{
    ApplicationError? ValidateCreate(PriceListCreateRequest request);
    ApplicationError? ValidateUpdate(PriceListUpdateRequest request);
}
