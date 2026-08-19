using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceProductOptionTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(E_POS.Infrastructure.Persistence.Seed.ReferenceProductOptionTemplateSeedData.UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(E_POS.Infrastructure.Persistence.Seed.ReferenceProductOptionTemplateSeedData.DownSql);
        }
    }
}
