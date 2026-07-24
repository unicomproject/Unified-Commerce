using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Step 6 Inspect Items hardening:
/// draft version/expiry, media soft-delete, tenant-safe FKs, CHECKs.
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260717150000_HardenReturnInspectionDraftMediaLifecycle")]
public partial class HardenReturnInspectionDraftMediaLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Draft version + expiry
            ALTER TABLE return_inspection_drafts
                ADD COLUMN IF NOT EXISTS version integer NOT NULL DEFAULT 1;
            ALTER TABLE return_inspection_drafts
                ADD COLUMN IF NOT EXISTS expires_at timestamptz NULL;

            UPDATE return_inspection_drafts
            SET expires_at = created_at + interval '24 hours'
            WHERE expires_at IS NULL;

            ALTER TABLE return_inspection_drafts
                ALTER COLUMN expires_at SET NOT NULL;

            -- Media soft-delete timestamp
            ALTER TABLE return_inspection_media_staging
                ADD COLUMN IF NOT EXISTS deleted_at timestamptz NULL;

            -- Alternate keys for composite FKs
            CREATE UNIQUE INDEX IF NOT EXISTS uq_return_inspection_drafts_tenant_id_id
                ON return_inspection_drafts (tenant_id, id);
            CREATE UNIQUE INDEX IF NOT EXISTS uq_return_inspection_draft_lines_tenant_id_id
                ON return_inspection_draft_lines (tenant_id, id);
            CREATE UNIQUE INDEX IF NOT EXISTS uq_return_inspection_conditions_tenant_id_id
                ON return_inspection_conditions (tenant_id, id);
            CREATE UNIQUE INDEX IF NOT EXISTS uq_outlets_tenant_id_id
                ON outlets (tenant_id, id);
            CREATE UNIQUE INDEX IF NOT EXISTS uq_sales_orders_tenant_id_id
                ON sales_orders (tenant_id, id);
            CREATE UNIQUE INDEX IF NOT EXISTS uq_sales_order_lines_tenant_id_id
                ON sales_order_lines (tenant_id, id);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_return_inspection_media_staging_storage_key
                ON return_inspection_media_staging (storage_key);

            -- Normalize invalid statuses before CHECKs
            UPDATE return_inspection_drafts
            SET status = 'DRAFT'
            WHERE status IS NULL OR UPPER(TRIM(status)) NOT IN ('DRAFT', 'VALIDATED', 'CONSUMED', 'CANCELLED');

            UPDATE return_inspection_draft_lines
            SET inspection_status = 'PENDING'
            WHERE inspection_status IS NULL
               OR UPPER(TRIM(inspection_status)) NOT IN ('PENDING', 'INSPECTED');

            UPDATE return_inspection_conditions
            SET sort_order = 0
            WHERE sort_order < 0;

            UPDATE return_inspection_conditions
            SET status_category = 'GOOD'
            WHERE status_category IS NULL
               OR UPPER(TRIM(status_category)) NOT IN ('GOOD', 'WARNING', 'DANGER');

            UPDATE return_inspection_conditions
            SET refund_impact = 'NONE'
            WHERE refund_impact IS NULL
               OR UPPER(TRIM(refund_impact)) NOT IN ('NONE', 'PARTIAL', 'FULL_DENIAL');

            UPDATE return_inspection_media_staging
            SET status = 'STAGED'
            WHERE status IS NULL
               OR UPPER(TRIM(status)) NOT IN ('STAGED', 'CONSUMED', 'EXPIRED', 'DELETED');

            -- Drop and restore CHECKs
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_status;
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_version;
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_expires_at;
            ALTER TABLE return_inspection_drafts
                ADD CONSTRAINT ck_return_inspection_drafts_status
                CHECK (status IN ('DRAFT', 'VALIDATED', 'CONSUMED', 'CANCELLED'));
            ALTER TABLE return_inspection_drafts
                ADD CONSTRAINT ck_return_inspection_drafts_version CHECK (version >= 1);
            ALTER TABLE return_inspection_drafts
                ADD CONSTRAINT ck_return_inspection_drafts_expires_at CHECK (expires_at > created_at);

            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS ck_return_inspection_draft_lines_status;
            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS ck_return_inspection_draft_lines_notes_length;
            ALTER TABLE return_inspection_draft_lines
                ADD CONSTRAINT ck_return_inspection_draft_lines_status
                CHECK (inspection_status IN ('PENDING', 'INSPECTED'));
            ALTER TABLE return_inspection_draft_lines
                ADD CONSTRAINT ck_return_inspection_draft_lines_notes_length
                CHECK (inspection_notes IS NULL OR char_length(inspection_notes) <= 200);

            ALTER TABLE return_inspection_conditions DROP CONSTRAINT IF EXISTS ck_return_inspection_conditions_sort_order;
            ALTER TABLE return_inspection_conditions DROP CONSTRAINT IF EXISTS ck_return_inspection_conditions_status_category;
            ALTER TABLE return_inspection_conditions DROP CONSTRAINT IF EXISTS ck_return_inspection_conditions_refund_impact;
            ALTER TABLE return_inspection_conditions
                ADD CONSTRAINT ck_return_inspection_conditions_sort_order CHECK (sort_order >= 0);
            ALTER TABLE return_inspection_conditions
                ADD CONSTRAINT ck_return_inspection_conditions_status_category
                CHECK (status_category IN ('GOOD', 'WARNING', 'DANGER'));
            ALTER TABLE return_inspection_conditions
                ADD CONSTRAINT ck_return_inspection_conditions_refund_impact
                CHECK (refund_impact IN ('NONE', 'PARTIAL', 'FULL_DENIAL'));

            ALTER TABLE return_inspection_media_staging DROP CONSTRAINT IF EXISTS ck_return_inspection_media_staging_status;
            ALTER TABLE return_inspection_media_staging DROP CONSTRAINT IF EXISTS ck_return_inspection_media_staging_size_bytes;
            ALTER TABLE return_inspection_media_staging DROP CONSTRAINT IF EXISTS ck_return_inspection_media_staging_expires_at;
            ALTER TABLE return_inspection_media_staging
                ADD CONSTRAINT ck_return_inspection_media_staging_status
                CHECK (status IN ('STAGED', 'CONSUMED', 'EXPIRED', 'DELETED'));
            ALTER TABLE return_inspection_media_staging
                ADD CONSTRAINT ck_return_inspection_media_staging_size_bytes
                CHECK (size_bytes > 0 AND size_bytes <= 5242880);
            ALTER TABLE return_inspection_media_staging
                ADD CONSTRAINT ck_return_inspection_media_staging_expires_at
                CHECK (expires_at > created_at);

            -- Tenant-safe FKs (drop prior loose FKs if any)
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS fk_return_inspection_drafts_outlet_tenant;
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS fk_return_inspection_drafts_sale_tenant;
            ALTER TABLE return_inspection_drafts
                ADD CONSTRAINT fk_return_inspection_drafts_outlet_tenant
                FOREIGN KEY (tenant_id, outlet_id) REFERENCES outlets (tenant_id, id) ON DELETE RESTRICT;
            ALTER TABLE return_inspection_drafts
                ADD CONSTRAINT fk_return_inspection_drafts_sale_tenant
                FOREIGN KEY (tenant_id, sale_id) REFERENCES sales_orders (tenant_id, id) ON DELETE RESTRICT;

            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS fk_return_inspection_draft_lines_draft_tenant;
            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS fk_return_inspection_draft_lines_sale_line_tenant;
            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS fk_return_inspection_draft_lines_condition_tenant;
            ALTER TABLE return_inspection_draft_lines
                ADD CONSTRAINT fk_return_inspection_draft_lines_draft_tenant
                FOREIGN KEY (tenant_id, return_inspection_draft_id)
                REFERENCES return_inspection_drafts (tenant_id, id) ON DELETE RESTRICT;
            ALTER TABLE return_inspection_draft_lines
                ADD CONSTRAINT fk_return_inspection_draft_lines_sale_line_tenant
                FOREIGN KEY (tenant_id, sale_line_id)
                REFERENCES sales_order_lines (tenant_id, id) ON DELETE RESTRICT;
            ALTER TABLE return_inspection_draft_lines
                ADD CONSTRAINT fk_return_inspection_draft_lines_condition_tenant
                FOREIGN KEY (tenant_id, condition_id)
                REFERENCES return_inspection_conditions (tenant_id, id) ON DELETE RESTRICT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS fk_return_inspection_draft_lines_condition_tenant;
            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS fk_return_inspection_draft_lines_sale_line_tenant;
            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS fk_return_inspection_draft_lines_draft_tenant;
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS fk_return_inspection_drafts_sale_tenant;
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS fk_return_inspection_drafts_outlet_tenant;

            ALTER TABLE return_inspection_media_staging DROP CONSTRAINT IF EXISTS ck_return_inspection_media_staging_status;
            ALTER TABLE return_inspection_media_staging DROP CONSTRAINT IF EXISTS ck_return_inspection_media_staging_size_bytes;
            ALTER TABLE return_inspection_media_staging DROP CONSTRAINT IF EXISTS ck_return_inspection_media_staging_expires_at;
            ALTER TABLE return_inspection_conditions DROP CONSTRAINT IF EXISTS ck_return_inspection_conditions_sort_order;
            ALTER TABLE return_inspection_conditions DROP CONSTRAINT IF EXISTS ck_return_inspection_conditions_status_category;
            ALTER TABLE return_inspection_conditions DROP CONSTRAINT IF EXISTS ck_return_inspection_conditions_refund_impact;
            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS ck_return_inspection_draft_lines_status;
            ALTER TABLE return_inspection_draft_lines DROP CONSTRAINT IF EXISTS ck_return_inspection_draft_lines_notes_length;
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_status;
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_version;
            ALTER TABLE return_inspection_drafts DROP CONSTRAINT IF EXISTS ck_return_inspection_drafts_expires_at;

            DROP INDEX IF EXISTS ux_return_inspection_media_staging_storage_key;
            DROP INDEX IF EXISTS uq_return_inspection_conditions_tenant_id_id;
            DROP INDEX IF EXISTS uq_return_inspection_draft_lines_tenant_id_id;
            DROP INDEX IF EXISTS uq_return_inspection_drafts_tenant_id_id;

            ALTER TABLE return_inspection_media_staging DROP COLUMN IF EXISTS deleted_at;
            ALTER TABLE return_inspection_drafts DROP COLUMN IF EXISTS expires_at;
            ALTER TABLE return_inspection_drafts DROP COLUMN IF EXISTS version;
            """);
    }
}
