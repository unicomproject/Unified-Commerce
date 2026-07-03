using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalUnitOfMeasureSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_unit_of_measures_tenant_id_uom_code",
                table: "unit_of_measures");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "unit_of_measures",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "uq_unit_of_measures_global_uom_code",
                table: "unit_of_measures",
                column: "uom_code",
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_unit_of_measures_tenant_id_uom_code",
                table: "unit_of_measures",
                columns: new[] { "tenant_id", "uom_code" },
                unique: true,
                filter: "tenant_id IS NOT NULL");
            migrationBuilder.Sql("""
                INSERT INTO unit_of_measures (id, tenant_id, name, conversion_factor, uom_code, created_at, updated_at)
                VALUES
                    ('91000000-0000-4000-8000-000000000001', NULL, 'Pieces', NULL, 'PCS', now(), now()),
                    ('91000000-0000-4000-8000-000000000002', NULL, 'Kilogram', NULL, 'KG', now(), now()),
                    ('91000000-0000-4000-8000-000000000003', NULL, 'Gram', NULL, 'G', now(), now()),
                    ('91000000-0000-4000-8000-000000000004', NULL, 'Litre', NULL, 'L', now(), now()),
                    ('91000000-0000-4000-8000-000000000005', NULL, 'Millilitre', NULL, 'ML', now(), now()),
                    ('91000000-0000-4000-8000-000000000006', NULL, 'Box', NULL, 'BOX', now(), now()),
                    ('91000000-0000-4000-8000-000000000007', NULL, 'Pack', NULL, 'PACK', now(), now())
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM unit_of_measures
                WHERE tenant_id IS NULL
                  AND uom_code IN ('PCS', 'KG', 'G', 'L', 'ML', 'BOX', 'PACK');
                """);
            migrationBuilder.DropIndex(
                name: "uq_unit_of_measures_global_uom_code",
                table: "unit_of_measures");

            migrationBuilder.DropIndex(
                name: "uq_unit_of_measures_tenant_id_uom_code",
                table: "unit_of_measures");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "unit_of_measures",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_unit_of_measures_tenant_id_uom_code",
                table: "unit_of_measures",
                columns: new[] { "tenant_id", "uom_code" },
                unique: true);
        }
    }
}
