-- Focused local-development repair. Run inside a transaction.
-- Preserve revoked grants and limit changes to the development Tenant Admin role.
INSERT INTO tenant_role_permissions
    (id, tenant_id, role_id, permission_id, granted_by_tenant_user_id,
     granted_at, notes, created_at)
SELECT md5(role.id::text || ':' || permission.id::text)::uuid,
    role.tenant_id, role.id, permission.id, NULL, now(),
    'Development cart visibility permission repair.', now()
FROM tenant_roles role
JOIN permission_definitions permission ON permission.permission_code IN (
    'pos.sales.cart.manage',
    'pos.cart.lines.list',
    'pos.cart.lines.name',
    'pos.cart.lines.quantity',
    'pos.cart.lines.unit_price',
    'pos.cart.lines.line_total',
    'pos.cart.lines.note',
    'pos.cart.lines.image',
    'pos.cart.summary.view',
    'pos.cart.summary.item_count',
    'pos.cart.summary.subtotal',
    'pos.cart.summary.discount',
    'pos.cart.summary.tax',
    'pos.cart.summary.total')
WHERE role.tenant_id = '55555555-0000-4000-8000-000000000001'
    AND role.id = '88888888-0001-4000-8000-000000000001'
    AND role.role_code = 'TENANT_ADMIN' AND role.is_active AND permission.is_active
    AND EXISTS (
        SELECT 1 FROM tenant_role_permissions parent_grant
        JOIN permission_definitions parent ON parent.id = parent_grant.permission_id
        WHERE parent_grant.tenant_id = role.tenant_id AND parent_grant.role_id = role.id
            AND parent_grant.revoked_at IS NULL AND parent.is_active
            AND parent.permission_code IN ('sales.cart.manage', 'pos.sales.cart.manage'))
ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;


