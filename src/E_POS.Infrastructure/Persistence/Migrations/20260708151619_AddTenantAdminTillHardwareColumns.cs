using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAdminTillHardwareColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tills_status",
                table: "tills");

            migrationBuilder.AddColumn<string>(
                name: "card_reader_name",
                table: "tills",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cash_drawer_name",
                table: "tills",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "device_name",
                table: "tills",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "internal_note",
                table: "tills",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "printer_name",
                table: "tills",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scanner_name",
                table: "tills",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tills_status",
                table: "tills",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'MAINTENANCE', 'DELETED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tills_status",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "card_reader_name",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "cash_drawer_name",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "device_name",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "internal_note",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "printer_name",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "scanner_name",
                table: "tills");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tills_status",
                table: "tills",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        }
    }
}
