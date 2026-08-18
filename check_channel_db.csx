#r "nuget: Npgsql, 8.0.3"
using System;
using Npgsql;

var connString = "Host=localhost;Port=5434;Database=UnifiedCommerceDb;Username=postgres;Password=Nive@123";
using (var conn = new NpgsqlConnection(connString))
{
    conn.Open();
    var query = @"
        SELECT p.product_code, cv.sales_channel_id, cv.is_visible, cv.created_at
        FROM product_channel_visibility cv
        JOIN products p ON cv.product_id = p.id
        ORDER BY cv.created_at DESC LIMIT 10;";
    using (var cmd = new NpgsqlCommand(query, conn))
    {
        using (var reader = cmd.ExecuteReader())
        {
            Console.WriteLine("Recent product_channel_visibility entries:");
            while (reader.Read())
            {
                Console.WriteLine($"Code: {reader[0]}, Channel: {reader[1]}, IsVisible: {reader[2]}, CreatedAt: {reader[3]}");
            }
        }
    }
}
