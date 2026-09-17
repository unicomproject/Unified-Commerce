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
    public ProductMetadataCacheOptions Cache { get; set; } = new();
    public ProviderResilienceOptions Resilience { get; set; } = new();
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
