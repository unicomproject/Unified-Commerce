using System;
using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteOnlineOrderPrepareFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentPosOnlineOrderPermissionsSeedData.UpSql);
            migrationBuilder.Sql(DevelopmentPosOnlineOrderPermissionsSeedData.CashierAssignmentUpSql);
            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "pickup_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "row_version",
                table: "fulfillment_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "inventory_reservation_line_id",
                table: "fulfillment_order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fulfillment_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    staging_inventory_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    package_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    packed_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    packed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ready_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    packing_note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fulfillment_packages", x => x.id);
                    table.CheckConstraint("ck_fulfillment_packages_status", "package_status IN ('OPEN','PACKED','READY','HANDED_OVER','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_fulfillment_packages_fulfillment_orders_fulfillment_order_id",
                        column: x => x.fulfillment_order_id,
                        principalTable: "fulfillment_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fulfillment_package_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fulfillment_package_lines", x => x.id);
                    table.CheckConstraint("ck_fulfillment_package_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_fulfillment_package_lines_fulfillment_order_lines_fulfillme~",
                        column: x => x.fulfillment_order_line_id,
                        principalTable: "fulfillment_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fulfillment_package_lines_fulfillment_packages_fulfillment_~",
                        column: x => x.fulfillment_package_id,
                        principalTable: "fulfillment_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_order_lines_inventory_reservation_line_id",
                table: "fulfillment_order_lines",
                column: "inventory_reservation_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_package_lines_fulfillment_order_line_id",
                table: "fulfillment_package_lines",
                column: "fulfillment_order_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_package_lines_fulfillment_package_id",
                table: "fulfillment_package_lines",
                column: "fulfillment_package_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_package_lines_tenant_id_fulfillment_package_id_~",
                table: "fulfillment_package_lines",
                columns: new[] { "tenant_id", "fulfillment_package_id", "fulfillment_order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_packages_fulfillment_order_id",
                table: "fulfillment_packages",
                column: "fulfillment_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_packages_tenant_id_fulfillment_order_id_package~",
                table: "fulfillment_packages",
                columns: new[] { "tenant_id", "fulfillment_order_id", "package_number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_fulfillment_order_lines_reservation_line",
                table: "fulfillment_order_lines",
                column: "inventory_reservation_line_id",
                principalTable: "inventory_reservation_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentPosOnlineOrderPermissionsSeedData.CashierAssignmentDownSql);
            migrationBuilder.Sql(DevelopmentPosOnlineOrderPermissionsSeedData.DownSql);
            migrationBuilder.DropForeignKey(
                name: "fk_fulfillment_order_lines_reservation_line",
                table: "fulfillment_order_lines");

            migrationBuilder.DropTable(
                name: "fulfillment_package_lines");

            migrationBuilder.DropTable(
                name: "fulfillment_packages");

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_order_lines_inventory_reservation_line_id",
                table: "fulfillment_order_lines");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "pickup_orders");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "fulfillment_orders");

            migrationBuilder.DropColumn(
                name: "inventory_reservation_line_id",
                table: "fulfillment_order_lines");
        }
    }
}
