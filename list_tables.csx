#r "nuget: Npgsql, 8.0.3"
using System;
using Npgsql;

var connString = "Host=localhost;Port=5434;Database=UnifiedCommerceDb;Username=postgres;Password=Nive@123";
using (var conn = new NpgsqlConnection(connString))
{
    conn.Open();
    var query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';";
    using (var cmd = new NpgsqlCommand(query, conn))
    using (var reader = cmd.ExecuteReader())
    {
        while (reader.Read())
        {
            Console.WriteLine(reader.GetString(0));
        }
    }
}
