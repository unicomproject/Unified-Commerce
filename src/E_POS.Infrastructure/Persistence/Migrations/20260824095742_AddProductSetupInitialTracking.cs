using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSetupInitialTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_setup_initial_tracking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initial_batch_number = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    initial_expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    initial_serial_number = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    assigned_product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    incompatible_clear_confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_setup_initial_tracking", x => x.id);
                    table.CheckConstraint("ck_product_setup_initial_tracking_row_version", "row_version >= 1");
                    table.ForeignKey(
                        name: "fk_product_setup_initial_tracking_assigned_variant",
                        columns: x => new { x.tenant_id, x.assigned_product_variant_id },
                        principalTable: "product_variants",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_setup_initial_tracking_created_by",
                        column: x => x.created_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_setup_initial_tracking_product_id_products",
                        columns: x => new { x.tenant_id, x.product_id },
                        principalTable: "products",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_setup_initial_tracking_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_setup_initial_tracking_updated_by",
                        column: x => x.updated_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_setup_initial_tracking_created_by_tenant_user_id",
                table: "product_setup_initial_tracking",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_setup_initial_tracking_tenant_id_assigned_product_v~",
                table: "product_setup_initial_tracking",
                columns: new[] { "tenant_id", "assigned_product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_setup_initial_tracking_tenant_id_consumed_at",
                table: "product_setup_initial_tracking",
                columns: new[] { "tenant_id", "consumed_at" },
                filter: "consumed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_setup_initial_tracking_updated_by_tenant_user_id",
                table: "product_setup_initial_tracking",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_setup_initial_tracking_tenant_id_id",
                table: "product_setup_initial_tracking",
                columns: new[] { "tenant_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_setup_initial_tracking_tenant_id_product_id",
                table: "product_setup_initial_tracking",
                columns: new[] { "tenant_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_setup_initial_tracking");
        }
    }
}
