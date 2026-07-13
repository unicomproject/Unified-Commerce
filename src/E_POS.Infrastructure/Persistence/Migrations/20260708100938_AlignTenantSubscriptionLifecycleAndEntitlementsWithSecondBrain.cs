using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignTenantSubscriptionLifecycleAndEntitlementsWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_platform_user_id",
                table: "tenant_subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "tenant_subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "tenant_subscriptions",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "current_period_end",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "current_period_start",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ended_at",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_billing_date",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "plan_id",
                table: "tenant_subscriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "plan_price",
                table: "tenant_subscriptions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "started_at",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tenant_subscriptions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "trial_ends_at",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "trial_started_at",
                table: "tenant_subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "change_data",
                table: "tenant_subscription_history",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "change_type",
                table: "tenant_subscription_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "changed_at",
                table: "tenant_subscription_history",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "changed_by_platform_user_id",
                table: "tenant_subscription_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "new_plan_id",
                table: "tenant_subscription_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "new_status",
                table: "tenant_subscription_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "old_plan_id",
                table: "tenant_subscription_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_status",
                table: "tenant_subscription_history",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "tenant_subscription_history",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "subscription_id",
                table: "tenant_subscription_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "tenant_subscription_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "addon_id",
                table: "tenant_subscription_addons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "auto_renew",
                table: "tenant_subscription_addons",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "tenant_subscription_addons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "tenant_subscription_addons",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ends_at",
                table: "tenant_subscription_addons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "starts_at",
                table: "tenant_subscription_addons",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "subscription_id",
                table: "tenant_subscription_addons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "unit_price",
                table: "tenant_subscription_addons",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "tenant_subscription_addons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_platform_user_id",
                table: "tenant_feature_entitlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_from",
                table: "tenant_feature_entitlements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "effective_until",
                table: "tenant_feature_entitlements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "feature_id",
                table: "tenant_feature_entitlements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "tenant_feature_entitlements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                table: "tenant_feature_entitlements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "revoked_by_platform_user_id",
                table: "tenant_feature_entitlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                table: "tenant_feature_entitlements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_reference_id",
                table: "tenant_feature_entitlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                table: "tenant_feature_entitlements",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_platform_user_id",
                table: "tenant_feature_entitlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE tenant_subscriptions ts
                SET plan_id = ts.subscription_plan_id,
                    status = ts.subscription_status,
                    currency_code = COALESCE(NULLIF(sp.base_currency_code, ''), NULLIF(sp.base_currency, ''), 'LKR'),
                    plan_price = COALESCE(sp.base_price, sp.price_amount, 0),
                    started_at = COALESCE(ts.billing_start_at, ts.created_at),
                    current_period_start = COALESCE(ts.billing_start_at, ts.created_at),
                    current_period_end = NULL,
                    next_billing_date = ts.next_billing_at,
                    trial_started_at = ts.trial_start_at,
                    trial_ends_at = ts.trial_end_at
                FROM subscription_plans sp
                WHERE ts.subscription_plan_id = sp.id;

                UPDATE tenant_subscriptions
                SET plan_id = subscription_plan_id,
                    status = subscription_status,
                    currency_code = COALESCE(NULLIF(currency_code, ''), 'LKR'),
                    plan_price = COALESCE(plan_price, 0),
                    started_at = COALESCE(started_at, billing_start_at, created_at),
                    current_period_start = COALESCE(current_period_start, billing_start_at, created_at),
                    next_billing_date = COALESCE(next_billing_date, next_billing_at),
                    trial_started_at = COALESCE(trial_started_at, trial_start_at),
                    trial_ends_at = COALESCE(trial_ends_at, trial_end_at)
                WHERE plan_id = '00000000-0000-0000-0000-000000000000'
                   OR COALESCE(currency_code, '') = ''
                   OR started_at = TIMESTAMPTZ '0001-01-01 00:00:00+00'
                   OR current_period_start = TIMESTAMPTZ '0001-01-01 00:00:00+00';

                UPDATE tenant_subscription_addons tsa
                SET subscription_id = tsa.tenant_subscription_id,
                    addon_id = tsa.subscription_addon_id,
                    unit_price = COALESCE(sa.base_price, sa.price_amount, 0),
                    currency_code = COALESCE(NULLIF(sa.base_currency_code, ''), 'LKR'),
                    auto_renew = true,
                    starts_at = COALESCE(tsa.created_at, now()),
                    ends_at = NULL
                FROM subscription_addons sa
                WHERE tsa.subscription_addon_id = sa.id;

                UPDATE tenant_subscription_addons
                SET subscription_id = tenant_subscription_id,
                    addon_id = subscription_addon_id,
                    unit_price = COALESCE(unit_price, 0),
                    currency_code = COALESCE(NULLIF(currency_code, ''), 'LKR'),
                    starts_at = COALESCE(starts_at, created_at, now())
                WHERE subscription_id = '00000000-0000-0000-0000-000000000000'
                   OR addon_id = '00000000-0000-0000-0000-000000000000'
                   OR COALESCE(currency_code, '') = ''
                   OR starts_at = TIMESTAMPTZ '0001-01-01 00:00:00+00';

                UPDATE tenant_feature_entitlements
                SET feature_id = platform_feature_id,
                    source_type = 'MANUAL',
                    source_reference_id = NULL,
                    is_enabled = (UPPER(entitlement_status) = 'ENABLED'),
                    effective_from = created_at,
                    effective_until = NULL,
                    revoked_at = NULL,
                    revoked_by_platform_user_id = NULL,
                    revoked_reason = NULL
                WHERE feature_id = '00000000-0000-0000-0000-000000000000'
                   OR COALESCE(source_type, '') = ''
                   OR effective_from = TIMESTAMPTZ '0001-01-01 00:00:00+00';

                UPDATE tenant_subscription_history tsh
                SET subscription_id = tsh.tenant_subscription_id,
                    tenant_id = ts.tenant_id,
                    change_type = COALESCE(NULLIF(tsh.change_type, ''), 'LEGACY'),
                    changed_at = COALESCE(tsh.updated_at, tsh.created_at, now()),
                    old_plan_id = NULL,
                    new_plan_id = ts.subscription_plan_id,
                    old_status = NULL,
                    new_status = ts.subscription_status
                FROM tenant_subscriptions ts
                WHERE tsh.tenant_subscription_id = ts.id;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_assigned_by_platform_user_id",
                table: "tenant_subscriptions",
                column: "assigned_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_subscriptions_tenant_id_status",
                table: "tenant_subscriptions",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_subscriptions_plan_price",
                table: "tenant_subscriptions",
                sql: "plan_price >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_history_changed_by_platform_user_id",
                table: "tenant_subscription_history",
                column: "changed_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_history_new_plan_id",
                table: "tenant_subscription_history",
                column: "new_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_history_old_plan_id",
                table: "tenant_subscription_history",
                column: "old_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_history_subscription_id",
                table: "tenant_subscription_history",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_history_tenant_id",
                table: "tenant_subscription_history",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_addons_created_by_platform_user_id",
                table: "tenant_subscription_addons",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_addons_updated_by_platform_user_id",
                table: "tenant_subscription_addons",
                column: "updated_by_platform_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_subscription_addons_quantity",
                table: "tenant_subscription_addons",
                sql: "quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_subscription_addons_unit_price",
                table: "tenant_subscription_addons",
                sql: "unit_price >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_feature_entitlements_created_by_platform_user_id",
                table: "tenant_feature_entitlements",
                column: "created_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_feature_entitlements_revoked_by_platform_user_id",
                table: "tenant_feature_entitlements",
                column: "revoked_by_platform_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_feature_entitlements_updated_by_platform_user_id",
                table: "tenant_feature_entitlements",
                column: "updated_by_platform_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_feature_entitlements_effective_dates",
                table: "tenant_feature_entitlements",
                sql: "effective_until IS NULL OR effective_until > effective_from");

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_feature_entitlements_created_by_platform_user_id_platform_users",
                table: "tenant_feature_entitlements",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_feature_entitlements_revoked_by_platform_user_id_platform_users",
                table: "tenant_feature_entitlements",
                column: "revoked_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_feature_entitlements_updated_by_platform_user_id_platform_users",
                table: "tenant_feature_entitlements",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_subscription_addons_created_by_platform_user_id_platform_users",
                table: "tenant_subscription_addons",
                column: "created_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_subscription_addons_updated_by_platform_user_id_platform_users",
                table: "tenant_subscription_addons",
                column: "updated_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_subscription_history_changed_by_platform_user_id_platform_users",
                table: "tenant_subscription_history",
                column: "changed_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_subscription_history_new_plan_id_subscription_plans",
                table: "tenant_subscription_history",
                column: "new_plan_id",
                principalTable: "subscription_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_subscription_history_old_plan_id_subscription_plans",
                table: "tenant_subscription_history",
                column: "old_plan_id",
                principalTable: "subscription_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_subscription_history_subscription_id_tenant_subscriptions",
                table: "tenant_subscription_history",
                column: "subscription_id",
                principalTable: "tenant_subscriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_subscription_history_tenant_id_tenants",
                table: "tenant_subscription_history",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_subscriptions_assigned_by_platform_user_id_platform_users",
                table: "tenant_subscriptions",
                column: "assigned_by_platform_user_id",
                principalTable: "platform_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tenant_feature_entitlements_created_by_platform_user_id_platform_users",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_feature_entitlements_revoked_by_platform_user_id_platform_users",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_feature_entitlements_updated_by_platform_user_id_platform_users",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_subscription_addons_created_by_platform_user_id_platform_users",
                table: "tenant_subscription_addons");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_subscription_addons_updated_by_platform_user_id_platform_users",
                table: "tenant_subscription_addons");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_subscription_history_changed_by_platform_user_id_platform_users",
                table: "tenant_subscription_history");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_subscription_history_new_plan_id_subscription_plans",
                table: "tenant_subscription_history");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_subscription_history_old_plan_id_subscription_plans",
                table: "tenant_subscription_history");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_subscription_history_subscription_id_tenant_subscriptions",
                table: "tenant_subscription_history");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_subscription_history_tenant_id_tenants",
                table: "tenant_subscription_history");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_subscriptions_assigned_by_platform_user_id_platform_users",
                table: "tenant_subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscriptions_assigned_by_platform_user_id",
                table: "tenant_subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_tenant_subscriptions_tenant_id_status",
                table: "tenant_subscriptions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_subscriptions_plan_price",
                table: "tenant_subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscription_history_changed_by_platform_user_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscription_history_new_plan_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscription_history_old_plan_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscription_history_subscription_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscription_history_tenant_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscription_addons_created_by_platform_user_id",
                table: "tenant_subscription_addons");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscription_addons_updated_by_platform_user_id",
                table: "tenant_subscription_addons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_subscription_addons_quantity",
                table: "tenant_subscription_addons");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_subscription_addons_unit_price",
                table: "tenant_subscription_addons");

            migrationBuilder.DropIndex(
                name: "IX_tenant_feature_entitlements_created_by_platform_user_id",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropIndex(
                name: "IX_tenant_feature_entitlements_revoked_by_platform_user_id",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropIndex(
                name: "IX_tenant_feature_entitlements_updated_by_platform_user_id",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_feature_entitlements_effective_dates",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "assigned_by_platform_user_id",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "current_period_end",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "current_period_start",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "ended_at",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "next_billing_date",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "plan_id",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "plan_price",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "trial_ends_at",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "trial_started_at",
                table: "tenant_subscriptions");

            migrationBuilder.DropColumn(
                name: "change_data",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "change_type",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "changed_at",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "changed_by_platform_user_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "new_plan_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "new_status",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "old_plan_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "old_status",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "subscription_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "tenant_subscription_history");

            migrationBuilder.DropColumn(
                name: "addon_id",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "auto_renew",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "ends_at",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "starts_at",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "subscription_id",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "tenant_subscription_addons");

            migrationBuilder.DropColumn(
                name: "created_by_platform_user_id",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "effective_until",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "feature_id",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "revoked_at",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "revoked_by_platform_user_id",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "source_reference_id",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "tenant_feature_entitlements");

            migrationBuilder.DropColumn(
                name: "updated_by_platform_user_id",
                table: "tenant_feature_entitlements");
        }
    }
}
