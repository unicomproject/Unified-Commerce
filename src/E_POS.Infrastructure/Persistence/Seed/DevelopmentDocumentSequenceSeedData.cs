namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentDocumentSequenceSeedData
{
    public const string UpSql = """
        INSERT INTO document_number_sequences (
            id, tenant_id, document_type, current_value, padding_length, 
            prefix, reset_rule, row_version, status, created_at, updated_at
        )
        VALUES (
            '44444444-0000-4000-8000-000000000001',
            '55555555-0000-4000-8000-000000000001',
            'SALES_ORDER',
            0,
            6,
            'ORD-',
            'NONE',
            1,
            'ACTIVE',
            now(),
            now()
        )
        ON CONFLICT (id) DO UPDATE
        SET document_type = EXCLUDED.document_type,
            prefix = EXCLUDED.prefix,
            padding_length = EXCLUDED.padding_length,
            status = 'ACTIVE',
            updated_at = now();
        """;
}
