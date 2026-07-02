using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletFullProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "outlets",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                table: "outlets",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_online_visible",
                table: "outlets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "outlet_type",
                table: "outlets",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "STORE");

            migrationBuilder.AddColumn<string>(
                name: "address_line_1",
                table: "outlet_addresses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "address_line_2",
                table: "outlet_addresses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "outlet_addresses",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "outlet_addresses",
                type: "char(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "LK");

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "outlet_addresses",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state_or_province",
                table: "outlet_addresses",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_outlets_outlet_type",
                table: "outlets",
                sql: "outlet_type IN ('STORE', 'WAREHOUSE')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_outlets_outlet_type",
                table: "outlets");

            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "outlets");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                table: "outlets");

            migrationBuilder.DropColumn(
                name: "is_online_visible",
                table: "outlets");

            migrationBuilder.DropColumn(
                name: "outlet_type",
                table: "outlets");

            migrationBuilder.DropColumn(
                name: "address_line_1",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "address_line_2",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "city",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "state_or_province",
                table: "outlet_addresses");
        }
    }
}
