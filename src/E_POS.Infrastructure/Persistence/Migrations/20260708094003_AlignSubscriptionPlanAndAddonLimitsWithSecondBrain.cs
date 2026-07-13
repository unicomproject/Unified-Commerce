using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignSubscriptionPlanAndAddonLimitsWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM subscription_plan_feature_limits spfl
                        LEFT JOIN subscription_plan_features spf
                            ON spfl.subscription_plan_feature_id = spf.id
                        WHERE spf.id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Orphan subscription_plan_feature_limits rows detected.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM subscription_addon_limits sal
                        LEFT JOIN subscription_addon_features saf
                            ON sal.subscription_addon_feature_id = saf.id
                        WHERE saf.id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Orphan subscription_addon_limits rows detected.';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM platform_features WHERE feature_code = 'outlet_management'
                    ) THEN
                        RAISE EXCEPTION 'Required platform feature outlet_management was not found.';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM platform_features WHERE feature_code = 'user_accounts'
                    ) THEN
                        RAISE EXCEPTION 'Required platform feature user_accounts was not found.';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM platform_features WHERE feature_code = 'till_management'
                    ) THEN
                        RAISE EXCEPTION 'Required platform feature till_management was not found.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                INSERT INTO feature_limit_definitions (
                    id,
                    limit_code,
                    name,
                    default_limit_value,
                    platform_feature_id,
                    created_at,
                    updated_at,
                    limit_key,
                    limit_name,
                    value_type,
                    unit_code,
                    is_hard_limit,
                    status)
                SELECT
                    '73000000-0000-0000-0000-000000000001',
                    'max_outlets',
                    'Maximum Outlets',
                    NULL,
                    pf.id,
                    now(),
                    now(),
                    'max_outlets',
                    'Maximum Outlets',
                    'INTEGER',
                    'outlet',
                    true,
                    'ACTIVE'
                FROM platform_features pf
                WHERE pf.feature_code = 'outlet_management'
                  AND NOT EXISTS (
                      SELECT 1 FROM feature_limit_definitions fld
                      WHERE fld.id = '73000000-0000-0000-0000-000000000001'
                         OR (fld.platform_feature_id = pf.id AND fld.limit_key = 'max_outlets')
                  );

                INSERT INTO feature_limit_definitions (
                    id,
                    limit_code,
                    name,
                    default_limit_value,
                    platform_feature_id,
                    created_at,
                    updated_at,
                    limit_key,
                    limit_name,
                    value_type,
                    unit_code,
                    is_hard_limit,
                    status)
                SELECT
                    '73000000-0000-0000-0000-000000000002',
                    'max_users',
                    'Maximum Users',
                    NULL,
                    pf.id,
                    now(),
                    now(),
                    'max_users',
                    'Maximum Users',
                    'INTEGER',
                    'user',
                    true,
                    'ACTIVE'
                FROM platform_features pf
                WHERE pf.feature_code = 'user_accounts'
                  AND NOT EXISTS (
                      SELECT 1 FROM feature_limit_definitions fld
                      WHERE fld.id = '73000000-0000-0000-0000-000000000002'
                         OR (fld.platform_feature_id = pf.id AND fld.limit_key = 'max_users')
                  );

                INSERT INTO feature_limit_definitions (
                    id,
                    limit_code,
                    name,
                    default_limit_value,
                    platform_feature_id,
                    created_at,
                    updated_at,
                    limit_key,
                    limit_name,
                    value_type,
                    unit_code,
                    is_hard_limit,
                    status)
                SELECT
                    '73000000-0000-0000-0000-000000000003',
                    'max_tills',
                    'Maximum Tills',
                    NULL,
                    pf.id,
                    now(),
                    now(),
                    'max_tills',
                    'Maximum Tills',
                    'INTEGER',
                    'till',
                    true,
                    'ACTIVE'
                FROM platform_features pf
                WHERE pf.feature_code = 'till_management'
                  AND NOT EXISTS (
                      SELECT 1 FROM feature_limit_definitions fld
                      WHERE fld.id = '73000000-0000-0000-0000-000000000003'
                         OR (fld.platform_feature_id = pf.id AND fld.limit_key = 'max_tills')
                  );
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_plan_feature_limits_subscription_plan_feature_id_subscription_plan_features",
                table: "subscription_plan_feature_limits");

            migrationBuilder.DropIndex(
                name: "uq_subscription_plan_feature_limits_subscription_plan_feature_id_feature_limit_definition_id",
                table: "subscription_plan_feature_limits");

            migrationBuilder.AddColumn<Guid>(
                name: "subscription_plan_id",
                table: "subscription_plan_feature_limits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_unlimited",
                table: "subscription_plan_feature_limits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE subscription_plan_feature_limits spfl
                SET subscription_plan_id = spf.subscription_plan_id
                FROM subscription_plan_features spf
                WHERE spfl.subscription_plan_feature_id = spf.id;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM subscription_plan_feature_limits
                        WHERE subscription_plan_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'subscription_plan_feature_limits rows could not be mapped to subscription_plan_id.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "subscription_plan_feature_id",
                table: "subscription_plan_feature_limits");

            migrationBuilder.AlterColumn<Guid>(
                name: "subscription_plan_id",
                table: "subscription_plan_feature_limits",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "limit_value",
                table: "subscription_plan_feature_limits",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_subscription_plan_feature_limits_subscription_plan_id_feature_limit_definition_id",
                table: "subscription_plan_feature_limits",
                columns: new[] { "subscription_plan_id", "feature_limit_definition_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_plan_feature_limits_subscription_plan_id_subscription_plans",
                table: "subscription_plan_feature_limits",
                column: "subscription_plan_id",
                principalTable: "subscription_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO subscription_plan_feature_limits (
                    id,
                    subscription_plan_id,
                    feature_limit_definition_id,
                    limit_value,
                    is_unlimited,
                    created_at,
                    updated_at)
                SELECT
                    gen_random_uuid(),
                    p.id,
                    '73000000-0000-0000-0000-000000000001',
                    p.max_outlets,
                    false,
                    now(),
                    now()
                FROM subscription_plans p
                WHERE p.max_outlets IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM subscription_plan_feature_limits spfl
                      WHERE spfl.subscription_plan_id = p.id
                        AND spfl.feature_limit_definition_id = '73000000-0000-0000-0000-000000000001'
                  );

                INSERT INTO subscription_plan_feature_limits (
                    id,
                    subscription_plan_id,
                    feature_limit_definition_id,
                    limit_value,
                    is_unlimited,
                    created_at,
                    updated_at)
                SELECT
                    gen_random_uuid(),
                    p.id,
                    '73000000-0000-0000-0000-000000000002',
                    p.max_users,
                    false,
                    now(),
                    now()
                FROM subscription_plans p
                WHERE p.max_users IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM subscription_plan_feature_limits spfl
                      WHERE spfl.subscription_plan_id = p.id
                        AND spfl.feature_limit_definition_id = '73000000-0000-0000-0000-000000000002'
                  );

                INSERT INTO subscription_plan_feature_limits (
                    id,
                    subscription_plan_id,
                    feature_limit_definition_id,
                    limit_value,
                    is_unlimited,
                    created_at,
                    updated_at)
                SELECT
                    gen_random_uuid(),
                    p.id,
                    '73000000-0000-0000-0000-000000000003',
                    p.max_tills,
                    false,
                    now(),
                    now()
                FROM subscription_plans p
                WHERE p.max_tills IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM subscription_plan_feature_limits spfl
                      WHERE spfl.subscription_plan_id = p.id
                        AND spfl.feature_limit_definition_id = '73000000-0000-0000-0000-000000000003'
                  );
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_addon_limits_subscription_addon_feature_id_subscription_addon_features",
                table: "subscription_addon_limits");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_addon_limits_limit_value",
                table: "subscription_addon_limits");

            migrationBuilder.DropIndex(
                name: "uq_subscription_addon_limits_subscription_addon_feature_id_feature_limit_definition_id",
                table: "subscription_addon_limits");

            migrationBuilder.AddColumn<Guid>(
                name: "subscription_addon_id",
                table: "subscription_addon_limits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "increment_value",
                table: "subscription_addon_limits",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE subscription_addon_limits sal
                SET subscription_addon_id = saf.subscription_addon_id,
                    increment_value = COALESCE(sal.limit_value, 1)
                FROM subscription_addon_features saf
                WHERE sal.subscription_addon_feature_id = saf.id;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM subscription_addon_limits
                        WHERE subscription_addon_id IS NULL OR increment_value IS NULL
                    ) THEN
                        RAISE EXCEPTION 'subscription_addon_limits rows could not be mapped to subscription_addon_id/increment_value.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM subscription_addon_limits
                        GROUP BY subscription_addon_id, feature_limit_definition_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate subscription_addon_limits rows detected for (subscription_addon_id, feature_limit_definition_id).';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM subscription_plan_feature_limits
                        GROUP BY subscription_plan_id, feature_limit_definition_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate subscription_plan_feature_limits rows detected for (subscription_plan_id, feature_limit_definition_id).';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "limit_value",
                table: "subscription_addon_limits");

            migrationBuilder.DropColumn(
                name: "subscription_addon_feature_id",
                table: "subscription_addon_limits");

            migrationBuilder.AlterColumn<Guid>(
                name: "subscription_addon_id",
                table: "subscription_addon_limits",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "increment_value",
                table: "subscription_addon_limits",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_subscription_addon_limits_subscription_addon_id_feature_limit_definition_id",
                table: "subscription_addon_limits",
                columns: new[] { "subscription_addon_id", "feature_limit_definition_id" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_addon_limits_increment_value",
                table: "subscription_addon_limits",
                sql: "increment_value > 0");

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_addon_limits_subscription_addon_id_subscription_addons",
                table: "subscription_addon_limits",
                column: "subscription_addon_id",
                principalTable: "subscription_addons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO subscription_addon_limits (
                    id,
                    subscription_addon_id,
                    feature_limit_definition_id,
                    increment_value,
                    created_at,
                    updated_at)
                SELECT
                    '74000000-0000-0000-0000-000000000001',
                    sa.id,
                    '73000000-0000-0000-0000-000000000001',
                    1,
                    now(),
                    now()
                FROM subscription_addons sa
                WHERE sa.addon_code = 'extra_outlet'
                  AND NOT EXISTS (
                      SELECT 1 FROM subscription_addon_limits sal
                      WHERE sal.subscription_addon_id = sa.id
                        AND sal.feature_limit_definition_id = '73000000-0000-0000-0000-000000000001'
                  );

                INSERT INTO subscription_addon_limits (
                    id,
                    subscription_addon_id,
                    feature_limit_definition_id,
                    increment_value,
                    created_at,
                    updated_at)
                SELECT
                    '74000000-0000-0000-0000-000000000002',
                    sa.id,
                    '73000000-0000-0000-0000-000000000002',
                    1,
                    now(),
                    now()
                FROM subscription_addons sa
                WHERE sa.addon_code = 'extra_user'
                  AND NOT EXISTS (
                      SELECT 1 FROM subscription_addon_limits sal
                      WHERE sal.subscription_addon_id = sa.id
                        AND sal.feature_limit_definition_id = '73000000-0000-0000-0000-000000000002'
                  );

                INSERT INTO subscription_addon_limits (
                    id,
                    subscription_addon_id,
                    feature_limit_definition_id,
                    increment_value,
                    created_at,
                    updated_at)
                SELECT
                    '74000000-0000-0000-0000-000000000003',
                    sa.id,
                    '73000000-0000-0000-0000-000000000003',
                    1,
                    now(),
                    now()
                FROM subscription_addons sa
                WHERE sa.addon_code = 'extra_till'
                  AND NOT EXISTS (
                      SELECT 1 FROM subscription_addon_limits sal
                      WHERE sal.subscription_addon_id = sa.id
                        AND sal.feature_limit_definition_id = '73000000-0000-0000-0000-000000000003'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_subscription_addon_limits_subscription_addon_id_subscription_addons",
                table: "subscription_addon_limits");

            migrationBuilder.DropForeignKey(
                name: "fk_subscription_plan_feature_limits_subscription_plan_id_subscription_plans",
                table: "subscription_plan_feature_limits");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscription_addon_limits_increment_value",
                table: "subscription_addon_limits");

            migrationBuilder.DropIndex(
                name: "uq_subscription_addon_limits_subscription_addon_id_feature_limit_definition_id",
                table: "subscription_addon_limits");

            migrationBuilder.DropIndex(
                name: "uq_subscription_plan_feature_limits_subscription_plan_id_feature_limit_definition_id",
                table: "subscription_plan_feature_limits");

            migrationBuilder.DropColumn(
                name: "increment_value",
                table: "subscription_addon_limits");

            migrationBuilder.DropColumn(
                name: "subscription_addon_id",
                table: "subscription_addon_limits");

            migrationBuilder.AddColumn<int>(
                name: "limit_value",
                table: "subscription_addon_limits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "subscription_addon_feature_id",
                table: "subscription_addon_limits",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.DropColumn(
                name: "is_unlimited",
                table: "subscription_plan_feature_limits");

            migrationBuilder.DropColumn(
                name: "subscription_plan_id",
                table: "subscription_plan_feature_limits");

            migrationBuilder.AddColumn<Guid>(
                name: "subscription_plan_feature_id",
                table: "subscription_plan_feature_limits",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AlterColumn<int>(
                name: "limit_value",
                table: "subscription_plan_feature_limits",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscription_addon_limits_limit_value",
                table: "subscription_addon_limits",
                sql: "limit_value IS NULL OR limit_value >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_subscription_addon_limits_subscription_addon_feature_id_feature_limit_definition_id",
                table: "subscription_addon_limits",
                columns: new[] { "subscription_addon_feature_id", "feature_limit_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_subscription_plan_feature_limits_subscription_plan_feature_id_feature_limit_definition_id",
                table: "subscription_plan_feature_limits",
                columns: new[] { "subscription_plan_feature_id", "feature_limit_definition_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_addon_limits_subscription_addon_feature_id_subscription_addon_features",
                table: "subscription_addon_limits",
                column: "subscription_addon_feature_id",
                principalTable: "subscription_addon_features",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_subscription_plan_feature_limits_subscription_plan_feature_id_subscription_plan_features",
                table: "subscription_plan_feature_limits",
                column: "subscription_plan_feature_id",
                principalTable: "subscription_plan_features",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
