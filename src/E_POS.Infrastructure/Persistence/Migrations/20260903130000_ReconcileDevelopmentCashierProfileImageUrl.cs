using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260903130000_ReconcileDevelopmentCashierProfileImageUrl")]
public partial class ReconcileDevelopmentCashierProfileImageUrl : Migration
{
    public const string RepairSql = """
        DO $repair$
        DECLARE
            seed_tenant constant uuid := '55555555-0000-4000-8000-000000000001';
            seed_asset constant uuid := 'dddddddd-0001-4000-8000-000000000001';
            seed_email constant text := 'CASHIER001@GMAIL.COM';
            seed_storage_key constant text := 'development/users/cashier001/profile.jpg';
            legacy_local_url constant text := '/uploads/images/tenants/55555555-0000-4000-8000-000000000001/users/cashier001/profile.jpg';
            canonical_url constant text := 'https://imgcdn.stablediffusionweb.com/2024/10/15/12d6f588-c9ab-4c05-82f0-99f9c2c0453f.jpg';
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM media_assets
                WHERE id = seed_asset
                  AND (
                      tenant_id <> seed_tenant
                      OR (storage_key <> seed_storage_key AND public_url <> legacy_local_url)
                  )
            ) THEN
                RAISE EXCEPTION 'BLOCKED — DEVELOPMENT CASHIER PROFILE MEDIA IDENTITY CONFLICT';
            END IF;

            UPDATE media_assets
            SET public_url = canonical_url,
                original_file_name = 'cashier001-profile.jpg',
                mime_type = 'image/jpeg',
                file_extension = '.jpg',
                checksum_hash = md5(canonical_url),
                status = 'ACTIVE',
                updated_at = now()
            WHERE id = seed_asset
              AND tenant_id = seed_tenant
              AND (storage_key = seed_storage_key OR public_url = legacy_local_url);

            UPDATE tenant_users
            SET profile_image_url = seed_asset,
                updated_at = now()
            WHERE tenant_id = seed_tenant
              AND email = seed_email
              AND (profile_image_url IS NULL OR profile_image_url = seed_asset);
        END $repair$;
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(RepairSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentional no-op. Do not restore the stale runtime-local URL.
    }
}
