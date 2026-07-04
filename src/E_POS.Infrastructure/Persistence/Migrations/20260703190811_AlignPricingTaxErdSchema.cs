using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPricingTaxErdSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_tax_rates_tax_jurisdiction_id_tax_rate_code",
                table: "tax_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_rates_rate_percent",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "uq_tax_class_rates_tax_class_id_tax_rate_id",
                table: "tax_class_rates");

            migrationBuilder.DropIndex(
                name: "uq_product_tax_assignments_product_id_product_variant_id_tax_class_id",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "uq_price_list_outlets_price_list_id_outlet_id",
                table: "price_list_outlets");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_price_list_id_product_id_product_variant_id",
                table: "price_list_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_list_items_price_amount",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "uq_price_list_channels_price_list_id_sales_channel_id",
                table: "price_list_channels");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "tax_rates",
                newName: "tax_rate_name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "tax_jurisdictions",
                newName: "jurisdiction_name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "tax_classes",
                newName: "tax_class_name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "price_lists",
                newName: "price_list_name");

            migrationBuilder.RenameColumn(
                name: "price_amount",
                table: "price_list_items",
                newName: "selling_price");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "tax_rates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_compound",
                table: "tax_rates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tax_rates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tax_rates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "tax_rates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_from",
                table: "tax_rates",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_until",
                table: "tax_rates",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                table: "tax_jurisdictions",
                type: "char(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "tax_jurisdictions",
                type: "uuid",
                nullable: true);

            

            migrationBuilder.AddColumn<string>(
                name: "jurisdiction_type",
                table: "tax_jurisdictions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "locality_name",
                table: "tax_jurisdictions",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_jurisdiction_id",
                table: "tax_jurisdictions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code_pattern",
                table: "tax_jurisdictions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region_code",
                table: "tax_jurisdictions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tax_jurisdictions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "tax_jurisdictions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "tax_classes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "tax_classes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_default_tax_class",
                table: "tax_classes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tax_classes",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "tax_classes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "tax_class_rates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "tax_class_rates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tax_class_rates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tax_class_rates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "tax_class_rates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "product_tax_assignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "applies_from",
                table: "product_tax_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "applies_until",
                table: "product_tax_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "product_tax_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "product_tax_assignments",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "product_tax_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "product_tax_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "price_lists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "price_lists",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_default_price_list",
                table: "price_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "price_includes_tax",
                table: "price_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            

            migrationBuilder.AddColumn<string>(
                name: "price_list_type",
                table: "price_lists",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "price_lists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "price_lists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "valid_from",
                table: "price_lists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "valid_until",
                table: "price_lists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "price_list_outlets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "price_list_outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "price_list_outlets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "price_list_outlets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "compare_at_price",
                table: "price_list_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "price_list_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_quantity",
                table: "price_list_items",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "price_list_items",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "price_list_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "uom_id",
                table: "price_list_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "price_list_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "valid_from",
                table: "price_list_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "valid_until",
                table: "price_list_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "price_list_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "price_list_channels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "price_list_channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE tax_rates tr
                SET tenant_id = tj.tenant_id
                FROM tax_jurisdictions tj
                WHERE tr.tax_jurisdiction_id = tj.id
                  AND tr.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE tax_class_rates tcr
                SET tenant_id = tc.tenant_id
                FROM tax_classes tc
                WHERE tcr.tax_class_id = tc.id
                  AND tcr.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE product_tax_assignments pta
                SET tenant_id = p.tenant_id
                FROM products p
                WHERE pta.product_id = p.id
                  AND pta.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE price_list_outlets plo
                SET tenant_id = pl.tenant_id
                FROM price_lists pl
                WHERE plo.price_list_id = pl.id
                  AND plo.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE price_list_channels plc
                SET tenant_id = pl.tenant_id
                FROM price_lists pl
                WHERE plc.price_list_id = pl.id
                  AND plc.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE price_list_items pli
                SET tenant_id = pl.tenant_id
                FROM price_lists pl
                WHERE pli.price_list_id = pl.id
                  AND pli.tenant_id = '00000000-0000-0000-0000-000000000000';

                UPDATE price_lists pl
                SET currency_code = t.currency_code
                FROM tenants t
                WHERE pl.tenant_id = t.id
                  AND pl.currency_code = '';

                UPDATE price_lists
                SET price_list_type = 'STANDARD'
                WHERE price_list_type = '';

                UPDATE tax_jurisdictions
                SET jurisdiction_type = 'COUNTRY'
                WHERE jurisdiction_type = '';

                UPDATE tax_jurisdictions
                SET country_code = 'LK'
                WHERE country_code = '';
            """);
            migrationBuilder.AddUniqueConstraint(
                name: "AK_currencies_currency_code",
                table: "currencies",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rates_created_by_tenant_user_id",
                table: "tax_rates",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rates_tax_jurisdiction_id",
                table: "tax_rates",
                column: "tax_jurisdiction_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rates_updated_by_tenant_user_id",
                table: "tax_rates",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tax_rates_tenant_id_id",
                table: "tax_rates",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tax_rates_tenant_id_tax_rate_code",
                table: "tax_rates",
                columns: new[] { "tenant_id", "tax_rate_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_rates_rate_percent_max",
                table: "tax_rates",
                sql: "rate_percent <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_rates_rate_percent_min",
                table: "tax_rates",
                sql: "rate_percent > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_rates_status",
                table: "tax_rates",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_rates_valid_period",
                table: "tax_rates",
                sql: "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");

            migrationBuilder.CreateIndex(
                name: "IX_tax_jurisdictions_created_by_tenant_user_id",
                table: "tax_jurisdictions",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_jurisdictions_parent_jurisdiction_id",
                table: "tax_jurisdictions",
                column: "parent_jurisdiction_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_jurisdictions_updated_by_tenant_user_id",
                table: "tax_jurisdictions",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tax_jurisdictions_tenant_id_id",
                table: "tax_jurisdictions",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_jurisdictions_parent",
                table: "tax_jurisdictions",
                sql: "parent_jurisdiction_id IS NULL OR parent_jurisdiction_id <> id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_jurisdictions_status",
                table: "tax_jurisdictions",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_tax_classes_created_by_tenant_user_id",
                table: "tax_classes",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_classes_updated_by_tenant_user_id",
                table: "tax_classes",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tax_classes_active_default_per_tenant",
                table: "tax_classes",
                column: "tenant_id",
                unique: true,
                filter: "is_default_tax_class = true AND status = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "uq_tax_classes_tenant_id_id",
                table: "tax_classes",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_classes_status",
                table: "tax_classes",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_tax_class_rates_created_by_tenant_user_id",
                table: "tax_class_rates",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_class_rates_tax_class_id",
                table: "tax_class_rates",
                column: "tax_class_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_class_rates_updated_by_tenant_user_id",
                table: "tax_class_rates",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_tax_class_rates_tenant_id_tax_class_id_tax_rate_id",
                table: "tax_class_rates",
                columns: new[] { "tenant_id", "tax_class_id", "tax_rate_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_class_rates_sort_order",
                table: "tax_class_rates",
                sql: "sort_order >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_class_rates_status",
                table: "tax_class_rates",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_product_tax_assignments_created_by_tenant_user_id",
                table: "product_tax_assignments",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_tax_assignments_product_id",
                table: "product_tax_assignments",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_tax_assignments_product_variant_id",
                table: "product_tax_assignments",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_tax_assignments_updated_by_tenant_user_id",
                table: "product_tax_assignments",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_tax_assignments_tenant_product_variant_tax_class",
                table: "product_tax_assignments",
                columns: new[] { "tenant_id", "product_id", "product_variant_id", "tax_class_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_tax_assignments_applies_period",
                table: "product_tax_assignments",
                sql: "applies_until IS NULL OR applies_from IS NULL OR applies_until >= applies_from");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_tax_assignments_status",
                table: "product_tax_assignments",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_created_by_tenant_user_id",
                table: "price_lists",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_currency_code",
                table: "price_lists",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_updated_by_tenant_user_id",
                table: "price_lists",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_price_lists_active_default_per_tenant",
                table: "price_lists",
                column: "tenant_id",
                unique: true,
                filter: "is_default_price_list = true AND status = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "uq_price_lists_tenant_id_id",
                table: "price_lists",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_lists_priority",
                table: "price_lists",
                sql: "priority >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_lists_valid_period",
                table: "price_lists",
                sql: "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_outlets_created_by_tenant_user_id",
                table: "price_list_outlets",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_outlets_price_list_id",
                table: "price_list_outlets",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_outlets_updated_by_tenant_user_id",
                table: "price_list_outlets",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_outlets_tenant_id_price_list_id_outlet_id",
                table: "price_list_outlets",
                columns: new[] { "tenant_id", "price_list_id", "outlet_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_list_outlets_status",
                table: "price_list_outlets",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_created_by_tenant_user_id",
                table: "price_list_items",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_price_list_id",
                table: "price_list_items",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_uom_id",
                table: "price_list_items",
                column: "uom_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_updated_by_tenant_user_id",
                table: "price_list_items",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_product_scope",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id", "product_id", "uom_id", "min_quantity" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_variant_scope",
                table: "price_list_items",
                columns: new[] { "tenant_id", "price_list_id", "product_variant_id", "uom_id", "min_quantity" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_list_items_compare_at_price",
                table: "price_list_items",
                sql: "compare_at_price IS NULL OR compare_at_price >= selling_price");

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_list_items_min_quantity",
                table: "price_list_items",
                sql: "min_quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_list_items_selling_price",
                table: "price_list_items",
                sql: "selling_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_list_items_status",
                table: "price_list_items",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_list_items_valid_period",
                table: "price_list_items",
                sql: "valid_until IS NULL OR valid_from IS NULL OR valid_until >= valid_from");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_channels_created_by_tenant_user_id",
                table: "price_list_channels",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_channels_price_list_id",
                table: "price_list_channels",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_channels_updated_by_tenant_user_id",
                table: "price_list_channels",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_channels_tenant_id_price_list_id_sales_channel_id",
                table: "price_list_channels",
                columns: new[] { "tenant_id", "price_list_id", "sales_channel_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_list_channels_status",
                table: "price_list_channels",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_channels_created_by_tenant_user_id_tenant_users",
                table: "price_list_channels",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_channels_tenant_id_tenants",
                table: "price_list_channels",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_channels_updated_by_tenant_user_id_tenant_users",
                table: "price_list_channels",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_created_by_tenant_user_id_tenant_users",
                table: "price_list_items",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_tenant_id_tenants",
                table: "price_list_items",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_uom_id_unit_of_measures",
                table: "price_list_items",
                column: "uom_id",
                principalTable: "unit_of_measures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_items_updated_by_tenant_user_id_tenant_users",
                table: "price_list_items",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_outlets_created_by_tenant_user_id_tenant_users",
                table: "price_list_outlets",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_outlets_tenant_id_tenants",
                table: "price_list_outlets",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_list_outlets_updated_by_tenant_user_id_tenant_users",
                table: "price_list_outlets",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_lists_created_by_tenant_user_id_tenant_users",
                table: "price_lists",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_lists_currency_code_currencies",
                table: "price_lists",
                column: "currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_price_lists_updated_by_tenant_user_id_tenant_users",
                table: "price_lists",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_created_by_tenant_user_id_tenant_users",
                table: "product_tax_assignments",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_product_variant_id_product_variants",
                table: "product_tax_assignments",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_tenant_id_tenants",
                table: "product_tax_assignments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_product_tax_assignments_updated_by_tenant_user_id_tenant_users",
                table: "product_tax_assignments",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_class_rates_created_by_tenant_user_id_tenant_users",
                table: "tax_class_rates",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_class_rates_tenant_id_tenants",
                table: "tax_class_rates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_class_rates_updated_by_tenant_user_id_tenant_users",
                table: "tax_class_rates",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_classes_created_by_tenant_user_id_tenant_users",
                table: "tax_classes",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_classes_updated_by_tenant_user_id_tenant_users",
                table: "tax_classes",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_jurisdictions_created_by_tenant_user_id_tenant_users",
                table: "tax_jurisdictions",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_jurisdictions_parent_jurisdiction_id_tax_jurisdictions",
                table: "tax_jurisdictions",
                column: "parent_jurisdiction_id",
                principalTable: "tax_jurisdictions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_jurisdictions_updated_by_tenant_user_id_tenant_users",
                table: "tax_jurisdictions",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_rates_created_by_tenant_user_id_tenant_users",
                table: "tax_rates",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_rates_tenant_id_tenants",
                table: "tax_rates",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tax_rates_updated_by_tenant_user_id_tenant_users",
                table: "tax_rates",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_price_list_channels_created_by_tenant_user_id_tenant_users",
                table: "price_list_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_channels_tenant_id_tenants",
                table: "price_list_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_channels_updated_by_tenant_user_id_tenant_users",
                table: "price_list_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_created_by_tenant_user_id_tenant_users",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_tenant_id_tenants",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_uom_id_unit_of_measures",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_items_updated_by_tenant_user_id_tenant_users",
                table: "price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_outlets_created_by_tenant_user_id_tenant_users",
                table: "price_list_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_outlets_tenant_id_tenants",
                table: "price_list_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_price_list_outlets_updated_by_tenant_user_id_tenant_users",
                table: "price_list_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_price_lists_created_by_tenant_user_id_tenant_users",
                table: "price_lists");

            migrationBuilder.DropForeignKey(
                name: "fk_price_lists_currency_code_currencies",
                table: "price_lists");

            migrationBuilder.DropForeignKey(
                name: "fk_price_lists_updated_by_tenant_user_id_tenant_users",
                table: "price_lists");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_created_by_tenant_user_id_tenant_users",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_product_variant_id_product_variants",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_tenant_id_tenants",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_product_tax_assignments_updated_by_tenant_user_id_tenant_users",
                table: "product_tax_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_class_rates_created_by_tenant_user_id_tenant_users",
                table: "tax_class_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_class_rates_tenant_id_tenants",
                table: "tax_class_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_class_rates_updated_by_tenant_user_id_tenant_users",
                table: "tax_class_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_classes_created_by_tenant_user_id_tenant_users",
                table: "tax_classes");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_classes_updated_by_tenant_user_id_tenant_users",
                table: "tax_classes");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_jurisdictions_created_by_tenant_user_id_tenant_users",
                table: "tax_jurisdictions");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_jurisdictions_parent_jurisdiction_id_tax_jurisdictions",
                table: "tax_jurisdictions");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_jurisdictions_updated_by_tenant_user_id_tenant_users",
                table: "tax_jurisdictions");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_rates_created_by_tenant_user_id_tenant_users",
                table: "tax_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_rates_tenant_id_tenants",
                table: "tax_rates");

            migrationBuilder.DropForeignKey(
                name: "fk_tax_rates_updated_by_tenant_user_id_tenant_users",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_rates_created_by_tenant_user_id",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_rates_tax_jurisdiction_id",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_rates_updated_by_tenant_user_id",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "uq_tax_rates_tenant_id_id",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "uq_tax_rates_tenant_id_tax_rate_code",
                table: "tax_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_rates_rate_percent_max",
                table: "tax_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_rates_rate_percent_min",
                table: "tax_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_rates_status",
                table: "tax_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_rates_valid_period",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_jurisdictions_created_by_tenant_user_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropIndex(
                name: "IX_tax_jurisdictions_parent_jurisdiction_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropIndex(
                name: "IX_tax_jurisdictions_updated_by_tenant_user_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropIndex(
                name: "uq_tax_jurisdictions_tenant_id_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_jurisdictions_parent",
                table: "tax_jurisdictions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_jurisdictions_status",
                table: "tax_jurisdictions");

            migrationBuilder.DropIndex(
                name: "IX_tax_classes_created_by_tenant_user_id",
                table: "tax_classes");

            migrationBuilder.DropIndex(
                name: "IX_tax_classes_updated_by_tenant_user_id",
                table: "tax_classes");

            migrationBuilder.DropIndex(
                name: "uq_tax_classes_active_default_per_tenant",
                table: "tax_classes");

            migrationBuilder.DropIndex(
                name: "uq_tax_classes_tenant_id_id",
                table: "tax_classes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_classes_status",
                table: "tax_classes");

            migrationBuilder.DropIndex(
                name: "IX_tax_class_rates_created_by_tenant_user_id",
                table: "tax_class_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_class_rates_tax_class_id",
                table: "tax_class_rates");

            migrationBuilder.DropIndex(
                name: "IX_tax_class_rates_updated_by_tenant_user_id",
                table: "tax_class_rates");

            migrationBuilder.DropIndex(
                name: "uq_tax_class_rates_tenant_id_tax_class_id_tax_rate_id",
                table: "tax_class_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_class_rates_sort_order",
                table: "tax_class_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tax_class_rates_status",
                table: "tax_class_rates");

            migrationBuilder.DropIndex(
                name: "IX_product_tax_assignments_created_by_tenant_user_id",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "IX_product_tax_assignments_product_id",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "IX_product_tax_assignments_product_variant_id",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "IX_product_tax_assignments_updated_by_tenant_user_id",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "uq_product_tax_assignments_tenant_product_variant_tax_class",
                table: "product_tax_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_tax_assignments_applies_period",
                table: "product_tax_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_tax_assignments_status",
                table: "product_tax_assignments");

            migrationBuilder.DropIndex(
                name: "IX_price_lists_created_by_tenant_user_id",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "IX_price_lists_currency_code",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "IX_price_lists_updated_by_tenant_user_id",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "uq_price_lists_active_default_per_tenant",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "uq_price_lists_tenant_id_id",
                table: "price_lists");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_lists_priority",
                table: "price_lists");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_lists_valid_period",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "IX_price_list_outlets_created_by_tenant_user_id",
                table: "price_list_outlets");

            migrationBuilder.DropIndex(
                name: "IX_price_list_outlets_price_list_id",
                table: "price_list_outlets");

            migrationBuilder.DropIndex(
                name: "IX_price_list_outlets_updated_by_tenant_user_id",
                table: "price_list_outlets");

            migrationBuilder.DropIndex(
                name: "uq_price_list_outlets_tenant_id_price_list_id_outlet_id",
                table: "price_list_outlets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_list_outlets_status",
                table: "price_list_outlets");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_created_by_tenant_user_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_price_list_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_uom_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_updated_by_tenant_user_id",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_product_scope",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "uq_price_list_items_variant_scope",
                table: "price_list_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_list_items_compare_at_price",
                table: "price_list_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_list_items_min_quantity",
                table: "price_list_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_list_items_selling_price",
                table: "price_list_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_list_items_status",
                table: "price_list_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_list_items_valid_period",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_price_list_channels_created_by_tenant_user_id",
                table: "price_list_channels");

            migrationBuilder.DropIndex(
                name: "IX_price_list_channels_price_list_id",
                table: "price_list_channels");

            migrationBuilder.DropIndex(
                name: "IX_price_list_channels_updated_by_tenant_user_id",
                table: "price_list_channels");

            migrationBuilder.DropIndex(
                name: "uq_price_list_channels_tenant_id_price_list_id_sales_channel_id",
                table: "price_list_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_price_list_channels_status",
                table: "price_list_channels");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_currencies_currency_code",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "is_compound",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "tax_rate_name",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "valid_from",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "valid_until",
                table: "tax_rates");

            migrationBuilder.DropColumn(
                name: "country_code",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "jurisdiction_name",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "jurisdiction_type",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "locality_name",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "parent_jurisdiction_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "postal_code_pattern",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "region_code",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "tax_jurisdictions");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "tax_classes");

            migrationBuilder.DropColumn(
                name: "description",
                table: "tax_classes");

            migrationBuilder.DropColumn(
                name: "is_default_tax_class",
                table: "tax_classes");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tax_classes");

            migrationBuilder.DropColumn(
                name: "tax_class_name",
                table: "tax_classes");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "tax_classes");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "tax_class_rates");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "tax_class_rates");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tax_class_rates");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tax_class_rates");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "tax_class_rates");

            migrationBuilder.DropColumn(
                name: "applies_from",
                table: "product_tax_assignments");

            migrationBuilder.DropColumn(
                name: "applies_until",
                table: "product_tax_assignments");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "product_tax_assignments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "product_tax_assignments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "product_tax_assignments");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "product_tax_assignments");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "is_default_price_list",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "price_includes_tax",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "price_list_name",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "price_list_type",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "valid_from",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "valid_until",
                table: "price_lists");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "price_list_outlets");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "price_list_outlets");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "price_list_outlets");

            migrationBuilder.DropColumn(
                name: "compare_at_price",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "min_quantity",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "selling_price",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "status",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "uom_id",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "valid_from",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "valid_until",
                table: "price_list_items");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "price_list_channels");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "price_list_channels");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "price_list_channels");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "tax_rates",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "tax_jurisdictions",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "tax_classes",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_variant_id",
                table: "product_tax_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "price_lists",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "outlet_id",
                table: "price_list_outlets",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<decimal>(
                name: "price_amount",
                table: "price_list_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "uq_tax_rates_tax_jurisdiction_id_tax_rate_code",
                table: "tax_rates",
                columns: new[] { "tax_jurisdiction_id", "tax_rate_code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tax_rates_rate_percent",
                table: "tax_rates",
                sql: "rate_percent >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_tax_class_rates_tax_class_id_tax_rate_id",
                table: "tax_class_rates",
                columns: new[] { "tax_class_id", "tax_rate_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_tax_assignments_product_id_product_variant_id_tax_class_id",
                table: "product_tax_assignments",
                columns: new[] { "product_id", "product_variant_id", "tax_class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_price_list_outlets_price_list_id_outlet_id",
                table: "price_list_outlets",
                columns: new[] { "price_list_id", "outlet_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_price_list_items_price_list_id_product_id_product_variant_id",
                table: "price_list_items",
                columns: new[] { "price_list_id", "product_id", "product_variant_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_price_list_items_price_amount",
                table: "price_list_items",
                sql: "price_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_price_list_channels_price_list_id_sales_channel_id",
                table: "price_list_channels",
                columns: new[] { "price_list_id", "sales_channel_id" },
                unique: true);
        }
    }
}


