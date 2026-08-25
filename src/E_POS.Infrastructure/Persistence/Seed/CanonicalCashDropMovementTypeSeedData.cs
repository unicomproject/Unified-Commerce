namespace E_POS.Infrastructure.Persistence.Seed;

/// Canonical system OUT movement types for Cash Drop / Cash Out.
/// Codes are unique across global types (IN OTHER vs OUT OTHER use distinct codes).
public static class CanonicalCashDropMovementTypeSeedData
{
    public static readonly Guid CashDropId = Guid.Parse("77777777-0460-4000-8000-000000000001");
    public static readonly Guid BankDepositId = Guid.Parse("77777777-0461-4000-8000-000000000001");
    public static readonly Guid CashPickupId = Guid.Parse("77777777-0462-4000-8000-000000000001");
    public static readonly Guid SecurityTransferId = Guid.Parse("77777777-0463-4000-8000-000000000001");
    public static readonly Guid OutCashCorrectionId = Guid.Parse("77777777-0464-4000-8000-000000000001");
    public static readonly Guid OutOtherId = Guid.Parse("77777777-0465-4000-8000-000000000001");

    public const string UpSql = """
        INSERT INTO cash_movement_types
            (id, tenant_id, movement_type_code, movement_type_name, direction,
             affects_expected_cash, requires_reason, is_system_type, status, created_at, updated_at)
        VALUES
            ('77777777-0460-4000-8000-000000000001', NULL, 'CASH_DROP', 'Safe Drop', 'OUT', TRUE, FALSE, TRUE, 'ACTIVE', NOW(), NOW()),
            ('77777777-0461-4000-8000-000000000001', NULL, 'BANK_DEPOSIT', 'Bank Deposit', 'OUT', TRUE, TRUE, TRUE, 'ACTIVE', NOW(), NOW()),
            ('77777777-0462-4000-8000-000000000001', NULL, 'CASH_PICKUP', 'Cash Pickup', 'OUT', TRUE, TRUE, TRUE, 'ACTIVE', NOW(), NOW()),
            ('77777777-0463-4000-8000-000000000001', NULL, 'SECURITY_TRANSFER', 'Security Transfer', 'OUT', TRUE, TRUE, TRUE, 'ACTIVE', NOW(), NOW()),
            ('77777777-0464-4000-8000-000000000001', NULL, 'OUT_CASH_CORRECTION', 'Cash Correction', 'OUT', TRUE, TRUE, TRUE, 'ACTIVE', NOW(), NOW()),
            ('77777777-0465-4000-8000-000000000001', NULL, 'OUT_OTHER', 'Other', 'OUT', TRUE, TRUE, TRUE, 'ACTIVE', NOW(), NOW())
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
            '77777777-0460-4000-8000-000000000001',
            '77777777-0461-4000-8000-000000000001',
            '77777777-0462-4000-8000-000000000001',
            '77777777-0463-4000-8000-000000000001',
            '77777777-0464-4000-8000-000000000001',
            '77777777-0465-4000-8000-000000000001');
        """;
}
