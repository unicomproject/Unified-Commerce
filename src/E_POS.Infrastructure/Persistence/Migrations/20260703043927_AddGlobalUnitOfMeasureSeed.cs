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
                    ('91000000-0000-4000-8000-000000000007', NULL, 'Pack', NULL, 'PACK', now(), now()),
                    ('91000000-0000-4000-8000-000000000008', NULL, 'Pair', NULL, 'PAIR', now(), now()),
                    ('91000000-0000-4000-8000-000000000009', NULL, 'Set', NULL, 'SET', now(), now()),
                    ('91000000-0000-4000-8000-000000000010', NULL, 'Roll', NULL, 'ROLL', now(), now()),
                    ('91000000-0000-4000-8000-000000000011', NULL, 'Meter', NULL, 'MTR', now(), now()),
                    ('91000000-0000-4000-8000-000000000012', NULL, 'Bottle', NULL, 'BTL', now(), now()),
                    ('91000000-0000-4000-8000-000000000013', NULL, 'Bag', NULL, 'BAG', now(), now()),
                    ('91000000-0000-4000-8000-000000000014', NULL, 'Carton', NULL, 'CTN', now(), now()),
                    ('91000000-0000-4000-8000-000000000015', NULL, 'Case', NULL, 'CASE', now(), now()),
                    ('91000000-0000-4000-8000-000000000016', NULL, 'Each', NULL, 'EA', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000017', NULL, 'Packet', NULL, 'PKT', now(), now()),
                    ('91000000-0000-4000-8000-000000000018', NULL, 'Can', NULL, 'CAN', now(), now()),
                    ('91000000-0000-4000-8000-000000000019', NULL, 'Jar', NULL, 'JAR', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000020', NULL, 'Tube', NULL, 'TUBE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000021', NULL, 'Sheet', NULL, 'SHT', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000022', NULL, 'Foot', NULL, 'FT', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000023', NULL, 'Master Carton / Master Case', NULL, 'MCTN', now(), now()),
                    ('91000000-0000-4000-8000-000000000024', NULL, 'Bundle', NULL, 'BNDL', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000025', NULL, 'Bale', NULL, 'BALE', now(), now()),
                    ('91000000-0000-4000-8000-000000000026', NULL, 'Crate', NULL, 'CRTE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000027', NULL, 'Tray', NULL, 'TRAY', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000028', NULL, 'Sack', NULL, 'SACK', now(), now()),
                    ('91000000-0000-4000-8000-000000000029', NULL, 'Drum', NULL, 'DRUM', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000030', NULL, 'Pail', NULL, 'PAIL', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000031', NULL, 'Tote', NULL, 'TOTE', now(), now()),
                    ('91000000-0000-4000-8000-000000000032', NULL, 'Pallet', NULL, 'PLT', now(), now())
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM unit_of_measures
                WHERE tenant_id IS NULL
                  AND uom_code IN ('PCS', 'KG', 'G', 'L', 'ML', 'BOX', 'PACK', 'PAIR', 'SET', 'ROLL', 'MTR', 'BTL', 'BAG', 'CTN', 'CASE', 'EA', /*'PKT',*/ 'CAN', 'JAR', /*'TUBE', 'SHT', 'FT', 'MCTN',*/ 'BNDL', /*'BALE',*/ 'CRTE', /*'TRAY', 'SACK',*/ 'DRUM', /*'PAIL', 'TOTE',*/ 'PLT');
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
