namespace E_POS.Infrastructure.Persistence.Seed;

public static class ReferenceProductOptionTemplateSeedData
{
    public const string UpSql = """
        -- Seed Product Option Templates (Master Data)
        INSERT INTO product_option_templates (
            id, template_code, template_name, option_type, input_type, sort_order, status,
            created_by_platform_user_id, updated_by_platform_user_id, created_at, updated_at
        )
        VALUES
            ('d0000000-0000-4000-8000-000000000101', 'SIZE', 'Size', 'VARIANT', 'SELECT', 10, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000102', 'COLOUR', 'Colour', 'VARIANT', 'SELECT', 20, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000103', 'MATERIAL', 'Material', 'VARIANT', 'SELECT', 30, 'ACTIVE', NULL, NULL, now(), now())
        ON CONFLICT (id) DO UPDATE
        SET template_code = EXCLUDED.template_code,
            template_name = EXCLUDED.template_name,
            option_type = EXCLUDED.option_type,
            input_type = EXCLUDED.input_type,
            sort_order = EXCLUDED.sort_order,
            status = 'ACTIVE',
            updated_at = now();

        -- Seed Product Option Template Values (Master Data)
        INSERT INTO product_option_template_values (
            id, option_template_id, value_code, value_name, display_name, color_hex, image_url, sort_order, status,
            created_by_platform_user_id, updated_by_platform_user_id, created_at, updated_at
        )
        VALUES
            -- SIZE Values
            ('d0000000-0000-4000-8000-000000000111', 'd0000000-0000-4000-8000-000000000101', 'XS', 'Extra Small', 'XS', NULL, NULL, 10, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000112', 'd0000000-0000-4000-8000-000000000101', 'S', 'Small', 'S', NULL, NULL, 20, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000113', 'd0000000-0000-4000-8000-000000000101', 'M', 'Medium', 'M', NULL, NULL, 30, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000114', 'd0000000-0000-4000-8000-000000000101', 'L', 'Large', 'L', NULL, NULL, 40, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000115', 'd0000000-0000-4000-8000-000000000101', 'XL', 'Extra Large', 'XL', NULL, NULL, 50, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000116', 'd0000000-0000-4000-8000-000000000101', 'XXL', 'Double Extra Large', 'XXL', NULL, NULL, 60, 'ACTIVE', NULL, NULL, now(), now()),
            
            -- COLOUR Values
            ('d0000000-0000-4000-8000-000000000121', 'd0000000-0000-4000-8000-000000000102', 'BLACK', 'Black', 'Black', '#000000', NULL, 10, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000122', 'd0000000-0000-4000-8000-000000000102', 'WHITE', 'White', 'White', '#FFFFFF', NULL, 20, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000123', 'd0000000-0000-4000-8000-000000000102', 'RED', 'Red', 'Red', '#FF0000', NULL, 30, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000124', 'd0000000-0000-4000-8000-000000000102', 'BLUE', 'Blue', 'Blue', '#0000FF', NULL, 40, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000125', 'd0000000-0000-4000-8000-000000000102', 'GREEN', 'Green', 'Green', '#008000', NULL, 50, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000126', 'd0000000-0000-4000-8000-000000000102', 'YELLOW', 'Yellow', 'Yellow', '#FFFF00', NULL, 60, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000127', 'd0000000-0000-4000-8000-000000000102', 'GREY', 'Grey', 'Grey', '#808080', NULL, 70, 'ACTIVE', NULL, NULL, now(), now()),
            
            -- MATERIAL Values
            ('d0000000-0000-4000-8000-000000000131', 'd0000000-0000-4000-8000-000000000103', 'COTTON', 'Cotton', 'Cotton', NULL, NULL, 10, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000132', 'd0000000-0000-4000-8000-000000000103', 'POLYESTER', 'Polyester', 'Polyester', NULL, NULL, 20, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000133', 'd0000000-0000-4000-8000-000000000103', 'LINEN', 'Linen', 'Linen', NULL, NULL, 30, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000134', 'd0000000-0000-4000-8000-000000000103', 'WOOL', 'Wool', 'Wool', NULL, NULL, 40, 'ACTIVE', NULL, NULL, now(), now()),
            ('d0000000-0000-4000-8000-000000000135', 'd0000000-0000-4000-8000-000000000103', 'DENIM', 'Denim', 'Denim', NULL, NULL, 50, 'ACTIVE', NULL, NULL, now(), now())
        ON CONFLICT (id) DO UPDATE
        SET option_template_id = EXCLUDED.option_template_id,
            value_code = EXCLUDED.value_code,
            value_name = EXCLUDED.value_name,
            display_name = EXCLUDED.display_name,
            color_hex = EXCLUDED.color_hex,
            image_url = EXCLUDED.image_url,
            sort_order = EXCLUDED.sort_order,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM product_option_template_values
        WHERE id IN (
            'd0000000-0000-4000-8000-000000000111',
            'd0000000-0000-4000-8000-000000000112',
            'd0000000-0000-4000-8000-000000000113',
            'd0000000-0000-4000-8000-000000000114',
            'd0000000-0000-4000-8000-000000000115',
            'd0000000-0000-4000-8000-000000000116',
            'd0000000-0000-4000-8000-000000000121',
            'd0000000-0000-4000-8000-000000000122',
            'd0000000-0000-4000-8000-000000000123',
            'd0000000-0000-4000-8000-000000000124',
            'd0000000-0000-4000-8000-000000000125',
            'd0000000-0000-4000-8000-000000000126',
            'd0000000-0000-4000-8000-000000000127',
            'd0000000-0000-4000-8000-000000000131',
            'd0000000-0000-4000-8000-000000000132',
            'd0000000-0000-4000-8000-000000000133',
            'd0000000-0000-4000-8000-000000000134',
            'd0000000-0000-4000-8000-000000000135'
        );

        DELETE FROM product_option_templates
        WHERE id IN (
            'd0000000-0000-4000-8000-000000000101',
            'd0000000-0000-4000-8000-000000000102',
            'd0000000-0000-4000-8000-000000000103'
        );
        """;
}
