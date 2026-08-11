using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260808120000_AddProductWizardStep1AndStagedMedia")]
public sealed class AddProductWizardStep1AndStagedMedia : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE products
            ADD COLUMN IF NOT EXISTS desired_publish_status varchar(40) NULL;
            """);

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'ck_products_desired_publish_status'
                ) THEN
                    ALTER TABLE products
                    ADD CONSTRAINT ck_products_desired_publish_status
                    CHECK (desired_publish_status IS NULL OR desired_publish_status IN ('ACTIVE','INACTIVE'));
                END IF;
            END $$;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE media_assets DROP CONSTRAINT IF EXISTS ck_media_assets_status;
            ALTER TABLE media_assets
            ADD CONSTRAINT ck_media_assets_status
            CHECK (status IN ('ACTIVE', 'INACTIVE', 'STAGED', 'DELETE_PENDING', 'DELETED'));
            """);

        migrationBuilder.Sql("""
            WITH ranked_primary AS (
                SELECT id,
                       ROW_NUMBER() OVER (
                           PARTITION BY tenant_id, product_id
                           ORDER BY created_at ASC, id ASC
                       ) as rn
                FROM product_images
                WHERE is_primary_image = true AND status = 'ACTIVE' AND product_variant_id IS NULL
            )
            UPDATE product_images
            SET is_primary_image = false
            WHERE id IN (
                SELECT id FROM ranked_primary WHERE rn > 1
            );
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS uq_product_images_tenant_product_primary
            ON product_images (tenant_id, product_id)
            WHERE is_primary_image = true AND status = 'ACTIVE' AND product_variant_id IS NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS uq_product_images_tenant_product_primary;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE media_assets DROP CONSTRAINT IF EXISTS ck_media_assets_status;
            ALTER TABLE media_assets
            ADD CONSTRAINT ck_media_assets_status
            CHECK (status IN ('ACTIVE', 'INACTIVE', 'DELETE_PENDING', 'DELETED'));
            """);

        migrationBuilder.Sql("""
            ALTER TABLE products DROP CONSTRAINT IF EXISTS ck_products_desired_publish_status;
            ALTER TABLE products DROP COLUMN IF EXISTS desired_publish_status;
            """);
    }
}
