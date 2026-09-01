using E_POS.Domain.Modules.Tenant.CatalogProduct;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class CategoryHierarchyTests
{
    [Fact]
    public void ComputeLevel_Root_IsOne()
    {
        var id = Guid.NewGuid();
        var parents = new Dictionary<Guid, Guid?> { [id] = null };

        Assert.Equal(1, CategoryHierarchy.ComputeLevel(id, parents));
    }

    [Fact]
    public void ComputeLevel_FiveLevels_IsFive()
    {
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var parents = new Dictionary<Guid, Guid?>
        {
            [ids[0]] = null,
            [ids[1]] = ids[0],
            [ids[2]] = ids[1],
            [ids[3]] = ids[2],
            [ids[4]] = ids[3]
        };

        Assert.Equal(5, CategoryHierarchy.ComputeLevel(ids[4], parents));
        Assert.Equal("A > B > C > D > E", CategoryHierarchy.ComputePath(
            ids[4],
            new Dictionary<Guid, string>
            {
                [ids[0]] = "A",
                [ids[1]] = "B",
                [ids[2]] = "C",
                [ids[3]] = "D",
                [ids[4]] = "E"
            },
            parents));
    }

    [Fact]
    public void WouldExceedMaxDepth_ParentLevel3_SubtreeDepth3_IsTrue()
    {
        Assert.True(CategoryHierarchy.WouldExceedMaxDepth(3, 3));
        Assert.False(CategoryHierarchy.WouldExceedMaxDepth(3, 2));
        Assert.False(CategoryHierarchy.WouldExceedMaxDepth(4, 1));
        Assert.Equal(5, CategoryConstants.MaxHierarchyDepth);
    }

    [Fact]
    public void NormalizeCode_TrimsAndUppercases()
    {
        Assert.Equal("ABC", CategoryConstants.NormalizeCode("abc"));
        Assert.Equal("ABC", CategoryConstants.NormalizeCode("ABC"));
        Assert.Equal("ABC", CategoryConstants.NormalizeCode(" ABC "));
    }

    [Fact]
    public void NormalizeNameForComparison_IsCaseInsensitiveAndTrimmed()
    {
        Assert.Equal("beverages", CategoryConstants.NormalizeNameForComparison("Beverages"));
        Assert.Equal("beverages", CategoryConstants.NormalizeNameForComparison("beverages"));
        Assert.Equal("beverages", CategoryConstants.NormalizeNameForComparison(" Beverages "));
        Assert.Equal("beverages", CategoryConstants.NormalizeNameForComparison("BEVERAGES"));
    }
}
