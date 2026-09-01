using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class DepartmentCategoryRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DepartmentListAsync_ReturnsCurrentTenantNonDeletedDepartmentsOnly()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Departments.AddRange(
            Department.Create(Guid.NewGuid(), tenantId, "GROCERY", "Grocery", null, 0, DepartmentConstants.ActiveStatus, null, Now),
            Department.Create(Guid.NewGuid(), tenantId, "OLD", "Old", null, 0, DepartmentConstants.DeletedStatus, null, Now),
            Department.Create(Guid.NewGuid(), otherTenantId, "OTHER", "Other", null, 0, DepartmentConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new DepartmentRepository(dbContext);

        var result = await repository.ListAsync(tenantId, 1, 50, null, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("GROCERY", Assert.Single(result.Items).DepartmentCode);
    }

    [Fact]
    public async Task CategoryListAsync_ReturnsParentDetailsAndCurrentTenantOnly()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            Category.Create(parentId, tenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(childId, tenantId, parentId, "MILK", "Milk", "milk", null, 2, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(Guid.NewGuid(), otherTenantId, null, "OTHER", "Other", "other", null, 1, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CategoryRepository(dbContext);

        var result = await repository.ListAsync(tenantId, new CategoryListQuery(1, 50), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        var child = Assert.Single(result.Items, x => x.CategoryCode == "MILK");
        Assert.Equal(parentId, child.ParentCategoryId);
        Assert.Equal("FOOD", child.ParentCategoryCode);
        Assert.Equal("Food", child.ParentCategoryName);
        Assert.DoesNotContain(result.Items, x => x.CategoryCode == "OTHER");
    }

    [Fact]
    public async Task WouldCreateParentCycleAsync_WhenNewParentIsDescendant_ReturnsTrue()
    {
        var tenantId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            Category.Create(parentId, tenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(childId, tenantId, parentId, "MILK", "Milk", "milk", null, 2, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CategoryRepository(dbContext);

        var result = await repository.WouldCreateParentCycleAsync(tenantId, parentId, childId, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CategoryListAsync_AppliesSearchStatusParentAndRootFilters()
    {
        var tenantId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            Category.Create(rootId, tenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(childId, tenantId, rootId, "MILK", "Milk", "milk", null, 2, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(inactiveId, tenantId, null, "ARCHIVE", "Archive", "archive", null, 3, CategoryConstants.InactiveStatus, null, Now),
            Category.Create(deletedId, tenantId, null, "GONE", "Gone", "gone", null, 4, CategoryConstants.DeletedStatus, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CategoryRepository(dbContext);

        var children = await repository.ListAsync(tenantId, new CategoryListQuery(1, 50, ParentCategoryId: rootId), CancellationToken.None);
        Assert.Equal("MILK", Assert.Single(children.Items).CategoryCode);

        var roots = await repository.ListAsync(tenantId, new CategoryListQuery(1, 50, RootOnly: true), CancellationToken.None);
        Assert.DoesNotContain(roots.Items, x => x.Id == childId || x.Id == deletedId);
        Assert.Contains(roots.Items, x => x.Id == rootId);

        var search = await repository.ListAsync(tenantId, new CategoryListQuery(1, 50, Search: "mil"), CancellationToken.None);
        Assert.Equal("MILK", Assert.Single(search.Items).CategoryCode);

        var inactive = await repository.ListAsync(tenantId, new CategoryListQuery(1, 50, Status: CategoryConstants.InactiveStatus), CancellationToken.None);
        Assert.Equal(inactiveId, Assert.Single(inactive.Items).Id);
        Assert.DoesNotContain(inactive.Items, x => x.Id == deletedId);
    }

    [Fact]
    public async Task CategoryTree_ExcludesDeleted_IncludesInactive_AndProjectsHierarchy()
    {
        var tenantId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            Category.Create(rootId, tenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(childId, tenantId, rootId, "MILK", "Milk", "milk", null, 2, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(grandchildId, tenantId, childId, "SKIM", "Skim", "skim", null, 3, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(inactiveId, tenantId, rootId, "FROZEN", "Frozen", "frozen", null, 4, CategoryConstants.InactiveStatus, null, Now),
            Category.Create(deletedId, tenantId, rootId, "OLD", "Old", "old", null, 5, CategoryConstants.DeletedStatus, null, Now));
        dbContext.Products.Add(Product.Create(productId, tenantId, "P-1", "Product 1", "p-1", "STANDARD", "SIMPLE", null, null, null, null, null, true, true, ProductConstants.ActiveStatus, null, Now));
        dbContext.ProductCategories.Add(ProductCategory.Create(Guid.NewGuid(), tenantId, productId, childId, true, 0, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CategoryRepository(dbContext);

        var tree = await repository.GetTreeAsync(tenantId, CancellationToken.None);
        var root = Assert.Single(tree.Items);
        Assert.Equal(1, root.Level);
        Assert.Equal("Food", root.HierarchyPath);
        Assert.Equal(2, root.ChildCount);
        Assert.True(root.HasChildren);
        Assert.DoesNotContain(Flatten(root), x => x.Id == deletedId);
        Assert.Contains(Flatten(root), x => x.Id == inactiveId);

        var milk = Assert.Single(root.Children, x => x.Id == childId);
        Assert.Equal(2, milk.Level);
        Assert.Equal("Food > Milk", milk.HierarchyPath);
        Assert.Equal(1, milk.ProductCount);
        Assert.Equal(1, milk.ChildCount);
        Assert.Equal("SKIM", Assert.Single(milk.Children).CategoryCode);
    }

    [Fact]
    public async Task CategoryTree_InactiveParentWithActiveChild_PreservesRealParentAndDoesNotPromoteChild()
    {
        var tenantId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            Category.Create(parentId, tenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.InactiveStatus, null, Now),
            Category.Create(childId, tenantId, parentId, "MILK", "Milk", "milk", null, 2, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CategoryRepository(dbContext);

        var tree = await repository.GetTreeAsync(tenantId, CancellationToken.None);
        var root = Assert.Single(tree.Items);
        Assert.Equal(parentId, root.Id);
        Assert.Equal(CategoryConstants.InactiveStatus, root.Status);
        Assert.Null(root.ParentCategoryId);
        var child = Assert.Single(root.Children);
        Assert.Equal(childId, child.Id);
        Assert.Equal(parentId, child.ParentCategoryId);
        Assert.Equal(2, child.Level);
        Assert.Equal("Food > Milk", child.HierarchyPath);
        Assert.DoesNotContain(tree.Items, x => x.Id == childId);
    }

    private static IEnumerable<CategoryTreeNodeResponse> Flatten(CategoryTreeNodeResponse node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}


