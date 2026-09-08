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

const string receiptNumber = "RCP-000178";
Guid tenantId;
Guid saleId;
await using (var command = new NpgsqlCommand("SELECT tenant_id, to_jsonb(r)::text FROM receipts r WHERE receipt_number = @receipt", connection))
{
    command.Parameters.AddWithValue("receipt", receiptNumber);
    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) throw new Exception($"Missing receipt {receiptNumber}");
    tenantId = reader.GetGuid(0);
    var receiptRow = reader.GetString(1);
    Console.WriteLine($"receipt={receiptNumber};tenant={tenantId}");
    Console.WriteLine($"receipts.rows={receiptRow}");
    using var rowJson = JsonDocument.Parse(receiptRow);
    var saleProperty = rowJson.RootElement.EnumerateObject().First(x => x.Name is "sales_order_id" or "order_id");
    saleId = saleProperty.Value.GetGuid();
    Console.WriteLine($"sale={saleId}");
}

var tables = new[] { "sales_orders", "sales_order_lines", "sales_payments", "sales_payment_transactions", "sales_payment_events", "receipts", "stock_movements", "cash_drawer_operations", "receipt_print_logs" };
foreach (var table in tables)
{
    await using var existsCommand = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name=@table)", connection);
    existsCommand.Parameters.AddWithValue("table", table);
    if (!(bool)(await existsCommand.ExecuteScalarAsync())!) { Console.WriteLine($"{table}=TABLE_NOT_PRESENT"); continue; }
    var sql = $"SELECT count(*), COALESCE(jsonb_agg(to_jsonb(t)), '[]'::jsonb)::text FROM {table} t WHERE to_jsonb(t)->>'tenant_id' = @tenant AND to_jsonb(t)::text LIKE @sale";
    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("tenant", tenantId.ToString());
    command.Parameters.AddWithValue("sale", $"%{saleId}%");
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    Console.WriteLine($"{table}.count={reader.GetInt64(0)}");
    Console.WriteLine($"{table}.rows={reader.GetString(1)}");
    if (reader.GetInt64(0) == 0)
    {
        await reader.DisposeAsync();
        var latestSql = $"SELECT COALESCE(jsonb_agg(to_jsonb(x)), '[]'::jsonb)::text FROM (SELECT * FROM {table} WHERE tenant_id = @tenant ORDER BY created_at DESC LIMIT 3) x";
        await using var latest = new NpgsqlCommand(latestSql, connection);
        latest.Parameters.AddWithValue("tenant", tenantId);
        Console.WriteLine($"{table}.latest={await latest.ExecuteScalarAsync()}");
    }
}
