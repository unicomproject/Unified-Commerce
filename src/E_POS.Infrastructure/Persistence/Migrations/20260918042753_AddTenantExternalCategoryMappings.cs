using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantExternalCategoryMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_category_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    external_category_key = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    external_category_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    tenant_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mapping_source = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, defaultValue: "PRODUCT_CONFIRMED"),
                    created_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_tenant_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_category_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_external_category_mappings_created_by_tenant_user_id",
                        column: x => x.created_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_external_category_mappings_tenant_category",
                        columns: x => new { x.tenant_id, x.tenant_category_id },
                        principalTable: "categories",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_external_category_mappings_tenant_id_tenants",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_external_category_mappings_updated_by_tenant_user_id",
                        column: x => x.updated_by_tenant_user_id,
                        principalTable: "tenant_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_external_category_mappings_created_by_tenant_user_id",
                table: "external_category_mappings",
                column: "created_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_category_mappings_tenant_tenant_category_id",
                table: "external_category_mappings",
                columns: new[] { "tenant_id", "tenant_category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_external_category_mappings_updated_by_tenant_user_id",
                table: "external_category_mappings",
                column: "updated_by_tenant_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_external_category_mappings_tenant_provider_key",
                table: "external_category_mappings",
                columns: new[] { "tenant_id", "provider", "external_category_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_category_mappings");
        }
    }
}
