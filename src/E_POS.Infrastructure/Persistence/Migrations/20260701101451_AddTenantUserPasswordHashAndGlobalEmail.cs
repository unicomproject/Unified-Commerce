using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantUserPasswordHashAndGlobalEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_tenant_users_tenant_id_normalized_email",
                table: "tenant_users");

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "tenant_users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_tenant_users_normalized_email",
                table: "tenant_users",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_tenant_users_normalized_email",
                table: "tenant_users");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "tenant_users");

            migrationBuilder.CreateIndex(
                name: "uq_tenant_users_tenant_id_normalized_email",
                table: "tenant_users",
                columns: new[] { "tenant_id", "normalized_email" },
                unique: true);
        }
    }
}
