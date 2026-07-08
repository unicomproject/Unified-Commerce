using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrdersEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sales_order_addresses");

            migrationBuilder.DropTable(
                name: "sales_order_number_sequences");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_lines_line_number",
                table: "sales_order_lines",
                sql: "line_number > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_lines_line_number",
                table: "sales_order_lines");

            migrationBuilder.CreateTable(
                name: "sales_order_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_line1 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    address_line2 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    address_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    city = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    contact_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    contact_phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    country_code = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    customer_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    postal_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_or_province = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_order_addresses_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_order_addresses_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_number_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    current_value = table.Column<long>(type: "bigint", nullable: false),
                    last_generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    order_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    prefix = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    reset_rule = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    sales_channel = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_number_sequences", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_order_number_sequences_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_addresses_sales_order_id",
                table: "sales_order_addresses",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_addresses_tenant_id",
                table: "sales_order_addresses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "uq_sales_order_number_sequences_tenant_id_sales_channel_order_type",
                table: "sales_order_number_sequences",
                columns: new[] { "tenant_id", "sales_channel", "order_type" },
                unique: true);
        }
    }
}
