using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStageAReturnPolicyTemplatesAndProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "terms_snapshot",
                table: "sales_orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "terms_version_id",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_policy_snapshot",
                table: "sales_order_lines",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "return_policy_version_id",
                table: "sales_order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_defective_return",
                table: "return_policy_templates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                table: "return_policy_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "return_policy_templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "exchange_window_days",
                table: "return_policy_templates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_platform_default",
                table: "return_policy_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "lifecycle_status",
                table: "return_policy_templates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "DRAFT");

            migrationBuilder.AddColumn<bool>(
                name: "requires_manager_approval",
                table: "return_policy_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_receipt",
                table: "return_policy_templates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "version_number",
                table: "return_policy_templates",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "concurrency_token",
                table: "return_policies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "lifecycle_status",
                table: "return_policies",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PUBLISHED");

            migrationBuilder.AddColumn<bool>(
                name: "review_required",
                table: "return_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "seeded_at",
                table: "return_policies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_template_id",
                table: "return_policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_template_version",
                table: "return_policies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version_number",
                table: "return_policies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_policy_templates_exchange_window_days",
                table: "return_policy_templates",
                sql: "exchange_window_days IS NULL OR exchange_window_days >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_policy_templates_lifecycle_status",
                table: "return_policy_templates",
                sql: "lifecycle_status IN ('DRAFT', 'PUBLISHED', 'ARCHIVED')");

            migrationBuilder.CreateIndex(
                name: "uq_return_policy_templates_platform_default",
                table: "return_policy_templates",
                column: "is_platform_default",
                unique: true,
                filter: "is_platform_default = true AND status = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "uq_return_policies_tenant_default",
                table: "return_policies",
                columns: new[] { "tenant_id", "is_default_policy" },
                unique: true,
                filter: "is_default_policy = true AND status != 'DELETED'");

            migrationBuilder.CreateIndex(
                name: "ix_return_policies_source_template_id",
                table: "return_policies",
                column: "source_template_id");

            migrationBuilder.AddForeignKey(
                name: "fk_return_policies_source_template",
                table: "return_policies",
                column: "source_template_id",
                principalTable: "return_policy_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_return_policies_source_template",
                table: "return_policies");

            migrationBuilder.DropIndex(
                name: "ix_return_policies_source_template_id",
                table: "return_policies");

            migrationBuilder.DropIndex(
                name: "uq_return_policies_tenant_default",
                table: "return_policies");

            migrationBuilder.DropIndex(
                name: "uq_return_policy_templates_platform_default",
                table: "return_policy_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_policy_templates_exchange_window_days",
                table: "return_policy_templates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_policy_templates_lifecycle_status",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "terms_snapshot",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "terms_version_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "return_policy_snapshot",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "return_policy_version_id",
                table: "sales_order_lines");

            migrationBuilder.DropColumn(
                name: "allow_defective_return",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "description",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "exchange_window_days",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "is_platform_default",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "lifecycle_status",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "requires_manager_approval",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "requires_receipt",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "version_number",
                table: "return_policy_templates");

            migrationBuilder.DropColumn(
                name: "concurrency_token",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "lifecycle_status",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "review_required",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "seeded_at",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "source_template_id",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "source_template_version",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "version_number",
                table: "return_policies");
        }
    }
}
