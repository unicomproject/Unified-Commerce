using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260711120000_LinkPosTillClosePermission")]
    public partial class LinkPosTillClosePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentPosPermissionCatalogSeedData.UpSql);
            migrationBuilder.Sql(DevelopmentPosPaymentReceiptPermissionsSeedData.UpSql);
            migrationBuilder.Sql(DevelopmentPosCashierPermissionAssignmentSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE permission_definitions
                SET module_id = NULL,
                    feature_id = NULL,
                    action_type = 'close',
                    description = 'Close Till',
                    updated_at = now()
                WHERE permission_code = 'pos.till.close';
                """);
        }
    }
}
