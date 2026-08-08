using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosHoldIdempotencyAndEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "pos_order_holds",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_fingerprint",
                table: "pos_order_holds",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_pos_order_holds_tenant_id_id",
                table: "pos_order_holds",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "pos_order_hold_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hold_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    event_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    event_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outlet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    till_id = table.Column<Guid>(type: "uuid", nullable: true),
                    till_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pos_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hold_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    previous_status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    new_status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    correlation_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pos_order_hold_events", x => x.id);
                    table.CheckConstraint("ck_pos_order_hold_events_event_type", "event_type IN ('PARK_CREATED', 'PARK_IDEMPOTENT_REPLAY', 'PARK_RECALLED', 'PARK_CANCELLED', 'PARK_EXPIRED')");
                    table.ForeignKey(
                        name: "fk_pos_order_hold_events_hold_id_pos_order_holds",
                        columns: x => new { x.tenant_id, x.hold_id },
                        principalTable: "pos_order_holds",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pos_order_hold_events_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_pos_order_holds_tenant_id_idempotency_key",
                table: "pos_order_holds",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pos_order_hold_events_tenant_id_hold_id",
                table: "pos_order_hold_events",
                columns: new[] { "tenant_id", "hold_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pos_order_hold_events");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_pos_order_holds_tenant_id_id",
                table: "pos_order_holds");

            migrationBuilder.DropIndex(
                name: "uq_pos_order_holds_tenant_id_idempotency_key",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "pos_order_holds");

            migrationBuilder.DropColumn(
                name: "request_fingerprint",
                table: "pos_order_holds");
        }
    }
}
