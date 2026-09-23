using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.Extensions.Logging;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class TenantExternalBrandResolver : ITenantExternalBrandResolver
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "or", "the", "for", "with", "in", "of", "to", "a", "an", "&"
    };

    private readonly IExternalBrandMappingRepository _mappingRepository;
    private readonly ITenantAdminProductRepository _productRepository;
    private readonly ILogger<TenantExternalBrandResolver> _logger;

    public TenantExternalBrandResolver(
        IExternalBrandMappingRepository mappingRepository,
        ITenantAdminProductRepository productRepository,
        ILogger<TenantExternalBrandResolver> logger)
    {
        _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantBrandResolutionResult> ResolveAsync(
        TenantBrandResolutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = request.Provider?.Trim().ToLowerInvariant() ?? string.Empty;
        var key = request.ExternalBrandKey?.Trim().ToLowerInvariant();
        var name = request.ExternalBrandName?.Trim();

        if (request.TenantId == Guid.Empty || string.IsNullOrWhiteSpace(key))
        {
            return new TenantBrandResolutionResult(
                Provider: provider,
                ExternalBrandKey: key,
                ExternalBrandName: name,
                MappedBrand: null,
                Suggestions: Array.Empty<TenantBrandSuggestionItem>());
        }

        // 1. Saved Tenant Mapping Lookup
        var mapping = await _mappingRepository.GetAsync(request.TenantId, provider, key, cancellationToken);
        if (mapping is not null)
        {
            var isSelectable = await _productRepository.BrandBelongsToTenantAsync(
                request.TenantId,
                mapping.TenantBrandId,
                cancellationToken);

            if (isSelectable)
            {
                var createOptions = await _productRepository.GetCreateOptionsAsync(request.TenantId, cancellationToken);
                var brand = createOptions.Brands.FirstOrDefault(b => b.BrandId == mapping.TenantBrandId);
                var brandName = brand?.BrandName ?? mapping.ExternalBrandName;
                var brandCode = brand?.BrandCode ?? string.Empty;

                _logger.LogInformation(
                    "Tenant brand mapping resolved for Tenant={TenantId}, Key={Key} -> BrandId={BrandId}, Name={Name}",
                    request.TenantId, key, mapping.TenantBrandId, brandName);

                return new TenantBrandResolutionResult(
                    Provider: provider,
                    ExternalBrandKey: key,
                    ExternalBrandName: name,
                    MappedBrand: new TenantBrandCandidate(mapping.TenantBrandId, brandName, brandCode),
                    Suggestions: Array.Empty<TenantBrandSuggestionItem>());
            }

            _logger.LogInformation(
                "Tenant brand mapping for Tenant={TenantId}, Key={Key} points to inactive/unselectable BrandId={BrandId}. Falling back to suggestions.",
                request.TenantId, key, mapping.TenantBrandId);
        }

        // 2. Suggestions Evaluation (against current tenant's active brands)
        if (string.IsNullOrWhiteSpace(name))
        {
            return new TenantBrandResolutionResult(
                Provider: provider,
                ExternalBrandKey: key,
                ExternalBrandName: name,
                MappedBrand: null,
                Suggestions: Array.Empty<TenantBrandSuggestionItem>());
        }

        var options = await _productRepository.GetCreateOptionsAsync(request.TenantId, cancellationToken);
        var availableBrands = options.Brands;
        if (availableBrands.Count == 0)
        {
            return new TenantBrandResolutionResult(
                Provider: provider,
                ExternalBrandKey: key,
                ExternalBrandName: name,
                MappedBrand: null,
                Suggestions: Array.Empty<TenantBrandSuggestionItem>());
        }

        var suggestions = EvaluateSuggestions(name, availableBrands);

        return new TenantBrandResolutionResult(
            Provider: provider,
            ExternalBrandKey: key,
            ExternalBrandName: name,
            MappedBrand: null,
            Suggestions: suggestions);
    }

    private static IReadOnlyList<TenantBrandSuggestionItem> EvaluateSuggestions(
        string externalName,
        IReadOnlyList<TenantAdminProductBrandOptionResponse> brands)
    {
        var suggestions = new List<TenantBrandSuggestionItem>();
        var seenIds = new HashSet<Guid>();

        var normalizedExternalName = BrandConstants.NormalizeNameForComparison(externalName);
        var strippedExternalName = StripPunctuation(normalizedExternalName);

        // 1. EXACT Match (case-insensitive name comparison)
        foreach (var b in brands)
        {
            var normalizedBrandName = BrandConstants.NormalizeNameForComparison(b.BrandName);
            if (string.Equals(normalizedBrandName, normalizedExternalName, StringComparison.Ordinal))
            {
                if (seenIds.Add(b.BrandId))
                {
                    suggestions.Add(new TenantBrandSuggestionItem(b.BrandId, b.BrandName, b.BrandCode, "EXACT"));
                    if (suggestions.Count >= 3) return suggestions;
                }
            }
        }

        // 2. NORMALIZED Match (punctuation-stripped, space-collapsed)
        foreach (var b in brands)
        {
            if (seenIds.Contains(b.BrandId)) continue;

            var strippedBrandName = StripPunctuation(BrandConstants.NormalizeNameForComparison(b.BrandName));
            if (string.Equals(strippedBrandName, strippedExternalName, StringComparison.Ordinal))
            {
                if (seenIds.Add(b.BrandId))
                {
                    suggestions.Add(new TenantBrandSuggestionItem(b.BrandId, b.BrandName, b.BrandCode, "NORMALIZED"));
                    if (suggestions.Count >= 3) return suggestions;
                }
            }
        }

        // 3. SIMILARITY Match (conservative token overlap or substring contains)
        var externalTokens = Tokenize(externalName);
        if (externalTokens.Count > 0)
        {
            foreach (var b in brands)
            {
                if (seenIds.Contains(b.BrandId)) continue;

                var normalizedBrandName = BrandConstants.NormalizeNameForComparison(b.BrandName);
                var brandTokens = Tokenize(b.BrandName);

                var isSubstringMatch = normalizedBrandName.Length >= 4 &&
                    (normalizedBrandName.Contains(normalizedExternalName, StringComparison.Ordinal) ||
                     normalizedExternalName.Contains(normalizedBrandName, StringComparison.Ordinal));

                var hasTokenOverlap = false;
                if (!isSubstringMatch)
                {
                    foreach (var extToken in externalTokens)
                    {
                        if (brandTokens.Contains(extToken))
                        {
                            hasTokenOverlap = true;
                            break;
                        }
                    }
                }

                if (isSubstringMatch || hasTokenOverlap)
                {
                    if (seenIds.Add(b.BrandId))
                    {
                        suggestions.Add(new TenantBrandSuggestionItem(b.BrandId, b.BrandName, b.BrandCode, "SIMILARITY"));
                        if (suggestions.Count >= 3) return suggestions;
                    }
                }
            }
        }

        return suggestions;
    }

    private static HashSet<string> Tokenize(string text)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text)) return tokens;

        var parts = text.Split(new[] { ' ', '-', '_', ',', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var clean = part.Trim().ToLowerInvariant();
            if (clean.Length >= 3 && !StopWords.Contains(clean))
            {
                tokens.Add(clean);
            }
        }

        return tokens;
    }

    private static string StripPunctuation(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var chars = text.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray();
        return string.Join(" ", new string(chars).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
}
