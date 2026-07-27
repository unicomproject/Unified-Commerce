namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPosBrandingSeedData
{
    public const string UpSql = """
        UPDATE tenants
        SET display_name = 'OneVerz POS',
            updated_at = now()
        WHERE id = '55555555-0000-4000-8000-000000000001';
        """;

    public const string DownSql = """
        UPDATE tenants
        SET display_name = 'TM-EPOS Development Tenant',
            updated_at = now()
        WHERE id = '55555555-0000-4000-8000-000000000001'
          AND display_name = 'OneVerz POS';
        """;
}
