using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;

namespace E_POS.Application.Modules.Tenant.PricingTax.Validators;

public class TaxSetupRequestValidator : ITaxSetupRequestValidator
{
    public ApplicationError? ValidateTaxClassCreate(TaxClassCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TaxClassCode) || request.TaxClassCode.Length > 50)
            return new ApplicationError("validation.tax_class.invalid_code", "Tax Class Code is required and must not exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(request.TaxClassName))
            return new ApplicationError("validation.tax_class.invalid_name", "Tax Class Name is required.");

        return null;
    }

    public ApplicationError? ValidateTaxClassUpdate(TaxClassUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TaxClassName))
            return new ApplicationError("validation.tax_class.invalid_name", "Tax Class Name is required.");

        return null;
    }

    public ApplicationError? ValidateTaxRateCreate(TaxRateCreateRequest request)
    {
        if (request.TaxJurisdictionId == Guid.Empty)
            return new ApplicationError("validation.tax_rate.invalid_jurisdiction", "Tax Jurisdiction ID is required.");

        if (string.IsNullOrWhiteSpace(request.TaxRateCode) || request.TaxRateCode.Length > 50)
            return new ApplicationError("validation.tax_rate.invalid_code", "Tax Rate Code is required and must not exceed 50 characters.");

        if (string.IsNullOrWhiteSpace(request.TaxRateName))
            return new ApplicationError("validation.tax_rate.invalid_name", "Tax Rate Name is required.");

        if (request.RatePercent < 0 || request.RatePercent > 100)
            return new ApplicationError("validation.tax_rate.invalid_percent", "Rate Percent must be between 0 and 100.");

        if (request.ValidFrom.HasValue && request.ValidUntil.HasValue && request.ValidFrom.Value > request.ValidUntil.Value)
            return new ApplicationError("validation.tax_rate.invalid_dates", "Valid From date cannot be after Valid Until date.");

        return null;
    }

    public ApplicationError? ValidateTaxRateUpdate(TaxRateUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TaxRateName))
            return new ApplicationError("validation.tax_rate.invalid_name", "Tax Rate Name is required.");

        if (request.RatePercent < 0 || request.RatePercent > 100)
            return new ApplicationError("validation.tax_rate.invalid_percent", "Rate Percent must be between 0 and 100.");

        if (request.ValidFrom.HasValue && request.ValidUntil.HasValue && request.ValidFrom.Value > request.ValidUntil.Value)
            return new ApplicationError("validation.tax_rate.invalid_dates", "Valid From date cannot be after Valid Until date.");

        if (string.IsNullOrWhiteSpace(request.Status))
            return new ApplicationError("validation.tax_rate.invalid_status", "Status is required.");

        return null;
    }
}

