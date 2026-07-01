using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260701053000_SeedPlatformAdmin")]
    public partial class SeedPlatformAdmin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO platform_users (id, email, normalized_email, password_hash, status, created_at, updated_at)
                VALUES (
                    '11111111-1111-4111-8111-111111111111',
                    'posunique001@gmail.com',
                    'POSUNIQUE001@GMAIL.COM',
                    'PBKDF2-SHA256:100000:zG7O+AY1EJBG5+sCXDBinA==:weI+nABmBRNW19gQODOHn5D2q8SUQ0rVJy0NITO/Qyo=',
                    'ACTIVE',
                    now(),
                    now()
                )
                ON CONFLICT (normalized_email) DO UPDATE
                SET email = EXCLUDED.email,
                    password_hash = EXCLUDED.password_hash,
                    status = 'ACTIVE',
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO platform_permissions (id, permission_code, name, description, status, created_at, updated_at)
                VALUES (
                    '22222222-2222-4222-8222-222222222222',
                    'platform.admin.access',
                    'Platform Admin Access',
                    'Allows the seeded development platform admin to login.',
                    'ACTIVE',
                    now(),
                    now()
                )
                ON CONFLICT (permission_code) DO UPDATE
                SET name = EXCLUDED.name,
                    description = EXCLUDED.description,
                    status = 'ACTIVE',
                    updated_at = now();
                """);

            migrationBuilder.Sql("""
                INSERT INTO platform_user_permissions (id, platform_user_id, platform_permission_id, description, created_at, updated_at)
                SELECT
                    '33333333-3333-4333-8333-333333333333',
                    platform_users.id,
                    platform_permissions.id,
                    'Migration seed permission for development platform admin login.',
                    now(),
                    now()
                FROM platform_users
                CROSS JOIN platform_permissions
                WHERE platform_users.normalized_email = 'POSUNIQUE001@GMAIL.COM'
                  AND platform_permissions.permission_code = 'platform.admin.access'
                ON CONFLICT (platform_user_id, platform_permission_id) DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM platform_user_permissions
                WHERE id = '33333333-3333-4333-8333-333333333333'
                   OR (
                        platform_user_id = '11111111-1111-4111-8111-111111111111'
                    AND platform_permission_id = '22222222-2222-4222-8222-222222222222'
                   );
                """);

            migrationBuilder.Sql("""
                DELETE FROM platform_permissions
                WHERE id = '22222222-2222-4222-8222-222222222222'
                  AND permission_code = 'platform.admin.access';
                """);

            migrationBuilder.Sql("""
                DELETE FROM platform_users
                WHERE id = '11111111-1111-4111-8111-111111111111'
                  AND normalized_email = 'POSUNIQUE001@GMAIL.COM';
                """);
        }
    }
}
