using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxTypeToTaxClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tax_type",
                table: "tax_classes",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "OTHER");
                
            migrationBuilder.Sql("UPDATE tax_classes SET tax_type = 'OTHER' WHERE tax_type IS NULL OR tax_type = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tax_type",
                table: "tax_classes");
        }
    }
}
