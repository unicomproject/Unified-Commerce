using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignOrdersCartCheckoutTablesWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_session_line_components");

            migrationBuilder.DropTable(
                name: "shopping_cart_item_components");

            migrationBuilder.CreateTable(
                name: "checkout_session_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    contact_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    contact_phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    address_line1 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    address_line2 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    state_or_province = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    country_code = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_session_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_checkout_session_addresses_checkout_session_id_checkout_sessions",
                        column: x => x.checkout_session_id,
                        principalTable: "checkout_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_checkout_session_addresses_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    customer_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    contact_phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    address_line1 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    address_line2 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    state_or_province = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    country_code = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_channel = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    order_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    prefix = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    current_value = table.Column<long>(type: "bigint", nullable: false),
                    reset_rule = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    last_generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                name: "IX_checkout_session_addresses_checkout_session_id",
                table: "checkout_session_addresses",
                column: "checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_addresses_tenant_id",
                table: "checkout_session_addresses",
                column: "tenant_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_session_addresses");

            migrationBuilder.DropTable(
                name: "sales_order_addresses");

            migrationBuilder.DropTable(
                name: "sales_order_number_sequences");

            migrationBuilder.CreateTable(
                name: "checkout_session_line_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_session_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_session_line_components", x => x.id);
                    table.CheckConstraint("ck_checkout_session_line_components_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_checkout_session_line_components_checkout_session_line_id_checkout_session_lines",
                        column: x => x.checkout_session_line_id,
                        principalTable: "checkout_session_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shopping_cart_item_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    shopping_cart_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shopping_cart_item_components", x => x.id);
                    table.CheckConstraint("ck_shopping_cart_item_components_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_shopping_cart_item_components_shopping_cart_item_id_shopping_cart_items",
                        column: x => x.shopping_cart_item_id,
                        principalTable: "shopping_cart_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checkout_session_line_components_checkout_session_line_id",
                table: "checkout_session_line_components",
                column: "checkout_session_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_shopping_cart_item_components_shopping_cart_item_id",
                table: "shopping_cart_item_components",
                column: "shopping_cart_item_id");
        }
    }
}
