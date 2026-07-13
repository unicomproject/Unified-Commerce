using E_POS.Domain.Modules.Tenant.Orders.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosDiscountWorkflowSeedData
{
    private static readonly TenantPermissionSeedDefinition ApprovePermission = new(
        Guid.Parse("77777777-0337-4000-8000-000000000001"),
        SalesPermissions.Discount.Approve,
        DevelopmentPosPermissionCatalogSeedConstants.CorePosModuleId,
        DevelopmentPosPermissionCatalogSeedConstants.PosSalesFeatureId,
        "approve_discount",
        "Approve or reject POS discounts above cashier authority.");

    public static IReadOnlyList<TenantPermissionSeedDefinition> PermissionDefinitions { get; } =
        [ApprovePermission];

    public static string UpSql => $$"""
        {{TenantPermissionSeedSqlBuilder.BuildPermissionUpsertSql(PermissionDefinitions)}}

        INSERT INTO discount_types (
            id, discount_type_code, discount_type_name, calculation_method,
            is_system_type, status, created_at, updated_at)
        VALUES
            ('d1500000-0001-4000-8000-000000000001', 'POS_PERCENTAGE', 'POS Percentage', 'PERCENTAGE', TRUE, 'ACTIVE', now(), now()),
            ('d1500000-0002-4000-8000-000000000001', 'POS_FIXED_AMOUNT', 'POS Fixed Amount', 'FIXED_AMOUNT', TRUE, 'ACTIVE', now(), now())
        ON CONFLICT (discount_type_code) DO UPDATE
        SET discount_type_name = EXCLUDED.discount_type_name,
            calculation_method = EXCLUDED.calculation_method,
            status = 'ACTIVE',
            updated_at = now();

        DO $pos_discount_seed$
        BEGIN
        IF EXISTS (SELECT 1 FROM tenants WHERE id = '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}')
           AND EXISTS (SELECT 1 FROM tenant_users WHERE id = '{{DevelopmentTenantSeedConstants.CashierUserId}}') THEN
        INSERT INTO discount_policies (
            id, tenant_id, discount_type_id, discount_policy_code,
            discount_policy_name, description, discount_scope, discount_value,
            currency_code, max_discount_amount, min_order_amount, min_quantity,
            requires_manager_approval, is_stackable, stacking_group_code,
            priority, starts_at, ends_at, status, created_at, updated_at)
        VALUES
            ('d1500000-1001-4000-8000-000000000001', '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}',
             (SELECT id FROM discount_types WHERE discount_type_code = 'POS_PERCENTAGE'), 'POS_MANUAL_PERCENTAGE',
             'Manual Percentage Discount', 'Cashier-entered percentage discount envelope.',
             'ORDER', 50, NULL, NULL, NULL, NULL, FALSE, FALSE, NULL, 100, NULL, NULL, 'ACTIVE', now(), now()),
            ('d1500000-1002-4000-8000-000000000001', '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}',
             (SELECT id FROM discount_types WHERE discount_type_code = 'POS_FIXED_AMOUNT'), 'POS_MANUAL_FIXED',
             'Manual Fixed Discount', 'Cashier-entered fixed discount envelope.',
             'ORDER', 10000, 'LKR', 10000, NULL, NULL, FALSE, FALSE, NULL, 90, NULL, NULL, 'ACTIVE', now(), now()),
            ('d1500000-1003-4000-8000-000000000001', '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}',
             (SELECT id FROM discount_types WHERE discount_type_code = 'POS_PERCENTAGE'), 'WELCOME10',
             'Welcome 10%', 'Predefined ten percent POS discount.',
             'ORDER', 10, NULL, NULL, NULL, NULL, FALSE, FALSE, NULL, 80, NULL, NULL, 'ACTIVE', now(), now()),
            ('d1500000-1004-4000-8000-000000000001', '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}',
             (SELECT id FROM discount_types WHERE discount_type_code = 'POS_PERCENTAGE'), 'POS_MANUAL_PERCENTAGE_LINE',
             'Manual Line Percentage Discount', 'Cashier-entered percentage discount envelope for a selected POS line.',
             'LINE', 50, NULL, NULL, NULL, NULL, FALSE, FALSE, NULL, 70, NULL, NULL, 'ACTIVE', now(), now()),
            ('d1500000-1005-4000-8000-000000000001', '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}',
             (SELECT id FROM discount_types WHERE discount_type_code = 'POS_FIXED_AMOUNT'), 'POS_MANUAL_FIXED_LINE',
             'Manual Line Fixed Discount', 'Cashier-entered fixed discount envelope for a selected POS line.',
             'LINE', 10000, 'LKR', 10000, NULL, NULL, FALSE, FALSE, NULL, 60, NULL, NULL, 'ACTIVE', now(), now()),
            ('d1500000-1006-4000-8000-000000000001', '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}',
             (SELECT id FROM discount_types WHERE discount_type_code = 'POS_PERCENTAGE'), 'JERSEY_LINE_5',
             'Jersey Line 5%', 'Development-only targeted item discount for Team Jersey.',
             'LINE', 5, NULL, NULL, NULL, NULL, FALSE, FALSE, NULL, 50, NULL, NULL, 'ACTIVE', now(), now())
        ON CONFLICT (tenant_id, discount_policy_code) DO UPDATE
        SET discount_policy_name = EXCLUDED.discount_policy_name,
            description = EXCLUDED.description,
            discount_scope = EXCLUDED.discount_scope,
            discount_value = EXCLUDED.discount_value,
            currency_code = EXCLUDED.currency_code,
            max_discount_amount = EXCLUDED.max_discount_amount,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO discount_policy_targets (
            id, tenant_id, discount_policy_id, target_type, target_mode,
            product_id, product_variant_id, category_id, brand_id, collection_id,
            status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at)
        VALUES (
            'd1500000-3001-4000-8000-000000000001',
            '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}',
            'd1500000-1006-4000-8000-000000000001',
            'PRODUCT_VARIANT',
            'INCLUDE',
            NULL,
            'cccc0005-0001-4000-8000-000000000001',
            NULL,
            NULL,
            NULL,
            'ACTIVE',
            '{{DevelopmentTenantSeedConstants.CashierUserId}}',
            '{{DevelopmentTenantSeedConstants.CashierUserId}}',
            now(),
            now())
        ON CONFLICT (id) DO UPDATE
        SET target_mode = EXCLUDED.target_mode,
            status = 'ACTIVE',
            updated_by_tenant_user_id = EXCLUDED.updated_by_tenant_user_id,
            updated_at = now();

        INSERT INTO pos_discount_authority_limits (
            id, tenant_id, tenant_user_id, max_percentage, max_fixed_amount,
            currency_code, status, created_at, updated_at)
        VALUES (
            'd1500000-2001-4000-8000-000000000001',
            '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}',
            '{{DevelopmentTenantSeedConstants.CashierUserId}}',
            10, 1000, 'LKR', 'ACTIVE', now(), now())
        ON CONFLICT (tenant_id, tenant_user_id) DO UPDATE
        SET max_percentage = EXCLUDED.max_percentage,
            max_fixed_amount = EXCLUDED.max_fixed_amount,
            currency_code = EXCLUDED.currency_code,
            status = 'ACTIVE',
            updated_at = now();
        END IF;
        END $pos_discount_seed$;
        """;

    public static string DownSql => $$"""
        DELETE FROM pos_discount_authority_limits
        WHERE tenant_id = '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}'
          AND tenant_user_id = '{{DevelopmentTenantSeedConstants.CashierUserId}}';
        DELETE FROM discount_policy_targets
        WHERE tenant_id = '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}'
          AND discount_policy_id IN (
              'd1500000-1004-4000-8000-000000000001',
              'd1500000-1005-4000-8000-000000000001',
              'd1500000-1006-4000-8000-000000000001');
        DELETE FROM discount_policies
        WHERE tenant_id = '{{DevelopmentTenantSeedConstants.DevelopmentTenantId}}'
          AND discount_policy_code IN ('POS_MANUAL_PERCENTAGE', 'POS_MANUAL_FIXED', 'WELCOME10',
              'POS_MANUAL_PERCENTAGE_LINE', 'POS_MANUAL_FIXED_LINE', 'JERSEY_LINE_5');
        {{TenantPermissionSeedSqlBuilder.BuildPermissionDeleteSql(PermissionDefinitions)}}
        """;
}
