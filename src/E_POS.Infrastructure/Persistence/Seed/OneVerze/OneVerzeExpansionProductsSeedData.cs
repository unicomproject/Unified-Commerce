namespace E_POS.Infrastructure.Persistence.Seed.OneVerze;
public static class OneVerzeExpansionProductsSeedData {
        public const string UpSql = @"


INSERT INTO media_assets (id, tenant_id, container_name, storage_key, public_url, original_file_name, mime_type, file_extension, file_size_bytes, width_px, height_px, checksum_hash, asset_type, asset_purpose, status, created_at, updated_at) VALUES
('aeae0000-2000-4000-8000-000000000000', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_batg.jpg', 'https://images.pexels.com/photos/35801178/pexels-photo-35801178.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_batg.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_wkg.jpg', 'https://images.pexels.com/photos/33851213/pexels-photo-33851213.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_wkg.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-000000000002', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_hel.jpg', 'https://images.pexels.com/photos/30401163/pexels-photo-30401163.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_hel.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-000000000003', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_pad.jpg', 'https://images.pexels.com/photos/32721928/pexels-photo-32721928.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_pad.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-000000000004', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_stm.jpg', 'https://images.pexels.com/photos/8422410/pexels-photo-8422410.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_stm.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-000000000005', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_bag.jpg', 'https://images.pexels.com/photos/28726897/pexels-photo-28726897.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_bag.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-000000000006', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_shoe.jpg', 'https://images.pexels.com/photos/8454904/pexels-photo-8454904.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_shoe.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-000000000007', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_cap.jpg', 'https://images.pexels.com/photos/6230437/pexels-photo-6230437.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_cap.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-000000000008', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_slv.jpg', 'https://images.pexels.com/photos/8980688/pexels-photo-8980688.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_slv.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-000000000009', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_grp.jpg', 'https://images.pexels.com/photos/36553773/pexels-photo-36553773.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_grp.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-00000000000a', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_sock.jpg', 'https://images.pexels.com/photos/10923072/pexels-photo-10923072.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_sock.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-00000000000b', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_sun.jpg', 'https://images.pexels.com/photos/38011337/pexels-photo-38011337.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_sun.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-00000000000c', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_jkt.jpg', 'https://images.pexels.com/photos/4036172/pexels-photo-4036172.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_jkt.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now()),
('aeae0000-2000-4000-8000-00000000000d', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'images', 'tenants/08b0c8b0-a5bf-44f0-8814-cb2fe0120000/products/ext_abg.jpg', 'https://images.pexels.com/photos/6538995/pexels-photo-6538995.jpeg?cs=tinysrgb&w=800&h=800&fit=crop', 'ext_abg.jpg', 'image/jpeg', '.jpg', 50000, 800, 800, '', 'IMAGE', 'PRODUCT_IMAGE', 'ACTIVE', now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO products (id, tenant_id, product_code, product_name, product_slug, product_type, product_structure, short_description, is_sellable, is_taxable, status, created_at, updated_at) VALUES
('aeae0000-1000-4000-8000-000000000000', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-BATG', 'Elite Batting Gloves', 'elite-batting-gloves', 'STANDARD', 'VARIABLE', 'Premium cowhide leather batting gloves with extra padding.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-WKG', 'Pro Wicket Keeping Gloves', 'pro-wicket-keeping-gloves', 'STANDARD', 'VARIABLE', 'Padded wicket keeping gloves built for match play.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-000000000002', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-HEL', 'Cricket Helmet with Grille', 'cricket-helmet-grille', 'STANDARD', 'VARIABLE', 'ABS shell helmet with adjustable steel grille.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-000000000003', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-PAD', 'Batting Leg Pads', 'batting-leg-pads', 'STANDARD', 'VARIABLE', 'Lightweight batting leg guards with high-density foam.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-000000000004', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-STM', 'Cricket Stumps Set', 'cricket-stumps-set', 'STANDARD', 'VARIABLE', 'Poplar willow stumps set with bails, complete set of 6.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-000000000005', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-BAG', 'Cricket Kit Duffel Bag', 'cricket-kit-duffel-bag', 'STANDARD', 'VARIABLE', 'Spacious duffel bag with a dedicated bat compartment.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-000000000006', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-SHOE', 'Performance Running Shoes', 'performance-running-shoes', 'STANDARD', 'VARIABLE', 'Lightweight spikes designed for pitch traction.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-000000000007', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-CAP', 'Sports Cap', 'sports-cap', 'STANDARD', 'VARIABLE', 'Adjustable breathable sports cap with UV protection.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-000000000008', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-SLV', 'Compression Arm Sleeve', 'compression-arm-sleeve', 'STANDARD', 'VARIABLE', 'UV-protective compression arm sleeve for batting.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-000000000009', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-GRP', 'Cricket Bat Grip Pack', 'cricket-bat-grip-pack', 'STANDARD', 'VARIABLE', 'Pack of 3 anti-slip replacement bat grips.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-00000000000a', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-SOCK', 'Sports Ankle Socks', 'sports-ankle-socks', 'STANDARD', 'VARIABLE', 'Cushioned ankle socks, pack of 3.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-00000000000b', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-SUN', 'Sports Sunglasses', 'sports-sunglasses', 'STANDARD', 'VARIABLE', 'UV400 polarized sports sunglasses.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-00000000000c', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-JKT', 'Team Training Jacket', 'team-training-jacket', 'STANDARD', 'VARIABLE', 'Lightweight full-zip training jacket.', true, true, 'ACTIVE', now(), now()),
('aeae0000-1000-4000-8000-00000000000d', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'OVZ-ABG', 'Cricket Abdominal Guard', 'cricket-abdominal-guard', 'STANDARD', 'VARIABLE', 'Protective abdominal guard for batting.', true, true, 'ACTIVE', now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_categories (id, tenant_id, product_id, category_id, is_primary_category, sort_order, created_at, updated_at) VALUES
('aeae0000-3000-4000-8000-000000000000', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000000', '66666666-0002-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000000', '66666666-0008-4000-8000-000000000001', false, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000002', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000001', '66666666-0002-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000003', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000002', '66666666-0002-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000004', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000003', '66666666-0002-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000005', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000004', '66666666-0002-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000006', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000004', '66666666-0011-4000-8000-000000000001', false, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000007', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000005', '66666666-0010-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000008', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000006', '66666666-0009-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000009', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000006', '66666666-0006-4000-8000-000000000001', false, 0, now(), now()),
('aeae0000-3000-4000-8000-00000000000a', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000007', '66666666-0007-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-00000000000b', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000008', '66666666-0008-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-00000000000c', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000009', '66666666-0007-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-00000000000d', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000a', '66666666-0007-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-00000000000e', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000b', '66666666-0007-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-00000000000f', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000c', '66666666-0003-4000-8000-000000000001', true, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000010', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000c', '66666666-0008-4000-8000-000000000001', false, 0, now(), now()),
('aeae0000-3000-4000-8000-000000000011', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000d', '66666666-0002-4000-8000-000000000001', true, 0, now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_channel_visibility (id, tenant_id, product_id, sales_channel_id, is_visible, status, created_at, updated_at)
SELECT ('aeae0000-4000-4000-8000-' || lpad(to_hex(row_number() over (order by p.id, c.sales_channel_id) - 1), 12, '0'))::uuid,
       '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', p.id, c.sales_channel_id, true, 'ACTIVE', now(), now()
FROM (VALUES
    ('aeae0000-1000-4000-8000-000000000000'::uuid), ('aeae0000-1000-4000-8000-000000000001'::uuid),
    ('aeae0000-1000-4000-8000-000000000002'::uuid), ('aeae0000-1000-4000-8000-000000000003'::uuid),
    ('aeae0000-1000-4000-8000-000000000004'::uuid), ('aeae0000-1000-4000-8000-000000000005'::uuid),
    ('aeae0000-1000-4000-8000-000000000006'::uuid), ('aeae0000-1000-4000-8000-000000000007'::uuid),
    ('aeae0000-1000-4000-8000-000000000008'::uuid), ('aeae0000-1000-4000-8000-000000000009'::uuid),
    ('aeae0000-1000-4000-8000-00000000000a'::uuid), ('aeae0000-1000-4000-8000-00000000000b'::uuid),
    ('aeae0000-1000-4000-8000-00000000000c'::uuid), ('aeae0000-1000-4000-8000-00000000000d'::uuid)
) AS p(id)
CROSS JOIN (VALUES ('11111111-0002-4000-8000-000000000001'::uuid), ('11111111-0003-4000-8000-000000000001'::uuid)) AS c(sales_channel_id)
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_options (id, tenant_id, product_id, option_code, option_name, option_type, input_type, is_required, sort_order, status, created_at, updated_at) VALUES
('aeae0000-5000-4000-8000-000000000000', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000000', 'SIZE', 'Size', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000001', 'SIZE', 'Size', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-000000000002', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000002', 'SIZE', 'Size', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-000000000003', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000003', 'SIZE', 'Size', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-000000000004', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000004', 'TYPE', 'Type', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-000000000005', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000005', 'COLOR', 'Color', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-000000000006', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000006', 'SIZE', 'Size', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-000000000007', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000007', 'COLOR', 'Color', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-000000000008', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000008', 'SIZE', 'Size', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-000000000009', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000009', 'COLOR', 'Color', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-00000000000a', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000a', 'SIZE', 'Size', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-00000000000b', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000b', 'COLOR', 'Color', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-00000000000c', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000c', 'SIZE', 'Size', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now()),
('aeae0000-5000-4000-8000-00000000000d', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000d', 'SIZE', 'Size', 'VARIANT', 'SELECT', true, 0, 'ACTIVE', now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_option_values (id, tenant_id, product_option_id, value_code, value_name, display_name, sort_order, status, created_at, updated_at) VALUES
('aeae0000-6000-4000-8000-000000000000', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000000', 'M', 'Medium', 'Medium', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000000', 'L', 'Large', 'Large', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000002', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000001', 'M', 'Medium', 'Medium', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000003', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000001', 'L', 'Large', 'Large', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000004', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000002', 'M', 'Medium', 'Medium', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000005', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000002', 'L', 'Large', 'Large', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000006', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000003', 'M', 'Medium', 'Medium', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000007', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000003', 'L', 'Large', 'Large', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000008', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000004', 'STD', 'Standard', 'Standard', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000009', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000005', 'BLK', 'Black', 'Black', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-00000000000a', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000005', 'BLU', 'Blue', 'Blue', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-00000000000b', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000006', 'U8', 'UK 8', 'UK 8', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-00000000000c', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000006', 'U9', 'UK 9', 'UK 9', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-00000000000d', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000007', 'BLK', 'Black', 'Black', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-00000000000e', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000007', 'NVY', 'Navy', 'Navy', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-00000000000f', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000008', 'M', 'Medium', 'Medium', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000010', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000008', 'L', 'Large', 'Large', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000011', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000009', 'BLK', 'Black', 'Black', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000012', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-000000000009', 'WHT', 'White', 'White', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000013', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-00000000000a', 'M', 'Medium', 'Medium', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000014', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-00000000000a', 'L', 'Large', 'Large', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000015', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-00000000000b', 'BLK', 'Black', 'Black', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000016', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-00000000000b', 'TRT', 'Tortoise', 'Tortoise', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000017', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-00000000000c', 'M', 'Medium', 'Medium', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000018', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-00000000000c', 'L', 'Large', 'Large', 1, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-000000000019', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-00000000000d', 'M', 'Medium', 'Medium', 0, 'ACTIVE', now(), now()),
('aeae0000-6000-4000-8000-00000000001a', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-5000-4000-8000-00000000000d', 'L', 'Large', 'Large', 1, 'ACTIVE', now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_variants (id, tenant_id, product_id, variant_code, variant_name, sku, stock_uom_id, sales_uom_id, option_combination_hash, is_default_variant, is_sellable, status, created_at, updated_at) VALUES
('aeae0000-7000-4000-8000-000000000000', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000000', 'BATG-M', 'Medium', 'SKU-BATG-M', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:M', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000000', 'BATG-L', 'Large', 'SKU-BATG-L', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:L', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000002', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000001', 'WKG-M', 'Medium', 'SKU-WKG-M', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:M', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000003', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000001', 'WKG-L', 'Large', 'SKU-WKG-L', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:L', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000004', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000002', 'HEL-M', 'Medium', 'SKU-HEL-M', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:M', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000005', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000002', 'HEL-L', 'Large', 'SKU-HEL-L', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:L', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000006', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000003', 'PAD-M', 'Medium', 'SKU-PAD-M', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:M', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000007', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000003', 'PAD-L', 'Large', 'SKU-PAD-L', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:L', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000008', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000004', 'STM-STD', 'Standard', 'SKU-STM-STD', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'TYPE:STD', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000009', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000005', 'BAG-BLK', 'Black', 'SKU-BAG-BLK', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'COLOR:BLK', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-00000000000a', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000005', 'BAG-BLU', 'Blue', 'SKU-BAG-BLU', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'COLOR:BLU', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-00000000000b', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000006', 'SHOE-U8', 'UK 8', 'SKU-SHOE-U8', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:U8', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-00000000000c', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000006', 'SHOE-U9', 'UK 9', 'SKU-SHOE-U9', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:U9', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-00000000000d', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000007', 'CAP-BLK', 'Black', 'SKU-CAP-BLK', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'COLOR:BLK', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-00000000000e', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000007', 'CAP-NVY', 'Navy', 'SKU-CAP-NVY', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'COLOR:NVY', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-00000000000f', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000008', 'SLV-M', 'Medium', 'SKU-SLV-M', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:M', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000010', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000008', 'SLV-L', 'Large', 'SKU-SLV-L', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:L', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000011', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000009', 'GRP-BLK', 'Black', 'SKU-GRP-BLK', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'COLOR:BLK', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000012', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000009', 'GRP-WHT', 'White', 'SKU-GRP-WHT', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'COLOR:WHT', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000013', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000a', 'SOCK-M', 'Medium', 'SKU-SOCK-M', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:M', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000014', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000a', 'SOCK-L', 'Large', 'SKU-SOCK-L', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:L', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000015', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000b', 'SUN-BLK', 'Black', 'SKU-SUN-BLK', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'COLOR:BLK', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000016', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000b', 'SUN-TRT', 'Tortoise', 'SKU-SUN-TRT', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'COLOR:TRT', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000017', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000c', 'JKT-M', 'Medium', 'SKU-JKT-M', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:M', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000018', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000c', 'JKT-L', 'Large', 'SKU-JKT-L', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:L', false, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-000000000019', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000d', 'ABG-M', 'Medium', 'SKU-ABG-M', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:M', true, true, 'ACTIVE', now(), now()),
('aeae0000-7000-4000-8000-00000000001a', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000d', 'ABG-L', 'Large', 'SKU-ABG-L', '91000000-0000-4000-8000-000000000001', '91000000-0000-4000-8000-000000000001', 'SIZE:L', false, true, 'ACTIVE', now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_variant_option_values (id, tenant_id, product_id, product_variant_id, product_option_id, product_option_value_id, created_at, updated_at)
SELECT ('aeae0000-8000-4000-8000-' || lpad(to_hex(row_number() over (order by v.id) - 1), 12, '0'))::uuid,
       '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', v.product_id, v.id, v.product_option_id, v.product_option_value_id, now(), now()
FROM (VALUES
    ('aeae0000-1000-4000-8000-000000000000'::uuid, 'aeae0000-7000-4000-8000-000000000000'::uuid, 'aeae0000-5000-4000-8000-000000000000'::uuid, 'aeae0000-6000-4000-8000-000000000000'::uuid),
    ('aeae0000-1000-4000-8000-000000000000'::uuid, 'aeae0000-7000-4000-8000-000000000001'::uuid, 'aeae0000-5000-4000-8000-000000000000'::uuid, 'aeae0000-6000-4000-8000-000000000001'::uuid),
    ('aeae0000-1000-4000-8000-000000000001'::uuid, 'aeae0000-7000-4000-8000-000000000002'::uuid, 'aeae0000-5000-4000-8000-000000000001'::uuid, 'aeae0000-6000-4000-8000-000000000002'::uuid),
    ('aeae0000-1000-4000-8000-000000000001'::uuid, 'aeae0000-7000-4000-8000-000000000003'::uuid, 'aeae0000-5000-4000-8000-000000000001'::uuid, 'aeae0000-6000-4000-8000-000000000003'::uuid),
    ('aeae0000-1000-4000-8000-000000000002'::uuid, 'aeae0000-7000-4000-8000-000000000004'::uuid, 'aeae0000-5000-4000-8000-000000000002'::uuid, 'aeae0000-6000-4000-8000-000000000004'::uuid),
    ('aeae0000-1000-4000-8000-000000000002'::uuid, 'aeae0000-7000-4000-8000-000000000005'::uuid, 'aeae0000-5000-4000-8000-000000000002'::uuid, 'aeae0000-6000-4000-8000-000000000005'::uuid),
    ('aeae0000-1000-4000-8000-000000000003'::uuid, 'aeae0000-7000-4000-8000-000000000006'::uuid, 'aeae0000-5000-4000-8000-000000000003'::uuid, 'aeae0000-6000-4000-8000-000000000006'::uuid),
    ('aeae0000-1000-4000-8000-000000000003'::uuid, 'aeae0000-7000-4000-8000-000000000007'::uuid, 'aeae0000-5000-4000-8000-000000000003'::uuid, 'aeae0000-6000-4000-8000-000000000007'::uuid),
    ('aeae0000-1000-4000-8000-000000000004'::uuid, 'aeae0000-7000-4000-8000-000000000008'::uuid, 'aeae0000-5000-4000-8000-000000000004'::uuid, 'aeae0000-6000-4000-8000-000000000008'::uuid),
    ('aeae0000-1000-4000-8000-000000000005'::uuid, 'aeae0000-7000-4000-8000-000000000009'::uuid, 'aeae0000-5000-4000-8000-000000000005'::uuid, 'aeae0000-6000-4000-8000-000000000009'::uuid),
    ('aeae0000-1000-4000-8000-000000000005'::uuid, 'aeae0000-7000-4000-8000-00000000000a'::uuid, 'aeae0000-5000-4000-8000-000000000005'::uuid, 'aeae0000-6000-4000-8000-00000000000a'::uuid),
    ('aeae0000-1000-4000-8000-000000000006'::uuid, 'aeae0000-7000-4000-8000-00000000000b'::uuid, 'aeae0000-5000-4000-8000-000000000006'::uuid, 'aeae0000-6000-4000-8000-00000000000b'::uuid),
    ('aeae0000-1000-4000-8000-000000000006'::uuid, 'aeae0000-7000-4000-8000-00000000000c'::uuid, 'aeae0000-5000-4000-8000-000000000006'::uuid, 'aeae0000-6000-4000-8000-00000000000c'::uuid),
    ('aeae0000-1000-4000-8000-000000000007'::uuid, 'aeae0000-7000-4000-8000-00000000000d'::uuid, 'aeae0000-5000-4000-8000-000000000007'::uuid, 'aeae0000-6000-4000-8000-00000000000d'::uuid),
    ('aeae0000-1000-4000-8000-000000000007'::uuid, 'aeae0000-7000-4000-8000-00000000000e'::uuid, 'aeae0000-5000-4000-8000-000000000007'::uuid, 'aeae0000-6000-4000-8000-00000000000e'::uuid),
    ('aeae0000-1000-4000-8000-000000000008'::uuid, 'aeae0000-7000-4000-8000-00000000000f'::uuid, 'aeae0000-5000-4000-8000-000000000008'::uuid, 'aeae0000-6000-4000-8000-00000000000f'::uuid),
    ('aeae0000-1000-4000-8000-000000000008'::uuid, 'aeae0000-7000-4000-8000-000000000010'::uuid, 'aeae0000-5000-4000-8000-000000000008'::uuid, 'aeae0000-6000-4000-8000-000000000010'::uuid),
    ('aeae0000-1000-4000-8000-000000000009'::uuid, 'aeae0000-7000-4000-8000-000000000011'::uuid, 'aeae0000-5000-4000-8000-000000000009'::uuid, 'aeae0000-6000-4000-8000-000000000011'::uuid),
    ('aeae0000-1000-4000-8000-000000000009'::uuid, 'aeae0000-7000-4000-8000-000000000012'::uuid, 'aeae0000-5000-4000-8000-000000000009'::uuid, 'aeae0000-6000-4000-8000-000000000012'::uuid),
    ('aeae0000-1000-4000-8000-00000000000a'::uuid, 'aeae0000-7000-4000-8000-000000000013'::uuid, 'aeae0000-5000-4000-8000-00000000000a'::uuid, 'aeae0000-6000-4000-8000-000000000013'::uuid),
    ('aeae0000-1000-4000-8000-00000000000a'::uuid, 'aeae0000-7000-4000-8000-000000000014'::uuid, 'aeae0000-5000-4000-8000-00000000000a'::uuid, 'aeae0000-6000-4000-8000-000000000014'::uuid),
    ('aeae0000-1000-4000-8000-00000000000b'::uuid, 'aeae0000-7000-4000-8000-000000000015'::uuid, 'aeae0000-5000-4000-8000-00000000000b'::uuid, 'aeae0000-6000-4000-8000-000000000015'::uuid),
    ('aeae0000-1000-4000-8000-00000000000b'::uuid, 'aeae0000-7000-4000-8000-000000000016'::uuid, 'aeae0000-5000-4000-8000-00000000000b'::uuid, 'aeae0000-6000-4000-8000-000000000016'::uuid),
    ('aeae0000-1000-4000-8000-00000000000c'::uuid, 'aeae0000-7000-4000-8000-000000000017'::uuid, 'aeae0000-5000-4000-8000-00000000000c'::uuid, 'aeae0000-6000-4000-8000-000000000017'::uuid),
    ('aeae0000-1000-4000-8000-00000000000c'::uuid, 'aeae0000-7000-4000-8000-000000000018'::uuid, 'aeae0000-5000-4000-8000-00000000000c'::uuid, 'aeae0000-6000-4000-8000-000000000018'::uuid),
    ('aeae0000-1000-4000-8000-00000000000d'::uuid, 'aeae0000-7000-4000-8000-000000000019'::uuid, 'aeae0000-5000-4000-8000-00000000000d'::uuid, 'aeae0000-6000-4000-8000-000000000019'::uuid),
    ('aeae0000-1000-4000-8000-00000000000d'::uuid, 'aeae0000-7000-4000-8000-00000000001a'::uuid, 'aeae0000-5000-4000-8000-00000000000d'::uuid, 'aeae0000-6000-4000-8000-00000000001a'::uuid)
) AS v(product_id, id, product_option_id, product_option_value_id)
ON CONFLICT (id) DO NOTHING;

INSERT INTO price_list_items (id, tenant_id, price_list_id, product_id, product_variant_id, selling_price, compare_at_price, min_quantity, status, created_at, updated_at)
SELECT ('aeae0000-9000-4000-8000-' || lpad(to_hex(row_number() over (order by v.product_id, v.variant_id) - 1), 12, '0'))::uuid,
       '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'cccc0003-0001-4000-8000-000000000002', v.product_id, v.variant_id, v.selling_price, v.compare_at_price, 1, 'ACTIVE', now(), now()
FROM (VALUES
    ('aeae0000-1000-4000-8000-000000000000'::uuid, 'aeae0000-7000-4000-8000-000000000000'::uuid, 1800::numeric, 2200::numeric),
    ('aeae0000-1000-4000-8000-000000000000'::uuid, 'aeae0000-7000-4000-8000-000000000001'::uuid, 1800::numeric, 2200::numeric),
    ('aeae0000-1000-4000-8000-000000000001'::uuid, 'aeae0000-7000-4000-8000-000000000002'::uuid, 2500::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000001'::uuid, 'aeae0000-7000-4000-8000-000000000003'::uuid, 2500::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000002'::uuid, 'aeae0000-7000-4000-8000-000000000004'::uuid, 3500::numeric, 4200::numeric),
    ('aeae0000-1000-4000-8000-000000000002'::uuid, 'aeae0000-7000-4000-8000-000000000005'::uuid, 3500::numeric, 4200::numeric),
    ('aeae0000-1000-4000-8000-000000000003'::uuid, 'aeae0000-7000-4000-8000-000000000006'::uuid, 2800::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000003'::uuid, 'aeae0000-7000-4000-8000-000000000007'::uuid, 2800::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000004'::uuid, 'aeae0000-7000-4000-8000-000000000008'::uuid, 2200::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000005'::uuid, 'aeae0000-7000-4000-8000-000000000009'::uuid, 3200::numeric, 3800::numeric),
    ('aeae0000-1000-4000-8000-000000000005'::uuid, 'aeae0000-7000-4000-8000-00000000000a'::uuid, 3200::numeric, 3800::numeric),
    ('aeae0000-1000-4000-8000-000000000006'::uuid, 'aeae0000-7000-4000-8000-00000000000b'::uuid, 4200::numeric, 5000::numeric),
    ('aeae0000-1000-4000-8000-000000000006'::uuid, 'aeae0000-7000-4000-8000-00000000000c'::uuid, 4200::numeric, 5000::numeric),
    ('aeae0000-1000-4000-8000-000000000007'::uuid, 'aeae0000-7000-4000-8000-00000000000d'::uuid, 600::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000007'::uuid, 'aeae0000-7000-4000-8000-00000000000e'::uuid, 600::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000008'::uuid, 'aeae0000-7000-4000-8000-00000000000f'::uuid, 450::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000008'::uuid, 'aeae0000-7000-4000-8000-000000000010'::uuid, 450::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000009'::uuid, 'aeae0000-7000-4000-8000-000000000011'::uuid, 350::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-000000000009'::uuid, 'aeae0000-7000-4000-8000-000000000012'::uuid, 350::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-00000000000a'::uuid, 'aeae0000-7000-4000-8000-000000000013'::uuid, 500::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-00000000000a'::uuid, 'aeae0000-7000-4000-8000-000000000014'::uuid, 500::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-00000000000b'::uuid, 'aeae0000-7000-4000-8000-000000000015'::uuid, 1500::numeric, 1800::numeric),
    ('aeae0000-1000-4000-8000-00000000000b'::uuid, 'aeae0000-7000-4000-8000-000000000016'::uuid, 1500::numeric, 1800::numeric),
    ('aeae0000-1000-4000-8000-00000000000c'::uuid, 'aeae0000-7000-4000-8000-000000000017'::uuid, 3800::numeric, 4500::numeric),
    ('aeae0000-1000-4000-8000-00000000000c'::uuid, 'aeae0000-7000-4000-8000-000000000018'::uuid, 3800::numeric, 4500::numeric),
    ('aeae0000-1000-4000-8000-00000000000d'::uuid, 'aeae0000-7000-4000-8000-000000000019'::uuid, 900::numeric, NULL::numeric),
    ('aeae0000-1000-4000-8000-00000000000d'::uuid, 'aeae0000-7000-4000-8000-00000000001a'::uuid, 900::numeric, NULL::numeric)
) AS v(product_id, variant_id, selling_price, compare_at_price)
ON CONFLICT (id) DO NOTHING;

INSERT INTO product_images (id, tenant_id, product_id, product_variant_id, media_asset_id, image_purpose, sort_order, is_primary_image, status, created_at, updated_at) VALUES
('aeae0000-a000-4000-8000-000000000000', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000000', NULL, 'aeae0000-2000-4000-8000-000000000000', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-000000000001', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000001', NULL, 'aeae0000-2000-4000-8000-000000000001', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-000000000002', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000002', NULL, 'aeae0000-2000-4000-8000-000000000002', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-000000000003', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000003', NULL, 'aeae0000-2000-4000-8000-000000000003', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-000000000004', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000004', NULL, 'aeae0000-2000-4000-8000-000000000004', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-000000000005', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000005', NULL, 'aeae0000-2000-4000-8000-000000000005', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-000000000006', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000006', NULL, 'aeae0000-2000-4000-8000-000000000006', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-000000000007', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000007', NULL, 'aeae0000-2000-4000-8000-000000000007', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-000000000008', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000008', NULL, 'aeae0000-2000-4000-8000-000000000008', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-000000000009', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-000000000009', NULL, 'aeae0000-2000-4000-8000-000000000009', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-00000000000a', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000a', NULL, 'aeae0000-2000-4000-8000-00000000000a', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-00000000000b', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000b', NULL, 'aeae0000-2000-4000-8000-00000000000b', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-00000000000c', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000c', NULL, 'aeae0000-2000-4000-8000-00000000000c', 'CATALOG', 0, true, 'ACTIVE', now(), now()),
('aeae0000-a000-4000-8000-00000000000d', '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', 'aeae0000-1000-4000-8000-00000000000d', NULL, 'aeae0000-2000-4000-8000-00000000000d', 'CATALOG', 0, true, 'ACTIVE', now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO inventory_balances (id, tenant_id, inventory_location_id, product_id, product_variant_id, on_hand_quantity, reserved_quantity, damaged_quantity, quarantine_quantity, row_version, created_at, updated_at)
SELECT ('aeae0000-b000-4000-8000-' || lpad(to_hex(row_number() over (order by v.product_id, v.variant_id) - 1), 12, '0'))::uuid,
       '08b0c8b0-a5bf-44f0-8814-cb2fe0120000', '33333333-0002-4000-8000-000000000001', v.product_id, v.variant_id, 100, 0, 0, 0, 1, now(), now()
FROM (VALUES
    ('aeae0000-1000-4000-8000-000000000000'::uuid, 'aeae0000-7000-4000-8000-000000000000'::uuid),
    ('aeae0000-1000-4000-8000-000000000000'::uuid, 'aeae0000-7000-4000-8000-000000000001'::uuid),
    ('aeae0000-1000-4000-8000-000000000001'::uuid, 'aeae0000-7000-4000-8000-000000000002'::uuid),
    ('aeae0000-1000-4000-8000-000000000001'::uuid, 'aeae0000-7000-4000-8000-000000000003'::uuid),
    ('aeae0000-1000-4000-8000-000000000002'::uuid, 'aeae0000-7000-4000-8000-000000000004'::uuid),
    ('aeae0000-1000-4000-8000-000000000002'::uuid, 'aeae0000-7000-4000-8000-000000000005'::uuid),
    ('aeae0000-1000-4000-8000-000000000003'::uuid, 'aeae0000-7000-4000-8000-000000000006'::uuid),
    ('aeae0000-1000-4000-8000-000000000003'::uuid, 'aeae0000-7000-4000-8000-000000000007'::uuid),
    ('aeae0000-1000-4000-8000-000000000004'::uuid, 'aeae0000-7000-4000-8000-000000000008'::uuid),
    ('aeae0000-1000-4000-8000-000000000005'::uuid, 'aeae0000-7000-4000-8000-000000000009'::uuid),
    ('aeae0000-1000-4000-8000-000000000005'::uuid, 'aeae0000-7000-4000-8000-00000000000a'::uuid),
    ('aeae0000-1000-4000-8000-000000000006'::uuid, 'aeae0000-7000-4000-8000-00000000000b'::uuid),
    ('aeae0000-1000-4000-8000-000000000006'::uuid, 'aeae0000-7000-4000-8000-00000000000c'::uuid),
    ('aeae0000-1000-4000-8000-000000000007'::uuid, 'aeae0000-7000-4000-8000-00000000000d'::uuid),
    ('aeae0000-1000-4000-8000-000000000007'::uuid, 'aeae0000-7000-4000-8000-00000000000e'::uuid),
    ('aeae0000-1000-4000-8000-000000000008'::uuid, 'aeae0000-7000-4000-8000-00000000000f'::uuid),
    ('aeae0000-1000-4000-8000-000000000008'::uuid, 'aeae0000-7000-4000-8000-000000000010'::uuid),
    ('aeae0000-1000-4000-8000-000000000009'::uuid, 'aeae0000-7000-4000-8000-000000000011'::uuid),
    ('aeae0000-1000-4000-8000-000000000009'::uuid, 'aeae0000-7000-4000-8000-000000000012'::uuid),
    ('aeae0000-1000-4000-8000-00000000000a'::uuid, 'aeae0000-7000-4000-8000-000000000013'::uuid),
    ('aeae0000-1000-4000-8000-00000000000a'::uuid, 'aeae0000-7000-4000-8000-000000000014'::uuid),
    ('aeae0000-1000-4000-8000-00000000000b'::uuid, 'aeae0000-7000-4000-8000-000000000015'::uuid),
    ('aeae0000-1000-4000-8000-00000000000b'::uuid, 'aeae0000-7000-4000-8000-000000000016'::uuid),
    ('aeae0000-1000-4000-8000-00000000000c'::uuid, 'aeae0000-7000-4000-8000-000000000017'::uuid),
    ('aeae0000-1000-4000-8000-00000000000c'::uuid, 'aeae0000-7000-4000-8000-000000000018'::uuid),
    ('aeae0000-1000-4000-8000-00000000000d'::uuid, 'aeae0000-7000-4000-8000-000000000019'::uuid),
    ('aeae0000-1000-4000-8000-00000000000d'::uuid, 'aeae0000-7000-4000-8000-00000000001a'::uuid)
) AS v(product_id, variant_id)
ON CONFLICT (id) DO UPDATE SET on_hand_quantity = 100;


";
}
