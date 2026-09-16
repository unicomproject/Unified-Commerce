using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260904150000_AlignPermissionDefinitionModulesWithFeatures")]
public partial class AlignPermissionDefinitionModulesWithFeatures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE permission_definitions permission
            SET module_id = feature.platform_module_id,
                updated_at = now()
            FROM platform_features feature
            JOIN platform_modules module
              ON module.id = feature.platform_module_id
            WHERE permission.feature_id = feature.id
              AND permission.module_id <> feature.platform_module_id
              AND permission.is_active
              AND feature.status = 'ACTIVE'
              AND module.status = 'ACTIVE';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The previous module links were inconsistent legacy catalog data.
    }
}
