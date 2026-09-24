using System.Globalization;
using System.Text;
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

        var suggestions = EvaluateSuggestions(name, request.ExternalCategoryHierarchy, selectableCategories);

        return new TenantCategoryResolutionResult(
            Provider: provider,
            ExternalCategoryKey: key,
            ExternalCategoryName: name,
            MappedCategory: null,
            Suggestions: suggestions);
    }

    private const int MaxSuggestions = 3;

    // Public match types (unchanged wire contract, see class remarks below for why the additional
    // HIERARCHY_* tiers are internal-only): LEAF_* fire against the provider's own leaf category
    // display name (ExternalCategoryName) exactly as before this enhancement. HIERARCHY_* fire
    // against the broader ExternalCategoryHierarchy (e.g. "Beverages" > "Carbonated Drinks" >
    // "Sodas" > "Colas") so an existing tenant category that matches a PARENT node — not just the
    // provider's own leaf — still surfaces as a suggestion. Priority (strongest first, matches
    // §2/§8 of the hierarchy-aware resolver spec):
    //   LEAF_EXACT > LEAF_NORMALIZED > HIERARCHY_EXACT > HIERARCHY_NORMALIZED
    //     > LEAF_SIMILARITY > HIERARCHY_SIMILARITY
    // A category already added by an earlier (stronger) tier is never re-added by a later one —
    // enforced by the shared `seenIds` set below.
    private const string MatchTypeLeafExact = "LEAF_EXACT";
    private const string MatchTypeLeafNormalized = "LEAF_NORMALIZED";
    private const string MatchTypeHierarchyExact = "HIERARCHY_EXACT";
    private const string MatchTypeHierarchyNormalized = "HIERARCHY_NORMALIZED";
    private const string MatchTypeLeafSimilarity = "LEAF_SIMILARITY";
    private const string MatchTypeHierarchySimilarity = "HIERARCHY_SIMILARITY";

    private static IReadOnlyList<TenantCategorySuggestionItem> EvaluateSuggestions(
        string externalName,
        IReadOnlyList<string>? externalHierarchy,
        IReadOnlyList<TenantAdminProductCategoryOptionResponse> categories)
    {
        var suggestions = new List<TenantCategorySuggestionItem>();
        var seenIds = new HashSet<Guid>();

        var normalizedExternalName = CategoryConstants.NormalizeNameForComparison(externalName);
        var strippedExternalName = StripPunctuation(normalizedExternalName);

        // Deepest-first, human-comparable hierarchy node names (e.g. "en:carbonated-drinks" ->
        // "carbonated drinks"), excluding a node identical to the leaf itself — the leaf is already
        // covered by the LEAF_* tiers above and re-running it as "hierarchy" would only waste work.
        var hierarchyNodes = DeriveHierarchyNodeNamesDeepestFirst(externalHierarchy, strippedExternalName);

        // 1. LEAF_EXACT — case-insensitive comparison against the provider's own leaf category name.
        if (TryAddExactMatches(categories, normalizedExternalName, MatchTypeLeafExact, suggestions, seenIds))
        {
            return suggestions;
        }

        // 2. LEAF_NORMALIZED — punctuation-stripped, diacritic-folded leaf comparison.
        if (TryAddNormalizedMatches(categories, strippedExternalName, MatchTypeLeafNormalized, suggestions, seenIds))
        {
            return suggestions;
        }

        // 3. HIERARCHY_EXACT — same case-insensitive comparison, against each hierarchy node in turn
        //    (deepest/most-specific node first), never auto-selected — suggestion only (see §6).
        foreach (var node in hierarchyNodes)
        {
            var normalizedNode = CategoryConstants.NormalizeNameForComparison(node);
            if (TryAddExactMatches(categories, normalizedNode, MatchTypeHierarchyExact, suggestions, seenIds))
            {
                return suggestions;
            }
        }

        // 4. HIERARCHY_NORMALIZED — punctuation-stripped/diacritic-folded hierarchy node comparison.
        foreach (var node in hierarchyNodes)
        {
            var strippedNode = StripPunctuation(CategoryConstants.NormalizeNameForComparison(node));
            if (TryAddNormalizedMatches(categories, strippedNode, MatchTypeHierarchyNormalized, suggestions, seenIds))
            {
                return suggestions;
            }
        }

        // 5. LEAF_SIMILARITY — conservative token overlap or substring contains against the leaf name.
        if (TryAddSimilarityMatches(categories, externalName, normalizedExternalName, MatchTypeLeafSimilarity, suggestions, seenIds))
        {
            return suggestions;
        }

        // 6. HIERARCHY_SIMILARITY — same conservative check, against each hierarchy node. This is
        //    still textual/token matching, never broad semantic classification (see §20): a node like
        //    "Beverages" will not match a tenant category "Food" just because both are drink-adjacent.
        foreach (var node in hierarchyNodes)
        {
            var normalizedNode = CategoryConstants.NormalizeNameForComparison(node);
            if (TryAddSimilarityMatches(categories, node, normalizedNode, MatchTypeHierarchySimilarity, suggestions, seenIds))
            {
                return suggestions;
            }
        }

        return suggestions;
    }

    private static bool TryAddExactMatches(
        IReadOnlyList<TenantAdminProductCategoryOptionResponse> categories,
        string normalizedTarget,
        string matchType,
        List<TenantCategorySuggestionItem> suggestions,
        HashSet<Guid> seenIds)
    {
        if (string.IsNullOrEmpty(normalizedTarget))
        {
            return false;
        }

        foreach (var c in categories)
        {
            if (seenIds.Contains(c.Id)) continue;

            var normalizedCatName = CategoryConstants.NormalizeNameForComparison(c.CategoryName);
            if (string.Equals(normalizedCatName, normalizedTarget, StringComparison.Ordinal) &&
                seenIds.Add(c.Id))
            {
                suggestions.Add(new TenantCategorySuggestionItem(c.Id, c.CategoryName, c.CategoryCode, matchType));
                if (suggestions.Count >= MaxSuggestions) return true;
            }
        }

        return false;
    }

    private static bool TryAddNormalizedMatches(
        IReadOnlyList<TenantAdminProductCategoryOptionResponse> categories,
        string strippedTarget,
        string matchType,
        List<TenantCategorySuggestionItem> suggestions,
        HashSet<Guid> seenIds)
    {
        if (string.IsNullOrEmpty(strippedTarget))
        {
            return false;
        }

        foreach (var c in categories)
        {
            if (seenIds.Contains(c.Id)) continue;

            var strippedCatName = StripPunctuation(CategoryConstants.NormalizeNameForComparison(c.CategoryName));
            if (string.Equals(strippedCatName, strippedTarget, StringComparison.Ordinal) &&
                seenIds.Add(c.Id))
            {
                suggestions.Add(new TenantCategorySuggestionItem(c.Id, c.CategoryName, c.CategoryCode, matchType));
                if (suggestions.Count >= MaxSuggestions) return true;
            }
        }

        return false;
    }

    private static bool TryAddSimilarityMatches(
        IReadOnlyList<TenantAdminProductCategoryOptionResponse> categories,
        string targetDisplayText,
        string normalizedTarget,
        string matchType,
        List<TenantCategorySuggestionItem> suggestions,
        HashSet<Guid> seenIds)
    {
        var targetTokens = Tokenize(targetDisplayText);
        if (targetTokens.Count == 0)
        {
            return false;
        }

        foreach (var c in categories)
        {
            if (seenIds.Contains(c.Id)) continue;

            var normalizedCatName = CategoryConstants.NormalizeNameForComparison(c.CategoryName);
            var catTokens = Tokenize(c.CategoryName);

            var isSubstringMatch = normalizedCatName.Length >= 4 &&
                (normalizedCatName.Contains(normalizedTarget, StringComparison.Ordinal) ||
                 normalizedTarget.Contains(normalizedCatName, StringComparison.Ordinal));

            var hasTokenOverlap = false;
            if (!isSubstringMatch)
            {
                foreach (var targetToken in targetTokens)
                {
                    if (catTokens.Contains(targetToken))
                    {
                        hasTokenOverlap = true;
                        break;
                    }
                }
            }

            if ((isSubstringMatch || hasTokenOverlap) && seenIds.Add(c.Id))
            {
                suggestions.Add(new TenantCategorySuggestionItem(c.Id, c.CategoryName, c.CategoryCode, matchType));
                if (suggestions.Count >= MaxSuggestions) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts a raw provider hierarchy (e.g. OpenFoodFacts <c>categories_hierarchy</c>:
    /// ["en:beverages-and-beverages-preparations", "en:beverages", "en:carbonated-drinks",
    /// "en:sodas", "en:colas"], ordered broadest-to-deepest) into comparable display-like node
    /// names, deepest/most-specific node first (per §7 — "Sodas" before "Carbonated Drinks" before
    /// "Beverages"), with the leaf itself excluded (already covered by the LEAF_* tiers).
    /// </summary>
    private static List<string> DeriveHierarchyNodeNamesDeepestFirst(
        IReadOnlyList<string>? externalHierarchy,
        string strippedExternalLeafName)
    {
        var nodes = new List<string>();
        if (externalHierarchy is null || externalHierarchy.Count == 0)
        {
            return nodes;
        }

        for (var i = externalHierarchy.Count - 1; i >= 0; i--)
        {
            var displayName = DeriveHierarchyNodeDisplayName(externalHierarchy[i]);
            if (string.IsNullOrEmpty(displayName))
            {
                continue;
            }

            // Skip a node that is textually identical to the already-evaluated leaf name — it isn't
            // a distinct "parent hierarchy" match, just the leaf tag repeated in the hierarchy array.
            if (string.Equals(StripPunctuation(CategoryConstants.NormalizeNameForComparison(displayName)),
                    strippedExternalLeafName, StringComparison.Ordinal))
            {
                continue;
            }

            nodes.Add(displayName);
        }

        return nodes;
    }

    /// <summary>
    /// Derives a comparable display-like name from a raw provider hierarchy tag, e.g.
    /// "en:carbonated-drinks" -> "carbonated drinks", "pt:bebidas cafeína" -> "bebidas cafeína".
    /// Strips a leading 2-3 letter language/locale prefix (OpenFoodFacts' "xx:" tag convention) and
    /// converts separator punctuation to spaces; actual case/diacritic normalization happens later
    /// via the same <see cref="CategoryConstants.NormalizeNameForComparison"/> / StripPunctuation
    /// pipeline already used for leaf names, so hierarchy nodes are never compared with a different
    /// normalization stack (see §19).
    /// </summary>
    private static string? DeriveHierarchyNodeDisplayName(string? hierarchyNode)
    {
        if (string.IsNullOrWhiteSpace(hierarchyNode))
        {
            return null;
        }

        var value = hierarchyNode.Trim();
        var colonIndex = value.IndexOf(':');
        if (colonIndex is > 0 and <= 3 && value.Take(colonIndex).All(char.IsLetter))
        {
            value = value[(colonIndex + 1)..];
        }

        value = value.Replace('-', ' ').Replace('_', ' ').Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static HashSet<string> Tokenize(string text)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text)) return tokens;

        var parts = text.Split(new[] { ' ', '-', '_', ',', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var clean = RemoveDiacritics(part.Trim().ToLowerInvariant());
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
        var chars = RemoveDiacritics(text).Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray();
        return string.Join(" ", new string(chars).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    /// <summary>
    /// Folds accented/diacritic characters to their base ASCII-ish form (e.g. "Café" -> "Cafe") so
    /// provider free-text category names compare equal to tenant category names entered without
    /// accents. Deliberately applied only at the NORMALIZED/SIMILARITY tiers (not EXACT) — see
    /// <see cref="TenantExternalBrandResolver"/> for the identical rationale on the Brand side.
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
