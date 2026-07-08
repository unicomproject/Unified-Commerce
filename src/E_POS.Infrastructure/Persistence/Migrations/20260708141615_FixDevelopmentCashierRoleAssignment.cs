using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixDevelopmentCashierRoleAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentCashierRoleSeedData.UpSql);
            migrationBuilder.Sql(DevelopmentPosCashierPermissionAssignmentSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentPosCashierPermissionAssignmentSeedData.DownSql);
            migrationBuilder.Sql(DevelopmentCashierRoleSeedData.DownSql);
        }
    }
}
