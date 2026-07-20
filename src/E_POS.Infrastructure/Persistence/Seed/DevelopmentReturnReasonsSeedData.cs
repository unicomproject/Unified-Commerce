namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentReturnReasonsSeedData
{
    public const string UpSql = """
        INSERT INTO return_reasons (
            id, tenant_id, reason_code, reason_name, description, applies_to,
            requires_note, requires_inspection, requires_manager_approval,
            is_active, sort_order, created_at, updated_at
        )
        VALUES
            ('dddd0001-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'DAMAGED', 'Damaged Item', 'Item was damaged when received or opened.', 'RETURN', false, false, false, true, 0, now(), now()),
            ('dddd0001-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'WRONG_ITEM', 'Wrong Item', 'Customer received a different product than ordered.', 'RETURN', false, false, false, true, 1, now(), now()),
            ('dddd0001-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'SIZE_ISSUE', 'Size / Fit Issue', 'Size or fit does not meet customer expectation.', 'BOTH', false, false, false, true, 2, now(), now()),
            ('dddd0001-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'CHANGED_MIND', 'Changed Mind', 'Customer no longer wants the item.', 'RETURN', false, false, false, true, 3, now(), now()),
            ('dddd0001-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'DEFECTIVE', 'Product Defective', 'Product has a manufacturing or functional defect.', 'RETURN', false, true, false, true, 4, now(), now()),
            ('dddd0001-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'OTHER', 'Other', 'Provide a brief note describing the return reason.', 'BOTH', true, false, false, true, 5, now(), now())
        ON CONFLICT (tenant_id, reason_code) DO NOTHING;
        """;

    // Intentionally irreversible: a tenant admin may customize a seeded row
    // after insertion, so rollback must not delete tenant configuration.
    public const string DownSql = "SELECT 1;";
}
