using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260716240000_AddReturnExchangeReplacementDraftLines")]
public partial class AddReturnExchangeReplacementDraftLines : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // This migration originally lacked EF discovery metadata. Keep reconciliation
        // idempotent so a database that already received the table manually can safely
        // acquire the matching __EFMigrationsHistory row.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS return_exchange_replacement_draft_lines (
                id uuid NOT NULL,
                created_at timestamptz NOT NULL,
                tenant_id uuid NOT NULL,
                return_inspection_draft_id uuid NOT NULL,
                returned_sale_line_id uuid NOT NULL,
                replacement_product_id uuid NOT NULL,
                replacement_variant_id uuid NOT NULL,
                quantity numeric(18,4) NOT NULL,
                selected_by_tenant_user_id uuid NOT NULL,
                selected_at timestamptz NOT NULL,
                CONSTRAINT pk_return_exchange_replacement_draft_lines PRIMARY KEY (id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_return_exchange_replacement_draft_line
                ON return_exchange_replacement_draft_lines
                (tenant_id, return_inspection_draft_id, returned_sale_line_id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "return_exchange_replacement_draft_lines");
    }
}
