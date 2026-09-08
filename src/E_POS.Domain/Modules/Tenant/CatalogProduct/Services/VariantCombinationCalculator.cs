using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Services;

public static class VariantCombinationCalculator
{
    public enum CombinationCountFailure
    {
        IncompleteConfiguration,
        ExceedsMaximum,
    }

    /// <summary>
    /// Calculates the Cartesian combination count from per-attribute selected value counts.
    /// Fails fast when the running product exceeds <see cref="ProductConstants.MaxVariantCombinationsPerProduct"/>.
    /// </summary>
    public static bool TryCalculateCombinationCount(
        IReadOnlyList<int> selectedValueCountsPerAttribute,
        out int combinationCount,
        out CombinationCountFailure? failure)
    {
        combinationCount = 0;
        failure = null;

        if (selectedValueCountsPerAttribute.Count == 0)
        {
            failure = CombinationCountFailure.IncompleteConfiguration;
            return false;
        }

        long running = 1;
        foreach (var valueCount in selectedValueCountsPerAttribute)
        {
            if (valueCount <= 0)
            {
                failure = CombinationCountFailure.IncompleteConfiguration;
                return false;
            }

            running *= valueCount;
            if (running > ProductConstants.MaxVariantCombinationsPerProduct)
            {
                failure = CombinationCountFailure.ExceedsMaximum;
                combinationCount = (int)Math.Min(running, int.MaxValue);
                return false;
            }
        }

        combinationCount = (int)running;
        return true;
    }

    /// <summary>
    /// Generates deterministic Cartesian index tuples for the supplied per-attribute value counts.
    /// Attribute order is preserved; value order within each attribute follows index 0..N-1.
    /// </summary>
    public static IReadOnlyList<int[]> GenerateCartesianIndexCombinations(
        IReadOnlyList<int> selectedValueCountsPerAttribute)
    {
        if (selectedValueCountsPerAttribute.Count == 0)
        {
            return Array.Empty<int[]>();
        }

        if (selectedValueCountsPerAttribute.Any(count => count <= 0))
        {
            return Array.Empty<int[]>();
        }

        if (!TryCalculateCombinationCount(
                selectedValueCountsPerAttribute,
                out _,
                out _))
        {
            return Array.Empty<int[]>();
        }

        IReadOnlyList<int[]> combinations = new List<int[]> { Array.Empty<int>() };

        foreach (var valueCount in selectedValueCountsPerAttribute)
        {
            var next = new List<int[]>();
            foreach (var existing in combinations)
            {
                for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    var expanded = new int[existing.Length + 1];
                    Array.Copy(existing, expanded, existing.Length);
                    expanded[^1] = valueIndex;
                    next.Add(expanded);
                }
            }

            combinations = next;
        }

        return combinations;
    }
}
