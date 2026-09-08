namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentMerchandiseCatalogSeedData
{
    public const string UpSql = """
        INSERT INTO departments (
            id, tenant_id, department_code, department_name, description, sort_order, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES (
            'cccc0001-0001-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'MERCH',
            'Merchandise',
            'Development merchandise department for POS catalog seed.',
            0,
            'ACTIVE',
            '99999999-0003-4000-8000-000000000001',
            '99999999-0003-4000-8000-000000000001',
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET department_name = EXCLUDED.department_name,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO categories (
            id, tenant_id, department_id, parent_category_id, category_code, category_name,
            category_slug, description, sort_order, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES
            ('cccc0002-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0001-0001-4000-8000-000000000001', NULL, 'APPAREL', 'Apparel', 'apparel', 'Jerseys, shorts and fan apparel.', 0, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0002-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0001-0001-4000-8000-000000000001', NULL, 'FOOTWEAR', 'Footwear', 'footwear', 'Shoes and sneakers.', 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0002-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0001-0001-4000-8000-000000000001', NULL, 'ACCESSORIES', 'Accessories', 'accessories', 'Caps, keychains and fan accessories.', 2, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0002-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0001-0001-4000-8000-000000000001', NULL, 'SPORTS', 'Sports', 'sports', 'Balls and sports equipment.', 3, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET category_name = EXCLUDED.category_name,
            status = 'ACTIVE',
            updated_at = now();

        UPDATE price_lists
        SET is_default_price_list = false,
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id <> 'cccc0003-0001-4000-8000-000000000001'
          AND is_default_price_list = true
          AND status = 'ACTIVE';

        INSERT INTO price_lists (
            id, tenant_id, price_list_code, price_list_name, price_list_type, currency_code,
            is_default_price_list, price_includes_tax, priority, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES (
            'cccc0003-0001-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'DEV-DEFAULT',
            'Development Default Price List',
            'POS',
            'LKR',
            true,
            true,
            0,
            'ACTIVE',
            '99999999-0003-4000-8000-000000000001',
            '99999999-0003-4000-8000-000000000001',
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET price_list_name = EXCLUDED.price_list_name,
            is_default_price_list = true,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO products (
            id, tenant_id, product_code, product_name, product_slug, product_type, product_structure,
            short_description, is_sellable, is_taxable, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES
            ('cccc0004-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-001', 'Team Jersey', 'team-jersey-mer-001', 'STANDARD', 'SIMPLE', 'Official home team jersey.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-002', 'Training Jersey', 'training-jersey-mer-002', 'STANDARD', 'SIMPLE', 'Lightweight training jersey.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-003', 'Match Shorts', 'match-shorts-mer-003', 'STANDARD', 'SIMPLE', 'Match day shorts.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-004', 'Training Shorts', 'training-shorts-mer-004', 'STANDARD', 'SIMPLE', 'Lightweight training shorts.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-005', 'Running Shoes', 'running-shoes-mer-005', 'STANDARD', 'SIMPLE', 'Lightweight running shoes.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-006', 'Training Shoes', 'training-shoes-mer-006', 'STANDARD', 'SIMPLE', 'Training shoes for indoor sessions.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0007-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-007', 'Team Cap', 'team-cap-mer-007', 'STANDARD', 'SIMPLE', 'Adjustable team cap.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-008', 'Fan Scarf', 'fan-scarf-mer-008', 'STANDARD', 'SIMPLE', 'Supporter scarf.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-009', 'Club Keychain', 'club-keychain-mer-009', 'STANDARD', 'SIMPLE', 'Metal club keychain.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-010', 'Stadium Lanyard', 'stadium-lanyard-mer-010', 'STANDARD', 'SIMPLE', 'Printed stadium lanyard.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-011', 'Match Football', 'match-football-mer-011', 'STANDARD', 'SIMPLE', 'Size 5 match football.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-012', 'Training Basketball', 'training-basketball-mer-012', 'STANDARD', 'SIMPLE', 'Indoor training basketball.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-013', 'Water Bottle', 'water-bottle-mer-013', 'STANDARD', 'SIMPLE', '750ml sports water bottle.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000e-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-014', 'Gym Bag', 'gym-bag-mer-014', 'STANDARD', 'SIMPLE', 'Durable sports gym bag.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000f-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-015', 'Silicone Wristband', 'silicone-wristband-mer-015', 'STANDARD', 'SIMPLE', 'Silicone supporter wristband.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET product_name = EXCLUDED.product_name,
            short_description = EXCLUDED.short_description,
            is_sellable = true,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO product_variants (
            id, tenant_id, product_id, variant_code, variant_name, sku, stock_uom_id, sales_uom_id,
            is_default_variant, is_sellable, allow_fractional_quantity, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES
            ('cccc0005-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0001-4000-8000-000000000001', 'DEFAULT', 'Team Jersey', 'MER-001-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0002-4000-8000-000000000001', 'DEFAULT', 'Training Jersey', 'MER-002-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0003-4000-8000-000000000001', 'DEFAULT', 'Match Shorts', 'MER-003-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0004-4000-8000-000000000001', 'DEFAULT', 'Training Shorts', 'MER-004-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0005-4000-8000-000000000001', 'DEFAULT', 'Running Shoes', 'MER-005-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0006-4000-8000-000000000001', 'DEFAULT', 'Training Shoes', 'MER-006-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0007-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0007-4000-8000-000000000001', 'DEFAULT', 'Team Cap', 'MER-007-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0008-4000-8000-000000000001', 'DEFAULT', 'Fan Scarf', 'MER-008-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0009-4000-8000-000000000001', 'DEFAULT', 'Club Keychain', 'MER-009-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000a-4000-8000-000000000001', 'DEFAULT', 'Stadium Lanyard', 'MER-010-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', 'DEFAULT', 'Match Football', 'MER-011-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000c-4000-8000-000000000001', 'DEFAULT', 'Training Basketball', 'MER-012-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000d-4000-8000-000000000001', 'DEFAULT', 'Water Bottle', 'MER-013-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-000e-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000e-4000-8000-000000000001', 'DEFAULT', 'Gym Bag', 'MER-014-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-000f-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000f-4000-8000-000000000001', 'DEFAULT', 'Silicone Wristband', 'MER-015-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET variant_name = EXCLUDED.variant_name,
            sku = EXCLUDED.sku,
            is_sellable = true,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO product_categories (
            id, tenant_id, product_id, category_id, is_primary_category, sort_order,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES
            ('cccc0006-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0001-4000-8000-000000000001', 'cccc0002-0001-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0002-4000-8000-000000000001', 'cccc0002-0001-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0003-4000-8000-000000000001', 'cccc0002-0001-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),

            ('cccc0006-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0008-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0009-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000a-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', 'cccc0002-0004-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000c-4000-8000-000000000001', 'cccc0002-0004-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000d-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),

            ('cccc0006-000f-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000f-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET category_id = EXCLUDED.category_id,
            is_primary_category = true,
            updated_at = now();

        INSERT INTO price_list_items (
            id, tenant_id, price_list_id, product_id, product_variant_id, selling_price, min_quantity, status,
            created_by_tenant_user_id, updated_by_tenant_user_id, created_at, updated_at
        )
        VALUES
            ('cccc0007-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0001-4000-8000-000000000001', 'cccc0005-0001-4000-8000-000000000001', 4500.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0002-4000-8000-000000000001', 'cccc0005-0002-4000-8000-000000000001', 3200.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0003-4000-8000-000000000001', 'cccc0005-0003-4000-8000-000000000001', 2800.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),

            ('cccc0007-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0008-4000-8000-000000000001', 'cccc0005-0008-4000-8000-000000000001', 1800.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0009-4000-8000-000000000001', 'cccc0005-0009-4000-8000-000000000001', 450.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000a-4000-8000-000000000001', 'cccc0005-000a-4000-8000-000000000001', 350.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', 'cccc0005-000b-4000-8000-000000000001', 3200.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000c-4000-8000-000000000001', 'cccc0005-000c-4000-8000-000000000001', 4500.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000d-4000-8000-000000000001', 'cccc0005-000d-4000-8000-000000000001', 950.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),

            ('cccc0007-000f-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000f-4000-8000-000000000001', 'cccc0005-000f-4000-8000-000000000001', 600.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET selling_price = EXCLUDED.selling_price,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO product_images (
            id, tenant_id, product_id, product_variant_id, sales_channel_id,
            alt_text, image_purpose, sort_order,
            is_primary_image, status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at
        )
        VALUES
            ('cccc0008-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0001-4000-8000-000000000001', NULL, NULL, 'Team Jersey product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0002-4000-8000-000000000001', NULL, NULL, 'Training Jersey product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0003-4000-8000-000000000001', NULL, NULL, 'Match Shorts product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),

            ('cccc0008-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0008-4000-8000-000000000001', NULL, NULL, 'Fan Scarf product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0009-4000-8000-000000000001', NULL, NULL, 'Club Keychain product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000a-4000-8000-000000000001', NULL, NULL, 'Stadium Lanyard product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', NULL, NULL, 'Match Football product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000c-4000-8000-000000000001', NULL, NULL, 'Training Basketball product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000d-4000-8000-000000000001', NULL, NULL, 'Water Bottle product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),

            ('cccc0008-000f-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000f-4000-8000-000000000001', NULL, NULL, 'Silicone Wristband product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET alt_text = EXCLUDED.alt_text,
            image_purpose = EXCLUDED.image_purpose,
            sort_order = EXCLUDED.sort_order,
            is_primary_image = EXCLUDED.is_primary_image,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public static string CurrentSchemaUpSql =>
        UpSql
            .Replace(
                "id, tenant_id, department_id, parent_category_id, category_code, category_name,",
                "id, tenant_id, parent_category_id, category_code, category_name,")
            .Replace(
                "'55555555-0000-4000-8000-000000000001', 'cccc0001-0001-4000-8000-000000000001', NULL,",
                "'55555555-0000-4000-8000-000000000001', NULL,");

    public const string ProductImageUpSql = """
        INSERT INTO product_images (
            id, tenant_id, product_id, product_variant_id, sales_channel_id,
            alt_text, image_purpose, sort_order,
            is_primary_image, status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at
        )
        VALUES
            ('cccc0008-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0001-4000-8000-000000000001', NULL, NULL, 'Team Jersey product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0002-4000-8000-000000000001', NULL, NULL, 'Training Jersey product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0003-4000-8000-000000000001', NULL, NULL, 'Match Shorts product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),

            ('cccc0008-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0008-4000-8000-000000000001', NULL, NULL, 'Fan Scarf product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0009-4000-8000-000000000001', NULL, NULL, 'Club Keychain product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000a-4000-8000-000000000001', NULL, NULL, 'Stadium Lanyard product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', NULL, NULL, 'Match Football product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000c-4000-8000-000000000001', NULL, NULL, 'Training Basketball product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000d-4000-8000-000000000001', NULL, NULL, 'Water Bottle product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),

            ('cccc0008-000f-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000f-4000-8000-000000000001', NULL, NULL, 'Silicone Wristband product image', 'CATALOG', 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET alt_text = EXCLUDED.alt_text,
            image_purpose = EXCLUDED.image_purpose,
            sort_order = EXCLUDED.sort_order,
            is_primary_image = EXCLUDED.is_primary_image,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string ProductImageDownSql = """
        DELETE FROM product_images
        WHERE id IN (
            'cccc0008-0001-4000-8000-000000000001','cccc0008-0002-4000-8000-000000000001','cccc0008-0003-4000-8000-000000000001',
            'cccc0008-0004-4000-8000-000000000001','cccc0008-0005-4000-8000-000000000001','cccc0008-0006-4000-8000-000000000001',
            'cccc0008-0007-4000-8000-000000000001','cccc0008-0008-4000-8000-000000000001','cccc0008-0009-4000-8000-000000000001',
            'cccc0008-000a-4000-8000-000000000001','cccc0008-000b-4000-8000-000000000001','cccc0008-000c-4000-8000-000000000001',
            'cccc0008-000d-4000-8000-000000000001','cccc0008-000e-4000-8000-000000000001','cccc0008-000f-4000-8000-000000000001'
        );
        """;

    public const string MerchandiseProductImageUrlUpSql = """
        UPDATE product_images AS image
        SET image_url = mapping.image_url,
            mime_type = 'image/jpeg',
            updated_at = now()
        FROM (VALUES
            ('cccc0008-0001-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1580087433295-ab2600c1030e?q=80&w=1000&auto=format&fit=crop'),
            ('cccc0008-0002-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1579952363873-27f3bade9f55?q=80&w=1000&auto=format&fit=crop'),
            ('cccc0008-0003-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1591195853828-0de695293be6?q=80&w=1000&auto=format&fit=crop'),
            ('cccc0008-0008-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?q=80&w=1000&auto=format&fit=crop'),
            ('cccc0008-0009-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1608231387042-66d1773070a5?q=80&w=1000&auto=format&fit=crop'),
            ('cccc0008-000a-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1588850561407-ed78c282e89b?q=80&w=1000&auto=format&fit=crop'),
            ('cccc0008-000b-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1574629810360-7efbbe195018?q=80&w=1000&auto=format&fit=crop'),
            ('cccc0008-000c-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1519861531473-9209d0d24ea7?q=80&w=1000&auto=format&fit=crop'),
            ('cccc0008-000d-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1602143407151-7111542de6e8?q=80&w=1000&auto=format&fit=crop'),
            ('cccc0008-000f-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?q=80&w=1000&auto=format&fit=crop')
        ) AS mapping(id, image_url)
        WHERE image.id = mapping.id
          AND image.tenant_id = '55555555-0000-4000-8000-000000000001';
        """;

    public const string MerchandiseProductImageUrlDownSql = """
        UPDATE product_images
        SET image_url = NULL,
            mime_type = NULL,
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id::text LIKE 'cccc0008-%';
        """;

    public const string MerchandiseProductImageMediaAssetsUpSql = """
        WITH seed_images AS (
            SELECT *
            FROM (VALUES
                ('cccc0008-0001-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1580087433295-ab2600c1030e?q=80&w=1000&auto=format&fit=crop'),
                ('cccc0008-0002-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1579952363873-27f3bade9f55?q=80&w=1000&auto=format&fit=crop'),
                ('cccc0008-0003-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1591195853828-0de695293be6?q=80&w=1000&auto=format&fit=crop'),
                ('cccc0008-0008-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1542291026-7eec264c27ff?q=80&w=1000&auto=format&fit=crop'),
                ('cccc0008-0009-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1608231387042-66d1773070a5?q=80&w=1000&auto=format&fit=crop'),
                ('cccc0008-000a-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1588850561407-ed78c282e89b?q=80&w=1000&auto=format&fit=crop'),
                ('cccc0008-000b-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1574629810360-7efbbe195018?q=80&w=1000&auto=format&fit=crop'),
                ('cccc0008-000c-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1519861531473-9209d0d24ea7?q=80&w=1000&auto=format&fit=crop'),
                ('cccc0008-000d-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1602143407151-7111542de6e8?q=80&w=1000&auto=format&fit=crop'),
                ('cccc0008-000f-4000-8000-000000000001'::uuid, 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?q=80&w=1000&auto=format&fit=crop')
            ) AS mapping(product_image_id, public_url)
        )
        INSERT INTO media_assets (
            id,
            tenant_id,
            container_name,
            storage_key,
            public_url,
            original_file_name,
            mime_type,
            file_extension,
            file_size_bytes,
            checksum_hash,
            asset_type,
            asset_purpose,
            status,
            created_at,
            updated_at
        )
        SELECT
            md5('media_asset:product_images:' || seed_images.product_image_id::text)::uuid,
            '55555555-0000-4000-8000-000000000001',
            'legacy-media',
            'legacy/product-images/' || seed_images.product_image_id::text,
            left(seed_images.public_url, 1000),
            left('seed-merchandise-product-image-' || seed_images.product_image_id::text || '.jpg', 255),
            'image/jpeg',
            '.jpg',
            1,
            md5('seed:merchandise:product_images:' || seed_images.product_image_id::text || ':' || seed_images.public_url),
            'IMAGE',
            'PRODUCT_IMAGE',
            'ACTIVE',
            now(),
            now()
        FROM seed_images
        ON CONFLICT (tenant_id, container_name, storage_key) DO UPDATE
        SET public_url = EXCLUDED.public_url,
            original_file_name = EXCLUDED.original_file_name,
            mime_type = EXCLUDED.mime_type,
            file_extension = EXCLUDED.file_extension,
            asset_type = EXCLUDED.asset_type,
            asset_purpose = EXCLUDED.asset_purpose,
            status = EXCLUDED.status,
            updated_at = now();

        UPDATE product_images pi
        SET media_asset_id = md5('media_asset:product_images:' || pi.id::text)::uuid,
            updated_at = now()
        WHERE pi.tenant_id = '55555555-0000-4000-8000-000000000001'
          AND pi.id::text LIKE 'cccc0008-%'
          AND EXISTS (
              SELECT 1
              FROM media_assets ma
              WHERE ma.tenant_id = pi.tenant_id
                AND ma.id = md5('media_asset:product_images:' || pi.id::text)::uuid
          );
        """;

    public const string MerchandiseProductImageMediaAssetsDownSql = """
        UPDATE product_images
        SET media_asset_id = NULL,
            updated_at = now()
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND id::text LIKE 'cccc0008-%'
          AND media_asset_id = md5('media_asset:product_images:' || id::text)::uuid;

        DELETE FROM media_assets
        WHERE tenant_id = '55555555-0000-4000-8000-000000000001'
          AND container_name = 'legacy-media'
          AND storage_key LIKE 'legacy/product-images/cccc0008-%';
        """;

    public const string DownSql = """
        DELETE FROM product_images
        WHERE id IN (
            'cccc0008-0001-4000-8000-000000000001','cccc0008-0002-4000-8000-000000000001','cccc0008-0003-4000-8000-000000000001',
            'cccc0008-0004-4000-8000-000000000001','cccc0008-0005-4000-8000-000000000001','cccc0008-0006-4000-8000-000000000001',
            'cccc0008-0007-4000-8000-000000000001','cccc0008-0008-4000-8000-000000000001','cccc0008-0009-4000-8000-000000000001',
            'cccc0008-000a-4000-8000-000000000001','cccc0008-000b-4000-8000-000000000001','cccc0008-000c-4000-8000-000000000001',
            'cccc0008-000d-4000-8000-000000000001','cccc0008-000e-4000-8000-000000000001','cccc0008-000f-4000-8000-000000000001'
        );

        DELETE FROM price_list_items
        WHERE id IN (
            'cccc0007-0001-4000-8000-000000000001','cccc0007-0002-4000-8000-000000000001','cccc0007-0003-4000-8000-000000000001',
            'cccc0007-0004-4000-8000-000000000001','cccc0007-0005-4000-8000-000000000001','cccc0007-0006-4000-8000-000000000001',
            'cccc0007-0007-4000-8000-000000000001','cccc0007-0008-4000-8000-000000000001','cccc0007-0009-4000-8000-000000000001',
            'cccc0007-000a-4000-8000-000000000001','cccc0007-000b-4000-8000-000000000001','cccc0007-000c-4000-8000-000000000001',
            'cccc0007-000d-4000-8000-000000000001','cccc0007-000e-4000-8000-000000000001','cccc0007-000f-4000-8000-000000000001'
        );

        DELETE FROM product_categories
        WHERE id IN (
            'cccc0006-0001-4000-8000-000000000001','cccc0006-0002-4000-8000-000000000001','cccc0006-0003-4000-8000-000000000001',
            'cccc0006-0004-4000-8000-000000000001','cccc0006-0005-4000-8000-000000000001','cccc0006-0006-4000-8000-000000000001',
            'cccc0006-0007-4000-8000-000000000001','cccc0006-0008-4000-8000-000000000001','cccc0006-0009-4000-8000-000000000001',
            'cccc0006-000a-4000-8000-000000000001','cccc0006-000b-4000-8000-000000000001','cccc0006-000c-4000-8000-000000000001',
            'cccc0006-000d-4000-8000-000000000001','cccc0006-000e-4000-8000-000000000001','cccc0006-000f-4000-8000-000000000001'
        );

        DELETE FROM product_variants
        WHERE id IN (
            'cccc0005-0001-4000-8000-000000000001','cccc0005-0002-4000-8000-000000000001','cccc0005-0003-4000-8000-000000000001',
            'cccc0005-0004-4000-8000-000000000001','cccc0005-0005-4000-8000-000000000001','cccc0005-0006-4000-8000-000000000001',
            'cccc0005-0007-4000-8000-000000000001','cccc0005-0008-4000-8000-000000000001','cccc0005-0009-4000-8000-000000000001',
            'cccc0005-000a-4000-8000-000000000001','cccc0005-000b-4000-8000-000000000001','cccc0005-000c-4000-8000-000000000001',
            'cccc0005-000d-4000-8000-000000000001','cccc0005-000e-4000-8000-000000000001','cccc0005-000f-4000-8000-000000000001'
        );

        DELETE FROM products
        WHERE id IN (
            'cccc0004-0001-4000-8000-000000000001','cccc0004-0002-4000-8000-000000000001','cccc0004-0003-4000-8000-000000000001',
            'cccc0004-0004-4000-8000-000000000001','cccc0004-0005-4000-8000-000000000001','cccc0004-0006-4000-8000-000000000001',
            'cccc0004-0007-4000-8000-000000000001','cccc0004-0008-4000-8000-000000000001','cccc0004-0009-4000-8000-000000000001',
            'cccc0004-000a-4000-8000-000000000001','cccc0004-000b-4000-8000-000000000001','cccc0004-000c-4000-8000-000000000001',
            'cccc0004-000d-4000-8000-000000000001','cccc0004-000e-4000-8000-000000000001','cccc0004-000f-4000-8000-000000000001'
        );

        DELETE FROM price_lists
        WHERE id = 'cccc0003-0001-4000-8000-000000000001';

        DELETE FROM categories
        WHERE id IN (
            'cccc0002-0001-4000-8000-000000000001',
            'cccc0002-0002-4000-8000-000000000001',
            'cccc0002-0003-4000-8000-000000000001',
            'cccc0002-0004-4000-8000-000000000001'
        );

        DELETE FROM departments
        WHERE id = 'cccc0001-0001-4000-8000-000000000001';
        """;
}
