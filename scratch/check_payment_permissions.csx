#r "nuget: Npgsql, 8.0.3"
using System;
using Npgsql;

var connString = "Host=localhost;Port=5432;Database=UnifiedCommerceDb;Username=postgres;Password=Admin@123";
using var conn = new NpgsqlConnection(connString);
conn.Open();

void Run(string title, string sql)
{
    Console.WriteLine($"\n=== {title} ===");
    using var cmd = new NpgsqlCommand(sql, conn);
    using var reader = cmd.ExecuteReader();
    var cols = reader.FieldCount;
    while (reader.Read())
    {
        var vals = new string[cols];
        for (int i = 0; i < cols; i++) vals[i] = reader[i]?.ToString() ?? "NULL";
        Console.WriteLine(string.Join(" | ", vals));
    }
}

Run("Tenants", "SELECT id, tenant_code, name, status FROM tenants ORDER BY created_at DESC LIMIT 10;");

Run("payment_methods row count per tenant", @"
    SELECT t.id, t.tenant_code, t.name, count(pm.id) AS method_count
    FROM tenants t
    LEFT JOIN payment_methods pm ON pm.tenant_id = t.id
    GROUP BY t.id, t.tenant_code, t.name
    ORDER BY t.created_at DESC;");

Run("payment_methods rows (any tenant)", @"
    SELECT tenant_id, method_code, method_name, is_active_for_pos, status FROM payment_methods
    ORDER BY tenant_id, sort_order;");

Run("permission_definitions for payment accept codes", @"
    SELECT id, permission_code, is_active FROM permission_definitions
    WHERE permission_code IN ('pos.payments.cash.accept','pos.payments.card.accept','pos.payments.qr.accept','pos.payments.split.accept');");

Run("tenant_roles", @"
    SELECT id, tenant_id, role_code, role_name, is_active FROM tenant_roles ORDER BY tenant_id, role_code;");

Run("tenant_role_permissions for payment accept codes", @"
    SELECT trp.tenant_id, r.role_code, pd.permission_code, trp.granted_at
    FROM tenant_role_permissions trp
    JOIN permission_definitions pd ON pd.id = trp.permission_id
    JOIN tenant_roles r ON r.id = trp.role_id
    WHERE pd.permission_code IN ('pos.payments.cash.accept','pos.payments.card.accept','pos.payments.qr.accept','pos.payments.split.accept')
    ORDER BY trp.tenant_id, r.role_code, pd.permission_code;");

Run("tenant_users and their role assignment", @"
    SELECT tu.id, tu.tenant_id, tu.email, tu.status, r.role_code
    FROM tenant_users tu
    LEFT JOIN tenant_user_roles tur ON tur.tenant_user_id = tu.id
    LEFT JOIN tenant_roles r ON r.id = tur.role_id
    ORDER BY tu.created_at DESC
    LIMIT 20;");
