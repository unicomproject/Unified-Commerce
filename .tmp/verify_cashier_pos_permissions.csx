#r "nuget: Npgsql, 8.0.3"
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Npgsql;

var settings = JsonDocument.Parse(File.ReadAllText("src/E_POS.Api/appsettings.Development.json"));
var connectionString = settings.RootElement.GetProperty("ConnectionStrings").EnumerateObject().First().Value.GetString();
var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

const string sql = """
WITH target_user AS (
    SELECT id, tenant_id, email
    FROM tenant_users
    WHERE lower(email) = 'cashier001@gmail.com'
), effective AS (
    SELECT tup.user_id, tup.tenant_id, pd.permission_code
    FROM tenant_user_permissions tup
    JOIN permission_definitions pd ON pd.id = tup.permission_id
    WHERE tup.revoked_at IS NULL
    UNION
    SELECT tur.user_id, tur.tenant_id, pd.permission_code
    FROM tenant_user_roles tur
    JOIN tenant_role_permissions trp
      ON trp.tenant_id = tur.tenant_id AND trp.role_id = tur.role_id
    JOIN permission_definitions pd ON pd.id = trp.permission_id
    WHERE trp.revoked_at IS NULL
), canonical_pos AS (
    SELECT permission_code
    FROM permission_definitions
    WHERE description LIKE 'Cashier POS canonical permission%'
), missing AS (
    SELECT cp.permission_code
    FROM canonical_pos cp
    CROSS JOIN target_user tu
    LEFT JOIN effective e
      ON e.user_id = tu.id AND e.tenant_id = tu.tenant_id
     AND e.permission_code = cp.permission_code
    WHERE e.permission_code IS NULL
)
SELECT
    (SELECT count(*) FROM target_user) AS matched_users,
    (SELECT count(*) FROM canonical_pos) AS canonical_pos_count,
    (SELECT count(*) FROM effective e JOIN target_user tu ON tu.id=e.user_id AND tu.tenant_id=e.tenant_id WHERE e.permission_code LIKE 'pos.%' OR e.permission_code='commerce.online_order.orders.access') AS effective_pos_count,
    (SELECT count(*) FROM missing) AS missing_count,
    COALESCE((SELECT string_agg(permission_code, ', ' ORDER BY permission_code) FROM missing), '') AS missing_codes,
    EXISTS (SELECT 1 FROM effective e JOIN target_user tu ON tu.id=e.user_id AND tu.tenant_id=e.tenant_id WHERE e.permission_code='pos.till.session.open') AS till_open,
    EXISTS (SELECT 1 FROM effective e JOIN target_user tu ON tu.id=e.user_id AND tu.tenant_id=e.tenant_id WHERE e.permission_code='pos.till.opening.starting_cash_entry') AS starting_cash_entry,
    EXISTS (SELECT 1 FROM effective e JOIN target_user tu ON tu.id=e.user_id AND tu.tenant_id=e.tenant_id WHERE e.permission_code='pos.till.opening.numpad') AS opening_numpad,
    EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId"='20260904140000_SeedCashierPosChunk3CanonicalPermissions') AS migration_applied;
""";

var command = new NpgsqlCommand(sql, connection);
var reader = await command.ExecuteReaderAsync();
await reader.ReadAsync();
Console.WriteLine($"matched_users={reader.GetInt64(0)}");
Console.WriteLine($"canonical_pos_count={reader.GetInt64(1)}");
Console.WriteLine($"effective_pos_count={reader.GetInt64(2)}");
Console.WriteLine($"missing_count={reader.GetInt64(3)}");
Console.WriteLine($"missing_codes={reader.GetString(4)}");
Console.WriteLine($"pos.till.session.open={reader.GetBoolean(5)}");
Console.WriteLine($"pos.till.opening.starting_cash_entry={reader.GetBoolean(6)}");
Console.WriteLine($"pos.till.opening.numpad={reader.GetBoolean(7)}");
Console.WriteLine($"migration_applied={reader.GetBoolean(8)}");
