using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignSubscriptionCatalogFoundationWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "base_currency_code",
                table: "subscription_plans",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LKR");

            migrationBuilder.AddColumn<decimal>(
                name: "base_price",
                table: "subscription_plans",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "billing_cycle",
                table: "subscription_plans",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "MONTHLY");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "subscription_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_custom_plan",
                table: "subscription_plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                table: "subscription_plans",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "plan_name",
                table: "subscription_plans",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "trial_days",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "subscription_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "config_json",
                table: "subscription_plan_features",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "subscription_plan_features",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "subscription_plan_features",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_quantity",
                table: "subscription_plan_addons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_quantity",
                table: "subscription_plan_addons",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "addon_name",
                table: "subscription_addons",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "addon_type",
                table: "subscription_addons",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "CAPACITY");

            migrationBuilder.AddColumn<string>(
                name: "base_currency_code",
                table: "subscription_addons",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "LKR");

            migrationBuilder.AddColumn<decimal>(
                name: "base_price",
                table: "subscription_addons",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "billing_cycle",
                table: "subscription_addons",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "MONTHLY");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "subscription_addons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "quantity_based",
                table: "subscription_addons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "subscription_addons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "config_json",
                table: "subscription_addon_features",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_core_module",
                table: "platform_modules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "module_key",
                table: "platform_modules",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "module_name",
                table: "platform_modules",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "feature_key",
                table: "platform_features",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "feature_name",
                table: "platform_features",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_core_feature",
                table: "platform_features",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "default_limit_value",
                table: "feature_limit_definitions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_hard_limit",
                table: "feature_limit_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "limit_key",
                table: "feature_limit_definitions",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "limit_name",
                table: "feature_limit_definitions",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "feature_limit_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "ACTIVE");

            migrationBuilder.AddColumn<string>(
                name: "unit_code",
                table: "feature_limit_definitions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "value_type",
                table: "feature_limit_definitions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_created_by_platform_user_id",
                table: "subscription_plans",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_updated_by_platform_user_id",
                table: "subscription_plans",
                column: "updated_by_platform_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_plans_base_price",
                table: "subscription_plans",
                sql: "base_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_plans_trial_days",
                table: "subscription_plans",
                sql: "trial_days >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plan_features_created_by_platform_user_id",
                table: "subscription_plan_features",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plan_features_updated_by_platform_user_id",
                table: "subscription_plan_features",
                column: "updated_by_platform_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_plan_addons_max_quantity",
                table: "subscription_plan_addons",
                sql: "max_quantity IS NULL OR max_quantity >= min_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_plan_addons_min_quantity",
                table: "subscription_plan_addons",
                sql: "min_quantity >= 1");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_addons_created_by_platform_user_id",
                table: "subscription_addons",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_addons_updated_by_platform_user_id",
                table: "subscription_addons",
                column: "updated_by_platform_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_addons_base_price",
                table: "subscription_addons",
                sql: "base_price >= 0");

            migrationBuilder.Sql("""
                UPDATE platform_modules
                SET module_key = module_code,
                    module_name = name
                WHERE COALESCE(module_key, '') = '';

                UPDATE platform_modules
                SET is_core_module = true
                WHERE module_code IN ('core_commerce', 'authentication', 'tenant_management', 'user_management', 'role_permission_management', 'billing_core', 'notification_system', 'integration_core');

                UPDATE platform_features
                SET feature_key = feature_code,
                    feature_name = name
                WHERE COALESCE(feature_key, '') = '';

                UPDATE feature_limit_definitions
                SET limit_key = limit_code,
                    limit_name = name
                WHERE COALESCE(limit_key, '') = '';

                UPDATE subscription_plans
                SET plan_name = name,
                    billing_cycle = billing_interval,
                    base_currency_code = base_currency,
                    base_price = price_amount
                WHERE COALESCE(plan_name, '') = '';

                UPDATE subscription_addons
                SET addon_name = name,
                    base_currency_code = COALESCE(NULLIF(base_currency_code, ''), 'LKR'),
                    base_price = price_amount,
                    billing_cycle = COALESCE(NULLIF(billing_cycle, ''), 'MONTHLY'),
                    addon_type = COALESCE(NULLIF(addon_type, ''), 'CAPACITY')
                WHERE COALESCE(addon_name, '') = '';
                """);

            migrationBuilder.CreateIndex(
                name: "uq_platform_modules_module_key",
                table: "platform_modules",
                column: "module_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_platform_features_feature_key",
                table: "platform_features",
                column: "feature_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_feature_limit_definitions_platform_feature_id_limit_key",
                table: "feature_limit_definitions",
                columns: new[] { "platform_feature_id", "limit_key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_addons_created_by_platform_user_id_platform_users",
                table: "subscription_addons",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_addons_updated_by_platform_user_id_platform_users",
                table: "subscription_addons",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_plan_features_created_by_platform_user_id_platform_users",
                table: "subscription_plan_features",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_plan_features_updated_by_platform_user_id_platform_users",
                table: "subscription_plan_features",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_plans_created_by_platform_user_id_platform_users",
                table: "subscription_plans",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_plans_updated_by_platform_user_id_platform_users",
                table: "subscription_plans",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_subscription_addons_created_by_platform_user_id_platform_users",
                table: "subscription_addons");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_addons_updated_by_platform_user_id_platform_users",
                table: "subscription_addons");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_plan_features_created_by_platform_user_id_platform_users",
                table: "subscription_plan_features");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_plan_features_updated_by_platform_user_id_platform_users",
                table: "subscription_plan_features");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_plans_created_by_platform_user_id_platform_users",
                table: "subscription_plans");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_plans_updated_by_platform_user_id_platform_users",
                table: "subscription_plans");

            migrationBuilder.DropIndex(
                name: "IX_subscription_plans_created_by_platform_user_id",
                table: "subscription_plans");

            migrationBuilder.DropIndex(
                name: "IX_subscription_plans_updated_by_platform_user_id",
                table: "subscription_plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_plans_base_price",
                table: "subscription_plans");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_plans_trial_days",
                table: "subscription_plans");

            migrationBuilder.DropIndex(
                name: "IX_subscription_plan_features_created_by_platform_user_id",
                table: "subscription_plan_features");

            migrationBuilder.DropIndex(
                name: "IX_subscription_plan_features_updated_by_platform_user_id",
                table: "subscription_plan_features");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_plan_addons_max_quantity",
                table: "subscription_plan_addons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_plan_addons_min_quantity",
                table: "subscription_plan_addons");

            migrationBuilder.DropIndex(
                name: "IX_subscription_addons_created_by_platform_user_id",
                table: "subscription_addons");

            migrationBuilder.DropIndex(
                name: "IX_subscription_addons_updated_by_platform_user_id",
                table: "subscription_addons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_addons_base_price",
                table: "subscription_addons");

            migrationBuilder.DropIndex(
                name: "uq_platform_modules_module_key",
                table: "platform_modules");

            migrationBuilder.DropIndex(
                name: "uq_platform_features_feature_key",
                table: "platform_features");

            migrationBuilder.DropIndex(
                name: "uq_feature_limit_definitions_platform_feature_id_limit_key",
                table: "feature_limit_definitions");

            migrationBuilder.DropColumn(
                name: "base_currency_code",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "base_price",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "billing_cycle",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "is_custom_plan",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "is_public",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "plan_name",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "trial_days",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "config_json",
                table: "subscription_plan_features");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "subscription_plan_features");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "subscription_plan_features");

            migrationBuilder.DropColumn(
                name: "max_quantity",
                table: "subscription_plan_addons");

            migrationBuilder.DropColumn(
                name: "min_quantity",
                table: "subscription_plan_addons");

            migrationBuilder.DropColumn(
                name: "addon_name",
                table: "subscription_addons");

            migrationBuilder.DropColumn(
                name: "addon_type",
                table: "subscription_addons");

            migrationBuilder.DropColumn(
                name: "base_currency_code",
                table: "subscription_addons");

            migrationBuilder.DropColumn(
                name: "base_price",
                table: "subscription_addons");

            migrationBuilder.DropColumn(
                name: "billing_cycle",
                table: "subscription_addons");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "subscription_addons");

            migrationBuilder.DropColumn(
                name: "quantity_based",
                table: "subscription_addons");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "subscription_addons");

            migrationBuilder.DropColumn(
                name: "config_json",
                table: "subscription_addon_features");

            migrationBuilder.DropColumn(
                name: "is_core_module",
                table: "platform_modules");

            migrationBuilder.DropColumn(
                name: "module_key",
                table: "platform_modules");

            migrationBuilder.DropColumn(
                name: "module_name",
                table: "platform_modules");

            migrationBuilder.DropColumn(
                name: "feature_key",
                table: "platform_features");

            migrationBuilder.DropColumn(
                name: "feature_name",
                table: "platform_features");

            migrationBuilder.DropColumn(
                name: "is_core_feature",
                table: "platform_features");

            migrationBuilder.DropColumn(
                name: "is_hard_limit",
                table: "feature_limit_definitions");

            migrationBuilder.DropColumn(
                name: "limit_key",
                table: "feature_limit_definitions");

            migrationBuilder.DropColumn(
                name: "limit_name",
                table: "feature_limit_definitions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "feature_limit_definitions");

            migrationBuilder.DropColumn(
                name: "unit_code",
                table: "feature_limit_definitions");

            migrationBuilder.DropColumn(
                name: "value_type",
                table: "feature_limit_definitions");

            migrationBuilder.AlterColumn<int>(
                name: "default_limit_value",
                table: "feature_limit_definitions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);
        }
    }
}
