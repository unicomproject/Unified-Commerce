using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class ProductVariantGenerationService
{
    public Task<ApplicationResult<VariantConfigurationDto>> GenerateAndReconcileVariantsAsync(
        Guid tenantId,
        Guid productId,
        VariantConfigurationDto variantConfiguration,
        CancellationToken cancellationToken)
    {
        // Simulate checking if product structure is VARIANT
        if (variantConfiguration == null || variantConfiguration.Options.Count == 0)
        {
            return Task.FromResult(ApplicationResult<VariantConfigurationDto>.Success(variantConfiguration!));
        }

        // Generate combinations using Cartesian Product
        var generatedVariants = GenerateCartesianVariants(variantConfiguration.Options);

        // Map them back to the DTO
        var updatedVariants = generatedVariants.Select(v => new VariantConfigurationVariantDto(
            ClientCombinationKey: Guid.NewGuid().ToString(),
            ProductVariantId: Guid.NewGuid(), // In reality, this would map from DB or generate sequentially.
            VariantCode: null,
            OptionCombinationHash: v.Hash,
            CombinationLabel: v.Label,
            DisplayLabel: v.Label,
            Included: true,
            Status: "ACTIVE",
            ExactImageMediaAssetId: null,
            SelectedValues: v.Values
        )).ToList();

        var updatedConfiguration = new VariantConfigurationDto(
            Options: variantConfiguration.Options,
            Variants: updatedVariants,
            ExcludedCombinationHashes: new List<VariantConfigurationDeletedCombinationDto>()
        );
        
        return Task.FromResult(ApplicationResult<VariantConfigurationDto>.Success(updatedConfiguration));
    }

    private IReadOnlyList<GeneratedCombination> GenerateCartesianVariants(IReadOnlyList<VariantConfigurationOptionDto> options)
    {
        var result = new List<GeneratedCombination>();
        
        // Base case
        if (options.Count == 0) return result;

        var firstOption = options[0];
        foreach (var val in firstOption.Values)
        {
            result.Add(new GeneratedCombination
            {
                Label = val.ValueName,
                Hash = val.ValueName.ToLowerInvariant(),
                Values = new List<VariantConfigurationSelectedValueDto>
                {
                    new(firstOption.SourceOptionTemplateId, val.SourceOptionTemplateValueId, firstOption.OptionName, val.ValueName)
                }
            });
        }

        // Cartesian product with remaining options
        for (int i = 1; i < options.Count; i++)
        {
            var currentOption = options[i];
            var temp = new List<GeneratedCombination>();

            foreach (var existing in result)
            {
                foreach (var val in currentOption.Values)
                {
                    var newCombination = new GeneratedCombination
                    {
                        Label = $"{existing.Label} / {val.ValueName}",
                        Hash = $"{existing.Hash}-{val.ValueName.ToLowerInvariant()}",
                        Values = new List<VariantConfigurationSelectedValueDto>(existing.Values)
                        {
                            new(currentOption.SourceOptionTemplateId, val.SourceOptionTemplateValueId, currentOption.OptionName, val.ValueName)
                        }
                    };
                    temp.Add(newCombination);
                }
            }
            result = temp;
        }

        return result;
    }

    private class GeneratedCombination
    {
        public string Label { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public List<VariantConfigurationSelectedValueDto> Values { get; set; } = new();
    }
}
