using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Contracts;
using E_POS.Application.Modules.PricingTax.Dtos;

namespace E_POS.Application.Modules.PricingTax.Validators;

public sealed class ProductTaxAssignmentRequestValidator : IProductTaxAssignmentRequestValidator
{
    public ApplicationError? ValidateCreate(ProductTaxAssignmentCreateRequest request)
    {
        if (request.ProductId == Guid.Empty)
            return new ApplicationError("validation.product_tax_assignment.invalid_product", "Product ID is required.");
            
        if (request.TaxClassId == Guid.Empty)
            return new ApplicationError("validation.product_tax_assignment.invalid_tax_class", "Tax Class ID is required.");

        if (request.AppliesFrom.HasValue && request.AppliesUntil.HasValue && request.AppliesFrom.Value >= request.AppliesUntil.Value)
            return new ApplicationError("validation.product_tax_assignment.invalid_dates", "Applies From date must be before Applies Until date.");

        return null;
    }

    public ApplicationError? ValidateUpdate(ProductTaxAssignmentUpdateRequest request)
    {
        if (request.TaxClassId == Guid.Empty)
            return new ApplicationError("validation.product_tax_assignment.invalid_tax_class", "Tax Class ID is required.");

        if (string.IsNullOrWhiteSpace(request.Status))
            return new ApplicationError("validation.product_tax_assignment.invalid_status", "Status is required.");

        if (request.AppliesFrom.HasValue && request.AppliesUntil.HasValue && request.AppliesFrom.Value >= request.AppliesUntil.Value)
            return new ApplicationError("validation.product_tax_assignment.invalid_dates", "Applies From date must be before Applies Until date.");

        return null;
    }
}
