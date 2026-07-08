namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosHomeContextSeedData
{
    public const string UpSql = """
        INSERT INTO outlets (
            id, tenant_id, outlet_name, outlet_code, status, outlet_type, timezone,
            is_default_outlet, created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-0001-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'Development Main Store',
            'DEV-STORE-01',
            'ACTIVE',
            'STORE',
            'UTC',
            true,
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET outlet_name = EXCLUDED.outlet_name,
            outlet_code = EXCLUDED.outlet_code,
            outlet_type = EXCLUDED.outlet_type,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO tills (
            id, tenant_id, outlet_id, till_area_name, till_number, till_name, till_code,
            till_type, default_opening_float_amount, currency_code, is_cash_managed, status,
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
            'STANDARD',
            0,
            'LKR',
            true,
            'ACTIVE',
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET outlet_id = EXCLUDED.outlet_id,
            till_area_name = EXCLUDED.till_area_name,
            till_number = EXCLUDED.till_number,
            till_name = EXCLUDED.till_name,
            till_code = EXCLUDED.till_code,
            till_type = EXCLUDED.till_type,
            currency_code = EXCLUDED.currency_code,
            is_cash_managed = EXCLUDED.is_cash_managed,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO pos_devices (
            id, tenant_id, outlet_id, device_name, device_code, device_type, status,
            is_trusted, created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-0003-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'bbbbbbbb-0001-4000-8000-000000000001',
            'Front POS Device',
            'POS-01',
            'TABLET',
            'ACTIVE',
            true,
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET outlet_id = EXCLUDED.outlet_id,
            device_name = EXCLUDED.device_name,
            device_code = EXCLUDED.device_code,
            device_type = EXCLUDED.device_type,
            is_trusted = true,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO till_device_assignments (
            id, tenant_id, outlet_id, till_id, pos_device_id, assigned_at, released_at,
            created_at, updated_at
        )
        VALUES (
            'bbbbbbbb-0004-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'bbbbbbbb-0001-4000-8000-000000000001',
            'bbbbbbbb-0002-4000-8000-000000000001',
            'bbbbbbbb-0003-4000-8000-000000000001',
            now(),
            NULL,
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET outlet_id = EXCLUDED.outlet_id,
            till_id = EXCLUDED.till_id,
            pos_device_id = EXCLUDED.pos_device_id,
            assigned_at = EXCLUDED.assigned_at,
            released_at = NULL,
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
