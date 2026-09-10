using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Services;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

internal static class VariantConfigurationValidationHelper
{
    internal static IReadOnlyList<ApplicationFieldError> ValidateCombinationLimits(
        VariantConfigurationDto? configuration,
        bool requireCompleteConfiguration)
    {
        if (configuration?.Options is null || configuration.Options.Count == 0)
        {
            return requireCompleteConfiguration
                ?
                [
                    new ApplicationFieldError(
                        "variantConfiguration.options",
                        "At least one attribute must be defined for a Variant product.",
                        "product.variant_options_required"),
                ]
                : [];
        }

        var fieldErrors = new List<ApplicationFieldError>();
        var valueCounts = new List<int>();

        for (var i = 0; i < configuration.Options.Count; i++)
        {
            var option = configuration.Options[i];
            if (option.Values is null || option.Values.Count == 0)
            {
                if (requireCompleteConfiguration)
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"variantConfiguration.options[{i}].values",
                        $"Attribute '{option.OptionCode}' must contain at least one selected value.",
                        "product.option_values_required"));
                }

                return fieldErrors;
            }

            valueCounts.Add(option.Values.Count);
        }

        if (!VariantCombinationCalculator.TryCalculateCombinationCount(
                valueCounts,
                out var combinationCount,
                out var failure))
        {
            if (failure == VariantCombinationCalculator.CombinationCountFailure.ExceedsMaximum)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "variantConfiguration",
                    $"Cartesian matrix produces {combinationCount} variants, exceeding maximum allowed limit of {ProductConstants.MaxVariantCombinationsPerProduct}.",
                    "product.max_variants_exceeded"));
            }
            else if (requireCompleteConfiguration)
            {
                fieldErrors.Add(new ApplicationFieldError(
                    "variantConfiguration",
                    "Variant configuration is incomplete.",
                    "product.option_values_required"));
            }
        }

        return fieldErrors;
    }

    internal static IReadOnlyList<ApplicationFieldError> ValidateSelectedValuesBelongToOptions(
        VariantConfigurationDto configuration)
    {
        var fieldErrors = new List<ApplicationFieldError>();
        if (configuration.Variants is null)
        {
            return fieldErrors;
        }

        var allowedValuesByTemplate = configuration.Options
            .Where(o => o.SourceOptionTemplateId.HasValue)
            .ToDictionary(
                o => o.SourceOptionTemplateId!.Value,
                o => o.Values
                    .Where(v => v.SourceOptionTemplateValueId.HasValue)
                    .Select(v => v.SourceOptionTemplateValueId!.Value)
                    .ToHashSet());

        for (var i = 0; i < configuration.Variants.Count; i++)
        {
            var variant = configuration.Variants[i];
            foreach (var selected in variant.SelectedValues)
            {
                if (!selected.SourceOptionTemplateId.HasValue ||
                    !selected.SourceOptionTemplateValueId.HasValue)
                {
                    continue;
                }

                if (!allowedValuesByTemplate.TryGetValue(
                        selected.SourceOptionTemplateId.Value,
                        out var allowedValues) ||
                    !allowedValues.Contains(selected.SourceOptionTemplateValueId.Value))
                {
                    fieldErrors.Add(new ApplicationFieldError(
                        $"variantConfiguration.variants[{i}].selectedValues",
                        "Selected value does not belong to the selected attribute.",
                        "product.option_value_not_owned_by_attribute"));
                }
            }
        }

        return fieldErrors;
    }
}
