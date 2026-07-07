using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;

namespace E_POS.Application.Modules.Tenant.PricingTax.Contracts;

public interface IPriceListRequestValidator
{
    ApplicationError? ValidateCreate(PriceListCreateRequest request);
    ApplicationError? ValidateUpdate(PriceListUpdateRequest request);
}

