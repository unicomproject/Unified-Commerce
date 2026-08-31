using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace E_POS.IntegrationTests.CatalogProduct;

public sealed class TenantAdminProductCreateOptionsRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetCreateOptions_ReturnsActiveHierarchyAwareCategoriesOnly()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var level5Id = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        var deletedId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();

        var chain = new List<Guid> { rootId, childId, Guid.NewGuid(), Guid.NewGuid(), level5Id };
        dbContext.Categories.Add(Category.Create(chain[0], tenantId, null, "L1", "Level 1", "l1", null, 1, CategoryConstants.ActiveStatus, null, Now));
        dbContext.Categories.Add(Category.Create(chain[1], tenantId, chain[0], "L2", "Level 2", "l2", null, 2, CategoryConstants.ActiveStatus, null, Now));
        dbContext.Categories.Add(Category.Create(chain[2], tenantId, chain[1], "L3", "Level 3", "l3", null, 3, CategoryConstants.ActiveStatus, null, Now));
        dbContext.Categories.Add(Category.Create(chain[3], tenantId, chain[2], "L4", "Level 4", "l4", null, 4, CategoryConstants.ActiveStatus, null, Now));
        dbContext.Categories.Add(Category.Create(chain[4], tenantId, chain[3], "L5", "Level 5", "l5", null, 5, CategoryConstants.ActiveStatus, null, Now));
        dbContext.Categories.Add(Category.Create(inactiveId, tenantId, null, "INACT", "Inactive", "inactive", null, 9, CategoryConstants.InactiveStatus, null, Now));
        dbContext.Categories.Add(Category.Create(deletedId, tenantId, null, "DEL", "Deleted", "deleted", null, 10, CategoryConstants.DeletedStatus, null, Now));
        dbContext.Categories.Add(Category.Create(Guid.NewGuid(), otherTenantId, null, "OTHER", "Other", "other", null, 1, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(dbContext, new NoOpCodeSequenceRepository());
        var options = await repository.GetCreateOptionsAsync(tenantId, CancellationToken.None);

        Assert.Equal(5, options.Categories.Count);
        Assert.DoesNotContain(options.Categories, x => x.Id == inactiveId || x.Id == deletedId);
        Assert.DoesNotContain(options.Categories, x => x.CategoryCode == "OTHER");

        var root = Assert.Single(options.Categories, x => x.Id == rootId);
        Assert.Null(root.ParentCategoryId);
        Assert.Equal(1, root.Level);
        Assert.Equal("Level 1", root.HierarchyPath);
        Assert.True(root.HasChildren);

        var level5 = Assert.Single(options.Categories, x => x.Id == level5Id);
        Assert.Equal(chain[3], level5.ParentCategoryId);
        Assert.Equal(5, level5.Level);
        Assert.Equal("Level 1 > Level 2 > Level 3 > Level 4 > Level 5", level5.HierarchyPath);
        Assert.False(level5.HasChildren);

        Assert.Empty(options.Brands);
        Assert.NotNull(options.Units);
        Assert.NotNull(options.Taxes);
        Assert.NotNull(options.Outlets);
        Assert.NotNull(options.VariantOptionTemplates);
        Assert.NotNull(options.SalesChannels);
    }

    [Fact]
    public async Task GetCreateOptions_ExcludesActiveChildWithInactiveAncestor()
    {
        var tenantId = Guid.NewGuid();
        var inactiveParentId = Guid.NewGuid();
        var activeChildId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            Category.Create(inactiveParentId, tenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.InactiveStatus, null, Now),
            Category.Create(activeChildId, tenantId, inactiveParentId, "MILK", "Milk", "milk", null, 2, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(dbContext, new NoOpCodeSequenceRepository());
        var options = await repository.GetCreateOptionsAsync(tenantId, CancellationToken.None);

        Assert.Empty(options.Categories);
    }

    [Fact]
    public async Task GetCreateOptions_ReturnsActiveRootAndActiveChildChain()
    {
        var tenantId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            Category.Create(rootId, tenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(childId, tenantId, rootId, "MILK", "Milk", "milk", null, 2, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(dbContext, new NoOpCodeSequenceRepository());
        var options = await repository.GetCreateOptionsAsync(tenantId, CancellationToken.None);

        Assert.Equal(2, options.Categories.Count);
        Assert.Contains(options.Categories, x => x.Id == rootId);
        Assert.Contains(options.Categories, x => x.Id == childId);
    }

    [Fact]
    public async Task GetCreateOptions_ExcludesActiveDescendantWhenMiddleAncestorInactive()
    {
        var tenantId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            Category.Create(a, tenantId, null, "A", "A", "a", null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(b, tenantId, a, "B", "B", "b", null, 2, CategoryConstants.InactiveStatus, null, Now),
            Category.Create(c, tenantId, b, "C", "C", "c", null, 3, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(dbContext, new NoOpCodeSequenceRepository());
        var options = await repository.GetCreateOptionsAsync(tenantId, CancellationToken.None);

        Assert.Single(options.Categories);
        Assert.Equal(a, options.Categories[0].Id);
    }

    [Fact]
    public async Task IsCategoryEffectivelySelectableAsync_RejectsInactiveAncestorPath()
    {
        var tenantId = Guid.NewGuid();
        var inactiveParentId = Guid.NewGuid();
        var activeChildId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Categories.AddRange(
            Category.Create(inactiveParentId, tenantId, null, "FOOD", "Food", "food", null, 1, CategoryConstants.InactiveStatus, null, Now),
            Category.Create(activeChildId, tenantId, inactiveParentId, "MILK", "Milk", "milk", null, 2, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();

        var repository = new TenantAdminProductRepository(dbContext, new NoOpCodeSequenceRepository());

        Assert.False(await repository.IsCategoryEffectivelySelectableAsync(tenantId, activeChildId, CancellationToken.None));
        Assert.False(await repository.IsCategoryEffectivelySelectableAsync(tenantId, inactiveParentId, CancellationToken.None));
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EPosDbContext(options);
    }

    private sealed class NoOpCodeSequenceRepository : ICodeSequenceRepository
    {
        public Task<string> GetNextCodeAsync(
            Guid tenantId,
            string sequenceKey,
            string prefix,
            int paddingLength,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult($"{prefix}1");
    }
}
