using System;
using Npgsql;

var connString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=admin";
using (var conn = new NpgsqlConnection(connString))
{
    conn.Open();
    var dbs = new System.Collections.Generic.List<string>();
    using (var cmd = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datistemplate = false;", conn))
    using (var reader = cmd.ExecuteReader())
    {
        while (reader.Read())
        {
            dbs.Add(reader.GetString(0));
        }
    }

    foreach (var db in dbs)
    {
        try {
            using var dbConn = new NpgsqlConnection($"Host=localhost;Port=5432;Database={db};Username=postgres;Password=admin");
            dbConn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM payment_methods;", dbConn);
            var count = cmd.ExecuteScalar();
            Console.WriteLine($"DB: {db}, Count: {count}");
        } catch {
            // Ignore errors (table might not exist)
        }
    }
}
