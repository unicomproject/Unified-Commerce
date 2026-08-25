using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent seed for Product Wizard specialized catalog permissions.
/// Grants them to every TENANT_ADMIN role and every role/user that already
/// holds product create, so Add Product start eligibility matches historical
/// Tenant Admin create access.
/// </summary>
public static class ProductWizardSpecializedPermissionSeedData
{
    public static readonly Guid BarcodesManageId =
        Guid.Parse("a6082412-0001-4000-8000-000000000001");

    public static readonly Guid ProductPricingManageId =
        Guid.Parse("a6082412-0002-4000-8000-000000000001");

    public static readonly Guid ProductsPublishId =
        Guid.Parse("a6082412-0003-4000-8000-000000000001");

    public static readonly Guid VariantsManageId =
        Guid.Parse("a6082412-0004-4000-8000-000000000001");

    public static readonly Guid ComboComponentsManageId =
        Guid.Parse("a6082412-0005-4000-8000-000000000001");

    public static readonly Guid TaxClassesViewId =
        Guid.Parse("a6082412-0006-4000-8000-000000000001");

    public static readonly IReadOnlyList<string> CatalogPermissionCodes =
    [
        ProductConstants.BarcodesManagePermission,
        ProductConstants.ProductPricingManagePermission,
        ProductConstants.PublishPermission,
        ProductConstants.VariantsManagePermission,
        ProductConstants.ComboComponentsManagePermission
    ];

    public static string UpSql => """
        INSERT INTO permission_definitions (
            id,
            permission_code,
            module_id,
            feature_id,
            action_type,
            description,
            is_system,
            is_active,
            created_at,
            updated_at
        )
        SELECT
            seed.id,
            seed.permission_code,
            product_template.module_id,
            product_template.feature_id,
            seed.action_type,
            seed.description,
            TRUE,
            TRUE,
            now(),
            now()
        FROM (
            VALUES
                ('a6082412-0001-4000-8000-000000000001'::uuid, 'catalog.barcodes.manage', 'manage', 'Manage product barcodes and SKUs'),
                ('a6082412-0002-4000-8000-000000000001'::uuid, 'catalog.product_pricing.manage', 'manage', 'Manage product selling price and tax assignment'),
                ('a6082412-0003-4000-8000-000000000001'::uuid, 'catalog.products.publish', 'publish', 'Publish product setup drafts'),
                ('a6082412-0004-4000-8000-000000000001'::uuid, 'catalog.variants.manage', 'manage', 'Manage product variant configuration'),
                ('a6082412-0005-4000-8000-000000000001'::uuid, 'catalog.combo_components.manage', 'manage', 'Manage bundle components')
        ) AS seed(id, permission_code, action_type, description)
        CROSS JOIN LATERAL (
            SELECT module_id, feature_id
            FROM permission_definitions
            WHERE permission_code IN ('catalog.products.create', 'catalog.products.view')
            ORDER BY CASE WHEN permission_code = 'catalog.products.create' THEN 0 ELSE 1 END
            LIMIT 1
        ) AS product_template
        WHERE NOT EXISTS (
            SELECT 1
            FROM permission_definitions existing
            WHERE existing.permission_code = seed.permission_code
               OR existing.id = seed.id
        );

        INSERT INTO permission_definitions (
            id,
            permission_code,
            module_id,
            feature_id,
            action_type,
            description,
            is_system,
            is_active,
            created_at,
            updated_at
        )
        SELECT
            'a6082412-0006-4000-8000-000000000001'::uuid,
            'pricing.tax_classes.view',
            tax_template.module_id,
            tax_template.feature_id,
            'view',
            'View tax classes for product pricing',
            TRUE,
            TRUE,
            now(),
            now()
        FROM (
            SELECT module_id, feature_id
            FROM permission_definitions
            WHERE permission_code IN ('tax.classes.view', 'catalog.products.create', 'catalog.products.view')
            ORDER BY CASE
                WHEN permission_code = 'tax.classes.view' THEN 0
                WHEN permission_code = 'catalog.products.create' THEN 1
                ELSE 2
            END
            LIMIT 1
        ) AS tax_template
        WHERE NOT EXISTS (
            SELECT 1
            FROM permission_definitions existing
            WHERE existing.permission_code = 'pricing.tax_classes.view'
               OR existing.id = 'a6082412-0006-4000-8000-000000000001'::uuid
        );

        UPDATE permission_definitions
        SET
            is_active = TRUE,
            updated_at = now()
        WHERE permission_code IN (
            'catalog.barcodes.manage',
            'catalog.product_pricing.manage',
            'catalog.products.publish',
            'catalog.variants.manage',
            'catalog.combo_components.manage',
            'pricing.tax_classes.view'
        );

        INSERT INTO tenant_role_permissions (
            id,
            tenant_id,
            role_id,
            permission_id,
            notes,
            granted_at,
            created_at
        )
        SELECT
            md5(tenant_roles.tenant_id::text || ':' || tenant_roles.id::text || ':' || permission_definitions.permission_code)::uuid,
            tenant_roles.tenant_id,
            tenant_roles.id,
            permission_definitions.id,
            'Product wizard specialized permission seed for Tenant Admin.',
            now(),
            now()
        FROM tenant_roles
        JOIN permission_definitions
            ON permission_definitions.permission_code IN (
                'catalog.barcodes.manage',
                'catalog.product_pricing.manage',
                'catalog.products.publish',
                'catalog.variants.manage',
                'catalog.combo_components.manage',
                'pricing.tax_classes.view'
            )
        WHERE tenant_roles.role_code = 'TENANT_ADMIN'
          AND tenant_roles.is_active = TRUE
        ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;

        INSERT INTO tenant_role_permissions (
            id,
            tenant_id,
            role_id,
            permission_id,
            notes,
            granted_at,
            created_at
        )
        SELECT DISTINCT
            md5(existing.tenant_id::text || ':' || existing.role_id::text || ':' || needed.permission_code)::uuid,
            existing.tenant_id,
            existing.role_id,
            needed.id,
            'Product wizard specialized permission seed for product-create roles.',
            now(),
            now()
        FROM tenant_role_permissions existing
        JOIN permission_definitions create_def
            ON create_def.id = existing.permission_id
        JOIN permission_definitions needed
            ON needed.permission_code IN (
                'catalog.barcodes.manage',
                'catalog.product_pricing.manage',
                'catalog.products.publish',
                'catalog.variants.manage',
                'catalog.combo_components.manage',
                'pricing.tax_classes.view'
            )
        WHERE create_def.permission_code IN ('catalog.products.create', 'tenant.products.create')
          AND existing.revoked_at IS NULL
        ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;

        INSERT INTO tenant_user_permissions (
            id,
            tenant_id,
            user_id,
            permission_id,
            assigned_at,
            created_at
        )
        SELECT DISTINCT
            md5(existing.tenant_id::text || ':' || existing.user_id::text || ':' || needed.permission_code)::uuid,
            existing.tenant_id,
            existing.user_id,
            needed.id,
            now(),
            now()
        FROM tenant_user_permissions existing
        JOIN permission_definitions create_def
            ON create_def.id = existing.permission_id
        JOIN permission_definitions needed
            ON needed.permission_code IN (
                'catalog.barcodes.manage',
                'catalog.product_pricing.manage',
                'catalog.products.publish',
                'catalog.variants.manage',
                'catalog.combo_components.manage',
                'pricing.tax_classes.view'
            )
        WHERE create_def.permission_code IN ('catalog.products.create', 'tenant.products.create')
          AND existing.revoked_at IS NULL
        ON CONFLICT (tenant_id, user_id, permission_id) DO NOTHING;
        """;

    public static string DownSql => """
        DELETE FROM tenant_user_permissions
        USING permission_definitions
        WHERE tenant_user_permissions.permission_id = permission_definitions.id
          AND permission_definitions.permission_code IN (
            'catalog.barcodes.manage',
            'catalog.product_pricing.manage',
            'catalog.products.publish',
            'catalog.variants.manage',
            'catalog.combo_components.manage',
            'pricing.tax_classes.view'
          );

        DELETE FROM tenant_role_permissions
        USING permission_definitions
        WHERE tenant_role_permissions.permission_id = permission_definitions.id
          AND permission_definitions.permission_code IN (
            'catalog.barcodes.manage',
            'catalog.product_pricing.manage',
            'catalog.products.publish',
            'catalog.variants.manage',
            'catalog.combo_components.manage',
            'pricing.tax_classes.view'
          );

        DELETE FROM permission_definitions
        WHERE id IN (
            'a6082412-0001-4000-8000-000000000001'::uuid,
            'a6082412-0002-4000-8000-000000000001'::uuid,
            'a6082412-0003-4000-8000-000000000001'::uuid,
            'a6082412-0004-4000-8000-000000000001'::uuid,
            'a6082412-0005-4000-8000-000000000001'::uuid,
            'a6082412-0006-4000-8000-000000000001'::uuid
        )
          AND permission_code IN (
            'catalog.barcodes.manage',
            'catalog.product_pricing.manage',
            'catalog.products.publish',
            'catalog.variants.manage',
            'catalog.combo_components.manage',
            'pricing.tax_classes.view'
          );
        """;
}
