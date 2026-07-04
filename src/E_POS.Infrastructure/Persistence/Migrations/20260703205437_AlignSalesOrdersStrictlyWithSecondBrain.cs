using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignSalesOrdersStrictlyWithSecondBrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sales_orders_document_number_sequence_id_document_number_sequences",
                table: "sales_orders");

            migrationBuilder.DropTable(
                name: "sales_order_charges");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_document_number_sequence_id",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "document_number_sequence_id",
                table: "sales_orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "document_number_sequence_id",
                table: "sales_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "sales_order_charges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_order_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_order_charges", x => x.id);
                    table.CheckConstraint("ck_sales_order_charges_charge_amount", "charge_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sales_order_charges_sales_order_id_sales_orders",
                        column: x => x.sales_order_id,
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_order_charges_sales_order_line_id_sales_order_lines",
                        column: x => x.sales_order_line_id,
                        principalTable: "sales_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_document_number_sequence_id",
                table: "sales_orders",
                column: "document_number_sequence_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_charges_sales_order_id",
                table: "sales_order_charges",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_charges_sales_order_line_id",
                table: "sales_order_charges",
                column: "sales_order_line_id");

            migrationBuilder.AddForeignKey(
                name: "fk_sales_orders_document_number_sequence_id_document_number_sequences",
                table: "sales_orders",
                column: "document_number_sequence_id",
                principalTable: "document_number_sequences",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
