using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncMediaAssetsModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Media asset tables, columns, indexes, and foreign keys were already
            // applied by AddMediaAssetsPhase1. This migration records that model in
            // the EF snapshot and only reconciles indexes omitted by older raw SQL migrations.
            migrationBuilder.CreateIndex(
                name: "IX_return_inspection_drafts_tenant_id_resolution_selected_by_t~",
                table: "return_inspection_drafts",
                columns: new[] { "tenant_id", "resolution_selected_by_tenant_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_return_inspection_drafts_tenant_id_sale_id",
                table: "return_inspection_drafts",
                columns: new[] { "tenant_id", "sale_id" });

            migrationBuilder.CreateIndex(
                name: "IX_return_exchange_replacement_draft_lines_tenant_id_replacem~1",
                table: "return_exchange_replacement_draft_lines",
                columns: new[] { "tenant_id", "replacement_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_return_exchange_replacement_draft_lines_tenant_id_replaceme~",
                table: "return_exchange_replacement_draft_lines",
                columns: new[] { "tenant_id", "replacement_product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_return_exchange_replacement_draft_lines_tenant_id_returned_~",
                table: "return_exchange_replacement_draft_lines",
                columns: new[] { "tenant_id", "returned_sale_line_id" });

            migrationBuilder.CreateIndex(
                name: "IX_return_exchange_replacement_draft_lines_tenant_id_selected_~",
                table: "return_exchange_replacement_draft_lines",
                columns: new[] { "tenant_id", "selected_by_tenant_user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_return_inspection_drafts_tenant_id_resolution_selected_by_t~",
                table: "return_inspection_drafts");

            migrationBuilder.DropIndex(
                name: "IX_return_inspection_drafts_tenant_id_sale_id",
                table: "return_inspection_drafts");

            migrationBuilder.DropIndex(
                name: "IX_return_exchange_replacement_draft_lines_tenant_id_replacem~1",
                table: "return_exchange_replacement_draft_lines");

            migrationBuilder.DropIndex(
                name: "IX_return_exchange_replacement_draft_lines_tenant_id_replaceme~",
                table: "return_exchange_replacement_draft_lines");

            migrationBuilder.DropIndex(
                name: "IX_return_exchange_replacement_draft_lines_tenant_id_returned_~",
                table: "return_exchange_replacement_draft_lines");

            migrationBuilder.DropIndex(
                name: "IX_return_exchange_replacement_draft_lines_tenant_id_selected_~",
                table: "return_exchange_replacement_draft_lines");
        }
    }
}