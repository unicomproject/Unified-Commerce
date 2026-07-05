using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule10MasterDataWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_unit_of_measures_conversion_factor",
                table: "unit_of_measures");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_policies_return_window_days",
                table: "return_policies");

            migrationBuilder.DropIndex(
                name: "uq_categories_tenant_id_category_code",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "uq_business_types_business_type_code",
                table: "business_types");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "unit_of_measures",
                newName: "uom_name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "return_policies",
                newName: "return_policy_name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "departments",
                newName: "department_name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "collections",
                newName: "collection_name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "categories",
                newName: "category_name");

            migrationBuilder.RenameColumn(
                name: "business_type_code",
                table: "business_types",
                newName: "business_type_key");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "business_types",
                newName: "business_type_name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "brands",
                newName: "brand_name");

            migrationBuilder.RenameColumn(
                name: "policy_code",
                table: "return_policies",
                newName: "return_policy_code");

            migrationBuilder.RenameIndex(
                name: "uq_return_policies_tenant_id_policy_code",
                table: "return_policies",
                newName: "uq_return_policies_tenant_id_return_policy_code");

            migrationBuilder.AlterColumn<string>(
                name: "uom_code",
                table: "unit_of_measures",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<decimal>(
                name: "conversion_factor",
                table: "unit_of_measures",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "base_uom_id",
                table: "unit_of_measures",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "unit_of_measures",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AddColumn<string>(
                name: "symbol",
                table: "unit_of_measures",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "uom_name",
                table: "unit_of_measures",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "uom_type",
                table: "unit_of_measures",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "return_policies",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "return_window_days",
                table: "return_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_defective_return",
                table: "return_policies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "return_policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "return_policies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "exchange_window_days",
                table: "return_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_default_policy",
                table: "return_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_manager_approval",
                table: "return_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_receipt",
                table: "return_policies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "return_policy_name",
                table: "return_policies",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "return_policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "departments",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "department_name",
                table: "departments",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "departments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "departments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "collections",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "collection_name",
                table: "collections",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "collection_slug",
                table: "collections",
                type: "varchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "collection_type",
                table: "collections",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "collections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "collections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ends_at",
                table: "collections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "collections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "starts_at",
                table: "collections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "collections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "categories",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "category_code",
                table: "categories",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "category_name",
                table: "categories",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "category_slug",
                table: "categories",
                type: "varchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                table: "categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "business_types",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "business_type_key",
                table: "business_types",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "business_type_name",
                table: "business_types",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<bool>(
                name: "is_system_type",
                table: "business_types",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "business_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "brands",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "brand_name",
                table: "brands",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "brand_slug",
                table: "brands",
                type: "varchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "brands",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "brands",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "brands",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "brands",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_unit_of_measures_base_uom_id",
                table: "unit_of_measures",
                column: "base_uom_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_unit_of_measures_base_uom_id",
                table: "unit_of_measures",
                sql: "base_uom_id IS NULL OR base_uom_id <> id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_unit_of_measures_conversion_factor",
                table: "unit_of_measures",
                sql: "conversion_factor > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_unit_of_measures_status",
                table: "unit_of_measures",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_return_policies_created_by_tenant_user_id",
                table: "return_policies",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_return_policies_updated_by_tenant_user_id",
                table: "return_policies",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_return_policies_tenant_id_id",
                table: "return_policies",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_policies_exchange_window_days",
                table: "return_policies",
                sql: "exchange_window_days >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_policies_return_window_days",
                table: "return_policies",
                sql: "return_window_days >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_departments_created_by_tenant_user_id",
                table: "departments",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_updated_by_tenant_user_id",
                table: "departments",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_departments_tenant_id_id",
                table: "departments",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_departments_sort_order",
                table: "departments",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_collections_created_by_tenant_user_id",
                table: "collections",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_collections_updated_by_tenant_user_id",
                table: "collections",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_collections_tenant_id_collection_slug",
                table: "collections",
                columns: new[] { "tenant_id", "collection_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_collections_tenant_id_id",
                table: "collections",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_collections_sort_order",
                table: "collections",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_collections_valid_period",
                table: "collections",
                sql: "ends_at IS NULL OR starts_at IS NULL OR ends_at >= starts_at");

            migrationBuilder.CreateIndex(
                name: "IX_categories_created_by_tenant_user_id",
                table: "categories",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_department_id",
                table: "categories",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_updated_by_tenant_user_id",
                table: "categories",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_categories_tenant_id_category_slug",
                table: "categories",
                columns: new[] { "tenant_id", "category_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_categories_tenant_id_department_id_category_code",
                table: "categories",
                columns: new[] { "tenant_id", "department_id", "category_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_categories_tenant_id_department_id_id",
                table: "categories",
                columns: new[] { "tenant_id", "department_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_categories_tenant_id_id",
                table: "categories",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_categories_parent_category_id",
                table: "categories",
                sql: "parent_category_id IS NULL OR parent_category_id <> id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_categories_sort_order",
                table: "categories",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_business_types_business_type_key",
                table: "business_types",
                column: "business_type_key",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_business_types_sort_order",
                table: "business_types",
                sql: "sort_order >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_brands_created_by_tenant_user_id",
                table: "brands",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_brands_updated_by_tenant_user_id",
                table: "brands",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_brands_tenant_id_brand_slug",
                table: "brands",
                columns: new[] { "tenant_id", "brand_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_brands_tenant_id_id",
                table: "brands",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_brands_created_by_tenant_user_id_tenant_users",
                table: "brands",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_brands_updated_by_tenant_user_id_tenant_users",
                table: "brands",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_categories_created_by_tenant_user_id_tenant_users",
                table: "categories",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_categories_department_id_departments",
                table: "categories",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_categories_updated_by_tenant_user_id_tenant_users",
                table: "categories",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_collections_created_by_tenant_user_id_tenant_users",
                table: "collections",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_collections_updated_by_tenant_user_id_tenant_users",
                table: "collections",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_departments_created_by_tenant_user_id_tenant_users",
                table: "departments",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_departments_updated_by_tenant_user_id_tenant_users",
                table: "departments",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_policies_created_by_tenant_user_id_tenant_users",
                table: "return_policies",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_return_policies_updated_by_tenant_user_id_tenant_users",
                table: "return_policies",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_unit_of_measures_base_uom_id_unit_of_measures",
                table: "unit_of_measures",
                column: "base_uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_brands_created_by_tenant_user_id_tenant_users",
                table: "brands");

            migrationBuilder.DropForeignKey(
                name: "fk_brands_updated_by_tenant_user_id_tenant_users",
                table: "brands");

            migrationBuilder.DropForeignKey(
                name: "fk_categories_created_by_tenant_user_id_tenant_users",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "fk_categories_department_id_departments",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "fk_categories_updated_by_tenant_user_id_tenant_users",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "fk_collections_created_by_tenant_user_id_tenant_users",
                table: "collections");

            migrationBuilder.DropForeignKey(
                name: "fk_collections_updated_by_tenant_user_id_tenant_users",
                table: "collections");

            migrationBuilder.DropForeignKey(
                name: "fk_departments_created_by_tenant_user_id_tenant_users",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "fk_departments_updated_by_tenant_user_id_tenant_users",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "fk_return_policies_created_by_tenant_user_id_tenant_users",
                table: "return_policies");

            migrationBuilder.DropForeignKey(
                name: "fk_return_policies_updated_by_tenant_user_id_tenant_users",
                table: "return_policies");

            migrationBuilder.DropForeignKey(
                name: "fk_unit_of_measures_base_uom_id_unit_of_measures",
                table: "unit_of_measures");

            migrationBuilder.DropIndex(
                name: "IX_unit_of_measures_base_uom_id",
                table: "unit_of_measures");

            migrationBuilder.DropCheckConstraint(
                name: "ck_unit_of_measures_base_uom_id",
                table: "unit_of_measures");

            migrationBuilder.DropCheckConstraint(
                name: "ck_unit_of_measures_conversion_factor",
                table: "unit_of_measures");

            migrationBuilder.DropCheckConstraint(
                name: "ck_unit_of_measures_status",
                table: "unit_of_measures");

            migrationBuilder.DropIndex(
                name: "IX_return_policies_created_by_tenant_user_id",
                table: "return_policies");

            migrationBuilder.DropIndex(
                name: "IX_return_policies_updated_by_tenant_user_id",
                table: "return_policies");

            migrationBuilder.DropIndex(
                name: "uq_return_policies_tenant_id_id",
                table: "return_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_policies_exchange_window_days",
                table: "return_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_return_policies_return_window_days",
                table: "return_policies");

            migrationBuilder.DropIndex(
                name: "IX_departments_created_by_tenant_user_id",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_departments_updated_by_tenant_user_id",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "uq_departments_tenant_id_id",
                table: "departments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_departments_sort_order",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_collections_created_by_tenant_user_id",
                table: "collections");

            migrationBuilder.DropIndex(
                name: "IX_collections_updated_by_tenant_user_id",
                table: "collections");

            migrationBuilder.DropIndex(
                name: "uq_collections_tenant_id_collection_slug",
                table: "collections");

            migrationBuilder.DropIndex(
                name: "uq_collections_tenant_id_id",
                table: "collections");

            migrationBuilder.DropCheckConstraint(
                name: "ck_collections_sort_order",
                table: "collections");

            migrationBuilder.DropCheckConstraint(
                name: "ck_collections_valid_period",
                table: "collections");

            migrationBuilder.DropIndex(
                name: "IX_categories_created_by_tenant_user_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_department_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_updated_by_tenant_user_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "uq_categories_tenant_id_category_slug",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "uq_categories_tenant_id_department_id_category_code",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "uq_categories_tenant_id_department_id_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "uq_categories_tenant_id_id",
                table: "categories");

            migrationBuilder.DropCheckConstraint(
                name: "ck_categories_parent_category_id",
                table: "categories");

            migrationBuilder.DropCheckConstraint(
                name: "ck_categories_sort_order",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "uq_business_types_business_type_key",
                table: "business_types");

            migrationBuilder.DropCheckConstraint(
                name: "ck_business_types_sort_order",
                table: "business_types");

            migrationBuilder.DropIndex(
                name: "IX_brands_created_by_tenant_user_id",
                table: "brands");

            migrationBuilder.DropIndex(
                name: "IX_brands_updated_by_tenant_user_id",
                table: "brands");

            migrationBuilder.DropIndex(
                name: "uq_brands_tenant_id_brand_slug",
                table: "brands");

            migrationBuilder.DropIndex(
                name: "uq_brands_tenant_id_id",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "base_uom_id",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "status",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "symbol",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "uom_name",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "uom_type",
                table: "unit_of_measures");

            migrationBuilder.DropColumn(
                name: "allow_defective_return",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "description",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "exchange_window_days",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "is_default_policy",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "requires_manager_approval",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "requires_receipt",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "return_policy_name",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "return_policies");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "department_name",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "description",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "collection_name",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "collection_slug",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "collection_type",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "description",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "ends_at",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "starts_at",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "category_name",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "category_slug",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "department_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "description",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "business_type_key",
                table: "business_types");

            migrationBuilder.DropColumn(
                name: "business_type_name",
                table: "business_types");

            migrationBuilder.DropColumn(
                name: "is_system_type",
                table: "business_types");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "business_types");

            migrationBuilder.DropColumn(
                name: "brand_name",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "brand_slug",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "description",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "brands");

            migrationBuilder.RenameColumn(
                name: "return_policy_code",
                table: "return_policies",
                newName: "policy_code");

            migrationBuilder.RenameIndex(
                name: "uq_return_policies_tenant_id_return_policy_code",
                table: "return_policies",
                newName: "uq_return_policies_tenant_id_policy_code");

            migrationBuilder.AlterColumn<string>(
                name: "uom_code",
                table: "unit_of_measures",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<decimal>(
                name: "conversion_factor",
                table: "unit_of_measures",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldDefaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "unit_of_measures",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "return_policies",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<int>(
                name: "return_window_days",
                table: "return_policies",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "return_policies",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "departments",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "departments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "collections",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "collections",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "categories",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "category_code",
                table: "categories",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "categories",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "business_types",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "business_type_code",
                table: "business_types",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "business_types",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "brands",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "brands",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "ck_unit_of_measures_conversion_factor",
                table: "unit_of_measures",
                sql: "conversion_factor IS NULL OR conversion_factor > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_return_policies_return_window_days",
                table: "return_policies",
                sql: "return_window_days IS NULL OR return_window_days >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_categories_tenant_id_category_code",
                table: "categories",
                columns: new[] { "tenant_id", "category_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_business_types_business_type_code",
                table: "business_types",
                column: "business_type_code",
                unique: true);
        }
    }
}
