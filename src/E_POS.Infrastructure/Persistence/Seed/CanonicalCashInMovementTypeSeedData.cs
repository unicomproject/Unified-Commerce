namespace E_POS.Infrastructure.Persistence.Seed;

public static class CanonicalCashInMovementTypeSeedData
{
    public static readonly Guid FloatAddedId = Guid.Parse("77777777-0360-4000-8000-000000000001");
    public static readonly Guid PettyCashAddedId = Guid.Parse("77777777-0361-4000-8000-000000000001");
    public static readonly Guid CashCorrectionId = Guid.Parse("77777777-0362-4000-8000-000000000001");
    public static readonly Guid OtherId = Guid.Parse("77777777-0363-4000-8000-000000000001");

    public const string UpSql = """
        INSERT INTO cash_movement_types
            (id, tenant_id, movement_type_code, movement_type_name, direction,
             affects_expected_cash, requires_reason, is_system_type, status, created_at, updated_at)
        VALUES
            ('77777777-0360-4000-8000-000000000001', NULL, 'FLOAT_ADDED', 'Float Added', 'IN', TRUE, FALSE, TRUE, 'ACTIVE', NOW(), NOW()),
            ('77777777-0361-4000-8000-000000000001', NULL, 'PETTY_CASH_ADDED', 'Petty Cash Added', 'IN', TRUE, TRUE, TRUE, 'ACTIVE', NOW(), NOW()),
            ('77777777-0362-4000-8000-000000000001', NULL, 'CASH_CORRECTION', 'Cash Correction', 'IN', TRUE, TRUE, TRUE, 'ACTIVE', NOW(), NOW()),
            ('77777777-0363-4000-8000-000000000001', NULL, 'OTHER', 'Other', 'IN', TRUE, TRUE, TRUE, 'ACTIVE', NOW(), NOW())
        ON CONFLICT (movement_type_code) WHERE tenant_id IS NULL DO UPDATE SET
            movement_type_name = EXCLUDED.movement_type_name,
            direction = EXCLUDED.direction,
            affects_expected_cash = EXCLUDED.affects_expected_cash,
            requires_reason = EXCLUDED.requires_reason,
            is_system_type = EXCLUDED.is_system_type,
            status = EXCLUDED.status,
            updated_at = NOW();
        """;

    public const string DownSql = """
        DELETE FROM cash_movement_types
        WHERE tenant_id IS NULL
          AND id IN (
            '77777777-0360-4000-8000-000000000001',
            '77777777-0361-4000-8000-000000000001',
            '77777777-0362-4000-8000-000000000001',
            '77777777-0363-4000-8000-000000000001');
        """;
}
