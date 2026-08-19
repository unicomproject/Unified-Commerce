using System;
using System.Text.Json;
using System.Collections.Generic;

public class Program
{
    public sealed record VariantConfigurationDto(
        IReadOnlyList<VariantConfigurationOptionDto> Options,
        IReadOnlyList<VariantConfigurationVariantDto> Variants,
        IReadOnlyList<VariantConfigurationDeletedCombinationDto> ExcludedCombinationHashes);

    public sealed record VariantConfigurationOptionDto(
        Guid? ProductOptionId,
        Guid SourceOptionTemplateId,
        string OptionCode,
        string OptionName,
        string OptionType,
        string? InputType,
        int SortOrder,
        IReadOnlyList<VariantConfigurationOptionValueDto> Values);

    public sealed record VariantConfigurationOptionValueDto(
        Guid? ProductOptionValueId,
        Guid SourceOptionTemplateValueId,
        string ValueCode,
        string ValueName,
        string? DisplayName,
        string? ColorHex,
        int SortOrder,
        Guid? ImageMediaAssetId);

    public sealed record VariantConfigurationVariantDto(
        string ClientCombinationKey,
        Guid? ProductVariantId,
        string? DisplayLabel,
        bool IncludeVariant,
        Guid? ExactImageMediaAssetId,
        IReadOnlyList<VariantConfigurationSelectedValueDto> SelectedValues);

    public sealed record VariantConfigurationSelectedValueDto(
        Guid SourceOptionTemplateId,
        Guid SourceOptionTemplateValueId);

    public sealed record VariantConfigurationDeletedCombinationDto(
        string ClientCombinationKey,
        Guid? ProductVariantId,
        string OptionCombinationHash,
        IReadOnlyList<VariantConfigurationSelectedValueDto> SelectedValues);

    public sealed class SaveProductDraftRequest
    {
        public VariantConfigurationDto? VariantConfiguration { get; set; }
    }

    public static void Main()
    {
        string json = @"{ ""variantConfiguration"": { ""options"": [], ""variants"": [], ""excludedCombinationHashes"": [] } }";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<SaveProductDraftRequest>(json, options);
        Console.WriteLine(""Success: "" + (result?.VariantConfiguration != null));
    }
}
