#r "nuget: Npgsql, 8.0.3"
using System;
using Npgsql;

var connString = "Host=localhost;Port=5434;Database=UnifiedCommerceDb;Username=postgres;Password=Nive@123";
using (var conn = new NpgsqlConnection(connString))
{
    conn.Open();
    var query = "SELECT id, product_code, product_name, status, created_at FROM products ORDER BY created_at DESC LIMIT 5;";
    using (var cmd = new NpgsqlCommand(query, conn))
    {
        using (var reader = cmd.ExecuteReader())
        {
            Console.WriteLine("Recent 5 products in DB:");
            while (reader.Read())
            {
                Console.WriteLine($"ID: {reader[0]}, Code: {reader[1]}, Name: {reader[2]}, Status: {reader[3]}, CreatedAt: {reader[4]}");
            }
        }
    }
}
