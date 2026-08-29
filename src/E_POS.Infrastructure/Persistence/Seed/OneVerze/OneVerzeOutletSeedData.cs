namespace E_POS.Infrastructure.Persistence.Seed.OneVerze;

public static class OneVerzeOutletSeedData
{
    public static readonly Guid OutletOneId = Guid.Parse("22222222-0001-4000-8000-000000000001");
    public static readonly Guid OutletTwoId = Guid.Parse("22222222-0002-4000-8000-000000000002");
    public static readonly Guid OutletThreeId = Guid.Parse("22222222-0003-4000-8000-000000000003");

    public const string UpSql = """
        INSERT INTO outlets (
            id, tenant_id, outlet_name, outlet_code, status, outlet_type, timezone,
            is_default_outlet, created_at, updated_at
        )
        VALUES
            (
                '22222222-0001-4000-8000-000000000001',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                'OneVerze Flagship Arena Store',
                'OVZ-MAIN',
                'ACTIVE',
                'STORE',
                'Asia/Colombo',
                true,
                now(),
                now()
            ),
            (
                '22222222-0002-4000-8000-000000000002',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                'OneVerze Cricket Pavilion Store',
                'OVZ-PAVILION',
                'ACTIVE',
                'STORE',
                'Asia/Colombo',
                false,
                now(),
                now()
            )
        ON CONFLICT (id) DO UPDATE
        SET outlet_name = EXCLUDED.outlet_name,
            outlet_code = EXCLUDED.outlet_code,
            outlet_type = EXCLUDED.outlet_type,
            timezone = EXCLUDED.timezone,
            is_default_outlet = EXCLUDED.is_default_outlet,
            status = 'ACTIVE',
            updated_at = now();

        -- Outlet Addresses
        INSERT INTO outlet_addresses (
            id, tenant_id, outlet_id, address_type, address_line1, city, country_code, is_primary, status, created_at, updated_at
        )
        VALUES
            (
                '22222222-0004-4000-8000-000000000001',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                '22222222-0001-4000-8000-000000000001',
                'PHYSICAL',
                '45 Stadium Boulevard, Cricket City',
                'Colombo',
                'LK',
                true,
                'ACTIVE',
                now(),
                now()
            ),
            (
                '22222222-0004-4000-8000-000000000002',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                '22222222-0002-4000-8000-000000000002',
                'PHYSICAL',
                '12 Pavilion Arcade, Union Place',
                'Colombo',
                'LK',
                true,
                'ACTIVE',
                now(),
                now()
            )
        ON CONFLICT (id) DO UPDATE
        SET address_line1 = EXCLUDED.address_line1,
            city = EXCLUDED.city,
            status = 'ACTIVE',
            updated_at = now();

        -- Fulfillment Method (Click & Collect)
        INSERT INTO fulfillment_methods (
            id, tenant_id, method_code, method_name, method_type, description,
            requires_slot, requires_preparation, is_default, status, created_at, updated_at
        )
        VALUES (
            'dddd0004-0001-4000-8000-000000000002',
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
            'CLICK_COLLECT',
            'Click & Collect',
            'PICKUP',
            'Pickup in 1 hour from your selected OneVerze store.',
            true,
            true,
            true,
            'ACTIVE',
            now(),
            now()
        )
        ON CONFLICT (tenant_id, method_code) DO UPDATE
        SET method_name = EXCLUDED.method_name,
            method_type = EXCLUDED.method_type,
            description = EXCLUDED.description,
            requires_slot = true,
            requires_preparation = true,
            is_default = true,
            status = 'ACTIVE',
            updated_at = now();

        -- Fulfillment Method Outlets with 60 minutes (1 Hour) Lead Time
        INSERT INTO fulfillment_method_outlets (
            id, tenant_id, fulfillment_method_id, outlet_id,
            preparation_lead_minutes, pickup_window_minutes, cutoff_time, status, created_at, updated_at
        )
        VALUES
            (
                'dddd0004-0002-4000-8000-000000000002',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                'dddd0004-0001-4000-8000-000000000002',
                '22222222-0001-4000-8000-000000000001',
                60, -- 1 Hour Lead Time
                30,
                '20:00:00'::time,
                'ACTIVE',
                now(),
                now()
            ),
            (
                'dddd0004-0002-4000-8000-000000000003',
                '08b0c8b0-a5bf-44f0-8814-cb2fe0120000',
                'dddd0004-0001-4000-8000-000000000002',
                '22222222-0002-4000-8000-000000000002',
                60, -- 1 Hour Lead Time
                30,
                '20:00:00'::time,
                'ACTIVE',
                now(),
                now()
            )
        ON CONFLICT (tenant_id, fulfillment_method_id, outlet_id) DO UPDATE
        SET preparation_lead_minutes = 60,
            pickup_window_minutes = 30,
            cutoff_time = '20:00:00'::time,
            status = 'ACTIVE',
            updated_at = now();

        -- Outlet Business Hours (Monday through Sunday, 09:00 - 21:00)
        INSERT INTO outlet_business_hours (
            id, tenant_id, outlet_id, day_of_week, opening_time, closing_time, is_closed, valid_from, valid_until, created_at, updated_at
        )
        SELECT
            format('22220004-010%s-4000-8000-000000000001', day_number)::uuid,
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'::uuid,
            '22222222-0001-4000-8000-000000000001'::uuid,
            day_number::smallint,
            '09:00:00'::time,
            '21:00:00'::time,
            false,
            NULL::date,
            NULL::date,
            now(),
            now()
        FROM generate_series(0, 6) AS days(day_number)
        ON CONFLICT (outlet_id, day_of_week) DO UPDATE
        SET tenant_id = EXCLUDED.tenant_id,
            opening_time = EXCLUDED.opening_time,
            closing_time = EXCLUDED.closing_time,
            is_closed = false,
            valid_from = NULL,
            valid_until = NULL,
            updated_at = now();

        INSERT INTO outlet_business_hours (
            id, tenant_id, outlet_id, day_of_week, opening_time, closing_time, is_closed, valid_from, valid_until, created_at, updated_at
        )
        SELECT
            format('22220004-010%s-4000-8000-000000000002', day_number)::uuid,
            '08b0c8b0-a5bf-44f0-8814-cb2fe0120000'::uuid,
            '22222222-0002-4000-8000-000000000002'::uuid,
            day_number::smallint,
            '09:00:00'::time,
            '21:00:00'::time,
            false,
            NULL::date,
            NULL::date,
            now(),
            now()
        FROM generate_series(0, 6) AS days(day_number)
        ON CONFLICT (outlet_id, day_of_week) DO UPDATE
        SET tenant_id = EXCLUDED.tenant_id,
            opening_time = EXCLUDED.opening_time,
            closing_time = EXCLUDED.closing_time,
            is_closed = false,
            valid_from = NULL,
            valid_until = NULL,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM outlets
        WHERE id IN (
            '22222222-0001-4000-8000-000000000001',
            '22222222-0002-4000-8000-000000000002',
            '22222222-0003-4000-8000-000000000003'
        );
        """;
}
