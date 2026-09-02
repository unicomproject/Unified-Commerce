using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations.OneVerze
{
    /// <inheritdoc />
    public partial class SeedOneVerzeCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Seed.OneVerze.OneVerzeCategorySeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Seed.OneVerze.OneVerzeCategorySeedData.DownSql);
        }
    }
}
