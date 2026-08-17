using System;
using Npgsql;

string connString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";

using var conn = new NpgsqlConnection(connString);
conn.Open();

var sql = "SELECT id, tenant_slug, display_name FROM tenants";
using var cmd = new NpgsqlCommand(sql, conn);
using var reader = cmd.ExecuteReader();
Console.WriteLine("Tenants in DB:");
while (reader.Read()) {
    Console.WriteLine($"{reader.GetGuid(0)} | {reader.GetString(1)} | {reader.GetString(2)}");
}






