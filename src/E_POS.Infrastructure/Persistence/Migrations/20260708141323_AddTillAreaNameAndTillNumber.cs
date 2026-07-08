using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTillAreaNameAndTillNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "till_area_name",
                table: "tills",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "till_number",
                table: "tills",
                type: "integer",
                nullable: true);

            // Backfill any pre-existing tills so the NOT NULL + positive-number
            // constraints below can be applied safely on populated databases.
            migrationBuilder.Sql("""
                UPDATE tills
                SET till_area_name = 'Front',
                    till_number = 1
                WHERE till_area_name IS NULL
                  AND (lower(trim(till_name)) IN ('development front till', 'front till')
                       OR lower(trim(till_name)) LIKE '%development front till%');
                """);

            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT
                        id,
                        ROW_NUMBER() OVER (
                            PARTITION BY tenant_id, outlet_id
                            ORDER BY created_at, till_code, till_name
                        ) AS seq
                    FROM tills
                    WHERE till_area_name IS NULL
                )
                UPDATE tills t
                SET till_area_name = 'Main',
                    till_number = n.seq
                FROM numbered n
                WHERE t.id = n.id;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE tills
                ALTER COLUMN till_area_name SET NOT NULL;

                ALTER TABLE tills
                ALTER COLUMN till_number SET NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "uq_tills_tenant_id_outlet_id_till_area_name_till_number",
                table: "tills",
                columns: new[] { "tenant_id", "outlet_id", "till_area_name", "till_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tills_till_number_positive",
                table: "tills",
                sql: "till_number > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_tills_tenant_id_outlet_id_till_area_name_till_number",
                table: "tills");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tills_till_number_positive",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "till_area_name",
                table: "tills");

            migrationBuilder.DropColumn(
                name: "till_number",
                table: "tills");
        }
    }
}
