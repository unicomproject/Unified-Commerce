using E_POS.Infrastructure.Modules.Tenant.CatalogProduct;
using E_POS.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class CategoryMigrationRollbackTests
{
    [Fact]
    public void Down_IsForwardOnlyAndRequiresDatabaseRestore()
    {
        var migration = new ExposedMigration();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        var exception = Assert.Throws<InvalidOperationException>(() => migration.InvokeDown(builder));

        Assert.Contains("forward-only", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("backup", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("00000000-0000-0000-0000-000000000000", exception.Message);
    }

    [Fact]
    public void GuardSql_IncludesHierarchyConflictTypesAndForbidsSilentRepair()
    {
        var sql = CategoryMigrationPreflight.BuildGuardSql();
        Assert.Contains("CAT-MIG-PREFLIGHT-001", sql);
        Assert.Contains("SELF_PARENT", sql);
        Assert.Contains("DANGLING_PARENT", sql);
        Assert.Contains("CROSS_TENANT_PARENT", sql);
        Assert.Contains("PARENT_CYCLE", sql);
        Assert.Contains("MAX_DEPTH_EXCEEDED", sql);
        Assert.Contains("Silent repair is forbidden", sql);
        Assert.DoesNotContain("UPDATE categories SET parent_category_id = NULL", sql, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ExposedMigration : DecoupleCategoryFromDepartment
    {
        public void InvokeDown(MigrationBuilder builder) => Down(builder);
    }
}
