using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations.OneVerze
{
    /// <inheritdoc />
    public partial class SeedOneVerzeOutlets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Seed.OneVerze.OneVerzeOutletSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Seed.OneVerze.OneVerzeOutletSeedData.DownSql);
        }
    }
}
