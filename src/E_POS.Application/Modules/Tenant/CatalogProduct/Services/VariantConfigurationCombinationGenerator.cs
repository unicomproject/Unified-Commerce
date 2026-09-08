using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Services;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

/// <summary>
/// Server-authoritative Cartesian variant combination generation for Step 4 Variant Configuration.
/// Does not trust any client-supplied combination totals.
/// </summary>
public static class VariantConfigurationCombinationGenerator
{
    public sealed record GenerationResult(
        bool Succeeded,
        VariantConfigurationDto? Configuration,
        string? ErrorCode,
        string? ErrorMessage)
    {
        public static GenerationResult Success(VariantConfigurationDto configuration) =>
            new(true, configuration, null, null);

        public static GenerationResult Failure(string errorCode, string errorMessage) =>
            new(false, null, errorCode, errorMessage);
    }

    public static GenerationResult GenerateAndReconcile(VariantConfigurationDto input)
    {
        if (input.Options is null || input.Options.Count == 0)
        {
            return GenerationResult.Success(input);
        }

        var validOptions = input.Options
            .Where(o => o.Values is { Count: > 0 })
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.OptionCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (validOptions.Count == 0)
        {
            return GenerationResult.Success(input);
        }

        if (validOptions.Count != input.Options.Count)
        {
            return GenerationResult.Failure(
                "product.option_values_required",
                "Each selected variant attribute must contain at least one value.");
        }

        var valueCounts = validOptions.Select(o => o.Values.Count).ToList();
        if (!VariantCombinationCalculator.TryCalculateCombinationCount(
                valueCounts,
                out var expectedCount,
                out var failure))
        {
            return failure switch
            {
                VariantCombinationCalculator.CombinationCountFailure.ExceedsMaximum =>
                    GenerationResult.Failure(
                        "product.max_variants_exceeded",
                        $"Cartesian matrix produces more than the maximum allowed limit of {ProductConstants.MaxVariantCombinationsPerProduct} variants."),
                _ => GenerationResult.Failure(
                    "product.option_values_required",
                    "Variant configuration is incomplete."),
            };
        }

        var generatedVariants = new List<VariantConfigurationVariantDto>();
        var existingVariants = input.Variants ?? Array.Empty<VariantConfigurationVariantDto>();
        var deletedVariants = input.ExcludedCombinationHashes ?? Array.Empty<VariantConfigurationDeletedCombinationDto>();

        IEnumerable<List<VariantConfigurationSelectedValueDto>> currentCombinations =
            new List<List<VariantConfigurationSelectedValueDto>> { new() };

        foreach (var option in validOptions)
        {
            currentCombinations = currentCombinations.SelectMany(combo =>
                option.Values
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.ValueCode, StringComparer.OrdinalIgnoreCase)
                    .Select(val =>
                    {
                        var newCombo = new List<VariantConfigurationSelectedValueDto>(combo)
                        {
                            new(
                                option.SourceOptionTemplateId,
                                val.SourceOptionTemplateValueId,
                                option.OptionName,
                                val.ValueName),
                        };
                        return newCombo;
                    }));
        }

        foreach (var combo in currentCombinations)
        {
            var ordered = combo
                .OrderBy(x => x.SourceOptionTemplateId?.ToString("D") ?? x.OptionName ?? string.Empty,
                    StringComparer.Ordinal)
                .ToList();

            var clientKey = BuildClientCombinationKey(ordered);
            var legacyPreviewHash = BuildLegacyPreviewHash(ordered);

            if (deletedVariants.Any(d =>
                    ProductVariantCombinationHashHelper.MatchesCombinationHash(
                        d.OptionCombinationHash,
                        null,
                        legacyPreviewHash) ||
                    string.Equals(d.ClientCombinationKey, clientKey, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var existing = existingVariants.FirstOrDefault(v =>
                ProductVariantCombinationHashHelper.MatchesCombinationHash(
                    v.OptionCombinationHash,
                    null,
                    legacyPreviewHash) ||
                string.Equals(v.ClientCombinationKey, clientKey, StringComparison.OrdinalIgnoreCase) ||
                (v.SelectedValues.Count == ordered.Count && v.SelectedValues.All(sv =>
                    ordered.Any(o =>
                        (o.SourceOptionTemplateId != null &&
                         o.SourceOptionTemplateId == sv.SourceOptionTemplateId &&
                         o.SourceOptionTemplateValueId == sv.SourceOptionTemplateValueId) ||
                        (o.SourceOptionTemplateId == null &&
                         string.Equals(o.OptionName, sv.OptionName, StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(o.ValueName, sv.ValueName, StringComparison.OrdinalIgnoreCase))))));

            if (existing != null)
            {
                generatedVariants.Add(existing with
                {
                    ClientCombinationKey = string.IsNullOrWhiteSpace(existing.ClientCombinationKey)
                        ? clientKey
                        : existing.ClientCombinationKey,
                    OptionCombinationHash = existing.OptionCombinationHash,
                    SelectedValues = ordered,
                    CombinationLabel = existing.CombinationLabel ??
                                       string.Join(" / ", ordered.Select(x => x.ValueName)),
                });
            }
            else
            {
                var label = string.Join(" / ", ordered.Select(x => x.ValueName));
                generatedVariants.Add(new VariantConfigurationVariantDto(
                    clientKey,
                    null,
                    null,
                    null,
                    label,
                    label,
                    true,
                    null,
                    null,
                    ordered));
            }
        }

        if (generatedVariants.Count != expectedCount)
        {
            return GenerationResult.Failure(
                "product.variant_generation_mismatch",
                "Generated variant combinations do not match the server-derived Cartesian product.");
        }

        return GenerationResult.Success(new VariantConfigurationDto(
            input.Options,
            generatedVariants,
            deletedVariants));
    }

    internal static string BuildLegacyPreviewHash(
        IReadOnlyList<VariantConfigurationSelectedValueDto> orderedValues) =>
        ProductVariantCombinationHashHelper.GenerateLegacyMd5PreviewHash(
            orderedValues.Select(x => (x.SourceOptionTemplateId, x.SourceOptionTemplateValueId, x.OptionName, x.ValueName)));

    internal static string BuildClientCombinationKey(
        IReadOnlyList<VariantConfigurationSelectedValueDto> orderedValues)
    {
        var templatePairs = orderedValues
            .Where(v => v.SourceOptionTemplateId.HasValue && v.SourceOptionTemplateValueId.HasValue)
            .Select(v => (v.SourceOptionTemplateId!.Value, v.SourceOptionTemplateValueId!.Value))
            .ToList();

        if (templatePairs.Count == orderedValues.Count && templatePairs.Count > 0)
        {
            return ProductVariantClientKeyHelper.GenerateClientCombinationKey(templatePairs);
        }

        return string.Join(";", orderedValues.Select(v =>
            $"{v.SourceOptionTemplateId?.ToString("D") ?? v.OptionName}:{v.SourceOptionTemplateValueId?.ToString("D") ?? v.ValueName}"));
    }
}
