using System;
using System.Threading.Tasks;
using Npgsql;

namespace SqlRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var pgConnString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

            try
            {
                using var conn = new NpgsqlConnection(pgConnString);
                await conn.OpenAsync();
                
                var sql = @"
                    INSERT INTO platform_user_roles (id, platform_user_id, platform_role_id, created_at, updated_at)
                    SELECT gen_random_uuid(), platform_users.id, platform_roles.id, now(), now()
                    FROM platform_users
                    CROSS JOIN platform_roles
                    WHERE platform_users.normalized_email = 'ADMIN@NYTROZ.LOCAL'
                      AND platform_roles.role_code = 'super_administrator'
                    ON CONFLICT DO NOTHING;
                ";
                using var cmd = new NpgsqlCommand(sql, conn);
                int rows = await cmd.ExecuteNonQueryAsync();
                
                Console.WriteLine("Successfully assigned super_administrator role.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Error: " + ex.Message);
            }
        }
    }
}
