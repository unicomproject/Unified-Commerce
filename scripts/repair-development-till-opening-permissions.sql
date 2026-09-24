-- Focused local-development repair. Run inside a transaction.
-- Preserve revoked grants and limit changes to the development Tenant Admin role.
INSERT INTO tenant_role_permissions
    (id, tenant_id, role_id, permission_id, granted_by_tenant_user_id,
     granted_at, notes, created_at)
SELECT md5(role.id::text || ':' || permission.id::text)::uuid,
    role.tenant_id, role.id, permission.id, NULL, now(),
    'Development till opening cash-entry permission repair.', now()
FROM tenant_roles role
JOIN permission_definitions permission ON permission.permission_code IN (
    'pos.till.session.open',
    'pos.till.opening.starting_cash_view',
    'pos.till.opening.starting_cash_entry',
    'pos.till.opening.validation_message',
    'pos.till.opening.numpad',
    'pos.till.opening.backspace',
    'pos.till.opening.clear',
    'pos.till.opening.key_0', 'pos.till.opening.key_1',
    'pos.till.opening.key_2', 'pos.till.opening.key_3',
    'pos.till.opening.key_4', 'pos.till.opening.key_5',
    'pos.till.opening.key_6', 'pos.till.opening.key_7',
    'pos.till.opening.key_8', 'pos.till.opening.key_9',
    'pos.till.opening.key_00', 'pos.till.opening.key_decimal')
WHERE role.tenant_id = '55555555-0000-4000-8000-000000000001'
    AND role.id = '88888888-0001-4000-8000-000000000001'
    AND role.role_code = 'TENANT_ADMIN' AND role.is_active AND permission.is_active
    AND EXISTS (
        SELECT 1 FROM tenant_role_permissions parent_grant
        JOIN permission_definitions parent ON parent.id = parent_grant.permission_id
        WHERE parent_grant.tenant_id = role.tenant_id AND parent_grant.role_id = role.id
            AND parent_grant.revoked_at IS NULL AND parent.is_active
            AND parent.permission_code IN ('pos.till.open', 'pos.till.session.open'))
ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;
