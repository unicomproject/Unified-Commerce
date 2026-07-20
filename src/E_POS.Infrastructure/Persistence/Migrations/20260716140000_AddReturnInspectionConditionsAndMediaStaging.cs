using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260716140000_AddReturnInspectionConditionsAndMediaStaging")]
public partial class AddReturnInspectionConditionsAndMediaStaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS return_inspection_conditions (
                id uuid NOT NULL,
                tenant_id uuid NOT NULL,
                condition_code varchar(40) NOT NULL,
                display_name varchar(150) NOT NULL,
                description text NULL,
                status_category varchar(30) NOT NULL,
                is_resellable boolean NOT NULL DEFAULT false,
                refund_impact varchar(30) NOT NULL,
                requires_notes boolean NOT NULL DEFAULT false,
                requires_photo boolean NOT NULL DEFAULT false,
                requires_approval boolean NOT NULL DEFAULT false,
                is_active boolean NOT NULL DEFAULT true,
                sort_order int NOT NULL DEFAULT 0,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NULL,
                CONSTRAINT pk_return_inspection_conditions PRIMARY KEY (id),
                CONSTRAINT ux_return_inspection_conditions_tenant_code UNIQUE (tenant_id, condition_code)
            );

            CREATE TABLE IF NOT EXISTS return_inspection_media_staging (
                id uuid NOT NULL,
                tenant_id uuid NOT NULL,
                sale_id uuid NOT NULL,
                sale_line_id uuid NOT NULL,
                storage_key varchar(500) NOT NULL,
                file_name varchar(255) NOT NULL,
                content_type varchar(100) NOT NULL,
                size_bytes bigint NOT NULL,
                uploaded_by_tenant_user_id uuid NOT NULL,
                created_at timestamptz NOT NULL,
                CONSTRAINT pk_return_inspection_media_staging PRIMARY KEY (id)
            );

            CREATE INDEX IF NOT EXISTS ix_return_inspection_media_staging_sale_line
                ON return_inspection_media_staging (tenant_id, sale_id, sale_line_id);
            """);

        migrationBuilder.Sql(DevelopmentReturnInspectionConditionsSeedData.UpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentReturnInspectionConditionsSeedData.DownSql);
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS return_inspection_media_staging;
            DROP TABLE IF EXISTS return_inspection_conditions;
            """);
    }
}
