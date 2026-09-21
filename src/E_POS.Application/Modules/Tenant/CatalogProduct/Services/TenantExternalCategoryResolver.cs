using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.Extensions.Logging;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class TenantExternalCategoryResolver : ITenantExternalCategoryResolver
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "or", "the", "for", "with", "in", "of", "to", "a", "an", "&"
    };

    private readonly IExternalCategoryMappingRepository _mappingRepository;
    private readonly ITenantAdminProductRepository _productRepository;
    private readonly ILogger<TenantExternalCategoryResolver> _logger;

    public TenantExternalCategoryResolver(
        IExternalCategoryMappingRepository mappingRepository,
        ITenantAdminProductRepository productRepository,
        ILogger<TenantExternalCategoryResolver> logger)
    {
        _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantCategoryResolutionResult> ResolveAsync(
        TenantCategoryResolutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = request.Provider?.Trim().ToLowerInvariant() ?? string.Empty;
        var key = request.ExternalCategoryKey?.Trim().ToLowerInvariant();
        var name = request.ExternalCategoryName?.Trim();

        if (request.TenantId == Guid.Empty || string.IsNullOrWhiteSpace(key))
        {
            return new TenantCategoryResolutionResult(
                Provider: provider,
                ExternalCategoryKey: key,
                ExternalCategoryName: name,
                MappedCategory: null,
                Suggestions: Array.Empty<TenantCategorySuggestionItem>());
        }

        // 1. Saved Tenant Mapping Lookup
        var mapping = await _mappingRepository.GetAsync(request.TenantId, provider, key, cancellationToken);
        if (mapping is not null)
        {
            var isSelectable = await _productRepository.IsCategoryEffectivelySelectableAsync(
                request.TenantId,
                mapping.TenantCategoryId,
                cancellationToken);

            if (isSelectable)
            {
                var createOptions = await _productRepository.GetCreateOptionsAsync(request.TenantId, cancellationToken);
                var category = createOptions.Categories.FirstOrDefault(c => c.Id == mapping.TenantCategoryId);
                var categoryName = category?.CategoryName ?? mapping.ExternalCategoryName;
                var categoryCode = category?.CategoryCode ?? string.Empty;

                _logger.LogInformation(
                    "Tenant category mapping resolved for Tenant={TenantId}, Key={Key} -> CategoryId={CategoryId}, Name={Name}",
                    request.TenantId, key, mapping.TenantCategoryId, categoryName);

                return new TenantCategoryResolutionResult(
                    Provider: provider,
                    ExternalCategoryKey: key,
                    ExternalCategoryName: name,
                    MappedCategory: new TenantCategoryCandidate(mapping.TenantCategoryId, categoryName, categoryCode),
                    Suggestions: Array.Empty<TenantCategorySuggestionItem>());
            }

            _logger.LogInformation(
                "Tenant category mapping for Tenant={TenantId}, Key={Key} points to inactive/unselectable CategoryId={CategoryId}. Falling back to suggestions.",
                request.TenantId, key, mapping.TenantCategoryId);
        }

        // 2. Suggestions Evaluation (against current tenant's active, effectively selectable categories)
        if (string.IsNullOrWhiteSpace(name))
        {
            return new TenantCategoryResolutionResult(
                Provider: provider,
                ExternalCategoryKey: key,
                ExternalCategoryName: name,
                MappedCategory: null,
                Suggestions: Array.Empty<TenantCategorySuggestionItem>());
        }

        var options = await _productRepository.GetCreateOptionsAsync(request.TenantId, cancellationToken);
        var selectableCategories = options.Categories;
        if (selectableCategories.Count == 0)
        {
            return new TenantCategoryResolutionResult(
                Provider: provider,
                ExternalCategoryKey: key,
                ExternalCategoryName: name,
                MappedCategory: null,
                Suggestions: Array.Empty<TenantCategorySuggestionItem>());
        }

        var suggestions = EvaluateSuggestions(name, selectableCategories);

        return new TenantCategoryResolutionResult(
            Provider: provider,
            ExternalCategoryKey: key,
            ExternalCategoryName: name,
            MappedCategory: null,
            Suggestions: suggestions);
    }

    private static IReadOnlyList<TenantCategorySuggestionItem> EvaluateSuggestions(
        string externalName,
        IReadOnlyList<TenantAdminProductCategoryOptionResponse> categories)
    {
        var suggestions = new List<TenantCategorySuggestionItem>();
        var seenIds = new HashSet<Guid>();

        var normalizedExternalName = CategoryConstants.NormalizeNameForComparison(externalName);
        var strippedExternalName = StripPunctuation(normalizedExternalName);

        // 1. EXACT Match (case-insensitive name comparison)
        foreach (var c in categories)
        {
            var normalizedCatName = CategoryConstants.NormalizeNameForComparison(c.CategoryName);
            if (string.Equals(normalizedCatName, normalizedExternalName, StringComparison.Ordinal))
            {
                if (seenIds.Add(c.Id))
                {
                    suggestions.Add(new TenantCategorySuggestionItem(c.Id, c.CategoryName, c.CategoryCode, "EXACT"));
                    if (suggestions.Count >= 3) return suggestions;
                }
            }
        }

        // 2. NORMALIZED Match (punctuation-stripped, space-collapsed)
        foreach (var c in categories)
        {
            if (seenIds.Contains(c.Id)) continue;

            var strippedCatName = StripPunctuation(CategoryConstants.NormalizeNameForComparison(c.CategoryName));
            if (string.Equals(strippedCatName, strippedExternalName, StringComparison.Ordinal))
            {
                if (seenIds.Add(c.Id))
                {
                    suggestions.Add(new TenantCategorySuggestionItem(c.Id, c.CategoryName, c.CategoryCode, "NORMALIZED"));
                    if (suggestions.Count >= 3) return suggestions;
                }
            }
        }

        // 3. SIMILARITY Match (conservative token overlap or substring contains)
        var externalTokens = Tokenize(externalName);
        if (externalTokens.Count > 0)
        {
            foreach (var c in categories)
            {
                if (seenIds.Contains(c.Id)) continue;

                var normalizedCatName = CategoryConstants.NormalizeNameForComparison(c.CategoryName);
                var catTokens = Tokenize(c.CategoryName);

                var isSubstringMatch = normalizedCatName.Length >= 4 &&
                    (normalizedCatName.Contains(normalizedExternalName, StringComparison.Ordinal) ||
                     normalizedExternalName.Contains(normalizedCatName, StringComparison.Ordinal));

                var hasTokenOverlap = false;
                if (!isSubstringMatch)
                {
                    foreach (var extToken in externalTokens)
                    {
                        if (catTokens.Contains(extToken))
                        {
                            hasTokenOverlap = true;
                            break;
                        }
                    }
                }

                if (isSubstringMatch || hasTokenOverlap)
                {
                    if (seenIds.Add(c.Id))
                    {
                        suggestions.Add(new TenantCategorySuggestionItem(c.Id, c.CategoryName, c.CategoryCode, "SIMILARITY"));
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
