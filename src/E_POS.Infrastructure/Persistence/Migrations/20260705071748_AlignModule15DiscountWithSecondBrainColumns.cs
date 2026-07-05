using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule15DiscountWithSecondBrainColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "require_manager_approval",
                table: "expiry_discount_rules",
                newName: "requires_manager_approval");

            migrationBuilder.CreateIndex(
                name: "uq_expiry_discount_applications_tenant_id_id",
                table: "expiry_discount_applications",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_targets_tenant_id_id",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_outlets_tenant_id_id",
                table: "discount_policy_outlets",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_conditions_tenant_id_id",
                table: "discount_policy_conditions",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_discount_policy_channels_tenant_id_id",
                table: "discount_policy_channels",
                columns: new[] { "tenant_id", "id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_expiry_discount_applications_tenant_id_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "uq_discount_policy_targets_tenant_id_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "uq_discount_policy_outlets_tenant_id_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropIndex(
                name: "uq_discount_policy_conditions_tenant_id_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropIndex(
                name: "uq_discount_policy_channels_tenant_id_id",
                table: "discount_policy_channels");

            migrationBuilder.RenameColumn(
                name: "requires_manager_approval",
                table: "expiry_discount_rules",
                newName: "require_manager_approval");
        }
    }
}
