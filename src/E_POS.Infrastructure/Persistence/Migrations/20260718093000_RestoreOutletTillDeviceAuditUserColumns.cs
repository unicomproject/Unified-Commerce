using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(EPosDbContext))]
[Migration("20260718093000_RestoreOutletTillDeviceAuditUserColumns")]
public partial class RestoreOutletTillDeviceAuditUserColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddAuditColumns(migrationBuilder, "pos_devices");
        AddAuditColumns(migrationBuilder, "outlet_addresses");
        AddAuditColumns(migrationBuilder, "hardware_profiles");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropAuditColumns(migrationBuilder, "hardware_profiles");
        DropAuditColumns(migrationBuilder, "outlet_addresses");
        DropAuditColumns(migrationBuilder, "pos_devices");
    }

    private static void AddAuditColumns(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql($"""
            ALTER TABLE {table}
                ADD COLUMN IF NOT EXISTS created_by_tenant_user_id uuid NULL,
                ADD COLUMN IF NOT EXISTS updated_by_tenant_user_id uuid NULL;

            CREATE INDEX IF NOT EXISTS "IX_{table}_created_by_tenant_user_id"
                ON {table} (created_by_tenant_user_id);

            CREATE INDEX IF NOT EXISTS "IX_{table}_updated_by_tenant_user_id"
                ON {table} (updated_by_tenant_user_id);

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'fk_{table}_created_by_tenant_user_id_tenant_users'
                ) THEN
                    ALTER TABLE {table}
                        ADD CONSTRAINT "fk_{table}_created_by_tenant_user_id_tenant_users"
                        FOREIGN KEY (created_by_tenant_user_id)
                        REFERENCES tenant_users (id)
                        ON DELETE RESTRICT;
                END IF;

                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'fk_{table}_updated_by_tenant_user_id_tenant_users'
                ) THEN
                    ALTER TABLE {table}
                        ADD CONSTRAINT "fk_{table}_updated_by_tenant_user_id_tenant_users"
                        FOREIGN KEY (updated_by_tenant_user_id)
                        REFERENCES tenant_users (id)
                        ON DELETE RESTRICT;
                END IF;
            END $$;
            """);
    }

    private static void DropAuditColumns(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql($"""
            ALTER TABLE {table}
                DROP CONSTRAINT IF EXISTS "fk_{table}_created_by_tenant_user_id_tenant_users",
                DROP CONSTRAINT IF EXISTS "fk_{table}_updated_by_tenant_user_id_tenant_users";

            DROP INDEX IF EXISTS "IX_{table}_created_by_tenant_user_id";
            DROP INDEX IF EXISTS "IX_{table}_updated_by_tenant_user_id";

            ALTER TABLE {table}
                DROP COLUMN IF EXISTS created_by_tenant_user_id,
                DROP COLUMN IF EXISTS updated_by_tenant_user_id;
            """);
    }
}
