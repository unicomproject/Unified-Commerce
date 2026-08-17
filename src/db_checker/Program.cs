using System;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main()
    {
        var connString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        Console.WriteLine("--- Tills in Database ---");
        using (var cmd = new NpgsqlCommand("SELECT id, till_code, status, outlet_id FROM tills", conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"ID: {reader["id"]}, Code: {reader["till_code"]}, Status: {reader["status"]}, Outlet: {reader["outlet_id"]}");
            }
        }
    }
}
