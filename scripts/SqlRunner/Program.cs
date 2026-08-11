using System;
using System.Threading.Tasks;
using Npgsql;

namespace SqlRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var pgConnString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

            try
            {
                using var conn = new NpgsqlConnection(pgConnString);
                await conn.OpenAsync();
                
                var sql = @"
                    -- 1. Get the subcategory IDs and parent category image IDs
                    UPDATE categories c
                    SET image_media_asset_id = p.image_media_asset_id
                    FROM categories p
                    WHERE c.parent_category_id = p.id AND c.tenant_id = '00000000-0000-0000-0000-000000000010';

                    -- 2. Map products to the new subcategories
                    -- Jersey (00000000-0000-0000-0000-000000000070) -> Men's Jerseys
                    INSERT INTO product_categories (id, tenant_id, product_id, category_id, created_at, updated_at)
                    SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000010', '00000000-0000-0000-0000-000000000070', id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                    FROM categories WHERE category_code = 'CAT-MENS-JERS';

                    -- Bat (00000000-0000-0000-0000-000000000080) -> Cricket Bats
                    INSERT INTO product_categories (id, tenant_id, product_id, category_id, created_at, updated_at)
                    SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000010', '00000000-0000-0000-0000-000000000080', id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                    FROM categories WHERE category_code = 'CAT-CRIK-BATS';

                    -- Bottle (00000000-0000-0000-0000-000000000200) -> Sports Bottles
                    INSERT INTO product_categories (id, tenant_id, product_id, category_id, created_at, updated_at)
                    SELECT gen_random_uuid(), '00000000-0000-0000-0000-000000000010', '00000000-0000-0000-0000-000000000200', id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                    FROM categories WHERE category_code = 'CAT-SPRT-BOTL';
                ";
                using var cmd = new NpgsqlCommand(sql, conn);
                int rows = await cmd.ExecuteNonQueryAsync();
                
                Console.WriteLine($"Mapped products to subcategories and copied images.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Error: " + ex.Message);
            }
        }
    }
}
