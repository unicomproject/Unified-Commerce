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
            DO $$
            DECLARE
                rec RECORD;
                target_ids uuid[];
                target_variant_ids uuid[];
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'products') THEN
                    RETURN;
                END IF;

                -- 1. Find all target product IDs by keyword or seed ID
                SELECT array_agg(id) INTO target_ids FROM products 
                WHERE id IN ('cccc0004-0004-4000-8000-000000000001','cccc0004-0005-4000-8000-000000000001','cccc0004-0006-4000-8000-000000000001','cccc0004-0007-4000-8000-000000000001','cccc0004-000e-4000-8000-000000000001')
                   OR product_name ILIKE '%cap%' 
                   OR product_name ILIKE '%bag%' 
                   OR product_name ILIKE '%shoe%' 
                   OR product_name ILIKE '%shock%' 
                   OR product_name ILIKE '%shirt%';

                IF target_ids IS NOT NULL THEN
                    -- Find all associated product variant IDs
                    SELECT array_agg(id) INTO target_variant_ids FROM product_variants WHERE product_id = ANY(target_ids);

                    -- Delete from product_variant_option_values first if exists
                    IF target_variant_ids IS NOT NULL AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'product_variant_option_values') THEN
                        DELETE FROM product_variant_option_values WHERE product_variant_id = ANY(target_variant_ids);
                    END IF;

                    -- Delete from product_option_values and product_options
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'product_options') THEN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'product_option_values') THEN
                            DELETE FROM product_option_values WHERE product_option_id IN (SELECT id FROM product_options WHERE product_id = ANY(target_ids));
                        END IF;
                        DELETE FROM product_options WHERE product_id = ANY(target_ids);
                    END IF;

                    -- Explicitly delete inventory_reservation_allocations referencing inventory_balances
                    IF target_variant_ids IS NOT NULL THEN
                        IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'inventory_reservation_allocations') AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'inventory_balances') THEN
                            DELETE FROM inventory_reservation_allocations WHERE inventory_balance_id IN (SELECT id FROM inventory_balances WHERE product_variant_id = ANY(target_variant_ids));
                        END IF;
                    END IF;

                    -- Delete from any other tables referencing product_variants
                    IF target_variant_ids IS NOT NULL THEN
                        FOR rec IN 
                            SELECT DISTINCT kcu.table_name, kcu.column_name
                            FROM information_schema.referential_constraints rc
                            JOIN information_schema.key_column_usage kcu ON rc.constraint_name = kcu.constraint_name
                            JOIN information_schema.constraint_column_usage ccu ON rc.unique_constraint_name = ccu.constraint_name
                            WHERE ccu.table_name = 'product_variants' AND kcu.table_name != 'product_variants' AND kcu.table_name != 'product_variant_option_values'
                        LOOP
                            EXECUTE format('DELETE FROM %I WHERE %I = ANY($1)', rec.table_name, rec.column_name) USING target_variant_ids;
                        END LOOP;
                    END IF;

                    -- Delete from any other tables referencing products
                    FOR rec IN 
                        SELECT DISTINCT kcu.table_name, kcu.column_name
                        FROM information_schema.referential_constraints rc
                        JOIN information_schema.key_column_usage kcu ON rc.constraint_name = kcu.constraint_name
                        JOIN information_schema.constraint_column_usage ccu ON rc.unique_constraint_name = ccu.constraint_name
                        WHERE ccu.table_name = 'products' AND kcu.table_name != 'products' AND kcu.table_name != 'product_variants' AND kcu.table_name != 'product_options'
                    LOOP
                        EXECUTE format('DELETE FROM %I WHERE %I = ANY($1)', rec.table_name, rec.column_name) USING target_ids;
                    END LOOP;

                    -- Delete product_variants
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'product_variants') THEN
                        DELETE FROM product_variants WHERE product_id = ANY(target_ids);
                    END IF;

                    -- Delete products
                    DELETE FROM products WHERE id = ANY(target_ids);
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
