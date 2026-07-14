using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDevelopmentStorefrontData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "categories",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(E_POS.Infrastructure.Persistence.Seed.DevelopmentStorefrontSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(E_POS.Infrastructure.Persistence.Seed.DevelopmentStorefrontSeedData.DownSql);

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "categories");
        }
    }
}
