using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct;

public static class CategoryHierarchy
{
    public static int ComputeLevel(Guid categoryId, IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        var level = CategoryConstants.RootLevel;
        var current = categoryId;
        var visited = new HashSet<Guid> { current };

        while (parentById.TryGetValue(current, out var parentId) && parentId.HasValue)
        {
            if (!visited.Add(parentId.Value))
            {
                break;
            }

            level++;
            current = parentId.Value;
            if (level > CategoryConstants.MaxHierarchyDepth + 8)
            {
                break;
            }
        }

        return level;
    }

    public static string ComputePath(
        Guid categoryId,
        IReadOnlyDictionary<Guid, string> namesById,
        IReadOnlyDictionary<Guid, Guid?> parentById)
    {
        var parts = new List<string>();
        var current = (Guid?)categoryId;
        var visited = new HashSet<Guid>();

        while (current.HasValue && visited.Add(current.Value) && namesById.TryGetValue(current.Value, out var name))
        {
            parts.Add(name);
            current = parentById.GetValueOrDefault(current.Value);
        }

        parts.Reverse();
        return string.Join(" > ", parts);
    }

    public static int ComputeSubtreeRelativeDepth(
        Guid rootId,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> childrenByParent)
    {
        return Walk(rootId, childrenByParent, new HashSet<Guid>());
    }

    public static bool WouldExceedMaxDepth(int newParentLevel, int movedSubtreeRelativeDepth)
    {
        return newParentLevel + movedSubtreeRelativeDepth > CategoryConstants.MaxHierarchyDepth;
    }

    private static int Walk(
        Guid id,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> childrenByParent,
        HashSet<Guid> visited)
    {
        if (!visited.Add(id))
        {
            return 1;
        }

        if (!childrenByParent.TryGetValue(id, out var children) || children.Count == 0)
        {
            return 1;
        }

        var maxChild = 0;
        foreach (var child in children)
        {
            maxChild = Math.Max(maxChild, Walk(child, childrenByParent, visited));
        }

        return 1 + maxChild;
    }
}
