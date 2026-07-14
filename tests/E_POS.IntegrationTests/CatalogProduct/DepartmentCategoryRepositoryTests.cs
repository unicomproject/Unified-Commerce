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
            Category.Create(parentId, tenantId, Guid.Empty, null, "FOOD", "Food", "food", null, null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(childId, tenantId, Guid.Empty, parentId, "MILK", "Milk", "milk", null, null, 2, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(Guid.NewGuid(), otherTenantId, Guid.Empty, null, "OTHER", "Other", "other", null, null, 1, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CategoryRepository(dbContext);

        var result = await repository.ListAsync(tenantId, 1, 50, null, CancellationToken.None);

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
            Category.Create(parentId, tenantId, Guid.Empty, null, "FOOD", "Food", "food", null, null, 1, CategoryConstants.ActiveStatus, null, Now),
            Category.Create(childId, tenantId, Guid.Empty, parentId, "MILK", "Milk", "milk", null, null, 2, CategoryConstants.ActiveStatus, null, Now));
        await dbContext.SaveChangesAsync();
        var repository = new CategoryRepository(dbContext);

        var result = await repository.WouldCreateParentCycleAsync(tenantId, parentId, childId, CancellationToken.None);

        Assert.True(result);
    }

    private static EPosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EPosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EPosDbContext(options);
    }
}


