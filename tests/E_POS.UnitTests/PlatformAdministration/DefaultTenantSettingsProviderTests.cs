using System.Text.Json;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Exceptions;
using E_POS.Application.Modules.Tenant.TenantFoundation.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class TenantSettingCatalogTests
{
    [Fact]
    public void SeedCatalog_HasUniqueCanonicalKeys()
    {
        var keys = TenantSettingDefinitionSeed.All.Select(x => x.SettingKey).ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(TenantSettingKeys.AllMvpKeys.Count, keys.Count);
    }

    [Fact]
    public void SeedCatalog_ContainsMandatoryCoreKeys()
    {
        foreach (var key in TenantSettingKeys.CoreKeys)
        {
            Assert.Contains(TenantSettingDefinitionSeed.All, row => row.SettingKey == key);
        }
    }

    [Fact]
    public void SeedCatalog_TaxDefaultIsExclusive()
    {
        var tax = TenantSettingDefinitionSeed.All.Single(x => x.SettingKey == TenantSettingKeys.TaxPricingMode);
        Assert.Equal(TenantSettingDefinitionSeed.TaxPricingModeDefaultJson, tax.DefaultValueJson);
        Assert.Equal($"\"{TenantSettingKeys.TaxPricingModeExclusive}\"", tax.DefaultValueJson);
    }

    [Fact]
    public void SeedCatalog_DefaultValuesAreValidJson()
    {
        foreach (var row in TenantSettingDefinitionSeed.All)
        {
            var document = JsonDocument.Parse(row.DefaultValueJson);
            Assert.NotEqual(JsonValueKind.Undefined, document.RootElement.ValueKind);
        }
    }

    [Fact]
    public void SeedCatalog_ModuleDependenciesMatchPhase1FeatureCodes()
    {
        var inventory = TenantSettingDefinitionSeed.All.Single(x => x.SettingKey == TenantSettingKeys.InventoryStockBehaviour);
        var online = TenantSettingDefinitionSeed.All.Single(x => x.SettingKey == TenantSettingKeys.OnlineStoreDefaults);
        Assert.Equal(PlatformTenantFeatureCodes.InventoryTracking, inventory.RequiredFeatureCode);
        Assert.Equal(PlatformTenantFeatureCodes.OnlineStore, online.RequiredFeatureCode);
    }
}

public sealed class DefaultTenantSettingsProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.Parse("b1000000-0000-4000-8000-000000000001");
    private static readonly Guid PlatformUserId = Guid.Parse("b1000000-0000-4000-8000-000000000002");

    [Fact]
    public async Task BuildAsync_CoreDefaults_CreatedWithTaxExclusive()
    {
        var provider = CreateProvider(featureKeys: []);

        var result = await provider.BuildAsync(CreateRequest([]), CancellationToken.None);

        Assert.Equal("LKR", result.ResolvedCurrency);
        Assert.Equal("Asia/Colombo", result.ResolvedTimezone);
        Assert.Equal("en-LK", result.ResolvedLocale);
        Assert.Equal(TenantSettingKeys.CoreKeys.Count, result.SettingsToInsert.Count);
        Assert.Contains(TenantSettingKeys.TaxPricingMode, result.ProvisionedSettingKeys);
        Assert.DoesNotContain(TenantSettingKeys.InventoryStockBehaviour, result.ProvisionedSettingKeys);
        Assert.DoesNotContain(TenantSettingKeys.OnlineStoreDefaults, result.ProvisionedSettingKeys);
        Assert.Contains(TenantSettingKeys.InventoryStockBehaviour, result.SkippedEntitlementSettingKeys);
        Assert.Contains(TenantSettingKeys.OnlineStoreDefaults, result.SkippedEntitlementSettingKeys);
    }

    [Fact]
    public async Task BuildAsync_InventoryEntitled_CreatesInventoryDefaults()
    {
        var provider = CreateProvider([PlatformTenantFeatureCodes.InventoryTracking]);

        var result = await provider.BuildAsync(
            CreateRequest([PlatformTenantFeatureCodes.InventoryTracking]),
            CancellationToken.None);

        Assert.Contains(TenantSettingKeys.InventoryStockBehaviour, result.ProvisionedSettingKeys);
        Assert.DoesNotContain(TenantSettingKeys.OnlineStoreDefaults, result.ProvisionedSettingKeys);
    }

    [Fact]
    public async Task BuildAsync_OnlineStoreEntitled_CreatesOnlineStoreDefaults()
    {
        var provider = CreateProvider([PlatformTenantFeatureCodes.OnlineStore]);

        var result = await provider.BuildAsync(
            CreateRequest([PlatformTenantFeatureCodes.OnlineStore]),
            CancellationToken.None);

        Assert.Contains(TenantSettingKeys.OnlineStoreDefaults, result.ProvisionedSettingKeys);
        Assert.DoesNotContain(TenantSettingKeys.InventoryStockBehaviour, result.ProvisionedSettingKeys);
    }

    [Fact]
    public async Task BuildAsync_UnknownEntitlement_DoesNotCreateModuleSettings()
    {
        var provider = CreateProvider(["unknown_module_xyz"]);

        var result = await provider.BuildAsync(
            CreateRequest(["unknown_module_xyz"]),
            CancellationToken.None);

        Assert.DoesNotContain(TenantSettingKeys.InventoryStockBehaviour, result.ProvisionedSettingKeys);
        Assert.DoesNotContain(TenantSettingKeys.OnlineStoreDefaults, result.ProvisionedSettingKeys);
    }

    [Fact]
    public async Task BuildAsync_MissingPlatformCurrency_Fails()
    {
        var provider = CreateProvider(
            [],
            platform: new PlatformSettingsResponse
            {
                DefaultTimezone = "Asia/Colombo",
                DefaultLocale = "en-LK"
            });

        var ex = await Assert.ThrowsAsync<MissingPlatformGeneralDefaultException>(() =>
            provider.BuildAsync(
                CreateRequest([], requestCurrency: null, planCurrency: null),
                CancellationToken.None));

        Assert.Equal(PlatformSettingKeys.DefaultCurrencyCode, ex.SettingKey);
    }

    [Fact]
    public async Task BuildAsync_MissingPlatformTimezone_Fails()
    {
        var provider = CreateProvider(
            [],
            platform: new PlatformSettingsResponse
            {
                DefaultCurrencyCode = "LKR",
                DefaultLocale = "en-LK"
            });

        var ex = await Assert.ThrowsAsync<MissingPlatformGeneralDefaultException>(() =>
            provider.BuildAsync(CreateRequest([]), CancellationToken.None));

        Assert.Equal(PlatformSettingKeys.DefaultTimezone, ex.SettingKey);
    }

    [Fact]
    public async Task BuildAsync_MissingMandatoryDefinition_Fails()
    {
        var definitions = SeedDefinitions()
            .Where(x => x.SettingKey != TenantSettingKeys.TaxPricingMode)
            .ToList();
        var provider = CreateProvider([], definitions: definitions);

        var ex = await Assert.ThrowsAsync<MissingMandatoryTenantSettingDefinitionException>(() =>
            provider.BuildAsync(CreateRequest([]), CancellationToken.None));

        Assert.Equal(TenantSettingKeys.TaxPricingMode, ex.SettingKey);
    }

    [Fact]
    public async Task BuildAsync_ExistingSetting_PreservedOnRetry()
    {
        var existingId = TenantSettingDefinitionSeed.TaxPricingModeId;
        var provider = CreateProvider(
            [],
            existingDefinitionIds: new HashSet<Guid> { existingId });

        var result = await provider.BuildAsync(CreateRequest([]), CancellationToken.None);

        Assert.DoesNotContain(
            result.SettingsToInsert,
            setting => setting.SettingDefinitionId == existingId);
        Assert.Equal(TenantSettingKeys.CoreKeys.Count - 1, result.SettingsToInsert.Count);
    }

    [Fact]
    public async Task BuildAsync_NumberFormatUsesResolvedLocale()
    {
        var provider = CreateProvider([]);

        var result = await provider.BuildAsync(
            CreateRequest([], requestLocale: "en-GB"),
            CancellationToken.None);

        var numberFormat = result.SettingsToInsert.Single(x =>
            x.SettingDefinitionId == TenantSettingDefinitionSeed.LocaleNumberFormatId);
        Assert.Equal("\"en-GB\"", numberFormat.SettingValue);
    }

    private static DefaultTenantSettingsProvisionRequest CreateRequest(
        IReadOnlyCollection<string> featureKeys,
        string? requestCurrency = null,
        string? requestTimezone = null,
        string? requestLocale = null,
        string? planCurrency = "LKR") =>
        new(
            TenantId,
            PlatformUserId,
            Now,
            requestCurrency,
            requestTimezone,
            requestLocale,
            planCurrency,
            featureKeys);

    private static DefaultTenantSettingsProvider CreateProvider(
        IReadOnlyCollection<string> featureKeys,
        PlatformSettingsResponse? platform = null,
        IReadOnlyList<SettingDefinition>? definitions = null,
        ISet<Guid>? existingDefinitionIds = null)
    {
        _ = featureKeys;
        return new DefaultTenantSettingsProvider(
            new FakePlatformSettingsRepository(platform ?? new PlatformSettingsResponse
            {
                DefaultCurrencyCode = "LKR",
                DefaultTimezone = "Asia/Colombo",
                DefaultLocale = "en-LK"
            }),
            new FakeSettingDefinitionRepository(
                definitions ?? SeedDefinitions(),
                existingDefinitionIds ?? new HashSet<Guid>()),
            NullLogger<DefaultTenantSettingsProvider>.Instance);
    }

    private static List<SettingDefinition> SeedDefinitions() =>
        TenantSettingDefinitionSeed.All
            .Select(row => SettingDefinition.Create(
                row.Id,
                row.SettingKey,
                row.DisplayName,
                row.ValueType,
                row.DefaultValueJson,
                row.Description,
                row.IsTenantEditable,
                TenantSettingKeys.SettingDefinitionStatusActive,
                Now))
            .ToList();

    private sealed class FakePlatformSettingsRepository : IPlatformSettingsRepository
    {
        private readonly PlatformSettingsResponse _response;

        public FakePlatformSettingsRepository(PlatformSettingsResponse response) => _response = response;

        public Task<PlatformSettingsResponse> GetGeneralSettingsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_response);

        public Task<PlatformSettingsResponse> SaveGeneralSettingsAsync(
            UpdatePlatformSettingsRequest request,
            Guid updatedByPlatformUserId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class FakeSettingDefinitionRepository : ISettingDefinitionRepository
    {
        private readonly IReadOnlyList<SettingDefinition> _definitions;
        private readonly ISet<Guid> _existing;

        public FakeSettingDefinitionRepository(
            IReadOnlyList<SettingDefinition> definitions,
            ISet<Guid> existing)
        {
            _definitions = definitions;
            _existing = existing;
        }

        public Task<IReadOnlyList<SettingDefinition>> GetActiveByKeysAsync(
            IReadOnlyCollection<string> settingKeys,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SettingDefinition>>(
                _definitions.Where(x => settingKeys.Contains(x.SettingKey)).ToList());

        public Task<IReadOnlySet<Guid>> GetExistingSettingDefinitionIdsForTenantAsync(
            Guid tenantId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<Guid>>(_existing.ToHashSet());
    }
}
