using System.Text.Json;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Exceptions;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.Extensions.Logging;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Services;

public sealed class DefaultTenantSettingsProvider : IDefaultTenantSettingsProvider
{
    private static readonly HashSet<string> AllowedTaxPricingModes = new(StringComparer.Ordinal)
    {
        TenantSettingKeys.TaxPricingModeExclusive,
        TenantSettingKeys.TaxPricingModeInclusive
    };

    private readonly IPlatformSettingsRepository _platformSettingsRepository;
    private readonly ISettingDefinitionRepository _settingDefinitionRepository;
    private readonly ILogger<DefaultTenantSettingsProvider> _logger;

    public DefaultTenantSettingsProvider(
        IPlatformSettingsRepository platformSettingsRepository,
        ISettingDefinitionRepository settingDefinitionRepository,
        ILogger<DefaultTenantSettingsProvider> logger)
    {
        _platformSettingsRepository = platformSettingsRepository;
        _settingDefinitionRepository = settingDefinitionRepository;
        _logger = logger;
    }

    public async Task<DefaultTenantSettingsProvisionResult> BuildAsync(
        DefaultTenantSettingsProvisionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Tenant default settings provisioning started for tenant {TenantId}",
            request.TenantId);

        var platformDefaults = await _platformSettingsRepository.GetGeneralSettingsAsync(cancellationToken);

        var resolvedCurrency = ResolveRequired(
            request.RequestCurrency,
            platformDefaults.DefaultCurrencyCode,
            request.PlanCurrency,
            PlatformSettingKeys.DefaultCurrencyCode);
        _logger.LogInformation(
            "Platform default resolved for tenant {TenantId}: currency={Currency}",
            request.TenantId,
            resolvedCurrency);

        var resolvedTimezone = ResolveRequired(
            request.RequestTimezone,
            platformDefaults.DefaultTimezone,
            fallback: null,
            PlatformSettingKeys.DefaultTimezone);
        _logger.LogInformation(
            "Tenant operational default assigned for tenant {TenantId}: timezone={Timezone}",
            request.TenantId,
            resolvedTimezone);

        var resolvedLocale = ResolveRequired(
            request.RequestLocale,
            platformDefaults.DefaultLocale,
            fallback: null,
            PlatformSettingKeys.DefaultLocale);
        _logger.LogInformation(
            "Tenant operational default assigned for tenant {TenantId}: locale={Locale}",
            request.TenantId,
            resolvedLocale);

        var effectiveFeatures = new HashSet<string>(
            request.EffectiveFeatureKeys.Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        var requiredRows = TenantSettingDefinitionSeed.All
            .Where(row => IsDefinitionRequired(row, effectiveFeatures))
            .ToList();

        var skippedEntitlementKeys = TenantSettingDefinitionSeed.All
            .Where(row => row.RequiredFeatureCode is not null && !effectiveFeatures.Contains(row.RequiredFeatureCode))
            .Select(row => row.SettingKey)
            .ToList();

        foreach (var skippedKey in skippedEntitlementKeys)
        {
            _logger.LogInformation(
                "Module setting skipped due to entitlement for tenant {TenantId}: settingKey={SettingKey}",
                request.TenantId,
                skippedKey);
        }

        var requiredKeys = requiredRows.Select(row => row.SettingKey).ToList();
        var definitions = await _settingDefinitionRepository.GetActiveByKeysAsync(requiredKeys, cancellationToken);
        var definitionsByKey = definitions.ToDictionary(x => x.SettingKey, StringComparer.Ordinal);

        foreach (var row in requiredRows)
        {
            if (!definitionsByKey.ContainsKey(row.SettingKey))
            {
                _logger.LogError(
                    "Mandatory setting definition missing for tenant {TenantId}: settingKey={SettingKey}",
                    request.TenantId,
                    row.SettingKey);
                throw new MissingMandatoryTenantSettingDefinitionException(row.SettingKey);
            }
        }

        var existingDefinitionIds = await _settingDefinitionRepository
            .GetExistingSettingDefinitionIdsForTenantAsync(request.TenantId, cancellationToken);

        var settingsToInsert = new List<TenantSetting>();
        var provisionedKeys = new List<string>();

        foreach (var row in requiredRows)
        {
            var definition = definitionsByKey[row.SettingKey];
            if (existingDefinitionIds.Contains(definition.Id))
            {
                _logger.LogInformation(
                    "Setting already exists during retry for tenant {TenantId}: settingKey={SettingKey}",
                    request.TenantId,
                    row.SettingKey);
                continue;
            }

            var valueJson = ResolveInitialValueJson(row, definition, resolvedLocale);
            ValidateValue(row.SettingKey, valueJson);

            settingsToInsert.Add(TenantSetting.Create(
                Guid.NewGuid(),
                request.TenantId,
                definition.Id,
                valueJson,
                request.PlatformUserId,
                request.Now));

            provisionedKeys.Add(row.SettingKey);

            if (row.RequiredFeatureCode is null)
            {
                _logger.LogInformation(
                    "Core tenant setting created for tenant {TenantId}: settingKey={SettingKey}",
                    request.TenantId,
                    row.SettingKey);
            }
            else
            {
                _logger.LogInformation(
                    "Entitlement-scoped setting created for tenant {TenantId}: settingKey={SettingKey} entitlement={Entitlement}",
                    request.TenantId,
                    row.SettingKey,
                    row.RequiredFeatureCode);
            }
        }

        _logger.LogInformation(
            "Tenant default settings provisioning completed for tenant {TenantId}: created={CreatedCount} skippedEntitlement={SkippedCount}",
            request.TenantId,
            settingsToInsert.Count,
            skippedEntitlementKeys.Count);

        return new DefaultTenantSettingsProvisionResult(
            settingsToInsert,
            resolvedCurrency,
            resolvedTimezone,
            resolvedLocale,
            provisionedKeys,
            skippedEntitlementKeys);
    }

    private static bool IsDefinitionRequired(
        TenantSettingDefinitionSeedRow row,
        ISet<string> effectiveFeatures)
    {
        if (row.RequiredFeatureCode is null)
        {
            return true;
        }

        return effectiveFeatures.Contains(row.RequiredFeatureCode);
    }

    private static string ResolveRequired(
        string? requestValue,
        string? platformDefault,
        string? fallback,
        string platformSettingKey)
    {
        var resolved = Normalize(requestValue)
                       ?? Normalize(platformDefault)
                       ?? Normalize(fallback);

        if (resolved is null)
        {
            throw new MissingPlatformGeneralDefaultException(platformSettingKey);
        }

        return resolved;
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string ResolveInitialValueJson(
        TenantSettingDefinitionSeedRow row,
        SettingDefinition definition,
        string resolvedLocale)
    {
        if (string.Equals(row.SettingKey, TenantSettingKeys.LocaleNumberFormat, StringComparison.Ordinal))
        {
            return JsonSerializer.Serialize(resolvedLocale);
        }

        if (!string.IsNullOrWhiteSpace(definition.DefaultValue))
        {
            return definition.DefaultValue.Trim();
        }

        return row.DefaultValueJson;
    }

    private static void ValidateValue(string settingKey, string valueJson)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(valueJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidTenantSettingDefaultValueException(settingKey, $"JSON parse failed: {ex.Message}");
        }

        using (document)
        {
            if (string.Equals(settingKey, TenantSettingKeys.TaxPricingMode, StringComparison.Ordinal))
            {
                if (document.RootElement.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidTenantSettingDefaultValueException(settingKey, "Expected string.");
                }

                var mode = document.RootElement.GetString();
                if (string.IsNullOrWhiteSpace(mode) || !AllowedTaxPricingModes.Contains(mode))
                {
                    throw new InvalidTenantSettingDefaultValueException(
                        settingKey,
                        $"Expected one of: {string.Join(", ", AllowedTaxPricingModes)}.");
                }
            }
            else if (document.RootElement.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                throw new InvalidTenantSettingDefaultValueException(settingKey, "Value must not be null.");
            }
        }
    }
}
