using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Dtos;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface ITaxSetupRequestValidator
{
    ApplicationError? ValidateTaxClassCreate(TaxClassCreateRequest request);
    ApplicationError? ValidateTaxClassUpdate(TaxClassUpdateRequest request);
    ApplicationError? ValidateTaxRateCreate(TaxRateCreateRequest request);
    ApplicationError? ValidateTaxRateUpdate(TaxRateUpdateRequest request);
}
