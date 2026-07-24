using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260716190000_AddReturnInspectionDraftsAndMediaFinalization")]
public partial class AddReturnInspectionDraftsAndMediaFinalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS return_inspection_drafts (
              id uuid NOT NULL PRIMARY KEY, tenant_id uuid NOT NULL, outlet_id uuid NOT NULL, sale_id uuid NOT NULL,
              status varchar(20) NOT NULL DEFAULT 'DRAFT', validated_at timestamptz NULL,
              validated_by_tenant_user_id uuid NULL, created_by_tenant_user_id uuid NOT NULL, created_at timestamptz NOT NULL,
              CONSTRAINT ux_return_inspection_drafts_sale UNIQUE (tenant_id, outlet_id, sale_id));
            CREATE TABLE IF NOT EXISTS return_inspection_draft_lines (
              id uuid NOT NULL PRIMARY KEY, tenant_id uuid NOT NULL, return_inspection_draft_id uuid NOT NULL,
              sale_line_id uuid NOT NULL, condition_id uuid NULL, condition_code_snapshot varchar(40) NOT NULL,
              inspection_notes text NULL, inspection_status varchar(20) NOT NULL DEFAULT 'PENDING',
              inspected_by_tenant_user_id uuid NULL, inspected_at timestamptz NULL, created_at timestamptz NOT NULL,
              CONSTRAINT ux_return_inspection_draft_lines_sale_line UNIQUE (tenant_id, return_inspection_draft_id, sale_line_id));
            CREATE TABLE IF NOT EXISTS return_inspection_media (
              id uuid NOT NULL PRIMARY KEY, tenant_id uuid NOT NULL, return_inspection_id uuid NOT NULL,
              storage_key varchar(500) NOT NULL, file_name varchar(255) NOT NULL, content_type varchar(100) NOT NULL,
              size_bytes bigint NOT NULL, uploaded_by_tenant_user_id uuid NOT NULL, created_at timestamptz NOT NULL);
            ALTER TABLE return_inspection_media_staging ADD COLUMN IF NOT EXISTS outlet_id uuid NULL;
            ALTER TABLE return_inspection_media_staging ADD COLUMN IF NOT EXISTS inspection_draft_id uuid NULL;
            ALTER TABLE return_inspection_media_staging ADD COLUMN IF NOT EXISTS inspection_draft_line_id uuid NULL;
            ALTER TABLE return_inspection_media_staging ADD COLUMN IF NOT EXISTS status varchar(20) NOT NULL DEFAULT 'STAGED';
            ALTER TABLE return_inspection_media_staging ADD COLUMN IF NOT EXISTS expires_at timestamptz NULL;
            ALTER TABLE return_inspection_media_staging ADD COLUMN IF NOT EXISTS consumed_at timestamptz NULL;
            UPDATE return_inspection_media_staging SET expires_at = created_at + interval '24 hours' WHERE expires_at IS NULL;
            ALTER TABLE return_inspection_media_staging ALTER COLUMN expires_at SET NOT NULL;
            CREATE INDEX IF NOT EXISTS ix_return_inspection_media_inspection ON return_inspection_media (tenant_id, return_inspection_id);
            CREATE INDEX IF NOT EXISTS ix_return_inspection_media_staging_expiry ON return_inspection_media_staging (status, expires_at);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS return_inspection_media;
            DROP TABLE IF EXISTS return_inspection_draft_lines;
            DROP TABLE IF EXISTS return_inspection_drafts;
            ALTER TABLE return_inspection_media_staging DROP COLUMN IF EXISTS consumed_at;
            ALTER TABLE return_inspection_media_staging DROP COLUMN IF EXISTS expires_at;
            ALTER TABLE return_inspection_media_staging DROP COLUMN IF EXISTS status;
            ALTER TABLE return_inspection_media_staging DROP COLUMN IF EXISTS inspection_draft_line_id;
            ALTER TABLE return_inspection_media_staging DROP COLUMN IF EXISTS inspection_draft_id;
            ALTER TABLE return_inspection_media_staging DROP COLUMN IF EXISTS outlet_id;
            """);
    }
}
