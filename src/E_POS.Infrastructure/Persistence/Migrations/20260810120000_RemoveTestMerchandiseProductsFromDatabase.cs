using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260810120000_RemoveTestMerchandiseProductsFromDatabase")]
public partial class RemoveTestMerchandiseProductsFromDatabase : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Preserve inventory and sales audit history. These known development
            -- seed products are hidden from every sellable catalog instead of being
            -- hard-deleted through RESTRICT-protected financial/inventory records.
            UPDATE product_variants
            SET status = 'INACTIVE',
                is_sellable = false,
                updated_at = now()
            WHERE product_id IN (
                'cccc0004-0004-4000-8000-000000000001'::uuid,
                'cccc0004-0005-4000-8000-000000000001'::uuid,
                'cccc0004-0006-4000-8000-000000000001'::uuid,
                'cccc0004-0007-4000-8000-000000000001'::uuid,
                'cccc0004-000e-4000-8000-000000000001'::uuid
            );

            UPDATE products
            SET status = 'INACTIVE',
                is_sellable = false,
                archived_at = COALESCE(archived_at, now()),
                desired_publish_status = 'INACTIVE',
                updated_at = now()
            WHERE id IN (
                'cccc0004-0004-4000-8000-000000000001'::uuid,
                'cccc0004-0005-4000-8000-000000000001'::uuid,
                'cccc0004-0006-4000-8000-000000000001'::uuid,
                'cccc0004-0007-4000-8000-000000000001'::uuid,
                'cccc0004-000e-4000-8000-000000000001'::uuid
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
