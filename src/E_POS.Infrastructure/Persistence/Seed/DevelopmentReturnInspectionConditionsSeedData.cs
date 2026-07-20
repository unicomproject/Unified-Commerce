namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentReturnInspectionConditionsSeedData
{
    public const string UpSql = """
        INSERT INTO return_inspection_conditions (
            id, tenant_id, condition_code, display_name, description, status_category,
            is_resellable, refund_impact, requires_notes, requires_photo, requires_approval,
            is_active, sort_order, created_at, updated_at
        )
        VALUES
            ('eeee0001-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'OPENED_GOOD', 'Opened - Good', 'Item opened but in good resellable condition.', 'GOOD', true, 'NONE', false, false, false, true, 0, now(), now()),
            ('eeee0001-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'DAMAGED', 'Damaged', 'Item has visible damage affecting resale value.', 'WARNING', false, 'PARTIAL', true, true, false, true, 1, now(), now()),
            ('eeee0001-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'NOT_RESELLABLE', 'Not Resellable', 'Item cannot be restocked or resold.', 'DANGER', false, 'FULL_DENIAL', true, true, true, true, 2, now(), now())
        ON CONFLICT (tenant_id, condition_code) DO UPDATE
        SET display_name = EXCLUDED.display_name,
            description = EXCLUDED.description,
            status_category = EXCLUDED.status_category,
            is_resellable = EXCLUDED.is_resellable,
            refund_impact = EXCLUDED.refund_impact,
            requires_notes = EXCLUDED.requires_notes,
            requires_photo = EXCLUDED.requires_photo,
            requires_approval = EXCLUDED.requires_approval,
            is_active = true,
            sort_order = EXCLUDED.sort_order,
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM return_inspection_conditions
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id IN (
              'eeee0001-0001-4000-8000-000000000001',
              'eeee0001-0002-4000-8000-000000000001',
              'eeee0001-0003-4000-8000-000000000001'
          );
        """;
}
