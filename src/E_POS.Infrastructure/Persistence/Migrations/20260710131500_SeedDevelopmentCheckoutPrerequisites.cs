using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SeedDevelopmentCheckoutPrerequisites : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentCheckoutPrerequisitesSeedData.UpSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentCheckoutPrerequisitesSeedData.DownSql);
    }
}
