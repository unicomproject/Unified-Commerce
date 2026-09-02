using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations.OneVerze
{
    /// <inheritdoc />
    public partial class SeedOneVerzeOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(E_POS.Infrastructure.Persistence.Seed.OneVerze.OneVerzeClickCollectOrderStatusSeedData.DownSql);
            migrationBuilder.Sql(E_POS.Infrastructure.Persistence.Seed.OneVerze.OneVerzeClickCollectOrderStatusSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(E_POS.Infrastructure.Persistence.Seed.OneVerze.OneVerzeClickCollectOrderStatusSeedData.DownSql);
        }
    }
}
