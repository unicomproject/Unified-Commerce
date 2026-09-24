-- Focused local-development repair. Run inside a transaction.
-- Preserve revoked grants and limit changes to the development Tenant Admin role.
INSERT INTO tenant_role_permissions
    (id, tenant_id, role_id, permission_id, granted_by_tenant_user_id,
     granted_at, notes, created_at)
SELECT md5(role.id::text || ':' || permission.id::text)::uuid,
    role.tenant_id, role.id, permission.id, NULL, now(),
    'Development product card visibility permission repair.', now()
FROM tenant_roles role
JOIN permission_definitions permission ON permission.permission_code IN (
    'pos.sales.catalog.view',
    'pos.catalog.product_card.image',
    'pos.catalog.product_card.name',
    'pos.catalog.product_card.regular_price',
    'pos.catalog.product_card.sale_price',
    'pos.catalog.product_card.discount_badge')
WHERE role.tenant_id = '55555555-0000-4000-8000-000000000001'
    AND role.id = '88888888-0001-4000-8000-000000000001'
    AND role.role_code = 'TENANT_ADMIN' AND role.is_active AND permission.is_active
    AND EXISTS (
        SELECT 1 FROM tenant_role_permissions parent_grant
        JOIN permission_definitions parent ON parent.id = parent_grant.permission_id
        WHERE parent_grant.tenant_id = role.tenant_id AND parent_grant.role_id = role.id
            AND parent_grant.revoked_at IS NULL AND parent.is_active
            AND parent.permission_code IN ('products.view', 'pos.sales.catalog.view'))
ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;

