#r "nuget: Npgsql, 8.0.3"
using System;
using Npgsql;

var connString = "Host=localhost;Port=5434;Database=UnifiedCommerceDb;Username=postgres;Password=Nive@123";
using (var conn = new NpgsqlConnection(connString))
{
    conn.Open();
    var query = "UPDATE products SET status = 'ACTIVE' WHERE product_code = 'MER-030' AND status = 'DRAFT';";
    using (var cmd = new NpgsqlCommand(query, conn))
    {
        int count = cmd.ExecuteNonQuery();
        Console.WriteLine($"Updated {count} products.");
    }
}
