using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPosOperationsWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "till_session_events");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "till_cash_movements");

            migrationBuilder.DropColumn(
                name: "name",
                table: "receipt_templates");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "receipt_templates");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "receipt_print_logs");

            migrationBuilder.AlterColumn<string>(
                name: "template_code",
                table: "receipt_templates",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "receipt_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "template_name",
                table: "receipt_templates",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "receipt_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "receipt_template_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "receipt_template_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "receipt_template_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "receipt_template_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_templates_created_by_tenant_user_id",
                table: "receipt_templates",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_templates_updated_by_tenant_user_id",
                table: "receipt_templates",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_versions_created_by_tenant_user_id",
                table: "receipt_template_versions",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_versions_updated_by_tenant_user_id",
                table: "receipt_template_versions",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_assignments_created_by_tenant_user_id",
                table: "receipt_template_assignments",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_template_assignments_updated_by_tenant_user_id",
                table: "receipt_template_assignments",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_assignments_created_by_tenant_users",
                table: "receipt_template_assignments",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_assignments_updated_by_tenant_users",
                table: "receipt_template_assignments",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_versions_created_by_tenant_users",
                table: "receipt_template_versions",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_template_versions_updated_by_tenant_users",
                table: "receipt_template_versions",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_templates_created_by_tenant_users",
                table: "receipt_templates",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receipt_templates_updated_by_tenant_users",
                table: "receipt_templates",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_assignments_created_by_tenant_users",
                table: "receipt_template_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_assignments_updated_by_tenant_users",
                table: "receipt_template_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_versions_created_by_tenant_users",
                table: "receipt_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_template_versions_updated_by_tenant_users",
                table: "receipt_template_versions");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_templates_created_by_tenant_users",
                table: "receipt_templates");

            migrationBuilder.DropForeignKey(
                name: "fk_receipt_templates_updated_by_tenant_users",
                table: "receipt_templates");

            migrationBuilder.DropIndex(
                name: "IX_receipt_templates_created_by_tenant_user_id",
                table: "receipt_templates");

            migrationBuilder.DropIndex(
                name: "IX_receipt_templates_updated_by_tenant_user_id",
                table: "receipt_templates");

            migrationBuilder.DropIndex(
                name: "IX_receipt_template_versions_created_by_tenant_user_id",
                table: "receipt_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_receipt_template_versions_updated_by_tenant_user_id",
                table: "receipt_template_versions");

            migrationBuilder.DropIndex(
                name: "IX_receipt_template_assignments_created_by_tenant_user_id",
                table: "receipt_template_assignments");

            migrationBuilder.DropIndex(
                name: "IX_receipt_template_assignments_updated_by_tenant_user_id",
                table: "receipt_template_assignments");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "receipt_templates");

            migrationBuilder.DropColumn(
                name: "template_name",
                table: "receipt_templates");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "receipt_templates");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "receipt_template_versions");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "receipt_template_versions");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "receipt_template_assignments");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "receipt_template_assignments");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "till_session_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "till_cash_movements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "template_code",
                table: "receipt_templates",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "receipt_templates",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "receipt_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "receipt_print_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
