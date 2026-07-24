namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentStorefrontFulfillmentSeedData
{
    public static string UpSql => FulfillmentMethodSql + "\n" + BusinessHoursSql;

    private static string FulfillmentMethodSql => MethodSql + "\n" + OutletMappingSql;

    private const string MethodSql = """
        INSERT INTO fulfillment_methods (id, tenant_id, method_code, method_name, method_type, description, requires_slot, requires_preparation, is_default, status, created_at, updated_at) VALUES ('dddd0004-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'CLICK_COLLECT', 'Click & Collect', 'PICKUP', 'Customer pickup from the selected outlet.', true, true, true, 'ACTIVE', now(), now()) ON CONFLICT (tenant_id, method_code) DO UPDATE SET method_name = EXCLUDED.method_name, method_type = EXCLUDED.method_type, description = EXCLUDED.description, requires_slot = true, requires_preparation = true, is_default = true, status = 'ACTIVE', updated_at = now();
        """;

    private const string OutletMappingSql = """
        INSERT INTO fulfillment_method_outlets (id, tenant_id, fulfillment_method_id, outlet_id, preparation_lead_minutes, pickup_window_minutes, cutoff_time, status, created_at, updated_at) VALUES ('dddd0004-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', (SELECT id FROM fulfillment_methods WHERE tenant_id = '55555555-0000-4000-8000-000000000001' AND method_code = 'CLICK_COLLECT'), 'bbbbbbbb-0001-4000-8000-000000000001', 30, 30, '18:00:00'::time, 'ACTIVE', now(), now()) ON CONFLICT (tenant_id, fulfillment_method_id, outlet_id) DO UPDATE SET preparation_lead_minutes = 30, pickup_window_minutes = 30, cutoff_time = '18:00:00'::time, status = 'ACTIVE', updated_at = now();
        """;

    private const string BusinessHoursSql = """
        INSERT INTO outlet_business_hours (id, tenant_id, outlet_id, day_of_week, opening_time, closing_time, is_closed, valid_from, valid_until, created_at, updated_at) SELECT format('dddd0004-010%s-4000-8000-000000000001', day_number)::uuid, '55555555-0000-4000-8000-000000000001'::uuid, 'bbbbbbbb-0001-4000-8000-000000000001'::uuid, day_number::smallint, '09:00:00'::time, '21:00:00'::time, false, NULL::date, NULL::date, now(), now() FROM generate_series(0, 6) AS days(day_number) ON CONFLICT (outlet_id, day_of_week) DO UPDATE SET tenant_id = EXCLUDED.tenant_id, opening_time = EXCLUDED.opening_time, closing_time = EXCLUDED.closing_time, is_closed = false, valid_from = NULL, valid_until = NULL, updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM outlet_business_hours WHERE tenant_id = '55555555-0000-4000-8000-000000000001' AND outlet_id = 'bbbbbbbb-0001-4000-8000-000000000001' AND day_of_week BETWEEN 0 AND 6;
        DELETE FROM fulfillment_method_outlets WHERE id = 'dddd0004-0002-4000-8000-000000000001';
        DELETE FROM fulfillment_methods WHERE id = 'dddd0004-0001-4000-8000-000000000001';
        """;
}
