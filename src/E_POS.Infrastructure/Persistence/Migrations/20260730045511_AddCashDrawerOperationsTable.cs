using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashDrawerOperationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cash_drawer_operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hardware_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pos_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_id = table.Column<Guid>(type: "uuid", nullable: false),
                    till_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    drawer_purpose = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    business_reference_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    business_reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration_version = table.Column<int>(type: "integer", nullable: false),
                    drawer_port = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    pulse_on_time = table.Column<int>(type: "integer", nullable: false),
                    pulse_off_time = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    result_category = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    failure_category = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    agent_accepted = table.Column<bool>(type: "boolean", nullable: false),
                    physical_confirmation = table.Column<bool>(type: "boolean", nullable: true),
                    initiated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payload_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_drawer_operations", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_drawer_operations_approver_id_tenant_users",
                        column: x => x.approver_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_drawer_operations_hardware_device_id_hardware_devices",
                        column: x => x.hardware_device_id,
                        principalTable: "hardware_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_drawer_operations_outlet_id_outlets",
                        columns: x => new { x.tenant_id, x.outlet_id },
                        principalTable: "outlets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_drawer_operations_pos_device_id_pos_devices",
                        columns: x => new { x.tenant_id, x.pos_device_id },
                        principalTable: "pos_devices",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_drawer_operations_processed_by_user_id_tenant_users",
                        column: x => x.processed_by_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_drawer_operations_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_drawer_operations_till_id_tills",
                        columns: x => new { x.tenant_id, x.till_id },
                        principalTable: "tills",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_drawer_operations_till_session_id_till_sessions",
                        column: x => x.till_session_id,
                        principalTable: "till_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cash_drawer_operations_approver_id",
                table: "cash_drawer_operations",
                column: "approver_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_drawer_operations_device_history",
                table: "cash_drawer_operations",
                columns: new[] { "tenant_id", "pos_device_id", "initiated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_drawer_operations_hardware_device_id",
                table: "cash_drawer_operations",
                column: "hardware_device_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_drawer_operations_processed_by_user_id",
                table: "cash_drawer_operations",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_drawer_operations_tenant_id_outlet_id",
                table: "cash_drawer_operations",
                columns: new[] { "tenant_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_drawer_operations_tenant_id_till_id",
                table: "cash_drawer_operations",
                columns: new[] { "tenant_id", "till_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_drawer_operations_till_session_id",
                table: "cash_drawer_operations",
                column: "till_session_id");

            migrationBuilder.CreateIndex(
                name: "uq_cash_drawer_operations_tenant_id_request_id",
                table: "cash_drawer_operations",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_drawer_operations");
        }
    }
}
