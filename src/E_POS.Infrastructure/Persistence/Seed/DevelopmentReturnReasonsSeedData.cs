namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentReturnReasonsSeedData
{
    public const string UpSql = """
        INSERT INTO return_reasons (
            id, tenant_id, reason_code, reason_name, applies_to,
            requires_inspection, is_active, sort_order, created_at, updated_at
        )
        VALUES
            ('dddd0001-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'DAMAGED', 'Damaged Item', 'RETURN', false, true, 0, now(), now()),
            ('dddd0001-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'WRONG_ITEM', 'Wrong Item', 'RETURN', false, true, 1, now(), now()),
            ('dddd0001-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'SIZE_ISSUE', 'Size / Fit Issue', 'BOTH', false, true, 2, now(), now()),
            ('dddd0001-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'CHANGED_MIND', 'Changed Mind', 'RETURN', false, true, 3, now(), now()),
            ('dddd0001-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'DEFECTIVE', 'Product Defective', 'RETURN', true, true, 4, now(), now()),
            ('dddd0001-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'OTHER', 'Other', 'BOTH', false, true, 5, now(), now())
        ON CONFLICT (tenant_id, reason_code) DO NOTHING;
        """;

    // Intentionally irreversible: a tenant admin may customize a seeded row
    // after insertion, so rollback must not delete tenant configuration.
    public const string DownSql = "SELECT 1;";
}
