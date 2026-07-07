using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHardwareCashEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_cash_movement_type_id_cash_movement_types",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_device_assignments_pos_device_id_pos_devices",
                table: "hardware_device_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_devices_outlet_id_outlets",
                table: "hardware_devices");

            migrationBuilder.DropIndex(
                name: "uq_till_sessions_tenant_id_id",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "uq_till_sessions_tenant_id_till_id_session_number",
                table: "till_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_sessions_closing_cash_amount",
                table: "till_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_hardware_test_logs_test_result",
                table: "hardware_test_logs");

            migrationBuilder.DropIndex(
                name: "IX_hardware_devices_outlet_id",
                table: "hardware_devices");

            migrationBuilder.DropIndex(
                name: "uq_hardware_devices_serial_number",
                table: "hardware_devices");

            migrationBuilder.DropIndex(
                name: "IX_hardware_device_assignments_pos_device_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropIndex(
                name: "uq_hardware_device_assignments_hardware_device_id_pos_device_id_effective_from",
                table: "hardware_device_assignments");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_cash_movement_type_id",
                table: "cash_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_movements_amount",
                table: "cash_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_movement_types_status",
                table: "cash_movement_types");

            migrationBuilder.DropIndex(
                name: "uq_cash_count_denominations_cash_reconciliation_id_denomination_value",
                table: "cash_count_denominations");

            migrationBuilder.DropColumn(
                name: "closing_cash_amount",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "test_result",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "name",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "name",
                table: "cash_movement_types");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "hardware_test_logs",
                newName: "tested_at");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "hardware_device_assignments",
                newName: "assigned_at");

            migrationBuilder.RenameColumn(
                name: "cash_movement_type_id",
                table: "cash_movements",
                newName: "till_id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "cash_count_denominations",
                newName: "counted_at");

            migrationBuilder.AlterColumn<Guid>(
                name: "till_id",
                table: "till_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "till_sessions",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<DateOnly>(
                name: "business_date",
                table: "till_sessions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "till_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "closed_by_tenant_user_id",
                table: "till_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "closed_from_pos_device_id",
                table: "till_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "closing_note",
                table: "till_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "till_sessions",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "opened_at",
                table: "till_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "opened_from_pos_device_id",
                table: "till_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "opening_float_amount",
                table: "till_sessions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "opening_note",
                table: "till_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "till_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "initiated_from_pos_device_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "result_message",
                table: "hardware_test_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "result_payload_json",
                table: "hardware_test_logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "test_status",
                table: "hardware_test_logs",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "test_type",
                table: "hardware_test_logs",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "tested_by_tenant_user_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "hardware_devices",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "serial_number",
                table: "hardware_devices",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "asset_tag",
                table: "hardware_devices",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "config_json",
                table: "hardware_devices",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "connection_type",
                table: "hardware_devices",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_tenant_user_id",
                table: "hardware_devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "firmware_version",
                table: "hardware_devices",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hardware_device_name",
                table: "hardware_devices",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "hardware_device_type",
                table: "hardware_devices",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "hardware_profile_id",
                table: "hardware_devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_seen_at",
                table: "hardware_devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manufacturer",
                table: "hardware_devices",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model",
                table: "hardware_devices",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_tenant_user_id",
                table: "hardware_devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_tenant_user_id",
                table: "hardware_device_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_primary",
                table: "hardware_device_assignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "hardware_device_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "release_reason",
                table: "hardware_device_assignments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "released_at",
                table: "hardware_device_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "released_by_tenant_user_id",
                table: "hardware_device_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "hardware_device_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "till_id",
                table: "hardware_device_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "expected_cash_amount",
                table: "cash_reconciliations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "counted_cash_amount",
                table: "cash_reconciliations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "approval_note",
                table: "cash_reconciliations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                table: "cash_reconciliations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_tenant_user_id",
                table: "cash_reconciliations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "calculation_details_json",
                table: "cash_reconciliations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "cash_reconciliations",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "difference_amount",
                table: "cash_reconciliations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "difference_reason",
                table: "cash_reconciliations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reconciliation_number",
                table: "cash_reconciliations",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "reconciliation_status",
                table: "cash_reconciliations",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "submitted_at",
                table: "cash_reconciliations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submitted_by_tenant_user_id",
                table: "cash_reconciliations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "cash_reconciliations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "cash_movements",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "cash_movements",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "movement_number",
                table: "cash_movements",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "movement_type_id",
                table: "cash_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "order_id",
                table: "cash_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "outlet_id",
                table: "cash_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "payment_id",
                table: "cash_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "performed_at",
                table: "cash_movements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "performed_by_tenant_user_id",
                table: "cash_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "pos_device_id",
                table: "cash_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "cash_movements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "refund_id",
                table: "cash_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "cash_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "cash_movement_types",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "cash_movement_types",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "movement_type_code",
                table: "cash_movement_types",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<bool>(
                name: "affects_expected_cash",
                table: "cash_movement_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "direction",
                table: "cash_movement_types",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_system_type",
                table: "cash_movement_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "movement_type_name",
                table: "cash_movement_types",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "requires_reason",
                table: "cash_movement_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "quantity",
                table: "cash_count_denominations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "denomination_value",
                table: "cash_count_denominations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "count_type",
                table: "cash_count_denominations",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "counted_by_tenant_user_id",
                table: "cash_count_denominations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                table: "cash_count_denominations",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "line_total",
                table: "cash_count_denominations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "cash_count_denominations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_hardware_profiles_tenant_id_id",
                table: "hardware_profiles",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_till_sessions_closed_by_tenant_user_id",
                table: "till_sessions",
                column: "closed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_till_sessions_currency_code",
                table: "till_sessions",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_till_sessions_tenant_id_closed_from_pos_device_id",
                table: "till_sessions",
                columns: new[] { "tenant_id", "closed_from_pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_till_sessions_tenant_id_opened_from_pos_device_id",
                table: "till_sessions",
                columns: new[] { "tenant_id", "opened_from_pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_till_sessions_tenant_id_outlet_id",
                table: "till_sessions",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_till_sessions_tenant_id_till_id",
                table: "till_sessions",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "uq_till_sessions_tenant_id_session_number",
                table: "till_sessions",
                columns: new[] { "tenant_id", "session_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_till_sessions_till_id_active",
                table: "till_sessions",
                column: "till_id",
                unique: true,
                filter: "closed_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_sessions_opening_float_amount",
                table: "till_sessions",
                sql: "opening_float_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_sessions_status",
                table: "till_sessions",
                sql: "status <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_test_logs_tenant_id_initiated_from_pos_device_id",
                table: "hardware_test_logs",
                columns: new[] { "tenant_id", "initiated_from_pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_test_logs_tenant_id_outlet_id",
                table: "hardware_test_logs",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_test_logs_tested_by_tenant_user_id",
                table: "hardware_test_logs",
                column: "tested_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_hardware_test_logs_test_status",
                table: "hardware_test_logs",
                sql: "test_status <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "ck_hardware_test_logs_test_type",
                table: "hardware_test_logs",
                sql: "test_type <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_devices_created_by_tenant_user_id",
                table: "hardware_devices",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_devices_tenant_id_hardware_profile_id",
                table: "hardware_devices",
                columns: new[] { "tenant_id", "hardware_profile_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_devices_tenant_id_outlet_id",
                table: "hardware_devices",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_devices_updated_by_tenant_user_id",
                table: "hardware_devices",
                column: "updated_by_tenant_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_hardware_devices_connection_type",
                table: "hardware_devices",
                sql: "connection_type <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "ck_hardware_devices_device_type",
                table: "hardware_devices",
                sql: "hardware_device_type <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "ck_hardware_devices_status",
                table: "hardware_devices",
                sql: "status <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_device_assignments_assigned_by_tenant_user_id",
                table: "hardware_device_assignments",
                column: "assigned_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_device_assignments_released_by_tenant_user_id",
                table: "hardware_device_assignments",
                column: "released_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_device_assignments_tenant_id_outlet_id",
                table: "hardware_device_assignments",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_device_assignments_tenant_id_pos_device_id",
                table: "hardware_device_assignments",
                columns: new[] { "tenant_id", "pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_device_assignments_tenant_id_till_id",
                table: "hardware_device_assignments",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "uq_hardware_device_assignments_hardware_device_id_active",
                table: "hardware_device_assignments",
                column: "hardware_device_id",
                unique: true,
                filter: "released_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_hardware_device_assignments_target",
                table: "hardware_device_assignments",
                sql: "num_nonnulls(till_id, pos_device_id) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_cash_reconciliations_approved_by_tenant_user_id",
                table: "cash_reconciliations",
                column: "approved_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_reconciliations_currency_code",
                table: "cash_reconciliations",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_cash_reconciliations_submitted_by_tenant_user_id",
                table: "cash_reconciliations",
                column: "submitted_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_cash_reconciliations_tenant_id_reconciliation_number",
                table: "cash_reconciliations",
                columns: new[] { "tenant_id", "reconciliation_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_reconciliations_status",
                table: "cash_reconciliations",
                sql: "reconciliation_status <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_currency_code",
                table: "cash_movements",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_movement_type_id",
                table: "cash_movements",
                column: "movement_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_order_id",
                table: "cash_movements",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_payment_id",
                table: "cash_movements",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_performed_by_tenant_user_id",
                table: "cash_movements",
                column: "performed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_refund_id",
                table: "cash_movements",
                column: "refund_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_tenant_id_outlet_id",
                table: "cash_movements",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_tenant_id_pos_device_id",
                table: "cash_movements",
                columns: new[] { "tenant_id", "pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_tenant_id_till_id",
                table: "cash_movements",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "uq_cash_movements_tenant_id_movement_number",
                table: "cash_movements",
                columns: new[] { "tenant_id", "movement_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_movements_amount",
                table: "cash_movements",
                sql: "amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_movement_types_direction",
                table: "cash_movement_types",
                sql: "direction <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_movement_types_status",
                table: "cash_movement_types",
                sql: "status <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_cash_count_denominations_counted_by_tenant_user_id",
                table: "cash_count_denominations",
                column: "counted_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_count_denominations_currency_code",
                table: "cash_count_denominations",
                column: "currency_code");

            migrationBuilder.CreateIndex(
                name: "IX_cash_count_denominations_tenant_id",
                table: "cash_count_denominations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_cash_count_denominations_reconciliation_type_currency_value",
                table: "cash_count_denominations",
                columns: new[] { "cash_reconciliation_id", "count_type", "currency_code", "denomination_value" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_count_denominations_count_type",
                table: "cash_count_denominations",
                sql: "count_type <> ''");

            migrationBuilder.AddForeignKey(
                name: "fk_cash_count_denominations_counted_by_tenant_user_id_tenant_users",
                table: "cash_count_denominations",
                column: "counted_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_count_denominations_currency_code_currencies",
                table: "cash_count_denominations",
                column: "currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_count_denominations_tenant_id_tenants",
                table: "cash_count_denominations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movement_types_tenant_id_tenants",
                table: "cash_movement_types",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_currency_code_currencies",
                table: "cash_movements",
                column: "currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_movement_type_id_cash_movement_types",
                table: "cash_movements",
                column: "movement_type_id",
                principalTable: "cash_movement_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_order_id_sales_orders",
                table: "cash_movements",
                column: "order_id",
                principalTable: "sales_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_outlet_id_outlets",
                table: "cash_movements",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_payment_id_sales_payments",
                table: "cash_movements",
                column: "payment_id",
                principalTable: "sales_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_performed_by_tenant_user_id_tenant_users",
                table: "cash_movements",
                column: "performed_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_pos_device_id_pos_devices",
                table: "cash_movements",
                columns: new[] { "tenant_id", "pos_device_id" },
                principalTable: "pos_devices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_refund_id_refunds",
                table: "cash_movements",
                column: "refund_id",
                principalTable: "sales_refunds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_tenant_id_tenants",
                table: "cash_movements",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_till_id_tills",
                table: "cash_movements",
                columns: new[] { "tenant_id", "till_id" },
                principalTable: "tills",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_reconciliations_approved_by_tenant_user_id_tenant_users",
                table: "cash_reconciliations",
                column: "approved_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_reconciliations_currency_code_currencies",
                table: "cash_reconciliations",
                column: "currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_reconciliations_submitted_by_tenant_user_id_tenant_users",
                table: "cash_reconciliations",
                column: "submitted_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_reconciliations_tenant_id_tenants",
                table: "cash_reconciliations",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_device_assignments_assigned_by_tenant_user_id_tenant_users",
                table: "hardware_device_assignments",
                column: "assigned_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_device_assignments_outlet_id_outlets",
                table: "hardware_device_assignments",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_device_assignments_pos_device_id_pos_devices",
                table: "hardware_device_assignments",
                columns: new[] { "tenant_id", "pos_device_id" },
                principalTable: "pos_devices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_device_assignments_released_by_tenant_user_id_tenant_users",
                table: "hardware_device_assignments",
                column: "released_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_device_assignments_tenant_id_tenants",
                table: "hardware_device_assignments",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_device_assignments_till_id_tills",
                table: "hardware_device_assignments",
                columns: new[] { "tenant_id", "till_id" },
                principalTable: "tills",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_devices_created_by_tenant_user_id_tenant_users",
                table: "hardware_devices",
                column: "created_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_devices_hardware_profile_id_hardware_profiles",
                table: "hardware_devices",
                columns: new[] { "tenant_id", "hardware_profile_id" },
                principalTable: "hardware_profiles",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_devices_outlet_id_outlets",
                table: "hardware_devices",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_devices_tenant_id_tenants",
                table: "hardware_devices",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_devices_updated_by_tenant_user_id_tenant_users",
                table: "hardware_devices",
                column: "updated_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_test_logs_initiated_from_pos_device_id_pos_devices",
                table: "hardware_test_logs",
                columns: new[] { "tenant_id", "initiated_from_pos_device_id" },
                principalTable: "pos_devices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_test_logs_outlet_id_outlets",
                table: "hardware_test_logs",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_test_logs_tenant_id_tenants",
                table: "hardware_test_logs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_test_logs_tested_by_tenant_user_id_tenant_users",
                table: "hardware_test_logs",
                column: "tested_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_sessions_closed_by_tenant_user_id_tenant_users",
                table: "till_sessions",
                column: "closed_by_tenant_user_id",
                principalTable: "tenant_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_sessions_closed_from_pos_device_id_pos_devices",
                table: "till_sessions",
                columns: new[] { "tenant_id", "closed_from_pos_device_id" },
                principalTable: "pos_devices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_sessions_currency_code_currencies",
                table: "till_sessions",
                column: "currency_code",
                principalTable: "currencies",
                principalColumn: "currency_code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_sessions_opened_from_pos_device_id_pos_devices",
                table: "till_sessions",
                columns: new[] { "tenant_id", "opened_from_pos_device_id" },
                principalTable: "pos_devices",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_sessions_outlet_id_outlets",
                table: "till_sessions",
                columns: new[] { "tenant_id", "outlet_id" },
                principalTable: "outlets",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_till_sessions_tenant_id_tenants",
                table: "till_sessions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cash_count_denominations_counted_by_tenant_user_id_tenant_users",
                table: "cash_count_denominations");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_count_denominations_currency_code_currencies",
                table: "cash_count_denominations");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_count_denominations_tenant_id_tenants",
                table: "cash_count_denominations");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movement_types_tenant_id_tenants",
                table: "cash_movement_types");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_currency_code_currencies",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_movement_type_id_cash_movement_types",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_order_id_sales_orders",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_outlet_id_outlets",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_payment_id_sales_payments",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_performed_by_tenant_user_id_tenant_users",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_pos_device_id_pos_devices",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_refund_id_refunds",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_tenant_id_tenants",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_movements_till_id_tills",
                table: "cash_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_reconciliations_approved_by_tenant_user_id_tenant_users",
                table: "cash_reconciliations");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_reconciliations_currency_code_currencies",
                table: "cash_reconciliations");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_reconciliations_submitted_by_tenant_user_id_tenant_users",
                table: "cash_reconciliations");

            migrationBuilder.DropForeignKey(
                name: "fk_cash_reconciliations_tenant_id_tenants",
                table: "cash_reconciliations");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_device_assignments_assigned_by_tenant_user_id_tenant_users",
                table: "hardware_device_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_device_assignments_outlet_id_outlets",
                table: "hardware_device_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_device_assignments_pos_device_id_pos_devices",
                table: "hardware_device_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_device_assignments_released_by_tenant_user_id_tenant_users",
                table: "hardware_device_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_device_assignments_tenant_id_tenants",
                table: "hardware_device_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_device_assignments_till_id_tills",
                table: "hardware_device_assignments");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_devices_created_by_tenant_user_id_tenant_users",
                table: "hardware_devices");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_devices_hardware_profile_id_hardware_profiles",
                table: "hardware_devices");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_devices_outlet_id_outlets",
                table: "hardware_devices");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_devices_tenant_id_tenants",
                table: "hardware_devices");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_devices_updated_by_tenant_user_id_tenant_users",
                table: "hardware_devices");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_test_logs_initiated_from_pos_device_id_pos_devices",
                table: "hardware_test_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_test_logs_outlet_id_outlets",
                table: "hardware_test_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_test_logs_tenant_id_tenants",
                table: "hardware_test_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_test_logs_tested_by_tenant_user_id_tenant_users",
                table: "hardware_test_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_till_sessions_closed_by_tenant_user_id_tenant_users",
                table: "till_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_till_sessions_closed_from_pos_device_id_pos_devices",
                table: "till_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_till_sessions_currency_code_currencies",
                table: "till_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_till_sessions_opened_from_pos_device_id_pos_devices",
                table: "till_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_till_sessions_outlet_id_outlets",
                table: "till_sessions");

            migrationBuilder.DropForeignKey(
                name: "fk_till_sessions_tenant_id_tenants",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "IX_till_sessions_closed_by_tenant_user_id",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "IX_till_sessions_currency_code",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "IX_till_sessions_tenant_id_closed_from_pos_device_id",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "IX_till_sessions_tenant_id_opened_from_pos_device_id",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "IX_till_sessions_tenant_id_outlet_id",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "IX_till_sessions_tenant_id_till_id",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "uq_till_sessions_tenant_id_session_number",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "uq_till_sessions_till_id_active",
                table: "till_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_sessions_opening_float_amount",
                table: "till_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_till_sessions_status",
                table: "till_sessions");

            migrationBuilder.DropIndex(
                name: "IX_hardware_test_logs_tenant_id_initiated_from_pos_device_id",
                table: "hardware_test_logs");

            migrationBuilder.DropIndex(
                name: "IX_hardware_test_logs_tenant_id_outlet_id",
                table: "hardware_test_logs");

            migrationBuilder.DropIndex(
                name: "IX_hardware_test_logs_tested_by_tenant_user_id",
                table: "hardware_test_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_hardware_test_logs_test_status",
                table: "hardware_test_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_hardware_test_logs_test_type",
                table: "hardware_test_logs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_hardware_profiles_tenant_id_id",
                table: "hardware_profiles");

            migrationBuilder.DropIndex(
                name: "IX_hardware_devices_created_by_tenant_user_id",
                table: "hardware_devices");

            migrationBuilder.DropIndex(
                name: "IX_hardware_devices_tenant_id_hardware_profile_id",
                table: "hardware_devices");

            migrationBuilder.DropIndex(
                name: "IX_hardware_devices_tenant_id_outlet_id",
                table: "hardware_devices");

            migrationBuilder.DropIndex(
                name: "IX_hardware_devices_updated_by_tenant_user_id",
                table: "hardware_devices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_hardware_devices_connection_type",
                table: "hardware_devices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_hardware_devices_device_type",
                table: "hardware_devices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_hardware_devices_status",
                table: "hardware_devices");

            migrationBuilder.DropIndex(
                name: "IX_hardware_device_assignments_assigned_by_tenant_user_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropIndex(
                name: "IX_hardware_device_assignments_released_by_tenant_user_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropIndex(
                name: "IX_hardware_device_assignments_tenant_id_outlet_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropIndex(
                name: "IX_hardware_device_assignments_tenant_id_pos_device_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropIndex(
                name: "IX_hardware_device_assignments_tenant_id_till_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropIndex(
                name: "uq_hardware_device_assignments_hardware_device_id_active",
                table: "hardware_device_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_hardware_device_assignments_target",
                table: "hardware_device_assignments");

            migrationBuilder.DropIndex(
                name: "IX_cash_reconciliations_approved_by_tenant_user_id",
                table: "cash_reconciliations");

            migrationBuilder.DropIndex(
                name: "IX_cash_reconciliations_currency_code",
                table: "cash_reconciliations");

            migrationBuilder.DropIndex(
                name: "IX_cash_reconciliations_submitted_by_tenant_user_id",
                table: "cash_reconciliations");

            migrationBuilder.DropIndex(
                name: "uq_cash_reconciliations_tenant_id_reconciliation_number",
                table: "cash_reconciliations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_reconciliations_status",
                table: "cash_reconciliations");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_currency_code",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_movement_type_id",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_order_id",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_payment_id",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_performed_by_tenant_user_id",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_refund_id",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_tenant_id_outlet_id",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_tenant_id_pos_device_id",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "IX_cash_movements_tenant_id_till_id",
                table: "cash_movements");

            migrationBuilder.DropIndex(
                name: "uq_cash_movements_tenant_id_movement_number",
                table: "cash_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_movements_amount",
                table: "cash_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_movement_types_direction",
                table: "cash_movement_types");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_movement_types_status",
                table: "cash_movement_types");

            migrationBuilder.DropIndex(
                name: "IX_cash_count_denominations_counted_by_tenant_user_id",
                table: "cash_count_denominations");

            migrationBuilder.DropIndex(
                name: "IX_cash_count_denominations_currency_code",
                table: "cash_count_denominations");

            migrationBuilder.DropIndex(
                name: "IX_cash_count_denominations_tenant_id",
                table: "cash_count_denominations");

            migrationBuilder.DropIndex(
                name: "uq_cash_count_denominations_reconciliation_type_currency_value",
                table: "cash_count_denominations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cash_count_denominations_count_type",
                table: "cash_count_denominations");

            migrationBuilder.DropColumn(
                name: "business_date",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "closed_by_tenant_user_id",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "closed_from_pos_device_id",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "closing_note",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "opened_at",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "opened_from_pos_device_id",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "opening_float_amount",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "opening_note",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "till_sessions");

            migrationBuilder.DropColumn(
                name: "initiated_from_pos_device_id",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "result_message",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "result_payload_json",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "test_status",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "test_type",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "tested_by_tenant_user_id",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "asset_tag",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "config_json",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "connection_type",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "created_by_tenant_user_id",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "firmware_version",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "hardware_device_name",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "hardware_device_type",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "hardware_profile_id",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "last_seen_at",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "manufacturer",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "model",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "updated_by_tenant_user_id",
                table: "hardware_devices");

            migrationBuilder.DropColumn(
                name: "assigned_by_tenant_user_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "is_primary",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "release_reason",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "released_at",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "released_by_tenant_user_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "till_id",
                table: "hardware_device_assignments");

            migrationBuilder.DropColumn(
                name: "approval_note",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "approved_by_tenant_user_id",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "calculation_details_json",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "difference_amount",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "difference_reason",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "reconciliation_number",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "reconciliation_status",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "submitted_at",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "submitted_by_tenant_user_id",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "cash_reconciliations");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "movement_number",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "movement_type_id",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "order_id",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "payment_id",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "performed_at",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "performed_by_tenant_user_id",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "pos_device_id",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "refund_id",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "cash_movements");

            migrationBuilder.DropColumn(
                name: "affects_expected_cash",
                table: "cash_movement_types");

            migrationBuilder.DropColumn(
                name: "direction",
                table: "cash_movement_types");

            migrationBuilder.DropColumn(
                name: "is_system_type",
                table: "cash_movement_types");

            migrationBuilder.DropColumn(
                name: "movement_type_name",
                table: "cash_movement_types");

            migrationBuilder.DropColumn(
                name: "requires_reason",
                table: "cash_movement_types");

            migrationBuilder.DropColumn(
                name: "count_type",
                table: "cash_count_denominations");

            migrationBuilder.DropColumn(
                name: "counted_by_tenant_user_id",
                table: "cash_count_denominations");

            migrationBuilder.DropColumn(
                name: "currency_code",
                table: "cash_count_denominations");

            migrationBuilder.DropColumn(
                name: "line_total",
                table: "cash_count_denominations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "cash_count_denominations");

            migrationBuilder.RenameColumn(
                name: "tested_at",
                table: "hardware_test_logs",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "assigned_at",
                table: "hardware_device_assignments",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "till_id",
                table: "cash_movements",
                newName: "cash_movement_type_id");

            migrationBuilder.RenameColumn(
                name: "counted_at",
                table: "cash_count_denominations",
                newName: "updated_at");

            migrationBuilder.AlterColumn<Guid>(
                name: "till_id",
                table: "till_sessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "till_sessions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<decimal>(
                name: "closing_cash_amount",
                table: "till_sessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "test_result",
                table: "hardware_test_logs",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "hardware_devices",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "serial_number",
                table: "hardware_devices",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "hardware_devices",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "hardware_device_assignments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "effective_from",
                table: "hardware_device_assignments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "expected_cash_amount",
                table: "cash_reconciliations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "counted_cash_amount",
                table: "cash_reconciliations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "cash_movements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "cash_movement_types",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "cash_movement_types",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "movement_type_code",
                table: "cash_movement_types",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "cash_movement_types",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "cash_count_denominations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "denomination_value",
                table: "cash_count_denominations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.CreateIndex(
                name: "uq_till_sessions_tenant_id_id",
                table: "till_sessions",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_till_sessions_tenant_id_till_id_session_number",
                table: "till_sessions",
                columns: new[] { "tenant_id", "till_id", "session_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_till_sessions_closing_cash_amount",
                table: "till_sessions",
                sql: "closing_cash_amount IS NULL OR closing_cash_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_hardware_test_logs_test_result",
                table: "hardware_test_logs",
                sql: "test_result IN ('SUCCESS', 'FAILED', 'WARNING')");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_devices_outlet_id",
                table: "hardware_devices",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "uq_hardware_devices_serial_number",
                table: "hardware_devices",
                column: "serial_number",
                unique: true,
                filter: "serial_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_device_assignments_pos_device_id",
                table: "hardware_device_assignments",
                column: "pos_device_id");

            migrationBuilder.CreateIndex(
                name: "uq_hardware_device_assignments_hardware_device_id_pos_device_id_effective_from",
                table: "hardware_device_assignments",
                columns: new[] { "hardware_device_id", "pos_device_id", "effective_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cash_movements_cash_movement_type_id",
                table: "cash_movements",
                column: "cash_movement_type_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_movements_amount",
                table: "cash_movements",
                sql: "amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cash_movement_types_status",
                table: "cash_movement_types",
                sql: "status IN ('ACTIVE', 'INACTIVE', 'DELETED')");

            migrationBuilder.CreateIndex(
                name: "uq_cash_count_denominations_cash_reconciliation_id_denomination_value",
                table: "cash_count_denominations",
                columns: new[] { "cash_reconciliation_id", "denomination_value" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_cash_movements_cash_movement_type_id_cash_movement_types",
                table: "cash_movements",
                column: "cash_movement_type_id",
                principalTable: "cash_movement_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_device_assignments_pos_device_id_pos_devices",
                table: "hardware_device_assignments",
                column: "pos_device_id",
                principalTable: "pos_devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_devices_outlet_id_outlets",
                table: "hardware_devices",
                column: "outlet_id",
                principalTable: "outlets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
