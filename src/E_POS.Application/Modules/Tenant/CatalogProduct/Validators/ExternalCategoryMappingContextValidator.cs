using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public static class ExternalCategoryMappingContextValidator
{
    /// <param name="context">The client-submitted external category mapping context, if any.</param>
    /// <param name="allowedProviders">
    /// When supplied (non-null), <paramref name="context"/>.Provider must match one of these
    /// values (case-insensitive) or validation fails. Callers with access to configured/known
    /// external providers (e.g. via <c>ExternalProductLookupOptions</c>) should always pass this,
    /// so a value that never came from a real provider — such as the literal "cache" — can never
    /// be persisted into an external mapping table. Omitted (null) preserves prior behaviour for
    /// callers that do not yet have an allowlist available.
    /// </param>
    public static List<ApplicationFieldError> Validate(
        ExternalCategoryMappingContext? context,
        IReadOnlyCollection<string>? allowedProviders = null)
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
        else if (allowedProviders is { Count: > 0 } &&
                 !allowedProviders.Contains(context.Provider.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "externalCategoryMappingContext.provider",
                "Provider is not a recognized external product-lookup provider."));
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
