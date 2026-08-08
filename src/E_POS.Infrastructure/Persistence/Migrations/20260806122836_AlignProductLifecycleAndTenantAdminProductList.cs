using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignProductLifecycleAndTenantAdminProductList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_status",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_variants_status",
                table: "product_variants");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "archived_by_tenant_user_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "current_setup_step",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "draft_saved_at",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "published_by_tenant_user_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "products",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateIndex(
                name: "IX_products_tenant_id_archived_by_tenant_user_id",
                table: "products",
                columns: new[] { "tenant_id", "archived_by_tenant_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_products_tenant_id_published_by_tenant_user_id",
                table: "products",
                columns: new[] { "tenant_id", "published_by_tenant_user_id" });

            // Backfill Product and Product Variant tables
            migrationBuilder.Sql("UPDATE products SET status = 'ARCHIVED' WHERE status = 'DELETED';");
            migrationBuilder.Sql("UPDATE product_variants SET status = 'ARCHIVED' WHERE status = 'DELETED';");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_setup_step",
                table: "products",
                sql: "current_setup_step BETWEEN 1 AND 8");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_status",
                table: "products",
                sql: "status IN ('DRAFT', 'ACTIVE', 'INACTIVE', 'ARCHIVED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_variants_status",
                table: "product_variants",
                sql: "status IN ('DRAFT', 'ACTIVE', 'INACTIVE', 'ARCHIVED')");

            migrationBuilder.AddForeignKey(
                name: "fk_products_tenant_users_archived_by",
                table: "products",
                columns: new[] { "tenant_id", "archived_by_tenant_user_id" },
                principalTable: "tenant_users",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_tenant_users_published_by",
                table: "products",
                columns: new[] { "tenant_id", "published_by_tenant_user_id" },
                principalTable: "tenant_users",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_tenant_users_archived_by",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_tenant_users_published_by",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_tenant_id_archived_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_tenant_id_published_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_setup_step",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_status",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_variants_status",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "archived_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "archived_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "current_setup_step",
                table: "products");

            migrationBuilder.DropColumn(
                name: "draft_saved_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "published_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "published_by_tenant_user_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "products");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_status",
                table: "products",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_variants_status",
                table: "product_variants",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");
        }
    }
}
