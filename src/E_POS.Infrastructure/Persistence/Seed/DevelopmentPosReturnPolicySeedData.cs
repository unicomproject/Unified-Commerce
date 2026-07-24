namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-only active return policy + assignment to seeded MER merchandise products.
/// Resolver order: <c>products.return_policy_id</c> (ACTIVE) → tenant default ACTIVE policy.
/// </summary>
public static class DevelopmentPosReturnPolicySeedData
{
    public const string PolicyId = "dddd0001-0014-4000-8000-000000000001";
    public const string PolicyCode = "DEV-14DAYS";
    public const string PolicyName = "Development 14-Day Return Policy";
    public const int ReturnWindowDays = 14;
    public const int ExchangeWindowDays = 14;

    public static readonly Guid DevelopmentTenantId = DevelopmentTenantSeedConstants.DevelopmentTenantId;
    public static readonly Guid CashierUserId = DevelopmentTenantSeedConstants.CashierUserId;

    public static string UpSql => $$"""
        UPDATE return_policies
        SET
            return_policy_name = '{{PolicyName}}',
            description = 'Development default POS return/exchange policy for seeded merchandise (aligned with platform 14DAYS template).',
            return_window_days = {{ReturnWindowDays}},
            exchange_window_days = {{ExchangeWindowDays}},
            requires_receipt = TRUE,
            allow_defective_return = TRUE,
            requires_manager_approval = FALSE,
            is_default_policy = TRUE,
            status = 'ACTIVE',
            updated_by_tenant_user_id = '{{CashierUserId}}',
            updated_at = now()
        WHERE tenant_id = '{{DevelopmentTenantId}}'
          AND return_policy_code = '{{PolicyCode}}';

        INSERT INTO return_policies (
            id,
            tenant_id,
            return_policy_code,
            return_policy_name,
            description,
            return_window_days,
            exchange_window_days,
            requires_receipt,
            allow_defective_return,
            requires_manager_approval,
            is_default_policy,
            status,
            created_by_tenant_user_id,
            updated_by_tenant_user_id,
            created_at,
            updated_at
        )
        SELECT
            '{{PolicyId}}'::uuid,
            '{{DevelopmentTenantId}}'::uuid,
            '{{PolicyCode}}',
            '{{PolicyName}}',
            'Development default POS return/exchange policy for seeded merchandise (aligned with platform 14DAYS template).',
            {{ReturnWindowDays}},
            {{ExchangeWindowDays}},
            TRUE,
            TRUE,
            FALSE,
            TRUE,
            'ACTIVE',
            '{{CashierUserId}}'::uuid,
            '{{CashierUserId}}'::uuid,
            now(),
            now()
        WHERE NOT EXISTS (
            SELECT 1
            FROM return_policies
            WHERE tenant_id = '{{DevelopmentTenantId}}'
              AND return_policy_code = '{{PolicyCode}}'
        )
        AND NOT EXISTS (
            SELECT 1
            FROM return_policies
            WHERE id = '{{PolicyId}}'::uuid
        );

        -- Assign only sellable ACTIVE development merchandise products that still lack a policy.
        -- MER-* catalogue items are standard apparel/footwear/accessories/sports goods (returnable).
        UPDATE products AS p
        SET
            return_policy_id = rp.id,
            updated_by_tenant_user_id = '{{CashierUserId}}'::uuid,
            updated_at = now()
        FROM return_policies AS rp
        WHERE rp.tenant_id = '{{DevelopmentTenantId}}'::uuid
          AND rp.return_policy_code = '{{PolicyCode}}'
          AND rp.status = 'ACTIVE'
          AND p.tenant_id = rp.tenant_id
          AND p.status = 'ACTIVE'
          AND p.is_sellable = TRUE
          AND p.product_code LIKE 'MER-%'
          AND p.return_policy_id IS NULL;
        """;

    public static string DownSql => $$"""
        -- Clear MER assignments that point at the DEV-14DAYS policy for this tenant.
        UPDATE products AS p
        SET
            return_policy_id = NULL,
            updated_by_tenant_user_id = '{{CashierUserId}}'::uuid,
            updated_at = now()
        FROM return_policies AS rp
        WHERE rp.tenant_id = '{{DevelopmentTenantId}}'::uuid
          AND rp.return_policy_code = '{{PolicyCode}}'
          AND p.tenant_id = rp.tenant_id
          AND p.product_code LIKE 'MER-%'
          AND p.return_policy_id = rp.id;

        -- Delete only the migration-owned deterministic policy row.
        DELETE FROM return_policies
        WHERE id = '{{PolicyId}}'::uuid
          AND tenant_id = '{{DevelopmentTenantId}}'::uuid
          AND return_policy_code = '{{PolicyCode}}';
        """;
}
