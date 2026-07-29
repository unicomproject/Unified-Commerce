using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardwareChunk1Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_hardware_test_logs_tenant_id_initiated_from_pos_device_id",
                table: "hardware_test_logs");

            migrationBuilder.AlterColumn<Guid>(
                name: "hardware_device_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "hardware_test_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "configuration_version",
                table: "hardware_test_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "hardware_type",
                table: "hardware_test_logs",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "UNKNOWN");

            migrationBuilder.AddColumn<bool>(
                name: "physical_confirmation",
                table: "hardware_test_logs",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "request_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_payload_hash",
                table: "hardware_test_logs",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "LEGACY");

            migrationBuilder.AddColumn<string>(
                name: "result_category",
                table: "hardware_test_logs",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "till_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "till_session_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "configuration_version",
                table: "hardware_devices",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE hardware_test_logs
                SET request_id = gen_random_uuid()
                WHERE request_id IS NULL;

                UPDATE hardware_test_logs AS log
                SET hardware_type = device.hardware_device_type
                FROM hardware_devices AS device
                WHERE log.hardware_device_id = device.id
                  AND log.hardware_type = 'UNKNOWN';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "request_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "hardware_configuration_change_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pos_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hardware_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_id = table.Column<Guid>(type: "uuid", nullable: true),
                    till_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    old_version = table.Column<int>(type: "integer", nullable: false),
                    new_version = table.Column<int>(type: "integer", nullable: false),
                    change_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    change_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    safe_before_json = table.Column<string>(type: "jsonb", nullable: false),
                    safe_after_json = table.Column<string>(type: "jsonb", nullable: false),
                    changed_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hardware_configuration_change_audits", x => x.id);
                    table.ForeignKey(
                        name: "FK_hardware_configuration_change_audits_hardware_devices_hardw~",
                        column: x => x.hardware_device_id,
                        principalTable: "hardware_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hardware_configuration_change_audits_outlets_tenant_id_outl~",
                        columns: x => new { x.tenant_id, x.outlet_id },
                        principalTable: "outlets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hardware_configuration_change_audits_pos_devices_tenant_id_~",
                        columns: x => new { x.tenant_id, x.pos_device_id },
                        principalTable: "pos_devices",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hardware_configuration_change_audits_tenant_users_changed_b~",
                        column: x => x.changed_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hardware_configuration_change_audits_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hardware_configuration_change_audits_till_sessions_till_ses~",
                        column: x => x.till_session_id,
                        principalTable: "till_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_hardware_configuration_change_audits_tills_tenant_id_till_id",
                        columns: x => new { x.tenant_id, x.till_id },
                        principalTable: "tills",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hardware_test_logs_device_history",
                table: "hardware_test_logs",
                columns: new[] { "tenant_id", "initiated_from_pos_device_id", "tested_at" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_test_logs_tenant_id_till_id",
                table: "hardware_test_logs",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_test_logs_till_session_id",
                table: "hardware_test_logs",
                column: "till_session_id");

            migrationBuilder.CreateIndex(
                name: "uq_hardware_test_logs_tenant_id_request_id",
                table: "hardware_test_logs",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hardware_configuration_change_audits_changed_by_tenant_user~",
                table: "hardware_configuration_change_audits",
                column: "changed_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_configuration_change_audits_hardware_device_id",
                table: "hardware_configuration_change_audits",
                column: "hardware_device_id");

            migrationBuilder.CreateIndex(
                name: "IX_hardware_configuration_change_audits_tenant_id_outlet_id",
                table: "hardware_configuration_change_audits",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_configuration_change_audits_tenant_id_pos_device_id",
                table: "hardware_configuration_change_audits",
                columns: new[] { "tenant_id", "pos_device_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_configuration_change_audits_tenant_id_till_id",
                table: "hardware_configuration_change_audits",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "IX_hardware_configuration_change_audits_till_session_id",
                table: "hardware_configuration_change_audits",
                column: "till_session_id");

            migrationBuilder.CreateIndex(
                name: "uq_hardware_configuration_audit_version",
                table: "hardware_configuration_change_audits",
                columns: new[] { "tenant_id", "hardware_device_id", "new_version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_test_logs_till_id_tills",
                table: "hardware_test_logs",
                columns: new[] { "tenant_id", "till_id" },
                principalTable: "tills",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_hardware_test_logs_till_session_id_till_sessions",
                table: "hardware_test_logs",
                column: "till_session_id",
                principalTable: "till_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_hardware_test_logs_till_id_tills",
                table: "hardware_test_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_hardware_test_logs_till_session_id_till_sessions",
                table: "hardware_test_logs");

            migrationBuilder.DropTable(
                name: "hardware_configuration_change_audits");

            migrationBuilder.DropIndex(
                name: "ix_hardware_test_logs_device_history",
                table: "hardware_test_logs");

            migrationBuilder.DropIndex(
                name: "IX_hardware_test_logs_tenant_id_till_id",
                table: "hardware_test_logs");

            migrationBuilder.DropIndex(
                name: "IX_hardware_test_logs_till_session_id",
                table: "hardware_test_logs");

            migrationBuilder.DropIndex(
                name: "uq_hardware_test_logs_tenant_id_request_id",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "configuration_version",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "hardware_type",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "physical_confirmation",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "request_payload_hash",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "result_category",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "till_id",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "till_session_id",
                table: "hardware_test_logs");

            migrationBuilder.DropColumn(
                name: "configuration_version",
                table: "hardware_devices");

            migrationBuilder.AlterColumn<Guid>(
                name: "hardware_device_id",
                table: "hardware_test_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_hardware_test_logs_tenant_id_initiated_from_pos_device_id",
                table: "hardware_test_logs",
                columns: new[] { "tenant_id", "initiated_from_pos_device_id" });
        }
    }
}
