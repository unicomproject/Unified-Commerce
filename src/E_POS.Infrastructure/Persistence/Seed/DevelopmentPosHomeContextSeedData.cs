namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosHomeContextSeedData
{
    public const string UpSql = """
        INSERT INTO outlets (
            id, tenant_id, name, outlet_code, status, outlet_type, is_online_visible,
            created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-0001-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'Development Main Store',
            'DEV-STORE-01',
            'ACTIVE',
            'STORE',
            true,
            now(),
            now()
        )
        ON CONFLICT (tenant_id, outlet_code) DO UPDATE
        SET name = EXCLUDED.name,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO tills (
            id, tenant_id, outlet_id, till_area_name, till_number, name, till_code, status,
            created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-0002-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'bbbbbbbb-0001-4000-8000-000000000001',
            'Front',
            1,
            'Front Till 01',
            'FRONT-01',
            'ACTIVE',
            now(),
            now()
        )
        ON CONFLICT (tenant_id, id) DO UPDATE
        SET outlet_id = EXCLUDED.outlet_id,
            till_area_name = EXCLUDED.till_area_name,
            till_number = EXCLUDED.till_number,
            name = EXCLUDED.name,
            till_code = EXCLUDED.till_code,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO pos_devices (
            id, tenant_id, outlet_id, name, status, device_code, device_serial_number,
            created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-0003-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'bbbbbbbb-0001-4000-8000-000000000001',
            'Front POS Device',
            'ACTIVE',
            'POS-01',
            'DEV-POS-01',
            now(),
            now()
        )
        ON CONFLICT (tenant_id, device_code) DO UPDATE
        SET outlet_id = EXCLUDED.outlet_id,
            name = EXCLUDED.name,
            status = 'ACTIVE',
            device_serial_number = EXCLUDED.device_serial_number,
            updated_at = now();

        INSERT INTO till_device_assignments (
            id, till_id, pos_device_id, effective_from, effective_to, status,
            created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-0004-4000-8000-000000000001',
            'bbbbbbbb-0002-4000-8000-000000000001',
            'bbbbbbbb-0003-4000-8000-000000000001',
            to_char(now() AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.MS"Z"'),
            NULL,
            'ACTIVE',
            now(),
            now()
        )
        ON CONFLICT (till_id, pos_device_id, effective_from) DO UPDATE
        SET status = 'ACTIVE',
            effective_to = NULL,
            updated_at = now();

        INSERT INTO till_sessions (
            id, tenant_id, outlet_id, till_id, session_number, business_date,
            opened_by_tenant_user_id, opened_from_pos_device_id, opening_float_amount,
            currency_code, status, opened_at, closed_at, created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-0005-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'bbbbbbbb-0001-4000-8000-000000000001',
            'bbbbbbbb-0002-4000-8000-000000000001',
            'TS-DEV-0001',
            CURRENT_DATE,
            '99999999-0003-4000-8000-000000000001',
            'bbbbbbbb-0003-4000-8000-000000000001',
            0,
            'LKR',
            'OPEN',
            now(),
            NULL,
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET outlet_id = EXCLUDED.outlet_id,
            till_id = EXCLUDED.till_id,
            business_date = CURRENT_DATE,
            opened_by_tenant_user_id = EXCLUDED.opened_by_tenant_user_id,
            opened_from_pos_device_id = EXCLUDED.opened_from_pos_device_id,
            status = 'OPEN',
            closed_at = NULL,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM till_sessions
        WHERE id = 'bbbbbbbb-0005-4000-8000-000000000001';

        DELETE FROM till_device_assignments
        WHERE id = 'bbbbbbbb-0004-4000-8000-000000000001';

        DELETE FROM pos_devices
        WHERE id = 'bbbbbbbb-0003-4000-8000-000000000001';

        DELETE FROM tills
        WHERE id = 'bbbbbbbb-0002-4000-8000-000000000001';

        DELETE FROM outlets
        WHERE id = 'bbbbbbbb-0001-4000-8000-000000000001';
        """;
}
