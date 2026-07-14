using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260713120000_UpdateDevelopmentMerchandiseProductImageUrls")]
public partial class UpdateDevelopmentMerchandiseProductImageUrls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(DevelopmentMerchandiseCatalogSeedData.ProductImageUpSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE product_images AS image
            SET image_url = previous.image_url,
                mime_type = 'image/png',
                updated_at = now()
            FROM (VALUES
                ('cccc0008-0001-4000-8000-000000000001'::uuid, 'https://placehold.co/640x480/png?text=Team+Jersey'),
                ('cccc0008-0002-4000-8000-000000000001'::uuid, 'https://placehold.co/640x480/png?text=Training+Jersey'),
                ('cccc0008-0003-4000-8000-000000000001'::uuid, 'https://placehold.co/640x480/png?text=Match+Shorts'),
                ('cccc0008-0004-4000-8000-000000000001'::uuid, 'https://placehold.co/640x480/png?text=Team+Socks'),
                ('cccc0008-0006-4000-8000-000000000001'::uuid, 'https://placehold.co/640x480/png?text=Casual+Sneakers'),
                ('cccc0008-0007-4000-8000-000000000001'::uuid, 'https://placehold.co/640x480/png?text=Team+Cap'),
                ('cccc0008-0008-4000-8000-000000000001'::uuid, 'https://placehold.co/640x480/png?text=Fan+Scarf'),
                ('cccc0008-0009-4000-8000-000000000001'::uuid, 'https://placehold.co/640x480/png?text=Club+Keychain')
            ) AS previous(id, image_url)
            WHERE image.id = previous.id;
            """);
    }
}
