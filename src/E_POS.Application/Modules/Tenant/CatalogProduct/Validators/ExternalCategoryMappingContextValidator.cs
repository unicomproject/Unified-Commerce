using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public static class ExternalCategoryMappingContextValidator
{
    public static List<ApplicationFieldError> Validate(ExternalCategoryMappingContext? context)
    {
        var fieldErrors = new List<ApplicationFieldError>();
        if (context is null)
        {
            return fieldErrors;
        }

        if (string.IsNullOrWhiteSpace(context.Provider))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "externalCategoryMappingContext.provider",
                "Provider is required when mapping context is provided."));
        }
        else if (context.Provider.Trim().Length > ExternalCategoryMappingConstants.ProviderMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "externalCategoryMappingContext.provider",
                $"Provider cannot exceed {ExternalCategoryMappingConstants.ProviderMaxLength} characters."));
        }

        if (string.IsNullOrWhiteSpace(context.ExternalCategoryKey))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "externalCategoryMappingContext.externalCategoryKey",
                "External category key is required when mapping context is provided."));
        }
        else
        {
            var normalizedKey = ExternalProductSuggestionNormalizer.NormalizeExternalCategoryKey(context.ExternalCategoryKey);
            if (string.IsNullOrEmpty(normalizedKey))
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "externalCategoryMappingContext.externalCategoryKey",
                    "External category key is invalid or contains only control characters."));
            }
            else if (context.ExternalCategoryKey.Trim().Length > ExternalCategoryMappingConstants.ExternalCategoryKeyMaxLength)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "externalCategoryMappingContext.externalCategoryKey",
                    $"External category key cannot exceed {ExternalCategoryMappingConstants.ExternalCategoryKeyMaxLength} characters."));
            }
        }

        if (!string.IsNullOrWhiteSpace(context.ExternalCategoryName) &&
            context.ExternalCategoryName.Trim().Length > ExternalCategoryMappingConstants.ExternalCategoryNameMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "externalCategoryMappingContext.externalCategoryName",
                $"External category name cannot exceed {ExternalCategoryMappingConstants.ExternalCategoryNameMaxLength} characters."));
        }

        return fieldErrors;
    }
}
