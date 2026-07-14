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
            false,
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
            ('cccc0004-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-004', 'Team Socks', 'team-socks-mer-004', 'STANDARD', 'SIMPLE', 'Official team socks.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-005', 'Running Shoes', 'running-shoes-mer-005', 'STANDARD', 'SIMPLE', 'Performance running shoes.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-006', 'Casual Sneakers', 'casual-sneakers-mer-006', 'STANDARD', 'SIMPLE', 'Everyday casual sneakers.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0007-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-007', 'Team Cap', 'team-cap-mer-007', 'STANDARD', 'SIMPLE', 'Adjustable team cap.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-008', 'Fan Scarf', 'fan-scarf-mer-008', 'STANDARD', 'SIMPLE', 'Supporter scarf.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-009', 'Club Keychain', 'club-keychain-mer-009', 'STANDARD', 'SIMPLE', 'Metal club keychain.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-010', 'Stadium Lanyard', 'stadium-lanyard-mer-010', 'STANDARD', 'SIMPLE', 'Printed stadium lanyard.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-011', 'Match Football', 'match-football-mer-011', 'STANDARD', 'SIMPLE', 'Size 5 match football.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-012', 'Training Basketball', 'training-basketball-mer-012', 'STANDARD', 'SIMPLE', 'Indoor training basketball.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-013', 'Water Bottle', 'water-bottle-mer-013', 'STANDARD', 'SIMPLE', '750ml sports water bottle.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0004-000e-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'MER-014', 'Gym Bag', 'gym-bag-mer-014', 'STANDARD', 'SIMPLE', 'Medium gym carry bag.', true, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
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
            ('cccc0005-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0004-4000-8000-000000000001', 'DEFAULT', 'Team Socks', 'MER-004-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0005-4000-8000-000000000001', 'DEFAULT', 'Running Shoes', 'MER-005-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0005-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0006-4000-8000-000000000001', 'DEFAULT', 'Casual Sneakers', 'MER-006-SKU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', true, true, false, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
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
            ('cccc0006-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0004-4000-8000-000000000001', 'cccc0002-0001-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0005-4000-8000-000000000001', 'cccc0002-0002-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0006-4000-8000-000000000001', 'cccc0002-0002-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-0007-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0007-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0008-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0009-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000a-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', 'cccc0002-0004-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000c-4000-8000-000000000001', 'cccc0002-0004-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000d-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0006-000e-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000e-4000-8000-000000000001', 'cccc0002-0003-4000-8000-000000000001', true, 0, '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
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
            ('cccc0007-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0004-4000-8000-000000000001', 'cccc0005-0004-4000-8000-000000000001', 1200.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0005-4000-8000-000000000001', 'cccc0005-0005-4000-8000-000000000001', 8900.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0006-4000-8000-000000000001', 'cccc0005-0006-4000-8000-000000000001', 6500.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-0007-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0007-4000-8000-000000000001', 'cccc0005-0007-4000-8000-000000000001', 1500.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0008-4000-8000-000000000001', 'cccc0005-0008-4000-8000-000000000001', 1800.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-0009-4000-8000-000000000001', 'cccc0005-0009-4000-8000-000000000001', 450.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000a-4000-8000-000000000001', 'cccc0005-000a-4000-8000-000000000001', 350.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', 'cccc0005-000b-4000-8000-000000000001', 3200.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000c-4000-8000-000000000001', 'cccc0005-000c-4000-8000-000000000001', 4500.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000d-4000-8000-000000000001', 'cccc0005-000d-4000-8000-000000000001', 950.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000e-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000e-4000-8000-000000000001', 'cccc0005-000e-4000-8000-000000000001', 4200.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0007-000f-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0003-0001-4000-8000-000000000001', 'cccc0004-000f-4000-8000-000000000001', 'cccc0005-000f-4000-8000-000000000001', 600.0000, 1, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET selling_price = EXCLUDED.selling_price,
            status = 'ACTIVE',
            updated_at = now();

        INSERT INTO product_images (
            id, tenant_id, product_id, product_variant_id, sales_channel_id,
            image_storage_key, image_url, alt_text, image_purpose, mime_type,
            file_size_bytes, width_px, height_px, checksum_hash, sort_order,
            is_primary_image, status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at
        )
        VALUES
            ('cccc0008-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0001-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-001-team-jersey.png', 'https://img.magnific.com/premium-photo/3d-sportswear-jersey-shirt-template-design-mockup_1174662-7390.jpg?w=2000', 'Team Jersey product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0002-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-002-training-jersey.png', 'https://img.magnific.com/premium-photo/3d-sportswear-jersey-shirt-template-design-mockup_1174662-7361.jpg?w=1380', 'Training Jersey product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0003-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-003-match-shorts.png', 'https://media.sketchfab.com/models/69cc6043c37d430e89b94111fc857699/thumbnails/75a6cabd36f84e22965dd56d60d21013/ced7e1c0046e4a40a245a95f4223b207.jpeg', 'Match Shorts product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0004-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-004-team-socks.png', 'https://m.media-amazon.com/images/I/61xGKSlgM+L._AC_UY1000_.jpg', 'Team Socks product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0005-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-005-running-shoes.png', 'https://placehold.co/640x480/png?text=Running+Shoes', 'Running Shoes product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0006-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-006-casual-sneakers.png', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcT4-9oTUT1eJDwgzJj0-iD5uGYPutlDvCq-KaJ3vP7XAA&s=10', 'Casual Sneakers product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0007-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0007-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-007-team-cap.png', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQei_TDmU_Q6lxvD7mYQ5MrGgTqrlSucPdV5zEaJFUpsw&s=10', 'Team Cap product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0008-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-008-fan-scarf.png', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSdH_jMhQP8NpADYWW8bYe5nQ1ZKFlxNHRLDXNlc-XYOQ&s=10', 'Fan Scarf product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0009-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-009-club-keychain.png', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRn9lWmJgsHEg-M7ifVAvWJKUP6cNu42yJNTBYFMNtov7IB0LbTdaXnsHs&s=10', 'Club Keychain product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000a-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-010-stadium-lanyard.png', 'https://www.google.com/search?q=stadium+lanyard+for+merchandise+store+hd+image&sca_esv=71a32e65058fb37c&rlz=1C1GCEA_enLK1205LK1205&udm=2&biw=1536&bih=730&sxsrf=APpeQnvyCb1Iz4TnSPtPIpNKkJVJQ0_Xnw%3A1783930356927&ei=9J1UaqSWOPmTseMP05LouQ0&ved=0ahUKEwik4Om5ms-VAxX5SWwGHVMJOtcQ4dUDCBE&uact=5&oq=stadium+lanyard+for+merchandise+store+hd+image&gs_lp=Egtnd3Mtd2l6LWltZyIuc3RhZGl1bSBsYW55YXJkIGZvciBtZXJjaGFuZGlzZSBzdG9yZSBoZCBpbWFnZUjZEFDoA1i7DXABeACQAQCYAfQBoAGOBaoBBTAuMy4xuAEDyAEA-AEBmAIAoAIAmAMAiAYBkgcAoAcwsgcAuAcAwgcAyAcAgAgB&sclient=gws-wiz-img#sv=CAMSURoyKhBlLXlpdW9vVzFiY2FCX2NNMg55aXVvb1cxYmNhQl9jTToOTzZoM1V0d2pROUhTQ00gBCoXCgFzEhBlLXlpdW9vVzFiY2FCX2NNGAEwARgHILn5zrIMSggQARgBIAEoAQ', 'Stadium Lanyard product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-011-match-football.png', 'https://www.google.com/search?q=match+football+for+merchandise+store+hd+image&sca_esv=71a32e65058fb37c&rlz=1C1GCEA_enLK1205LK1205&udm=2&biw=1536&bih=730&sxsrf=APpeQnseqHZm9lv0UuYBM6MlIfFSZodMiQ%3A1783930366938&ei=_p1Uarb1OJ-QseMPnd7ZkAY&ved=0ahUKEwi27My-ms-VAxUfSGwGHR1vFmIQ4dUDCBE&uact=5&oq=match+football+for+merchandise+store+hd+image&gs_lp=Egtnd3Mtd2l6LWltZyItbWF0Y2ggZm9vdGJhbGwgZm9yIG1lcmNoYW5kaXNlIHN0b3JlIGhkIGltYWdlSMccUABYtRZwAHgAkAEAmAH6AaABihOqAQUwLjkuNbgBA8gBAPgBAZgCAKACAJgDAJIHAKAHqAGyBwC4BwDCBwDIBwCACAE&sclient=gws-wiz-img#sv=CAMSURoyKhBlLWR5SWJxelpKUWxBWmtNMg5keUlicXpaSlFsQVprTToOZzNPeUZDNVBxWDIzVk0gBCoXCgFzEhBlLWR5SWJxelpKUWxBWmtNGAEwARgHINC2wJsGSggQARgBIAEoAQ', 'Match Football product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000c-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-012-training-basketball.png', 'https://www.google.com/search?q=basket+ball+for+merchandise+store+hd+image&sca_esv=71a32e65058fb37c&rlz=1C1GCEA_enLK1205LK1205&udm=2&biw=1536&bih=730&sxsrf=APpeQnvl5-AKopfexk0Sq9cm-nRGvoPiBg%3A1783930467460&ei=Y55UatrbG6LkseMPysqgsAg&ved=0ahUKEwiamcTums-VAxUicmwGHUolCIYQ4dUDCBE&uact=5&oq=basket+ball+for+merchandise+store+hd+image&gs_lp=Egtnd3Mtd2l6LWltZyIqYmFza2V0IGJhbGwgZm9yIG1lcmNoYW5kaXNlIHN0b3JlIGhkIGltYWdlSKRDUJMIWJxCcAZ4AJABAJgB2QGgAdsVqgEGMC4xNy4xuAEDyAEA-AEBmAIAoAIAmAMAiAYBkgcAoAfYAbIHALgHAMIHAMgHAIAIAQ&sclient=gws-wiz-img#ip=1&sv=CAMSURoyKhBlLTR6MG1Ud1E0Zkl1OHdNMg40ejBtVHdRNGZJdTh3TToOSFUzUk1oUUVnVTFZTk0gBCoXCgFzEhBlLTR6MG1Ud1E0Zkl1OHdNGAEwARgHINbp0t0OSggQARgBIAEoAQ', 'Training Basketball product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000d-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-013-water-bottle.png', 'https://www.google.com/search?q=water+bottle+for+merchandise+store+hd+image&sca_esv=71a32e65058fb37c&rlz=1C1GCEA_enLK1205LK1205&udm=2&biw=1536&bih=730&sxsrf=APpeQnuFQdLmOUaimEug-uAXTXP558cs7g%3A1783930529676&ei=oZ5UaqD4KKyeseMP1cmYiQU&ved=0ahUKEwjgzJmMm8-VAxUsT2wGHdUkJlEQ4dUDCBE&uact=5&oq=water+bottle+for+merchandise+store+hd+image&gs_lp=Egtnd3Mtd2l6LWltZyIrd2F0ZXIgYm90dGxlIGZvciBtZXJjaGFuZGlzZSBzdG9yZSBoZCBpbWFnZUjBNFDbFFjdM3ABeACQAQCYAdkBoAGlEKoBBTAuOC40uAEDyAEA-AEBmAIAoAIAmAMA4gMFEgExIECIBgGSBwCgB5ABsgcAuAcAwgcAyAcAgAgB&sclient=gws-wiz-img#sv=CAMSURoyKhBlLXB4RU4zbkd5V1NHRFhNMg5weEVOM25HeVdTR0RYTToOblRhYWtJeDNQMUMxZ00gBCoXCgFzEhBlLXB4RU4zbkd5V1NHRFhNGAEwARgHIIGklqgDSggQARgBIAEoAQ', 'Water Bottle product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000e-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000e-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-014-gym-bag.png', 'https://www.google.com/search?q=water+bottle+for+merchandise+store+hd+image&sca_esv=71a32e65058fb37c&rlz=1C1GCEA_enLK1205LK1205&udm=2&biw=1536&bih=730&sxsrf=APpeQnuFQdLmOUaimEug-uAXTXP558cs7g%3A1783930529676&ei=oZ5UaqD4KKyeseMP1cmYiQU&ved=0ahUKEwjgzJmMm8-VAxUsT2wGHdUkJlEQ4dUDCBE&uact=5&oq=water+bottle+for+merchandise+store+hd+image&gs_lp=Egtnd3Mtd2l6LWltZyIrd2F0ZXIgYm90dGxlIGZvciBtZXJjaGFuZGlzZSBzdG9yZSBoZCBpbWFnZUjBNFDbFFjdM3ABeACQAQCYAdkBoAGlEKoBBTAuOC40uAEDyAEA-AEBmAIAoAIAmAMA4gMFEgExIECIBgGSBwCgB5ABsgcAuAcAwgcAyAcAgAgB&sclient=gws-wiz-img#sv=CAMSURoyKhBlLXB4RU4zbkd5V1NHRFhNMg5weEVOM25HeVdTR0RYTToOblRhYWtJeDNQMUMxZ00gBCoXCgFzEhBlLXB4RU4zbkd5V1NHRFhNGAEwARgHIIGklqgDSggQARgBIAEoAQ', 'Gym Bag product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000f-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000f-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-015-silicone-wristband.png', 'https://www.amazon.nl/-/en/Football-Inspirational-Adjustable-Wristbands-Stainless/dp/B07R1WSTM7', 'Silicone Wristband product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET image_storage_key = EXCLUDED.image_storage_key,
            image_url = EXCLUDED.image_url,
            alt_text = EXCLUDED.alt_text,
            image_purpose = EXCLUDED.image_purpose,
            mime_type = EXCLUDED.mime_type,
            width_px = EXCLUDED.width_px,
            height_px = EXCLUDED.height_px,
            sort_order = EXCLUDED.sort_order,
            is_primary_image = EXCLUDED.is_primary_image,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string ProductImageUpSql = """
        INSERT INTO product_images (
            id, tenant_id, product_id, product_variant_id, sales_channel_id,
            image_storage_key, image_url, alt_text, image_purpose, mime_type,
            file_size_bytes, width_px, height_px, checksum_hash, sort_order,
            is_primary_image, status, created_by_tenant_user_id, updated_by_tenant_user_id,
            created_at, updated_at
        )
        VALUES
            ('cccc0008-0001-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0001-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-001-team-jersey.png', 'https://img.magnific.com/premium-photo/3d-sportswear-jersey-shirt-template-design-mockup_1174662-7390.jpg?w=2000', 'Team Jersey product image', 'CATALOG', 'image/jpeg', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0002-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0002-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-002-training-jersey.png', 'https://img.magnific.com/premium-photo/3d-sportswear-jersey-shirt-template-design-mockup_1174662-7361.jpg?w=1380', 'Training Jersey product image', 'CATALOG', 'image/jpeg', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0003-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0003-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-003-match-shorts.png', 'https://media.sketchfab.com/models/69cc6043c37d430e89b94111fc857699/thumbnails/75a6cabd36f84e22965dd56d60d21013/ced7e1c0046e4a40a245a95f4223b207.jpeg', 'Match Shorts product image', 'CATALOG', 'image/jpeg', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0004-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0004-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-004-team-socks.png', 'https://m.media-amazon.com/images/I/61xGKSlgM+L._AC_UY1000_.jpg', 'Team Socks product image', 'CATALOG', 'image/jpeg', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0005-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0005-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-005-running-shoes.png', 'https://placehold.co/640x480/png?text=Running+Shoes', 'Running Shoes product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0006-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0006-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-006-casual-sneakers.png', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcT4-9oTUT1eJDwgzJj0-iD5uGYPutlDvCq-KaJ3vP7XAA&s=10', 'Casual Sneakers product image', 'CATALOG', 'image/jpeg', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0007-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0007-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-007-team-cap.png', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQei_TDmU_Q6lxvD7mYQ5MrGgTqrlSucPdV5zEaJFUpsw&s=10', 'Team Cap product image', 'CATALOG', 'image/jpeg', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0008-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0008-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-008-fan-scarf.png', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSdH_jMhQP8NpADYWW8bYe5nQ1ZKFlxNHRLDXNlc-XYOQ&s=10', 'Fan Scarf product image', 'CATALOG', 'image/jpeg', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-0009-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-0009-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-009-club-keychain.png', 'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRn9lWmJgsHEg-M7ifVAvWJKUP6cNu42yJNTBYFMNtov7IB0LbTdaXnsHs&s=10', 'Club Keychain product image', 'CATALOG', 'image/jpeg', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000a-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000a-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-010-stadium-lanyard.png', 'https://placehold.co/640x480/png?text=Stadium+Lanyard', 'Stadium Lanyard product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000b-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000b-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-011-match-football.png', 'https://placehold.co/640x480/png?text=Match+Football', 'Match Football product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000c-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000c-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-012-training-basketball.png', 'https://placehold.co/640x480/png?text=Training+Basketball', 'Training Basketball product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000d-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000d-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-013-water-bottle.png', 'https://placehold.co/640x480/png?text=Water+Bottle', 'Water Bottle product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000e-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000e-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-014-gym-bag.png', 'https://placehold.co/640x480/png?text=Gym+Bag', 'Gym Bag product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now()),
            ('cccc0008-000f-4000-8000-000000000001', '55555555-0000-4000-8000-000000000001', 'cccc0004-000f-4000-8000-000000000001', NULL, NULL, 'seed/merchandise/mer-015-silicone-wristband.png', 'https://placehold.co/640x480/png?text=Silicone+Wristband', 'Silicone Wristband product image', 'CATALOG', 'image/png', NULL, 640, 480, NULL, 0, true, 'ACTIVE', '99999999-0003-4000-8000-000000000001', '99999999-0003-4000-8000-000000000001', now(), now())
        ON CONFLICT (id) DO UPDATE
        SET image_storage_key = EXCLUDED.image_storage_key,
            image_url = EXCLUDED.image_url,
            alt_text = EXCLUDED.alt_text,
            image_purpose = EXCLUDED.image_purpose,
            mime_type = EXCLUDED.mime_type,
            width_px = EXCLUDED.width_px,
            height_px = EXCLUDED.height_px,
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
