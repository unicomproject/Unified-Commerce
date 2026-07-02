using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260703090000_AddOutletTillSearchTrigramIndexes")]
    public partial class AddOutletTillSearchTrigramIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_outlets_name_trgm
                ON outlets USING gin (name gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_outlets_outlet_code_trgm
                ON outlets USING gin (outlet_code gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_tills_name_trgm
                ON tills USING gin (name gin_trgm_ops);
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_tills_till_code_trgm
                ON tills USING gin (till_code gin_trgm_ops);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_tills_till_code_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_tills_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_outlets_outlet_code_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_outlets_name_trgm;");
        }
    }
}