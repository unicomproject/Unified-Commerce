#r "nuget: Npgsql, 8.0.3"
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Npgsql;

{
using var settings = JsonDocument.Parse(File.ReadAllText("src/E_POS.Api/appsettings.Development.json"));
var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? settings.RootElement.GetProperty("ConnectionStrings").EnumerateObject().First().Value.GetString();
await using var connection = new NpgsqlConnection(cs);
await connection.OpenAsync();
Console.WriteLine($"Database={connection.Database}; Host={connection.Host}");
await using var tx = await connection.BeginTransactionAsync();
await using var find = new NpgsqlCommand("SELECT id, tenant_id, account_status FROM tenant_users WHERE lower(email)=@email FOR UPDATE", connection, tx);
find.Parameters.AddWithValue("email", "cashier001@gmail.com");
Guid id; Guid tenant; string status;
await using (var reader = await find.ExecuteReaderAsync()) {
    if (!await reader.ReadAsync()) throw new Exception("Account not found; no changes made.");
    id=reader.GetGuid(0); tenant=reader.GetGuid(1); status=reader.GetString(2);
    if (await reader.ReadAsync()) throw new Exception("Multiple matching accounts; no changes made.");
}
Console.WriteLine($"Account={id}; Tenant={tenant}; Before={status}");
if (status != "LOCKED" && status != "ACTIVE") throw new Exception("Unexpected account status; no changes made.");
await using var update = new NpgsqlCommand("UPDATE tenant_users SET account_status='ACTIVE', locked_until=NULL, failed_login_attempts=0, updated_at=now() WHERE id=@id AND tenant_id=@tenant AND account_status='LOCKED'", connection, tx);
update.Parameters.AddWithValue("id", id); update.Parameters.AddWithValue("tenant", tenant);
Console.WriteLine($"UpdatedRows={await update.ExecuteNonQueryAsync()}");
await tx.CommitAsync();
await using var verify = new NpgsqlCommand("SELECT account_status FROM tenant_users WHERE id=@id AND tenant_id=@tenant", connection);
verify.Parameters.AddWithValue("id", id); verify.Parameters.AddWithValue("tenant", tenant);
Console.WriteLine($"VerifiedStatus={await verify.ExecuteScalarAsync()}");
}
