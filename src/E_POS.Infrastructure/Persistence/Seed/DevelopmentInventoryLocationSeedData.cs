namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentInventoryLocationSeedData
{
    public const string UpSql = """
        INSERT INTO inventory_locations (
            id,
            tenant_id,
            outlet_id,
            parent_inventory_location_id,
            location_code,
            location_name,
            location_type,
            is_sellable_location,
            is_return_location,
            is_receiving_location,
            is_quarantine_location,
            status,
            created_by_tenant_user_id,
            updated_by_tenant_user_id,
            created_at,
            updated_at
        )
        VALUES (
            'dddddddd-0001-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'bbbbbbbb-0001-4000-8000-000000000001',
            NULL,
            'MAIN',
            'Main Store Stock',
            'STORE',
            TRUE,
            TRUE,
            TRUE,
            FALSE,
            'ACTIVE',
            '99999999-0001-4000-8000-000000000001',
            '99999999-0001-4000-8000-000000000001',
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET location_name = EXCLUDED.location_name,
            is_sellable_location = TRUE,
            is_receiving_location = TRUE,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM inventory_locations
        WHERE id = 'dddddddd-0001-4000-8000-000000000001';
        """;
}
