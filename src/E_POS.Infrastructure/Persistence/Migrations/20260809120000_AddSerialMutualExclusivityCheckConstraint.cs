using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    public partial class AddSerialMutualExclusivityCheckConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_product_inventory_settings_serial_no_batch_or_expiry",
                table: "product_inventory_settings",
                sql: "requires_serial_tracking = false OR (requires_batch_tracking = false AND requires_expiry_tracking = false)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_product_inventory_settings_serial_no_batch_or_expiry",
                table: "product_inventory_settings");
        }
    }
}
