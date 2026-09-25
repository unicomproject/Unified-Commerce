using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Validators;

public static class ExternalBrandMappingContextValidator
{
    /// <param name="context">The client-submitted external brand mapping context, if any.</param>
    /// <param name="allowedProviders">
    /// When supplied (non-null), <paramref name="context"/>.Provider must match one of these
    /// values (case-insensitive) or validation fails. Callers with access to configured/known
    /// external providers (e.g. via <c>ExternalProductLookupOptions</c>) should always pass this,
    /// so a value that never came from a real provider — such as the literal "cache" — can never
    /// be persisted into an external mapping table. This is deliberately the set of *configured*
    /// providers, not *enabled* ones, so a temporarily disabled provider's historical mappings
    /// remain valid to write. Omitted (null) preserves prior behaviour for callers that do not yet
    /// have an allowlist available.
    /// </param>
    public static List<ApplicationFieldError> Validate(
        ExternalBrandMappingContext? context,
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
                "externalBrandMappingContext.provider",
                "Provider is required when mapping context is provided."));
        }
        else if (context.Provider.Trim().Length > ExternalBrandMappingConstants.ProviderMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "externalBrandMappingContext.provider",
                $"Provider cannot exceed {ExternalBrandMappingConstants.ProviderMaxLength} characters."));
        }
        else if (allowedProviders is { Count: > 0 } &&
                 !allowedProviders.Contains(context.Provider.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "externalBrandMappingContext.provider",
                "Provider is not a recognized external product-lookup provider."));
        }

        if (string.IsNullOrWhiteSpace(context.ExternalBrandKey))
        {
            fieldErrors.Add(new ApplicationFieldError(
                "externalBrandMappingContext.externalBrandKey",
                "External brand key is required when mapping context is provided."));
        }
        else
        {
            var normalizedKey = ExternalBrandKeyDeriver.DeriveExternalBrandKey(context.ExternalBrandKey);
            if (string.IsNullOrEmpty(normalizedKey))
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "externalBrandMappingContext.externalBrandKey",
                    "External brand key is invalid or contains only punctuation/control characters."));
            }
            else if (context.ExternalBrandKey.Trim().Length > ExternalBrandMappingConstants.ExternalBrandKeyMaxLength)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "externalBrandMappingContext.externalBrandKey",
                    $"External brand key cannot exceed {ExternalBrandMappingConstants.ExternalBrandKeyMaxLength} characters."));
            }
        }

        if (!string.IsNullOrWhiteSpace(context.ExternalBrandName) &&
            context.ExternalBrandName.Trim().Length > ExternalBrandMappingConstants.ExternalBrandNameMaxLength)
        {
            fieldErrors.Add(new ApplicationFieldError(
                "externalBrandMappingContext.externalBrandName",
                $"External brand name cannot exceed {ExternalBrandMappingConstants.ExternalBrandNameMaxLength} characters."));
        }

        return fieldErrors;
    }
}
