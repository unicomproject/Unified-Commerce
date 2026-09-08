using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260903120000_AlignTaxManagementCanonicalContract")]
public partial class AlignTaxManagementCanonicalContract : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "tax_treatment",
            table: "tax_classes",
            type: "varchar(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "TAXABLE");

        migrationBuilder.AddColumn<bool>(
            name: "is_seeded",
            table: "tax_classes",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "notes",
            table: "tax_rates",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "tax_treatment_snapshot",
            table: "sales_order_taxes",
            type: "varchar(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE tax_classes
            SET tax_treatment = CASE
                WHEN UPPER(TRIM(tax_type)) IN ('EXEMPT', 'TAX_EXEMPT', 'EXEMPTION') THEN 'EXEMPT'
                WHEN UPPER(TRIM(tax_type)) IN ('ZERO_RATED', 'ZERO-RATED', 'ZERORATED') THEN 'ZERO_RATED'
                WHEN UPPER(TRIM(tax_type)) IN ('TAXABLE', 'PERCENTAGE', 'VAT', 'GST', 'SALES_TAX', 'SERVICE_TAX') THEN 'TAXABLE'
                WHEN UPPER(TRIM(tax_type)) LIKE '%EXEMPT%' THEN 'EXEMPT'
                WHEN UPPER(TRIM(tax_type)) LIKE '%ZERO%' THEN 'ZERO_RATED'
                ELSE 'TAXABLE'
            END;
            """);

        migrationBuilder.Sql("""
            UPDATE tax_classes
            SET tax_type = tax_treatment
            WHERE tax_type IS DISTINCT FROM tax_treatment;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE tax_classes
            DROP CONSTRAINT IF EXISTS ck_tax_classes_tax_treatment;
            ALTER TABLE tax_classes
            ADD CONSTRAINT ck_tax_classes_tax_treatment
            CHECK (tax_treatment IN ('TAXABLE', 'ZERO_RATED', 'EXEMPT'));
            """);

        migrationBuilder.CreateIndex(
            name: "ix_tax_rates_tenant_id_valid_from",
            table: "tax_rates",
            columns: new[] { "tenant_id", "valid_from" });

        migrationBuilder.Sql("""
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
                tax_template.module_id,
                tax_template.feature_id,
                seed.action_type,
                seed.description,
                TRUE,
                TRUE,
                now(),
                now()
            FROM (
                VALUES
                    ('a6090312-0001-4000-8000-000000000001'::uuid, 'pricing.tax_classes.create', 'create', 'Create Tax Setup'),
                    ('a6090312-0002-4000-8000-000000000001'::uuid, 'pricing.tax_classes.update', 'update', 'Update Tax Setup basic details'),
                    ('a6090312-0003-4000-8000-000000000001'::uuid, 'pricing.tax_classes.status.manage', 'manage', 'Activate or deactivate Tax Setup'),
                    ('a6090312-0004-4000-8000-000000000001'::uuid, 'pricing.tax_classes.products.view', 'view', 'View products using a Tax Setup'),
                    ('a6090312-0005-4000-8000-000000000001'::uuid, 'pricing.tax_rates.view', 'view', 'View tax rate history'),
                    ('a6090312-0006-4000-8000-000000000001'::uuid, 'pricing.tax_rates.schedule.manage', 'manage', 'Schedule, edit, or delete future tax rates')
            ) AS seed(id, permission_code, action_type, description)
            CROSS JOIN LATERAL (
                SELECT module_id, feature_id
                FROM permission_definitions
                WHERE permission_code IN ('tax.classes.view', 'pricing.tax_classes.view', 'catalog.products.view')
                ORDER BY CASE
                    WHEN permission_code = 'tax.classes.view' THEN 0
                    WHEN permission_code = 'pricing.tax_classes.view' THEN 1
                    ELSE 2
                END
                LIMIT 1
            ) AS tax_template
            WHERE NOT EXISTS (
                SELECT 1
                FROM permission_definitions existing
                WHERE existing.permission_code = seed.permission_code
                   OR existing.id = seed.id
            );
            """);

        // Ensure pricing.tax_classes.view exists (may already be seeded by wizard seed).
        migrationBuilder.Sql("""
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
                'View Tax Setup list and detail',
                TRUE,
                TRUE,
                now(),
                now()
            FROM (
                SELECT module_id, feature_id
                FROM permission_definitions
                WHERE permission_code IN ('tax.classes.view', 'catalog.products.view')
                ORDER BY CASE WHEN permission_code = 'tax.classes.view' THEN 0 ELSE 1 END
                LIMIT 1
            ) AS tax_template
            WHERE NOT EXISTS (
                SELECT 1 FROM permission_definitions existing
                WHERE existing.permission_code = 'pricing.tax_classes.view'
                   OR existing.id = 'a6082412-0006-4000-8000-000000000001'::uuid
            );
            """);

        migrationBuilder.Sql("""
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
                md5(tr.tenant_id::text || ':' || tr.id::text || ':' || np.permission_code)::uuid,
                tr.tenant_id,
                tr.id,
                np.id,
                'Tax Management TARGET permission mapped from legacy tax.* grant.',
                now(),
                now()
            FROM tenant_role_permissions trp
            INNER JOIN tenant_roles tr ON tr.id = trp.role_id AND tr.tenant_id = trp.tenant_id
            INNER JOIN permission_definitions lp ON lp.id = trp.permission_id
            INNER JOIN permission_definitions np ON np.permission_code = CASE lp.permission_code
                WHEN 'tax.classes.view' THEN 'pricing.tax_classes.view'
                WHEN 'tax.classes.create' THEN 'pricing.tax_classes.create'
                WHEN 'tax.classes.update' THEN 'pricing.tax_classes.update'
                WHEN 'tax.classes.delete' THEN 'pricing.tax_classes.status.manage'
                WHEN 'tax.classes.manage' THEN 'pricing.tax_classes.status.manage'
                WHEN 'tax.rates.view' THEN 'pricing.tax_rates.view'
                WHEN 'tax.rates.create' THEN 'pricing.tax_rates.schedule.manage'
                WHEN 'tax.rates.update' THEN 'pricing.tax_rates.schedule.manage'
                WHEN 'tax.rates.delete' THEN 'pricing.tax_rates.schedule.manage'
                WHEN 'tax.rates.manage' THEN 'pricing.tax_rates.schedule.manage'
                ELSE NULL
            END
            WHERE trp.revoked_at IS NULL
              AND np.id IS NOT NULL
            ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;
            """);

        // Roles with create/update also get products.view for Tax Setup products-using where they had manage.
        migrationBuilder.Sql("""
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
                md5(tr.tenant_id::text || ':' || tr.id::text || ':pricing.tax_classes.products.view')::uuid,
                tr.tenant_id,
                tr.id,
                np.id,
                'Tax Management products-using grant for roles with Tax Setup view.',
                now(),
                now()
            FROM tenant_role_permissions trp
            INNER JOIN tenant_roles tr ON tr.id = trp.role_id AND tr.tenant_id = trp.tenant_id
            INNER JOIN permission_definitions lp ON lp.id = trp.permission_id
                AND lp.permission_code IN ('pricing.tax_classes.view', 'tax.classes.view', 'tax.classes.manage')
            INNER JOIN permission_definitions np ON np.permission_code = 'pricing.tax_classes.products.view'
            WHERE trp.revoked_at IS NULL
            ON CONFLICT (tenant_id, role_id, permission_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE tax_classes DROP CONSTRAINT IF EXISTS ck_tax_classes_tax_treatment;");

        migrationBuilder.DropIndex(
            name: "ix_tax_rates_tenant_id_valid_from",
            table: "tax_rates");

        migrationBuilder.DropColumn(name: "tax_treatment_snapshot", table: "sales_order_taxes");
        migrationBuilder.DropColumn(name: "notes", table: "tax_rates");
        migrationBuilder.DropColumn(name: "is_seeded", table: "tax_classes");
        migrationBuilder.DropColumn(name: "tax_treatment", table: "tax_classes");
    }
}
