using System;
using Npgsql;

class Program
{
    static void Main()
    {
        string connStr = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";
        using var conn = new NpgsqlConnection(connStr);
        conn.Open();

        using var cmd1 = new NpgsqlCommand("SELECT \"EntitlementStatus\" FROM \"TenantFeatureEntitlements\" LIMIT 5;", conn);
        using var reader = cmd1.ExecuteReader();
        Console.WriteLine("--- TenantFeatureEntitlements ---");
        while (reader.Read()) Console.WriteLine($"Status: {reader.GetString(0)}");
        reader.Close();

        using var cmd2 = new NpgsqlCommand("SELECT COUNT(*) FROM \"TenantUserRoles\";", conn);
        Console.WriteLine($"TenantUserRoles count: {cmd2.ExecuteScalar()}");
        
        using var cmd3 = new NpgsqlCommand("SELECT COUNT(*) FROM \"TenantRolePermissions\";", conn);
        Console.WriteLine($"TenantRolePermissions count: {cmd3.ExecuteScalar()}");

        using var cmd4 = new NpgsqlCommand("SELECT \"Status\" FROM \"PermissionDefinitions\" LIMIT 5;", conn);
        using var reader2 = cmd4.ExecuteReader();
        Console.WriteLine("--- PermissionDefinitions ---");
        while (reader2.Read()) Console.WriteLine($"Status: {reader2.GetString(0)}");
        reader2.Close();
    }
}
