using E_POS.Domain.Modules.Tenant.CatalogProduct;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class CategorySelectionRulesTests
{
    [Fact]
    public void IsEffectivelySelectable_ActiveRoot_ReturnsTrue()
    {
        var rootId = Guid.NewGuid();
        var statusById = new Dictionary<Guid, string> { [rootId] = CategoryConstants.ActiveStatus };
        var parentById = new Dictionary<Guid, Guid?> { [rootId] = null };

        Assert.True(CategorySelectionRules.IsEffectivelySelectable(rootId, statusById, parentById));
    }

    [Fact]
    public void IsEffectivelySelectable_ActiveChain_ReturnsTrueForEach()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var statusById = new Dictionary<Guid, string>
        {
            [a] = CategoryConstants.ActiveStatus,
            [b] = CategoryConstants.ActiveStatus,
            [c] = CategoryConstants.ActiveStatus
        };
        var parentById = new Dictionary<Guid, Guid?>
        {
            [a] = null,
            [b] = a,
            [c] = b
        };

        Assert.True(CategorySelectionRules.IsEffectivelySelectable(a, statusById, parentById));
        Assert.True(CategorySelectionRules.IsEffectivelySelectable(b, statusById, parentById));
        Assert.True(CategorySelectionRules.IsEffectivelySelectable(c, statusById, parentById));
    }

    [Fact]
    public void IsEffectivelySelectable_InactiveParentActiveChild_ReturnsFalseForChild()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var statusById = new Dictionary<Guid, string>
        {
            [parentId] = CategoryConstants.InactiveStatus,
            [childId] = CategoryConstants.ActiveStatus
        };
        var parentById = new Dictionary<Guid, Guid?>
        {
            [parentId] = null,
            [childId] = parentId
        };

        Assert.False(CategorySelectionRules.IsEffectivelySelectable(childId, statusById, parentById));
        Assert.False(CategorySelectionRules.IsEffectivelySelectable(parentId, statusById, parentById));
    }

    [Fact]
    public void IsEffectivelySelectable_InactiveMiddleAncestor_ExcludesDescendant()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var statusById = new Dictionary<Guid, string>
        {
            [a] = CategoryConstants.ActiveStatus,
            [b] = CategoryConstants.InactiveStatus,
            [c] = CategoryConstants.ActiveStatus
        };
        var parentById = new Dictionary<Guid, Guid?>
        {
            [a] = null,
            [b] = a,
            [c] = b
        };

        Assert.True(CategorySelectionRules.IsEffectivelySelectable(a, statusById, parentById));
        Assert.False(CategorySelectionRules.IsEffectivelySelectable(b, statusById, parentById));
        Assert.False(CategorySelectionRules.IsEffectivelySelectable(c, statusById, parentById));
    }

    [Fact]
    public void IsEffectivelySelectable_DeletedAncestor_ExcludesDescendant()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var statusById = new Dictionary<Guid, string>
        {
            [parentId] = CategoryConstants.DeletedStatus,
            [childId] = CategoryConstants.ActiveStatus
        };
        var parentById = new Dictionary<Guid, Guid?>
        {
            [parentId] = null,
            [childId] = parentId
        };

        Assert.False(CategorySelectionRules.IsEffectivelySelectable(childId, statusById, parentById));
    }
}
