using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct;

public static class CategorySelectionRules
{
    public static bool IsEffectivelySelectable(
        Guid categoryId,
        IReadOnlyDictionary<Guid, string> statusById,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        if (!statusById.TryGetValue(categoryId, out var status) ||
            !string.Equals(status, CategoryConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var level = CategoryHierarchy.ComputeLevel(categoryId, parentById);
        if (level > CategoryConstants.MaxHierarchyDepth)
        {
            return false;
        }

        var current = categoryId;
        var visited = new HashSet<Guid> { current };

        while (parentById.TryGetValue(current, out var parentId) && parentId.HasValue)
        {
            if (!visited.Add(parentId.Value))
            {
                return false;
            }

            if (!statusById.TryGetValue(parentId.Value, out var parentStatus))
            {
                return false;
            }

            if (string.Equals(parentStatus, CategoryConstants.DeletedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(parentStatus, CategoryConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            current = parentId.Value;
        }

        return true;
    }
}
