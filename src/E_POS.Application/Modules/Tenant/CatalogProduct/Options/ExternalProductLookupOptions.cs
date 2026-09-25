namespace E_POS.Application.Modules.Tenant.CatalogProduct.Options;

/// <summary>
/// Configuration for scanner-first external product-data lookup (B6).
/// Zero enabled providers is valid and resolves to NO_MATCH at runtime.
/// Do not store credentials here — reference secret stores via environment only when a real adapter is approved.
/// </summary>
public sealed class ExternalProductLookupOptions
{
    public const string SectionName = "ExternalProductLookup";

    /// <summary>Fallback timeout when a provider entry omits TimeoutSeconds.</summary>
    public int DefaultTimeoutSeconds { get; set; } = 5;

    public List<ExternalProductLookupProviderOptions> Providers { get; set; } = [];
    public OpenFoodFactsOptions OpenFoodFacts { get; set; } = new();
    public UpcItemDbOptions UpcItemDb { get; set; } = new();
    public ProductMetadataCacheOptions Cache { get; set; } = new();
    public ProviderResilienceOptions Resilience { get; set; } = new();

    /// <summary>
    /// Normalized (trimmed, case-insensitive) set of provider names configured for this
    /// deployment — regardless of Enabled/Priority. Used to allowlist any provider identity a
    /// client echoes back on write (e.g. ExternalCategoryMappingContext.Provider), so a value
    /// that never came from a real, configured provider (like the literal "cache") can never be
    /// persisted into a mapping table.
    /// </summary>
    public IReadOnlySet<string> GetConfiguredProviderNames() =>
        Providers
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => p.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

public sealed class ProductMetadataCacheOptions
{
    public bool Enabled { get; set; } = true;
    public int TtlDays { get; set; } = 30;
}

public sealed class ProviderResilienceOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 2;
    public int RetryBackoffMilliseconds { get; set; } = 200;
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

public sealed class CircuitBreakerOptions
{
    public bool Enabled { get; set; } = true;
    public int FailureThreshold { get; set; } = 3;
    public int BreakDurationSeconds { get; set; } = 30;
}

public sealed class OpenFoodFactsOptions
{
    public const string DefaultBaseUrl = "https://world.openfoodfacts.org";
    public const string DefaultUserAgent = "OneVerzEPOS/1.0 (https://oneverz.shop; contact: support@oneverz.com)";

    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public string UserAgent { get; set; } = DefaultUserAgent;
}

/// <summary>
/// UPCitemdb configuration. "trial" mode (default) uses the free, keyless endpoint and is the
/// only mode this adapter currently exercises. "paid" mode's UserKey must come from an
/// environment variable / secret store / secured app configuration at deploy time — never
/// hardcoded here and never sent to Flutter. No provider-specific cache TTL is introduced; the
/// shared ProductMetadataCacheOptions.TtlDays applies to every provider uniformly.
/// </summary>
public sealed class UpcItemDbOptions
{
    public const string DefaultBaseUrl = "https://api.upcitemdb.com";
    public const string TrialMode = "trial";
    public const string PaidMode = "paid";

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>"trial" (default, no credentials) or "paid" (requires UserKey).</summary>
    public string Mode { get; set; } = TrialMode;

    /// <summary>Only read when Mode="paid". Must be supplied via configuration/secret store.</summary>
    public string? UserKey { get; set; }
}

public sealed class ExternalProductLookupProviderOptions
{
    /// <summary>Logical provider name matching <c>IExternalProductLookupProvider.Name</c>.</summary>
    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    /// <summary>Lower values run first. Ties break by Name ordinal.</summary>
    public int Priority { get; set; }

    /// <summary>Per-provider timeout; falls back to DefaultTimeoutSeconds.</summary>
    public int? TimeoutSeconds { get; set; }
}
