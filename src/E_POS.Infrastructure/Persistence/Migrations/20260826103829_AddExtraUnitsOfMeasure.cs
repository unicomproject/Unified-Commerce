using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExtraUnitsOfMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO unit_of_measures (id, tenant_id, uom_name, conversion_factor, uom_code, uom_type, status, created_at, updated_at)
                VALUES
                    ('91000000-0000-4000-8000-000000000001', NULL, 'Pieces', 1, 'PCS', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000002', NULL, 'Kilogram', 1, 'KG', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000003', NULL, 'Gram', 1, 'G', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000004', NULL, 'Litre', 1, 'L', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000005', NULL, 'Millilitre', 1, 'ML', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000006', NULL, 'Box', 1, 'BOX', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000007', NULL, 'Pack', 1, 'PACK', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000008', NULL, 'Pair', 1, 'PAIR', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000009', NULL, 'Set', 1, 'SET', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000010', NULL, 'Roll', 1, 'ROLL', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000011', NULL, 'Meter', 1, 'MTR', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000012', NULL, 'Bottle', 1, 'BTL', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000013', NULL, 'Bag', 1, 'BAG', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000014', NULL, 'Carton', 1, 'CTN', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000015', NULL, 'Case', 1, 'CASE', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000016', NULL, 'Each', 1, 'EA', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000017', NULL, 'Packet', 1, 'PKT', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000018', NULL, 'Can', 1, 'CAN', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000019', NULL, 'Jar', 1, 'JAR', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000020', NULL, 'Tube', 1, 'TUBE', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000021', NULL, 'Sheet', 1, 'SHT', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000022', NULL, 'Foot', 1, 'FT', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000023', NULL, 'Master Carton / Master Case', 1, 'MCTN', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000024', NULL, 'Bundle', 1, 'BNDL', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000025', NULL, 'Bale', 1, 'BALE', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000026', NULL, 'Crate', 1, 'CRTE', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000027', NULL, 'Tray', 1, 'TRAY', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000028', NULL, 'Sack', 1, 'SACK', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000029', NULL, 'Drum', 1, 'DRUM', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000030', NULL, 'Pail', 1, 'PAIL', 'QUANTITY', 'ACTIVE', now(), now()),
                    -- ('91000000-0000-4000-8000-000000000031', NULL, 'Tote', 1, 'TOTE', 'QUANTITY', 'ACTIVE', now(), now()),
                    ('91000000-0000-4000-8000-000000000032', NULL, 'Pallet', 1, 'PLT', 'QUANTITY', 'ACTIVE', now(), now())
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM unit_of_measures
                WHERE tenant_id IS NULL
                  AND uom_code IN ('PAIR', 'SET', 'ROLL', 'MTR', 'BTL', 'BAG', 'CTN', 'CASE', 'EA', /*'PKT',*/ 'CAN', 'JAR', /*'TUBE', 'SHT', 'FT', 'MCTN',*/ 'BNDL', /*'BALE',*/ 'CRTE', /*'TRAY', 'SACK',*/ 'DRUM', /*'PAIL', 'TOTE',*/ 'PLT');
                """);
        }
    }
}
