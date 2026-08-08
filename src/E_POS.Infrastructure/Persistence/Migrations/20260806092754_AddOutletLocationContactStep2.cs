using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletLocationContactStep2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outlets_media_asset_tenant",
                table: "outlets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_media_assets_status",
                table: "media_assets");

            migrationBuilder.RenameColumn(
                name: "media_asset_id",
                table: "outlets",
                newName: "primary_image_media_asset_id");

            migrationBuilder.RenameIndex(
                name: "ix_outlets_tenant_id_media_asset_id",
                table: "outlets",
                newName: "ix_outlets_tenant_id_primary_image_media_asset_id");

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "outlet_addresses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deletion_retry_count",
                table: "media_assets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "last_deletion_error",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_retry_at",
                table: "media_assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "idempotency_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    request_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    response_status_code = table.Column<int>(type: "integer", nullable: false),
                    response_body = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_error_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    processing_leased_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_requests", x => x.id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_media_assets_status",
                table: "media_assets",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETE_PENDING', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "uq_idempotency_requests_scope",
                table: "idempotency_requests",
                columns: new[] { "tenant_id", "actor_user_id", "endpoint", "idempotency_key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_outlets_primary_image_media_asset_tenant",
                table: "outlets",
                columns: new[] { "tenant_id", "primary_image_media_asset_id" },
                principalTable: "media_assets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outlets_primary_image_media_asset_tenant",
                table: "outlets");

            migrationBuilder.DropTable(
                name: "idempotency_requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_media_assets_status",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "outlet_addresses");

            migrationBuilder.DropColumn(
                name: "deletion_retry_count",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "last_deletion_error",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "next_retry_at",
                table: "media_assets");

            migrationBuilder.RenameColumn(
                name: "primary_image_media_asset_id",
                table: "outlets",
                newName: "media_asset_id");

            migrationBuilder.RenameIndex(
                name: "ix_outlets_tenant_id_primary_image_media_asset_id",
                table: "outlets",
                newName: "ix_outlets_tenant_id_media_asset_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_media_assets_status",
                table: "media_assets",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddForeignKey(
                name: "fk_outlets_media_asset_tenant",
                table: "outlets",
                columns: new[] { "tenant_id", "media_asset_id" },
                principalTable: "media_assets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
