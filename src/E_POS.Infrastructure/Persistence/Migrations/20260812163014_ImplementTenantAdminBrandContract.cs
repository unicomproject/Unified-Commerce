using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementTenantAdminBrandContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    marker constant text := 'owned-by:20260812163014_ImplementTenantAdminBrandContract';
                    object_oid oid;
                    actual_columns text[];
                    actual_definition text;
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM products p
                        LEFT JOIN brands b
                          ON b.id = p.brand_id
                         AND b.tenant_id = p.tenant_id
                        WHERE p.brand_id IS NOT NULL
                          AND b.id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Brand contract migration blocked: products contain orphan or cross-tenant brand references.';
                    END IF;

                    -- Column: create it only when absent; otherwise require the exact
                    -- integer, NOT NULL, DEFAULT 0 contract.
                    SELECT a.attrelid
                    INTO object_oid
                    FROM pg_catalog.pg_attribute a
                    WHERE a.attrelid = 'public.brands'::regclass
                      AND a.attname = 'sort_order'
                      AND a.attnum > 0
                      AND NOT a.attisdropped;

                    IF object_oid IS NULL THEN
                        ALTER TABLE public.brands
                            ADD COLUMN sort_order integer NOT NULL DEFAULT 0;
                        COMMENT ON COLUMN public.brands.sort_order IS
                            'owned-by:20260812163014_ImplementTenantAdminBrandContract';
                    ELSIF NOT EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_attribute a
                        JOIN pg_catalog.pg_attrdef d
                          ON d.adrelid = a.attrelid AND d.adnum = a.attnum
                        WHERE a.attrelid = 'public.brands'::regclass
                          AND a.attname = 'sort_order'
                          AND a.atttypid = 'integer'::regtype
                          AND a.attnotnull
                          AND pg_get_expr(d.adbin, d.adrelid) IN ('0', '0::integer')
                    ) THEN
                        RAISE EXCEPTION 'Brand contract migration blocked: brands.sort_order exists with an incompatible type, nullability, or default.';
                    END IF;

                    -- Index helper checks are structural: btree, non-unique, valid,
                    -- non-partial, non-expression, and exact ordered key columns.
                    SELECT c.oid,
                           ARRAY(
                               SELECT a.attname::text
                               FROM unnest(i.indkey::smallint[]) WITH ORDINALITY key(attnum, ord)
                               JOIN pg_catalog.pg_attribute a
                                 ON a.attrelid = i.indrelid AND a.attnum = key.attnum
                               WHERE key.ord <= i.indnkeyatts
                               ORDER BY key.ord
                           )
                    INTO object_oid, actual_columns
                    FROM pg_catalog.pg_class c
                    JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                    JOIN pg_catalog.pg_index i ON i.indexrelid = c.oid
                    JOIN pg_catalog.pg_am am ON am.oid = c.relam
                    WHERE n.nspname = 'public'
                      AND c.relname = 'ix_products_tenant_id_brand_id'
                      AND c.relkind = 'i';

                    IF object_oid IS NULL THEN
                        CREATE INDEX ix_products_tenant_id_brand_id
                            ON public.products (tenant_id, brand_id);
                        COMMENT ON INDEX public.ix_products_tenant_id_brand_id IS
                            'owned-by:20260812163014_ImplementTenantAdminBrandContract';
                    ELSIF actual_columns <> ARRAY['tenant_id', 'brand_id'] OR NOT EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_index i
                        JOIN pg_catalog.pg_class c ON c.oid = i.indexrelid
                        JOIN pg_catalog.pg_am am ON am.oid = c.relam
                        WHERE i.indexrelid = object_oid
                          AND am.amname = 'btree'
                          AND NOT i.indisunique
                          AND i.indisvalid
                          AND i.indpred IS NULL
                          AND i.indexprs IS NULL
                          AND i.indnkeyatts = 2
                    ) THEN
                        RAISE EXCEPTION 'Brand contract migration blocked: ix_products_tenant_id_brand_id exists with an incompatible definition.';
                    END IF;

                    object_oid := NULL;
                    actual_columns := NULL;
                    SELECT c.oid,
                           ARRAY(
                               SELECT a.attname::text
                               FROM unnest(i.indkey::smallint[]) WITH ORDINALITY key(attnum, ord)
                               JOIN pg_catalog.pg_attribute a
                                 ON a.attrelid = i.indrelid AND a.attnum = key.attnum
                               WHERE key.ord <= i.indnkeyatts
                               ORDER BY key.ord
                           )
                    INTO object_oid, actual_columns
                    FROM pg_catalog.pg_class c
                    JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                    JOIN pg_catalog.pg_index i ON i.indexrelid = c.oid
                    WHERE n.nspname = 'public'
                      AND c.relname = 'ix_brands_tenant_id_sort_order_brand_code'
                      AND c.relkind = 'i';

                    IF object_oid IS NULL THEN
                        CREATE INDEX ix_brands_tenant_id_sort_order_brand_code
                            ON public.brands (tenant_id, sort_order, brand_code);
                        COMMENT ON INDEX public.ix_brands_tenant_id_sort_order_brand_code IS
                            'owned-by:20260812163014_ImplementTenantAdminBrandContract';
                    ELSIF actual_columns <> ARRAY['tenant_id', 'sort_order', 'brand_code'] OR NOT EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_index i
                        JOIN pg_catalog.pg_class c ON c.oid = i.indexrelid
                        JOIN pg_catalog.pg_am am ON am.oid = c.relam
                        WHERE i.indexrelid = object_oid
                          AND am.amname = 'btree'
                          AND NOT i.indisunique
                          AND i.indisvalid
                          AND i.indpred IS NULL
                          AND i.indexprs IS NULL
                          AND i.indnkeyatts = 3
                    ) THEN
                        RAISE EXCEPTION 'Brand contract migration blocked: ix_brands_tenant_id_sort_order_brand_code exists with an incompatible definition.';
                    END IF;

                    -- Check constraint must be validated and enforce sort_order >= 0.
                    object_oid := NULL;
                    actual_definition := NULL;
                    SELECT con.oid, regexp_replace(pg_get_constraintdef(con.oid, true), '\s+', '', 'g')
                    INTO object_oid, actual_definition
                    FROM pg_catalog.pg_constraint con
                    WHERE con.conrelid = 'public.brands'::regclass
                      AND con.conname = 'ck_brands_sort_order';

                    IF object_oid IS NULL THEN
                        ALTER TABLE public.brands
                            ADD CONSTRAINT ck_brands_sort_order CHECK (sort_order >= 0);
                        COMMENT ON CONSTRAINT ck_brands_sort_order ON public.brands IS
                            'owned-by:20260812163014_ImplementTenantAdminBrandContract';
                    ELSIF actual_definition NOT IN ('CHECK(sort_order>=0)', 'CHECK((sort_order>=0))') OR NOT EXISTS (
                        SELECT 1 FROM pg_catalog.pg_constraint
                        WHERE oid = object_oid AND contype = 'c' AND convalidated
                    ) THEN
                        RAISE EXCEPTION 'Brand contract migration blocked: ck_brands_sort_order exists with an incompatible or unvalidated definition.';
                    END IF;

                    -- Composite tenant-safe FK. PostgreSQL uses confdeltype 'r' for
                    -- RESTRICT. Column arrays are compared in their declared order.
                    object_oid := NULL;
                    SELECT con.oid,
                           ARRAY(SELECT a.attname::text FROM unnest(con.conkey) WITH ORDINALITY k(attnum, ord)
                                 JOIN pg_catalog.pg_attribute a ON a.attrelid = con.conrelid AND a.attnum = k.attnum
                                 ORDER BY k.ord)
                    INTO object_oid, actual_columns
                    FROM pg_catalog.pg_constraint con
                    WHERE con.conrelid = 'public.products'::regclass
                      AND con.conname = 'fk_products_brand_tenant';

                    IF object_oid IS NULL THEN
                        ALTER TABLE public.products
                            ADD CONSTRAINT fk_products_brand_tenant
                            FOREIGN KEY (tenant_id, brand_id)
                            REFERENCES public.brands (tenant_id, id)
                            ON DELETE RESTRICT;
                        COMMENT ON CONSTRAINT fk_products_brand_tenant ON public.products IS
                            'owned-by:20260812163014_ImplementTenantAdminBrandContract';
                    ELSIF actual_columns <> ARRAY['tenant_id', 'brand_id'] OR NOT EXISTS (
                        SELECT 1
                        FROM pg_catalog.pg_constraint con
                        WHERE con.oid = object_oid
                          AND con.contype = 'f'
                          AND con.confrelid = 'public.brands'::regclass
                          AND con.confdeltype = 'r'
                          AND con.convalidated
                          AND ARRAY(
                              SELECT a.attname::text
                              FROM unnest(con.confkey) WITH ORDINALITY k(attnum, ord)
                              JOIN pg_catalog.pg_attribute a
                                ON a.attrelid = con.confrelid AND a.attnum = k.attnum
                              ORDER BY k.ord
                          ) = ARRAY['tenant_id', 'id']
                    ) THEN
                        RAISE EXCEPTION 'Brand contract migration blocked: fk_products_brand_tenant exists with an incompatible definition.';
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    marker constant text := 'owned-by:20260812163014_ImplementTenantAdminBrandContract';
                    owned_count integer;
                BEGIN
                    SELECT count(*) INTO owned_count
                    FROM (VALUES
                        (col_description('public.brands'::regclass,
                            (SELECT attnum FROM pg_catalog.pg_attribute
                             WHERE attrelid = 'public.brands'::regclass AND attname = 'sort_order'))),
                        (obj_description(to_regclass('public.ix_products_tenant_id_brand_id'), 'pg_class')),
                        (obj_description(to_regclass('public.ix_brands_tenant_id_sort_order_brand_code'), 'pg_class')),
                        ((SELECT obj_description(oid, 'pg_constraint') FROM pg_catalog.pg_constraint
                          WHERE conrelid = 'public.brands'::regclass AND conname = 'ck_brands_sort_order')),
                        ((SELECT obj_description(oid, 'pg_constraint') FROM pg_catalog.pg_constraint
                          WHERE conrelid = 'public.products'::regclass AND conname = 'fk_products_brand_tenant'))
                    ) AS provenance(value)
                    WHERE value = marker;

                    IF owned_count <> 5 THEN
                        RAISE EXCEPTION 'Brand contract rollback blocked: schema objects are legacy-owned or have mixed provenance.';
                    END IF;

                    ALTER TABLE public.products DROP CONSTRAINT fk_products_brand_tenant;
                    DROP INDEX public.ix_products_tenant_id_brand_id;
                    DROP INDEX public.ix_brands_tenant_id_sort_order_brand_code;
                    ALTER TABLE public.brands DROP CONSTRAINT ck_brands_sort_order;
                    ALTER TABLE public.brands DROP COLUMN sort_order;
                END $$;
                """);
        }
    }
}
