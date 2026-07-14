namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentMerchandiseProductVariantsSeedData
{
    public const string UpSql = """
        DROP TABLE IF EXISTS _seed_merchandise_variants;

        CREATE TEMP TABLE _seed_merchandise_variants (
            product_suffix text NOT NULL,
            option_code text NOT NULL,
            option_name text NOT NULL,
            first_value_suffix text NOT NULL,
            first_value_code text NOT NULL,
            first_value_name text NOT NULL,
            first_color_hex text NULL,
            first_variant_code text NOT NULL,
            second_value_suffix text NOT NULL,
            second_value_code text NOT NULL,
            second_value_name text NOT NULL,
            second_color_hex text NULL,
            second_variant_code text NOT NULL
        );

        INSERT INTO _seed_merchandise_variants (
            product_suffix, option_code, option_name,
            first_value_suffix, first_value_code, first_value_name, first_color_hex, first_variant_code,
            second_value_suffix, second_value_code, second_value_name, second_color_hex, second_variant_code
        )
        VALUES
            ('0001', 'SIZE',      'Size',      '0001', 'SMALL',  'Small',  NULL,      'S',     '0002', 'MEDIUM', 'Medium', NULL,      'M'),
            ('0002', 'SIZE',      'Size',      '0003', 'SMALL',  'Small',  NULL,      'S',     '0004', 'MEDIUM', 'Medium', NULL,      'M'),
            ('0003', 'SIZE',      'Size',      '0005', 'SMALL',  'Small',  NULL,      'S',     '0006', 'MEDIUM', 'Medium', NULL,      'M'),
            ('0004', 'SIZE',      'Size',      '0007', 'SMALL',  'Small',  NULL,      'S',     '0008', 'MEDIUM', 'Medium', NULL,      'M'),
            ('0005', 'SHOE_SIZE', 'Shoe Size', '0009', '8',      'Size 8', NULL,      'SIZE-8','000a', '9',      'Size 9', NULL,      'SIZE-9'),
            ('0006', 'SHOE_SIZE', 'Shoe Size', '000b', '8',      'Size 8', NULL,      'SIZE-8','000c', '9',      'Size 9', NULL,      'SIZE-9'),
            ('0007', 'SIZE',      'Size',      '000d', 'SMALL',  'Small',  NULL,      'S',     '000e', 'MEDIUM', 'Medium', NULL,      'M'),
            ('0008', 'COLOR',     'Color',     '000f', 'BLUE',   'Blue',   '#1B5BFF', 'BLUE',  '0010', 'RED',    'Red',    '#DC2626', 'RED'),
            ('0009', 'FINISH',    'Finish',    '0011', 'SILVER', 'Silver', '#C0C0C0', 'SILVER','0012', 'BLACK',  'Black',  '#111827', 'BLACK'),
            ('000a', 'COLOR',     'Color',     '0013', 'BLUE',   'Blue',   '#1B5BFF', 'BLUE',  '0014', 'RED',    'Red',    '#DC2626', 'RED'),
            ('000b', 'BALL_SIZE', 'Ball Size', '0015', '4',      'Size 4', NULL,      'SIZE-4','0016', '5',      'Size 5', NULL,      'SIZE-5'),
            ('000c', 'BALL_SIZE', 'Ball Size', '0017', '6',      'Size 6', NULL,      'SIZE-6','0018', '7',      'Size 7', NULL,      'SIZE-7'),
            ('000d', 'CAPACITY',  'Capacity',  '0019', '500ML',  '500 ml', NULL,      '500ML', '001a', '750ML',  '750 ml', NULL,      '750ML'),
            ('000e', 'SIZE',      'Size',      '001b', 'MEDIUM', 'Medium', NULL,      'M',     '001c', 'LARGE',  'Large',  NULL,      'L'),
            ('000f', 'COLOR',     'Color',     '001d', 'BLUE',   'Blue',   '#1B5BFF', 'BLUE',  '001e', 'RED',    'Red',    '#DC2626', 'RED');

        UPDATE products AS product
        SET product_structure = 'VARIABLE',
            updated_at = now()
        FROM _seed_merchandise_variants AS seed
        WHERE product.id = format(
            'cccc0004-%s-4000-8000-000000000001',
            seed.product_suffix
        )::uuid
          AND product.tenant_id = '55555555-0000-4000-8000-000000000001';

        INSERT INTO product_options (
            id, tenant_id, product_id, source_option_template_id, option_code, option_name,
            option_type, input_type, is_required, sort_order, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        SELECT
            format('cccc0020-%s-4000-8000-000000000001', seed.product_suffix)::uuid,
            '55555555-0000-4000-8000-000000000001'::uuid,
            format('cccc0004-%s-4000-8000-000000000001', seed.product_suffix)::uuid,
            NULL,
            seed.option_code,
            seed.option_name,
            'VARIANT',
            'SELECT',
            true,
            0,
            'ACTIVE',
            '99999999-0003-4000-8000-000000000001'::uuid,
            '99999999-0003-4000-8000-000000000001'::uuid,
            now(),
            now()
        FROM _seed_merchandise_variants AS seed
        ON CONFLICT (id) DO UPDATE
        SET option_code = EXCLUDED.option_code,
            option_name = EXCLUDED.option_name,
            option_type = 'VARIANT',
            input_type = 'SELECT',
            is_required = true,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO product_option_values (
            id, tenant_id, product_option_id, source_option_template_value_id, value_code, value_name,
            display_name, color_hex, image_url, sort_order, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        SELECT
            format('cccc0021-%s-4000-8000-000000000001', value_seed.value_suffix)::uuid,
            '55555555-0000-4000-8000-000000000001'::uuid,
            format('cccc0020-%s-4000-8000-000000000001', value_seed.product_suffix)::uuid,
            NULL,
            value_seed.value_code,
            value_seed.value_name,
            value_seed.value_name,
            value_seed.color_hex,
            NULL,
            value_seed.sort_order,
            'ACTIVE',
            '99999999-0003-4000-8000-000000000001'::uuid,
            '99999999-0003-4000-8000-000000000001'::uuid,
            now(),
            now()
        FROM (
            SELECT product_suffix, first_value_suffix AS value_suffix, first_value_code AS value_code,
                   first_value_name AS value_name, first_color_hex AS color_hex, 0 AS sort_order
            FROM _seed_merchandise_variants
            UNION ALL
            SELECT product_suffix, second_value_suffix, second_value_code,
                   second_value_name, second_color_hex, 1
            FROM _seed_merchandise_variants
        ) AS value_seed
        ON CONFLICT (id) DO UPDATE
        SET value_code = EXCLUDED.value_code,
            value_name = EXCLUDED.value_name,
            display_name = EXCLUDED.display_name,
            color_hex = EXCLUDED.color_hex,
            sort_order = EXCLUDED.sort_order,
            status = 'ACTIVE',
            updated_at = now();

        UPDATE product_variants AS variant
        SET variant_code = seed.first_variant_code,
            variant_name = seed.first_value_name,
            sku = product.product_code || '-' || seed.first_variant_code,
            option_combination_hash = seed.option_code || ':' || seed.first_value_code,
            is_default_variant = true,
            is_sellable = true,
            status = 'ACTIVE',
            updated_at = now()
        FROM _seed_merchandise_variants AS seed
        JOIN products AS product
          ON product.id = format(
              'cccc0004-%s-4000-8000-000000000001',
              seed.product_suffix
          )::uuid
        WHERE variant.id = format(
            'cccc0005-%s-4000-8000-000000000001',
            seed.product_suffix
        )::uuid
          AND variant.tenant_id = '55555555-0000-4000-8000-000000000001';

        INSERT INTO product_variants (
            id, tenant_id, product_id, variant_code, variant_name, sku, stock_uom_id, sales_uom_id,
            option_combination_hash, is_default_variant, is_sellable, allow_fractional_quantity, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        SELECT
            format('cccc0022-%s-4000-8000-000000000001', seed.product_suffix)::uuid,
            '55555555-0000-4000-8000-000000000001'::uuid,
            product.id,
            seed.second_variant_code,
            seed.second_value_name,
            product.product_code || '-' || seed.second_variant_code,
            '91000000-0000-4000-8000-000000000001'::uuid,
            '91000000-0000-4000-8000-000000000001'::uuid,
            seed.option_code || ':' || seed.second_value_code,
            false,
            true,
            false,
            'ACTIVE',
            '99999999-0003-4000-8000-000000000001'::uuid,
            '99999999-0003-4000-8000-000000000001'::uuid,
            now(),
            now()
        FROM _seed_merchandise_variants AS seed
        JOIN products AS product
          ON product.id = format(
              'cccc0004-%s-4000-8000-000000000001',
              seed.product_suffix
          )::uuid
        ON CONFLICT (id) DO UPDATE
        SET variant_code = EXCLUDED.variant_code,
            variant_name = EXCLUDED.variant_name,
            sku = EXCLUDED.sku,
            option_combination_hash = EXCLUDED.option_combination_hash,
            is_default_variant = false,
            is_sellable = true,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO product_variant_option_values (
            id, tenant_id, product_id, product_variant_id, product_option_id, product_option_value_id,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        SELECT
            format('cccc0023-%s-4000-8000-000000000001', link_seed.value_suffix)::uuid,
            '55555555-0000-4000-8000-000000000001'::uuid,
            format('cccc0004-%s-4000-8000-000000000001', link_seed.product_suffix)::uuid,
            format(link_seed.variant_id_pattern, link_seed.product_suffix)::uuid,
            format('cccc0020-%s-4000-8000-000000000001', link_seed.product_suffix)::uuid,
            format('cccc0021-%s-4000-8000-000000000001', link_seed.value_suffix)::uuid,
            '99999999-0003-4000-8000-000000000001'::uuid,
            '99999999-0003-4000-8000-000000000001'::uuid,
            now(),
            now()
        FROM (
            SELECT product_suffix, first_value_suffix AS value_suffix,
                   'cccc0005-%s-4000-8000-000000000001' AS variant_id_pattern
            FROM _seed_merchandise_variants
            UNION ALL
            SELECT product_suffix, second_value_suffix,
                   'cccc0022-%s-4000-8000-000000000001'
            FROM _seed_merchandise_variants
        ) AS link_seed
        ON CONFLICT (id) DO UPDATE
        SET product_variant_id = EXCLUDED.product_variant_id,
            product_option_id = EXCLUDED.product_option_id,
            product_option_value_id = EXCLUDED.product_option_value_id,
            updated_at = now();

        INSERT INTO price_list_items (
            id, tenant_id, price_list_id, product_id, product_variant_id, selling_price,
            min_quantity, status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at
        )
        SELECT
            format('cccc0024-%s-4000-8000-000000000001', seed.product_suffix)::uuid,
            base_price.tenant_id,
            base_price.price_list_id,
            base_price.product_id,
            format('cccc0022-%s-4000-8000-000000000001', seed.product_suffix)::uuid,
            base_price.selling_price,
            base_price.min_quantity,
            'ACTIVE',
            '99999999-0003-4000-8000-000000000001'::uuid,
            '99999999-0003-4000-8000-000000000001'::uuid,
            now(),
            now()
        FROM _seed_merchandise_variants AS seed
        JOIN price_list_items AS base_price
          ON base_price.id = format(
              'cccc0007-%s-4000-8000-000000000001',
              seed.product_suffix
          )::uuid
        ON CONFLICT (id) DO UPDATE
        SET selling_price = EXCLUDED.selling_price,
            min_quantity = EXCLUDED.min_quantity,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO inventory_balances (
            id, tenant_id, inventory_location_id, product_id, product_variant_id, product_batch_id,
            on_hand_quantity, reserved_quantity, damaged_quantity, quarantine_quantity, row_version,
            created_at, updated_at
        )
        SELECT
            format('cccc0025-%s-4000-8000-000000000001', seed.product_suffix)::uuid,
            base_inventory.tenant_id,
            base_inventory.inventory_location_id,
            base_inventory.product_id,
            format('cccc0022-%s-4000-8000-000000000001', seed.product_suffix)::uuid,
            NULL,
            greatest(ceil(base_inventory.on_hand_quantity / 2.0), 1.0),
            0.0,
            0.0,
            0.0,
            0,
            now(),
            now()
        FROM _seed_merchandise_variants AS seed
        JOIN inventory_balances AS base_inventory
          ON base_inventory.id = format(
              'cccc0017-%s-4000-8000-000000000001',
              seed.product_suffix
          )::uuid
        ON CONFLICT (id) DO UPDATE
        SET on_hand_quantity = EXCLUDED.on_hand_quantity,
            reserved_quantity = EXCLUDED.reserved_quantity,
            damaged_quantity = EXCLUDED.damaged_quantity,
            quarantine_quantity = EXCLUDED.quarantine_quantity,
            updated_at = now();

        DROP TABLE _seed_merchandise_variants;
        """;

    public const string DownSql = """
        DELETE FROM inventory_balances
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id::text LIKE 'cccc0025-%';

        DELETE FROM price_list_items
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id::text LIKE 'cccc0024-%';

        DELETE FROM product_variant_option_values
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id::text LIKE 'cccc0023-%';

        DELETE FROM product_variants
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id::text LIKE 'cccc0022-%';

        DELETE FROM product_option_values
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id::text LIKE 'cccc0021-%';

        DELETE FROM product_options
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id::text LIKE 'cccc0020-%';

        UPDATE product_variants AS variant
        SET variant_code = 'DEFAULT',
            variant_name = product.product_name,
            sku = product.product_code || '-SKU',
            option_combination_hash = NULL,
            is_default_variant = true,
            is_sellable = true,
            status = 'ACTIVE',
            updated_at = now()
        FROM products AS product
        WHERE variant.product_id = product.id
          AND variant.id::text LIKE 'cccc0005-%'
          AND product.product_code IN (
              'MER-001', 'MER-002', 'MER-003', 'MER-004', 'MER-005',
              'MER-006', 'MER-007', 'MER-008', 'MER-009', 'MER-010',
              'MER-011', 'MER-012', 'MER-013', 'MER-014', 'MER-015'
          );

        UPDATE products
        SET product_structure = 'SIMPLE',
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND product_code IN (
              'MER-001', 'MER-002', 'MER-003', 'MER-004', 'MER-005',
              'MER-006', 'MER-007', 'MER-008', 'MER-009', 'MER-010',
              'MER-011', 'MER-012', 'MER-013', 'MER-014', 'MER-015'
          );
        """;
}
