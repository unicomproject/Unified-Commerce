using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260716220000_AddReturnResolutionToInspectionDraft")]
public partial class AddReturnResolutionToInspectionDraft : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE return_inspection_drafts
                ADD COLUMN IF NOT EXISTS resolution_type varchar(20) NULL,
                ADD COLUMN IF NOT EXISTS resolution_selected_at timestamptz NULL,
                ADD COLUMN IF NOT EXISTS resolution_selected_by_tenant_user_id uuid NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE return_inspection_drafts
                DROP COLUMN IF EXISTS resolution_selected_by_tenant_user_id,
                DROP COLUMN IF EXISTS resolution_selected_at,
                DROP COLUMN IF EXISTS resolution_type;
            """);
    }
}
