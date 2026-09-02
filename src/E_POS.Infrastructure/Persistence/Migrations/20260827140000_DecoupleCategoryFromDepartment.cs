using System;
using E_POS.Infrastructure.Modules.Tenant.CatalogProduct;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class DecoupleCategoryFromDepartment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(CategoryMigrationPreflight.BuildGuardSql());

        migrationBuilder.DropForeignKey(
            name: "fk_categories_department_id_departments",
            table: "categories");

        migrationBuilder.DropForeignKey(
            name: "fk_categories_parent_category_id_categories",
            table: "categories");

        migrationBuilder.DropForeignKey(
            name: "fk_discount_policy_targets_category_id_categories",
            table: "discount_policy_targets");

        migrationBuilder.Sql("""ALTER TABLE categories DROP CONSTRAINT IF EXISTS "AK_categories_tenant_id_id";""");

        migrationBuilder.DropIndex(
            name: "IX_categories_department_id",
            table: "categories");

        migrationBuilder.DropIndex(
            name: "IX_categories_parent_category_id",
            table: "categories");

        migrationBuilder.DropIndex(
            name: "uq_categories_tenant_id_department_id_category_code",
            table: "categories");

        migrationBuilder.DropIndex(
            name: "uq_categories_tenant_id_department_id_id",
            table: "categories");

        migrationBuilder.Sql("""DROP INDEX IF EXISTS uq_categories_tenant_id_id;""");

        migrationBuilder.DropColumn(
            name: "department_id",
            table: "categories");

        migrationBuilder.AlterColumn<string>(
            name: "description",
            table: "categories",
            type: "varchar(2000)",
            maxLength: 2000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AddUniqueConstraint(
            name: "uq_categories_tenant_id_id",
            table: "categories",
            columns: ["tenant_id", "id"]);

        migrationBuilder.CreateIndex(
            name: "ix_categories_tenant_id_parent_category_id",
            table: "categories",
            columns: ["tenant_id", "parent_category_id"]);

        migrationBuilder.CreateIndex(
            name: "ix_categories_tenant_id_status",
            table: "categories",
            columns: ["tenant_id", "status"]);

        migrationBuilder.CreateIndex(
            name: "uq_categories_tenant_id_category_code",
            table: "categories",
            columns: ["tenant_id", "category_code"],
            unique: true);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX uq_categories_tenant_id_normalized_category_name
            ON categories (tenant_id, LOWER(BTRIM(category_name)));
            """);

        migrationBuilder.AddCheckConstraint(
            name: "ck_categories_description_length",
            table: "categories",
            sql: "description IS NULL OR char_length(description) <= 2000");

        migrationBuilder.AddForeignKey(
            name: "fk_categories_tenant_parent_category",
            table: "categories",
            columns: ["tenant_id", "parent_category_id"],
            principalTable: "categories",
            principalColumns: ["tenant_id", "id"],
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_discount_policy_targets_category_id_categories",
            table: "discount_policy_targets",
            columns: ["tenant_id", "category_id"],
            principalTable: "categories",
            principalColumns: ["tenant_id", "id"],
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    /// <summary>
    /// Forward-only architecture migration. Department association cannot be reconstructed
    /// without inventing fake department_id values. Rollback requires a database backup restore.
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new InvalidOperationException(
            "DecoupleCategoryFromDepartment is a forward-only architecture migration. " +
            "Department association cannot be reconstructed safely. Rollback requires a database backup restore.");
    }
}
