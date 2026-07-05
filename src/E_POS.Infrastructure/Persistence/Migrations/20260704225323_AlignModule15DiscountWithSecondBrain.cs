using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignModule15DiscountWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_channels_discount_policy_id_discount_policies",
                table: "discount_policy_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_channels_sales_channel_id_sales_channels",
                table: "discount_policy_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_conditions_discount_policy_id_discount_policies",
                table: "discount_policy_conditions");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_outlets_discount_policy_id_discount_policies",
                table: "discount_policy_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_outlets_outlet_id_outlets",
                table: "discount_policy_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_brand_id_brands",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_category_id_categories",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_collection_id_collections",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_discount_policy_id_discount_policies",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_product_id_products",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_product_variant_id_product_variants",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_id_expiry_discount_rules",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_tier_id_expiry_discount_rule_tiers",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_outlet_id_outlets",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_product_batch_id_product_batches",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rule_tiers_expiry_discount_rule_id_expiry_discount_rules",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rules_discount_policy_id_discount_policies",
                table: "expiry_discount_rules");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rules_discount_policy_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rule_tiers_expiry_discount_rule_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rule_tiers_tenant_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_expiry_discount_rule_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_expiry_discount_rule_tier_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_outlet_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_product_batch_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_types_calculation_method",
                table: "discount_types");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_brand_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_category_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_collection_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_discount_policy_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_product_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_product_variant_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_tenant_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_outlets_discount_policy_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_outlets_outlet_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_conditions_discount_policy_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_conditions_tenant_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_channels_discount_policy_id",
                table: "discount_policy_channels");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_channels_sales_channel_id",
                table: "discount_policy_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policies_amounts",
                table: "discount_policies");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "applied_from",
                table: "expiry_discount_applications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_batches_tenant_id_id",
                table: "product_batches",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_expiry_discount_rules_tenant_id_id",
                table: "expiry_discount_rules",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_expiry_discount_rule_tiers_tenant_id_id",
                table: "expiry_discount_rule_tiers",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_discount_policies_tenant_id_id",
                table: "discount_policies",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_collections_tenant_id_id",
                table: "collections",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_categories_tenant_id_id",
                table: "categories",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_brands_tenant_id_id",
                table: "brands",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "uq_products_tenant_id_id",
                table: "products",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_variants_tenant_id_id",
                table: "product_variants",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_batches_tenant_id_id",
                table: "product_batches",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rules_created_by_tenant_user_id",
                table: "expiry_discount_rules",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rules_tenant_id_discount_policy_id",
                table: "expiry_discount_rules",
                columns: new[] { "tenant_id", "discount_policy_id" });

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rules_updated_by_tenant_user_id",
                table: "expiry_discount_rules",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_expiry_discount_rules_tenant_id_id",
                table: "expiry_discount_rules",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_rules_status",
                table: "expiry_discount_rules",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rule_tiers_created_by_tenant_user_id",
                table: "expiry_discount_rule_tiers",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rule_tiers_tenant_id_expiry_discount_rule_id",
                table: "expiry_discount_rule_tiers",
                columns: new[] { "tenant_id", "expiry_discount_rule_id" });

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rule_tiers_updated_by_tenant_user_id",
                table: "expiry_discount_rule_tiers",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_expiry_discount_rule_tiers_tenant_id_id",
                table: "expiry_discount_rule_tiers",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_status",
                table: "expiry_discount_rule_tiers",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_created_by_tenant_user_id",
                table: "expiry_discount_applications",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_tenant_id_expiry_discount_rul~1",
                table: "expiry_discount_applications",
                columns: new[] { "tenant_id", "expiry_discount_rule_tier_id" });

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_tenant_id_expiry_discount_rule~",
                table: "expiry_discount_applications",
                columns: new[] { "tenant_id", "expiry_discount_rule_id" });

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_tenant_id_outlet_id",
                table: "expiry_discount_applications",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_updated_by_tenant_user_id",
                table: "expiry_discount_applications",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_expiry_discount_applications_status",
                table: "expiry_discount_applications",
                sql: "application_status IN ('ACTIVE', 'INACTIVE', 'PENDING_APPROVAL', 'APPROVED', 'REJECTED', 'EXPIRED', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_types_calculation_method",
                table: "discount_types",
                sql: "calculation_method IN ('PERCENTAGE', 'FIXED_AMOUNT', 'BUY_X_GET_Y', 'FREE_ITEM', 'PRICE_OVERRIDE')");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_created_by_tenant_user_id",
                table: "discount_policy_targets",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_tenant_id_brand_id",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "brand_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_tenant_id_category_id",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_tenant_id_collection_id",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "collection_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_tenant_id_discount_policy_id",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "discount_policy_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_tenant_id_product_id",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_tenant_id_product_variant_id",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_updated_by_tenant_user_id",
                table: "discount_policy_targets",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_targets_status",
                table: "discount_policy_targets",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_targets_target_type",
                table: "discount_policy_targets",
                sql: "target_type IN ('PRODUCT', 'PRODUCT_VARIANT', 'CATEGORY', 'BRAND', 'COLLECTION')");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_outlets_created_by_tenant_user_id",
                table: "discount_policy_outlets",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_outlets_tenant_id_outlet_id",
                table: "discount_policy_outlets",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_outlets_updated_by_tenant_user_id",
                table: "discount_policy_outlets",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_outlets_status",
                table: "discount_policy_outlets",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_conditions_created_by_tenant_user_id",
                table: "discount_policy_conditions",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_conditions_tenant_id_discount_policy_id",
                table: "discount_policy_conditions",
                columns: new[] { "tenant_id", "discount_policy_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_conditions_updated_by_tenant_user_id",
                table: "discount_policy_conditions",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_conditions_status",
                table: "discount_policy_conditions",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_channels_created_by_tenant_user_id",
                table: "discount_policy_channels",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_channels_tenant_id_sales_channel_id",
                table: "discount_policy_channels",
                columns: new[] { "tenant_id", "sales_channel_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_channels_updated_by_tenant_user_id",
                table: "discount_policy_channels",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policy_channels_status",
                table: "discount_policy_channels",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "uq_discount_policies_tenant_id_id",
                table: "discount_policies",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policies_amounts",
                table: "discount_policies",
                sql: "(max_discount_amount IS NULL OR max_discount_amount >= 0) AND (min_order_amount IS NULL OR min_order_amount >= 0) AND (min_quantity IS NULL OR min_quantity > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policies_discount_scope",
                table: "discount_policies",
                sql: "discount_scope IN ('ORDER', 'LINE', 'PRODUCT', 'CATEGORY', 'BRAND', 'COLLECTION', 'BATCH')");

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_channels_created_by_tenant_user_id_tenant_users",
                table: "discount_policy_channels",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_channels_discount_policy_id_discount_policies",
                table: "discount_policy_channels",
                columns: new[] { "tenant_id", "discount_policy_id" },
                principalTable: "discount_policies",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_channels_sales_channel_id_sales_channels",
                table: "discount_policy_channels",
                columns: new[] { "tenant_id", "sales_channel_id" },
                principalTable: "sales_channels",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_channels_updated_by_tenant_user_id_tenant_users",
                table: "discount_policy_channels",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_conditions_created_by_tenant_user_id_tenant_users",
                table: "discount_policy_conditions",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_conditions_discount_policy_id_discount_policies",
                table: "discount_policy_conditions",
                columns: new[] { "tenant_id", "discount_policy_id" },
                principalTable: "discount_policies",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_conditions_updated_by_tenant_user_id_tenant_users",
                table: "discount_policy_conditions",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_outlets_created_by_tenant_user_id_tenant_users",
                table: "discount_policy_outlets",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_outlets_discount_policy_id_discount_policies",
                table: "discount_policy_outlets",
                columns: new[] { "tenant_id", "discount_policy_id" },
                principalTable: "discount_policies",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_outlets_outlet_id_outlets",
                table: "discount_policy_outlets",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_outlets_updated_by_tenant_user_id_tenant_users",
                table: "discount_policy_outlets",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_brand_id_brands",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "brand_id" },
                principalTable: "brands",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_category_id_categories",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "category_id" },
                principalTable: "categories",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_collection_id_collections",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "collection_id" },
                principalTable: "collections",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_created_by_tenant_user_id_tenant_users",
                table: "discount_policy_targets",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_discount_policy_id_discount_policies",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "discount_policy_id" },
                principalTable: "discount_policies",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_product_id_products",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "product_id" },
                principalTable: "products",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_product_variant_id_product_variants",
                table: "discount_policy_targets",
                columns: new[] { "tenant_id", "product_variant_id" },
                principalTable: "product_variants",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_updated_by_tenant_user_id_tenant_users",
                table: "discount_policy_targets",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_created_by_tenant_user_id_tenant_users",
                table: "expiry_discount_applications",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_id_expiry_discount_rules",
                table: "expiry_discount_applications",
                columns: new[] { "tenant_id", "expiry_discount_rule_id" },
                principalTable: "expiry_discount_rules",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_tier_id_expiry_discount_rule_tiers",
                table: "expiry_discount_applications",
                columns: new[] { "tenant_id", "expiry_discount_rule_tier_id" },
                principalTable: "expiry_discount_rule_tiers",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_outlet_id_outlets",
                table: "expiry_discount_applications",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_product_batch_id_product_batches",
                table: "expiry_discount_applications",
                columns: new[] { "tenant_id", "product_batch_id" },
                principalTable: "product_batches",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_updated_by_tenant_user_id_tenant_users",
                table: "expiry_discount_applications",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rule_tiers_created_by_tenant_user_id_tenant_users",
                table: "expiry_discount_rule_tiers",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rule_tiers_expiry_discount_rule_id_expiry_discount_rules",
                table: "expiry_discount_rule_tiers",
                columns: new[] { "tenant_id", "expiry_discount_rule_id" },
                principalTable: "expiry_discount_rules",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rule_tiers_updated_by_tenant_user_id_tenant_users",
                table: "expiry_discount_rule_tiers",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rules_created_by_tenant_user_id_tenant_users",
                table: "expiry_discount_rules",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rules_discount_policy_id_discount_policies",
                table: "expiry_discount_rules",
                columns: new[] { "tenant_id", "discount_policy_id" },
                principalTable: "discount_policies",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rules_updated_by_tenant_user_id_tenant_users",
                table: "expiry_discount_rules",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_channels_created_by_tenant_user_id_tenant_users",
                table: "discount_policy_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_channels_discount_policy_id_discount_policies",
                table: "discount_policy_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_channels_sales_channel_id_sales_channels",
                table: "discount_policy_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_channels_updated_by_tenant_user_id_tenant_users",
                table: "discount_policy_channels");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_conditions_created_by_tenant_user_id_tenant_users",
                table: "discount_policy_conditions");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_conditions_discount_policy_id_discount_policies",
                table: "discount_policy_conditions");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_conditions_updated_by_tenant_user_id_tenant_users",
                table: "discount_policy_conditions");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_outlets_created_by_tenant_user_id_tenant_users",
                table: "discount_policy_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_outlets_discount_policy_id_discount_policies",
                table: "discount_policy_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_outlets_outlet_id_outlets",
                table: "discount_policy_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_outlets_updated_by_tenant_user_id_tenant_users",
                table: "discount_policy_outlets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_brand_id_brands",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_category_id_categories",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_collection_id_collections",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_created_by_tenant_user_id_tenant_users",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_discount_policy_id_discount_policies",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_product_id_products",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_product_variant_id_product_variants",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_policy_targets_updated_by_tenant_user_id_tenant_users",
                table: "discount_policy_targets");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_created_by_tenant_user_id_tenant_users",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_id_expiry_discount_rules",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_tier_id_expiry_discount_rule_tiers",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_outlet_id_outlets",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_product_batch_id_product_batches",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_applications_updated_by_tenant_user_id_tenant_users",
                table: "expiry_discount_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rule_tiers_created_by_tenant_user_id_tenant_users",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rule_tiers_expiry_discount_rule_id_expiry_discount_rules",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rule_tiers_updated_by_tenant_user_id_tenant_users",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rules_created_by_tenant_user_id_tenant_users",
                table: "expiry_discount_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rules_discount_policy_id_discount_policies",
                table: "expiry_discount_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_expiry_discount_rules_updated_by_tenant_user_id_tenant_users",
                table: "expiry_discount_rules");

            migrationBuilder.DropIndex(
                name: "uq_products_tenant_id_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "uq_product_variants_tenant_id_id",
                table: "product_variants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_batches_tenant_id_id",
                table: "product_batches");

            migrationBuilder.DropIndex(
                name: "uq_product_batches_tenant_id_id",
                table: "product_batches");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_expiry_discount_rules_tenant_id_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rules_created_by_tenant_user_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rules_tenant_id_discount_policy_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rules_updated_by_tenant_user_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropIndex(
                name: "uq_expiry_discount_rules_tenant_id_id",
                table: "expiry_discount_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_rules_status",
                table: "expiry_discount_rules");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_expiry_discount_rule_tiers_tenant_id_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rule_tiers_created_by_tenant_user_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rule_tiers_tenant_id_expiry_discount_rule_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_rule_tiers_updated_by_tenant_user_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropIndex(
                name: "uq_expiry_discount_rule_tiers_tenant_id_id",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_rule_tiers_status",
                table: "expiry_discount_rule_tiers");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_created_by_tenant_user_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_tenant_id_expiry_discount_rul~1",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_tenant_id_expiry_discount_rule~",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_tenant_id_outlet_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropIndex(
                name: "IX_expiry_discount_applications_updated_by_tenant_user_id",
                table: "expiry_discount_applications");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expiry_discount_applications_status",
                table: "expiry_discount_applications");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_types_calculation_method",
                table: "discount_types");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_created_by_tenant_user_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_tenant_id_brand_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_tenant_id_category_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_tenant_id_collection_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_tenant_id_discount_policy_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_tenant_id_product_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_tenant_id_product_variant_id",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_targets_updated_by_tenant_user_id",
                table: "discount_policy_targets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_targets_status",
                table: "discount_policy_targets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_targets_target_type",
                table: "discount_policy_targets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_outlets_created_by_tenant_user_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_outlets_tenant_id_outlet_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_outlets_updated_by_tenant_user_id",
                table: "discount_policy_outlets");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_outlets_status",
                table: "discount_policy_outlets");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_conditions_created_by_tenant_user_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_conditions_tenant_id_discount_policy_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_conditions_updated_by_tenant_user_id",
                table: "discount_policy_conditions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_conditions_status",
                table: "discount_policy_conditions");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_channels_created_by_tenant_user_id",
                table: "discount_policy_channels");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_channels_tenant_id_sales_channel_id",
                table: "discount_policy_channels");

            migrationBuilder.DropIndex(
                name: "IX_discount_policy_channels_updated_by_tenant_user_id",
                table: "discount_policy_channels");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policy_channels_status",
                table: "discount_policy_channels");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_discount_policies_tenant_id_id",
                table: "discount_policies");

            migrationBuilder.DropIndex(
                name: "uq_discount_policies_tenant_id_id",
                table: "discount_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policies_amounts",
                table: "discount_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_policies_discount_scope",
                table: "discount_policies");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_collections_tenant_id_id",
                table: "collections");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_categories_tenant_id_id",
                table: "categories");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_brands_tenant_id_id",
                table: "brands");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "applied_from",
                table: "expiry_discount_applications",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rules_discount_policy_id",
                table: "expiry_discount_rules",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rule_tiers_expiry_discount_rule_id",
                table: "expiry_discount_rule_tiers",
                column: "expiry_discount_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_rule_tiers_tenant_id",
                table: "expiry_discount_rule_tiers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_expiry_discount_rule_id",
                table: "expiry_discount_applications",
                column: "expiry_discount_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_expiry_discount_rule_tier_id",
                table: "expiry_discount_applications",
                column: "expiry_discount_rule_tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_outlet_id",
                table: "expiry_discount_applications",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiry_discount_applications_product_batch_id",
                table: "expiry_discount_applications",
                column: "product_batch_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_types_calculation_method",
                table: "discount_types",
                sql: "calculation_method IN ('PERCENTAGE', 'FIXED_AMOUNT', 'FIXED_PRICE', 'BUY_X_GET_Y')");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_brand_id",
                table: "discount_policy_targets",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_category_id",
                table: "discount_policy_targets",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_collection_id",
                table: "discount_policy_targets",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_discount_policy_id",
                table: "discount_policy_targets",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_product_id",
                table: "discount_policy_targets",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_product_variant_id",
                table: "discount_policy_targets",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_targets_tenant_id",
                table: "discount_policy_targets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_outlets_discount_policy_id",
                table: "discount_policy_outlets",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_outlets_outlet_id",
                table: "discount_policy_outlets",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_conditions_discount_policy_id",
                table: "discount_policy_conditions",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_conditions_tenant_id",
                table: "discount_policy_conditions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_channels_discount_policy_id",
                table: "discount_policy_channels",
                column: "discount_policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_policy_channels_sales_channel_id",
                table: "discount_policy_channels",
                column: "sales_channel_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_policies_amounts",
                table: "discount_policies",
                sql: "(max_discount_amount IS NULL OR max_discount_amount >= 0) AND (min_order_amount IS NULL OR min_order_amount >= 0) AND (min_quantity IS NULL OR min_quantity >= 0)");

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_channels_discount_policy_id_discount_policies",
                table: "discount_policy_channels",
                column: "discount_policy_id",
                principalTable: "discount_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_channels_sales_channel_id_sales_channels",
                table: "discount_policy_channels",
                column: "sales_channel_id",
                principalTable: "sales_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_conditions_discount_policy_id_discount_policies",
                table: "discount_policy_conditions",
                column: "discount_policy_id",
                principalTable: "discount_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_outlets_discount_policy_id_discount_policies",
                table: "discount_policy_outlets",
                column: "discount_policy_id",
                principalTable: "discount_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_outlets_outlet_id_outlets",
                table: "discount_policy_outlets",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_brand_id_brands",
                table: "discount_policy_targets",
                column: "brand_id",
                principalTable: "brands",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_category_id_categories",
                table: "discount_policy_targets",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_collection_id_collections",
                table: "discount_policy_targets",
                column: "collection_id",
                principalTable: "collections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_discount_policy_id_discount_policies",
                table: "discount_policy_targets",
                column: "discount_policy_id",
                principalTable: "discount_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_product_id_products",
                table: "discount_policy_targets",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_policy_targets_product_variant_id_product_variants",
                table: "discount_policy_targets",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_id_expiry_discount_rules",
                table: "expiry_discount_applications",
                column: "expiry_discount_rule_id",
                principalTable: "expiry_discount_rules",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_expiry_discount_rule_tier_id_expiry_discount_rule_tiers",
                table: "expiry_discount_applications",
                column: "expiry_discount_rule_tier_id",
                principalTable: "expiry_discount_rule_tiers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_outlet_id_outlets",
                table: "expiry_discount_applications",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_applications_product_batch_id_product_batches",
                table: "expiry_discount_applications",
                column: "product_batch_id",
                principalTable: "product_batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rule_tiers_expiry_discount_rule_id_expiry_discount_rules",
                table: "expiry_discount_rule_tiers",
                column: "expiry_discount_rule_id",
                principalTable: "expiry_discount_rules",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_expiry_discount_rules_discount_policy_id_discount_policies",
                table: "expiry_discount_rules",
                column: "discount_policy_id",
                principalTable: "discount_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
