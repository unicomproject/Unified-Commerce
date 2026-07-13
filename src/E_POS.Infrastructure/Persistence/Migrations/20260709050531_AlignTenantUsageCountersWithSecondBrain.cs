using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignTenantUsageCountersWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "current_value",
                table: "tenant_usage_counters",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "feature_limit_definition_id",
                table: "tenant_usage_counters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_calculated_at",
                table: "tenant_usage_counters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "limit_value",
                table: "tenant_usage_counters",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "period_end",
                table: "tenant_usage_counters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "period_start",
                table: "tenant_usage_counters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "scope_reference_id",
                table: "tenant_usage_counters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "tenant_usage_counters",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "usage_scope",
                table: "tenant_usage_counters",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE tenant_usage_counters
                SET
                    current_value = GREATEST(used_quantity::numeric(18,4), 0),
                    usage_scope = COALESCE(NULLIF(usage_scope, ''), 'TENANT'),
                    status = COALESCE(NULLIF(status, ''), 'ACTIVE'),
                    period_start = CASE
                        WHEN usage_period_start ~ '^\d{4}-\d{2}-\d{2}'
                            THEN usage_period_start::timestamptz
                        ELSE created_at
                    END
                WHERE current_value = 0
                   OR COALESCE(usage_scope, '') = ''
                   OR COALESCE(status, '') = ''
                   OR period_start < TIMESTAMPTZ '1970-01-02';

                UPDATE tenant_usage_counters tuc
                SET feature_limit_definition_id = match.feature_limit_definition_id
                FROM (
                    SELECT
                        tuc2.id AS counter_id,
                        fld.id AS feature_limit_definition_id
                    FROM tenant_usage_counters tuc2
                    INNER JOIN feature_limit_definitions fld
                        ON fld.platform_feature_id = tuc2.platform_feature_id
                        AND fld.status = 'ACTIVE'
                    WHERE tuc2.feature_limit_definition_id = '00000000-0000-0000-0000-000000000000'::uuid
                      AND (
                          SELECT COUNT(*)
                          FROM feature_limit_definitions fld2
                          WHERE fld2.platform_feature_id = tuc2.platform_feature_id
                            AND fld2.status = 'ACTIVE'
                      ) = 1
                ) match
                WHERE tuc.id = match.counter_id;

                DO $$
                DECLARE
                    unresolved RECORD;
                BEGIN
                    FOR unresolved IN
                        SELECT
                            tuc.id,
                            tuc.platform_feature_id,
                            COUNT(fld.id) AS active_limit_count
                        FROM tenant_usage_counters tuc
                        LEFT JOIN feature_limit_definitions fld
                            ON fld.platform_feature_id = tuc.platform_feature_id
                            AND fld.status = 'ACTIVE'
                        WHERE tuc.feature_limit_definition_id = '00000000-0000-0000-0000-000000000000'::uuid
                        GROUP BY tuc.id, tuc.platform_feature_id
                        HAVING COUNT(fld.id) <> 1
                    LOOP
                        RAISE EXCEPTION 'tenant_usage_counters row % with platform_feature_id % requires exactly one active feature_limit_definitions row, found %',
                            unresolved.id,
                            unresolved.platform_feature_id,
                            unresolved.active_limit_count;
                    END LOOP;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_usage_counters_feature_limit_definition_id",
                table: "tenant_usage_counters",
                column: "feature_limit_definition_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_usage_counters_current_value",
                table: "tenant_usage_counters",
                sql: "current_value >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_usage_counters_limit_value",
                table: "tenant_usage_counters",
                sql: "limit_value IS NULL OR limit_value >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_usage_counters_feature_limit_definition_id_feature_limit_definitions",
                table: "tenant_usage_counters",
                column: "feature_limit_definition_id",
                principalTable: "feature_limit_definitions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tenant_usage_counters_feature_limit_definition_id_feature_limit_definitions",
                table: "tenant_usage_counters");

            migrationBuilder.DropIndex(
                name: "ix_tenant_usage_counters_feature_limit_definition_id",
                table: "tenant_usage_counters");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_usage_counters_current_value",
                table: "tenant_usage_counters");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_usage_counters_limit_value",
                table: "tenant_usage_counters");

            migrationBuilder.DropColumn(
                name: "current_value",
                table: "tenant_usage_counters");

            migrationBuilder.DropColumn(
                name: "feature_limit_definition_id",
                table: "tenant_usage_counters");

            migrationBuilder.DropColumn(
                name: "last_calculated_at",
                table: "tenant_usage_counters");

            migrationBuilder.DropColumn(
                name: "limit_value",
                table: "tenant_usage_counters");

            migrationBuilder.DropColumn(
                name: "period_end",
                table: "tenant_usage_counters");

            migrationBuilder.DropColumn(
                name: "period_start",
                table: "tenant_usage_counters");

            migrationBuilder.DropColumn(
                name: "scope_reference_id",
                table: "tenant_usage_counters");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tenant_usage_counters");

            migrationBuilder.DropColumn(
                name: "usage_scope",
                table: "tenant_usage_counters");
        }
    }
}
