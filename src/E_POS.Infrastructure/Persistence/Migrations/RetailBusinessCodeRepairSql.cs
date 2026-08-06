namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Production-safe repair for the legacy development Retail catalogue row.
/// The exact development description is the provenance marker written by the
/// original seed; no production row is inferred from a development UUID.
/// </summary>
public static class RetailBusinessCodeRepairSql
{
    public const string Up = """
        DO $flow4_retail_repair$
        DECLARE
            candidate_count integer;
            candidate_id uuid;
        BEGIN
            SELECT COUNT(*), (ARRAY_AGG(id ORDER BY id))[1]
            INTO candidate_count, candidate_id
            FROM business_types
            WHERE NULLIF(BTRIM(COALESCE(business_code, '')), '') IS NULL
              AND LOWER(BTRIM(business_name)) = 'retail'
              AND BTRIM(COALESCE(description, '')) = 'Development retail tenant seed business type.'
              AND UPPER(BTRIM(status)) = 'ACTIVE';

            IF candidate_count > 1 THEN
                RAISE EXCEPTION USING
                    ERRCODE = 'P0001',
                    MESSAGE = 'Flow 4 Retail business-code repair found multiple development-seed candidates; no rows were changed.';
            END IF;

            IF candidate_count = 1 AND EXISTS (
                SELECT 1
                FROM business_types
                WHERE id <> candidate_id
                  AND UPPER(BTRIM(business_code)) = 'RETAIL'
            ) THEN
                RAISE EXCEPTION USING
                    ERRCODE = '23505',
                    CONSTRAINT = 'ix_business_types_business_code',
                    MESSAGE = 'Flow 4 Retail business-code repair detected an existing RETAIL owner; no rows were changed.';
            END IF;

            IF candidate_count = 1 THEN
                UPDATE business_types
                SET business_code = 'RETAIL',
                    updated_at = now()
                WHERE id = candidate_id
                  AND NULLIF(BTRIM(COALESCE(business_code, '')), '') IS NULL;
            END IF;
        END
        $flow4_retail_repair$;
        """;
}
