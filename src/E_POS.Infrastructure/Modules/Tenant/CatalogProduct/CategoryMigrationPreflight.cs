namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct;

public static class CategoryMigrationPreflight
{
    public const string GuardName = "CAT-MIG-PREFLIGHT-001";

    public static string BuildGuardSql(string tableName = "categories")
    {
        return $"""
            DO $$
            DECLARE
              code_conflicts integer;
              name_conflicts integer;
              self_parent_conflicts integer;
              dangling_conflicts integer;
              cross_tenant_conflicts integer;
              cycle_conflicts integer;
              depth_conflicts integer;
              evidence text;
            BEGIN
              SELECT COUNT(*) INTO code_conflicts
              FROM (
                SELECT tenant_id, UPPER(BTRIM(category_code)) AS normalized_code
                FROM {tableName}
                GROUP BY tenant_id, UPPER(BTRIM(category_code))
                HAVING COUNT(*) > 1
              ) duplicate_codes;

              IF code_conflicts > 0 THEN
                SELECT string_agg(
                  format('DUPLICATE_CODE tenant=%s id=%s parent=%s code=%s name=%s', tenant_id, id, parent_category_id, category_code, category_name),
                  '; '
                )
                INTO evidence
                FROM {tableName} c
                WHERE EXISTS (
                  SELECT 1
                  FROM {tableName} d
                  WHERE d.tenant_id = c.tenant_id
                    AND UPPER(BTRIM(d.category_code)) = UPPER(BTRIM(c.category_code))
                    AND d.id <> c.id
                );

                RAISE EXCEPTION '{GuardName}: duplicate normalized category_code within tenant. Silent merge is forbidden. %', COALESCE(evidence, '');
              END IF;

              SELECT COUNT(*) INTO name_conflicts
              FROM (
                SELECT tenant_id, LOWER(BTRIM(category_name)) AS normalized_name
                FROM {tableName}
                GROUP BY tenant_id, LOWER(BTRIM(category_name))
                HAVING COUNT(*) > 1
              ) duplicate_names;

              IF name_conflicts > 0 THEN
                SELECT string_agg(
                  format('DUPLICATE_NAME tenant=%s id=%s parent=%s code=%s name=%s', tenant_id, id, parent_category_id, category_code, category_name),
                  '; '
                )
                INTO evidence
                FROM {tableName} c
                WHERE EXISTS (
                  SELECT 1
                  FROM {tableName} d
                  WHERE d.tenant_id = c.tenant_id
                    AND LOWER(BTRIM(d.category_name)) = LOWER(BTRIM(c.category_name))
                    AND d.id <> c.id
                );

                RAISE EXCEPTION '{GuardName}: duplicate normalized category_name within tenant. Silent merge is forbidden. %', COALESCE(evidence, '');
              END IF;

              SELECT COUNT(*) INTO self_parent_conflicts
              FROM {tableName}
              WHERE parent_category_id IS NOT NULL AND parent_category_id = id;

              IF self_parent_conflicts > 0 THEN
                SELECT string_agg(
                  format('SELF_PARENT tenant=%s id=%s parent=%s code=%s', tenant_id, id, parent_category_id, category_code),
                  '; '
                )
                INTO evidence
                FROM {tableName}
                WHERE parent_category_id IS NOT NULL AND parent_category_id = id;

                RAISE EXCEPTION '{GuardName}: self-parent category hierarchy conflict. Silent repair is forbidden. %', COALESCE(evidence, '');
              END IF;

              SELECT COUNT(*) INTO dangling_conflicts
              FROM {tableName} c
              WHERE c.parent_category_id IS NOT NULL
                AND NOT EXISTS (
                  SELECT 1 FROM {tableName} p WHERE p.id = c.parent_category_id
                );

              IF dangling_conflicts > 0 THEN
                SELECT string_agg(
                  format('DANGLING_PARENT tenant=%s id=%s parent=%s code=%s', tenant_id, id, parent_category_id, category_code),
                  '; '
                )
                INTO evidence
                FROM {tableName} c
                WHERE c.parent_category_id IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM {tableName} p WHERE p.id = c.parent_category_id
                  );

                RAISE EXCEPTION '{GuardName}: dangling parent_category_id. Silent repair is forbidden. %', COALESCE(evidence, '');
              END IF;

              SELECT COUNT(*) INTO cross_tenant_conflicts
              FROM {tableName} c
              JOIN {tableName} p ON p.id = c.parent_category_id
              WHERE p.tenant_id <> c.tenant_id;

              IF cross_tenant_conflicts > 0 THEN
                SELECT string_agg(
                  format('CROSS_TENANT_PARENT tenant=%s id=%s parent=%s code=%s', c.tenant_id, c.id, c.parent_category_id, c.category_code),
                  '; '
                )
                INTO evidence
                FROM {tableName} c
                JOIN {tableName} p ON p.id = c.parent_category_id
                WHERE p.tenant_id <> c.tenant_id;

                RAISE EXCEPTION '{GuardName}: parent_category_id belongs to another tenant. Silent repair is forbidden. %', COALESCE(evidence, '');
              END IF;

              SELECT COUNT(*) INTO cycle_conflicts
              FROM (
                WITH RECURSIVE walk AS (
                  SELECT c.id,
                         c.tenant_id,
                         c.parent_category_id,
                         c.category_code,
                         ARRAY[c.id]::uuid[] AS path,
                         false AS is_cycle
                  FROM {tableName} c
                  WHERE c.parent_category_id IS NOT NULL
                  UNION ALL
                  SELECT w.id,
                         w.tenant_id,
                         p.parent_category_id,
                         w.category_code,
                         w.path || p.id,
                         p.id = ANY (w.path)
                  FROM walk w
                  INNER JOIN {tableName} p ON p.id = w.parent_category_id
                  WHERE w.parent_category_id IS NOT NULL
                    AND NOT w.is_cycle
                    AND cardinality(w.path) < 64
                )
                SELECT DISTINCT id FROM walk WHERE is_cycle
              ) cycles;

              IF cycle_conflicts > 0 THEN
                SELECT string_agg(
                  format('PARENT_CYCLE tenant=%s id=%s parent=%s code=%s', tenant_id, id, parent_category_id, category_code),
                  '; '
                )
                INTO evidence
                FROM {tableName} c
                WHERE EXISTS (
                  WITH RECURSIVE walk AS (
                    SELECT s.id, s.parent_category_id, ARRAY[s.id]::uuid[] AS path, false AS is_cycle
                    FROM {tableName} s
                    WHERE s.id = c.id AND s.parent_category_id IS NOT NULL
                    UNION ALL
                    SELECT w.id, p.parent_category_id, w.path || p.id, p.id = ANY (w.path)
                    FROM walk w
                    INNER JOIN {tableName} p ON p.id = w.parent_category_id
                    WHERE w.parent_category_id IS NOT NULL
                      AND NOT w.is_cycle
                      AND cardinality(w.path) < 64
                  )
                  SELECT 1 FROM walk WHERE is_cycle
                );

                RAISE EXCEPTION '{GuardName}: circular parent chain. Silent repair is forbidden. %', COALESCE(evidence, '');
              END IF;

              SELECT COUNT(*) INTO depth_conflicts
              FROM (
                WITH RECURSIVE depths AS (
                  SELECT id, tenant_id, parent_category_id, category_code, 1 AS level, ARRAY[id]::uuid[] AS path
                  FROM {tableName}
                  WHERE parent_category_id IS NULL
                  UNION ALL
                  SELECT c.id, c.tenant_id, c.parent_category_id, c.category_code, d.level + 1, d.path || c.id
                  FROM depths d
                  INNER JOIN {tableName} c ON c.parent_category_id = d.id
                  WHERE d.level < 32
                    AND NOT (c.id = ANY (d.path))
                )
                SELECT id FROM depths WHERE level > 5
              ) too_deep;

              IF depth_conflicts > 0 THEN
                SELECT string_agg(
                  format('MAX_DEPTH_EXCEEDED tenant=%s id=%s parent=%s code=%s', tenant_id, id, parent_category_id, category_code),
                  '; '
                )
                INTO evidence
                FROM (
                  WITH RECURSIVE depths AS (
                    SELECT id, tenant_id, parent_category_id, category_code, 1 AS level, ARRAY[id]::uuid[] AS path
                    FROM {tableName}
                    WHERE parent_category_id IS NULL
                    UNION ALL
                    SELECT c.id, c.tenant_id, c.parent_category_id, c.category_code, d.level + 1, d.path || c.id
                    FROM depths d
                    INNER JOIN {tableName} c ON c.parent_category_id = d.id
                    WHERE d.level < 32
                      AND NOT (c.id = ANY (d.path))
                  )
                  SELECT tenant_id, id, parent_category_id, category_code
                  FROM depths
                  WHERE level > 5
                ) deep;

                RAISE EXCEPTION '{GuardName}: existing hierarchy depth exceeds 5. Silent repair is forbidden. %', COALESCE(evidence, '');
              END IF;
            END $$;
            """;
    }

    public static string BuildDuplicateCodeDetectionSql(string tableName = "categories")
    {
        return $"""
            SELECT tenant_id AS "TenantId",
                   id AS "CategoryId",
                   parent_category_id AS "ParentCategoryId",
                   category_code AS "CategoryCode",
                   category_name AS "CategoryName",
                   'DUPLICATE_CODE' AS "ConflictType"
            FROM {tableName} c
            WHERE EXISTS (
              SELECT 1
              FROM {tableName} d
              WHERE d.tenant_id = c.tenant_id
                AND UPPER(BTRIM(d.category_code)) = UPPER(BTRIM(c.category_code))
                AND d.id <> c.id
            )
            ORDER BY tenant_id, category_code, id
            """;
    }

    public static string BuildDuplicateNameDetectionSql(string tableName = "categories")
    {
        return $"""
            SELECT tenant_id AS "TenantId",
                   id AS "CategoryId",
                   parent_category_id AS "ParentCategoryId",
                   category_code AS "CategoryCode",
                   category_name AS "CategoryName",
                   'DUPLICATE_NAME' AS "ConflictType"
            FROM {tableName} c
            WHERE EXISTS (
              SELECT 1
              FROM {tableName} d
              WHERE d.tenant_id = c.tenant_id
                AND LOWER(BTRIM(d.category_name)) = LOWER(BTRIM(c.category_name))
                AND d.id <> c.id
            )
            ORDER BY tenant_id, category_name, id
            """;
    }

    public static string BuildHierarchyConflictDetectionSql(string tableName = "categories")
    {
        return $"""
            SELECT tenant_id AS "TenantId",
                   id AS "CategoryId",
                   parent_category_id AS "ParentCategoryId",
                   category_code AS "CategoryCode",
                   'SELF_PARENT' AS "ConflictType"
            FROM {tableName}
            WHERE parent_category_id IS NOT NULL AND parent_category_id = id
            UNION ALL
            SELECT c.tenant_id, c.id, c.parent_category_id, c.category_code, 'DANGLING_PARENT'
            FROM {tableName} c
            WHERE c.parent_category_id IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM {tableName} p WHERE p.id = c.parent_category_id)
            UNION ALL
            SELECT c.tenant_id, c.id, c.parent_category_id, c.category_code, 'CROSS_TENANT_PARENT'
            FROM {tableName} c
            JOIN {tableName} p ON p.id = c.parent_category_id
            WHERE p.tenant_id <> c.tenant_id
            UNION ALL
            SELECT DISTINCT w.tenant_id, w.id, w.origin_parent, w.category_code, 'PARENT_CYCLE'
            FROM (
              WITH RECURSIVE walk AS (
                SELECT c.id,
                       c.tenant_id,
                       c.parent_category_id AS origin_parent,
                       c.parent_category_id,
                       c.category_code,
                       ARRAY[c.id]::uuid[] AS path,
                       false AS is_cycle
                FROM {tableName} c
                WHERE c.parent_category_id IS NOT NULL
                UNION ALL
                SELECT w.id,
                       w.tenant_id,
                       w.origin_parent,
                       p.parent_category_id,
                       w.category_code,
                       w.path || p.id,
                       p.id = ANY (w.path)
                FROM walk w
                INNER JOIN {tableName} p ON p.id = w.parent_category_id
                WHERE w.parent_category_id IS NOT NULL
                  AND NOT w.is_cycle
                  AND cardinality(w.path) < 64
              )
              SELECT tenant_id, id, origin_parent, category_code FROM walk WHERE is_cycle
            ) w
            UNION ALL
            SELECT tenant_id, id, parent_category_id, category_code, 'MAX_DEPTH_EXCEEDED'
            FROM (
              WITH RECURSIVE depths AS (
                SELECT id, tenant_id, parent_category_id, category_code, 1 AS level, ARRAY[id]::uuid[] AS path
                FROM {tableName}
                WHERE parent_category_id IS NULL
                UNION ALL
                SELECT c.id, c.tenant_id, c.parent_category_id, c.category_code, d.level + 1, d.path || c.id
                FROM depths d
                INNER JOIN {tableName} c ON c.parent_category_id = d.id
                WHERE d.level < 32
                  AND NOT (c.id = ANY (d.path))
              )
              SELECT tenant_id, id, parent_category_id, category_code
              FROM depths
              WHERE level > 5
            ) deep
            ORDER BY "ConflictType", "TenantId", "CategoryId"
            """;
    }
}
