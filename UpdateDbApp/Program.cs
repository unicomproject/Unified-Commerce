using System;
using Npgsql;

var connString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=admin";
using (var conn = new NpgsqlConnection(connString))
{
    conn.Open();

    Console.WriteLine("Inserting default payment methods...");

    var insertQuery = @"
        INSERT INTO payment_methods (
            id, tenant_id, method_code, status, method_type, created_at, updated_at, 
            allows_change, is_active_for_online, is_active_for_pos, method_name, 
            requires_manual_confirmation, requires_reference, sort_order, supports_refund
        ) VALUES 
        (gen_random_uuid(), '55555555-0000-4000-8000-000000000001', 'CASH', 1, 1, now(), now(), 
         true, false, true, 'Cash', false, false, 1, true),
        (gen_random_uuid(), '55555555-0000-4000-8000-000000000001', 'CARD', 1, 2, now(), now(), 
         false, true, true, 'Credit / Debit Card', false, true, 2, true)
        ON CONFLICT DO NOTHING;
    ";

    try 
    {
        using (var cmd = new NpgsqlCommand(insertQuery, conn))
        {
            int rows = cmd.ExecuteNonQuery();
            Console.WriteLine($"Inserted {rows} payment methods.");
        }
    } 
    catch (Exception ex) 
    {
        Console.WriteLine("Error: " + ex.Message);
    }
}
