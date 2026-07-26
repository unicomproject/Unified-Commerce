namespace E_POS.Infrastructure.Persistence.Seed;

public static class OneverceOutletSeedData
{
    public static readonly Guid MainOutletId = Guid.Parse("d4e295a3-2556-4081-87ce-7ebe4db9e276");
    public static readonly Guid OutletTwoId = Guid.Parse("316892a5-e193-4edf-97cd-b6d1d1c6b86c");
    public static readonly Guid OutletThreeId = Guid.Parse("63ec79d4-6d56-48e8-ad95-8bd0276cfbd7");
    public static readonly Guid OutletFourId = Guid.Parse("66aff51a-2b04-48b7-a860-60553b46ba53");
    public static readonly Guid OutletFiveId = Guid.Parse("933e657f-b0a6-425c-a6da-31e0bc5acaac");

    public const string UpSql = """
        INSERT INTO outlets (
            id, tenant_id, outlet_name, outlet_code, status, outlet_type, timezone,
            is_default_outlet, created_at, updated_at
        )
        VALUES
            (
                'd4e295a3-2556-4081-87ce-7ebe4db9e276',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                'Oneverce Main Outlet',
                'ONEV-001',
                'ACTIVE',
                'STORE',
                'Asia/Colombo',
                true,
                now(),
                now()
            ),
            (
                '316892a5-e193-4edf-97cd-b6d1d1c6b86c',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                'Oneverce Outlet 02',
                'ONEV-002',
                'ACTIVE',
                'STORE',
                'Asia/Colombo',
                false,
                now(),
                now()
            ),
            (
                '63ec79d4-6d56-48e8-ad95-8bd0276cfbd7',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                'Oneverce Outlet 03',
                'ONEV-003',
                'ACTIVE',
                'STORE',
                'Asia/Colombo',
                false,
                now(),
                now()
            ),
            (
                '66aff51a-2b04-48b7-a860-60553b46ba53',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                'Oneverce Outlet 04',
                'ONEV-004',
                'ACTIVE',
                'STORE',
                'Asia/Colombo',
                false,
                now(),
                now()
            ),
            (
                '933e657f-b0a6-425c-a6da-31e0bc5acaac',
                '07fdfd9f-33a2-46e5-9af0-99acf219fd57',
                'Oneverce Outlet 05',
                'ONEV-005',
                'ACTIVE',
                'STORE',
                'Asia/Colombo',
                false,
                now(),
                now()
            )
        ON CONFLICT (id) DO UPDATE
        SET outlet_name = EXCLUDED.outlet_name,
            outlet_code = EXCLUDED.outlet_code,
            outlet_type = EXCLUDED.outlet_type,
            timezone = EXCLUDED.timezone,
            is_default_outlet = EXCLUDED.is_default_outlet,
            status = 'ACTIVE',
            updated_at = now();
        """;

    public const string DownSql = """
        DELETE FROM outlets
        WHERE id IN (
            'd4e295a3-2556-4081-87ce-7ebe4db9e276',
            '316892a5-e193-4edf-97cd-b6d1d1c6b86c',
            '63ec79d4-6d56-48e8-ad95-8bd0276cfbd7',
            '66aff51a-2b04-48b7-a860-60553b46ba53',
            '933e657f-b0a6-425c-a6da-31e0bc5acaac'
        );
        """;
}
