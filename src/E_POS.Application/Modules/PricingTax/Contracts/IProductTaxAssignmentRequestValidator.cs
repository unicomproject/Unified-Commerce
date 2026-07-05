using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Dtos;

namespace E_POS.Application.Modules.PricingTax.Contracts;

public interface IProductTaxAssignmentRequestValidator
{
    ApplicationError? ValidateCreate(ProductTaxAssignmentCreateRequest request);
    ApplicationError? ValidateUpdate(ProductTaxAssignmentUpdateRequest request);
}
