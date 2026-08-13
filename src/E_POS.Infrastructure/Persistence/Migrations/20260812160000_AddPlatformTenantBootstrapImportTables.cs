using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260812160000_AddPlatformTenantBootstrapImportTables")]
public partial class AddPlatformTenantBootstrapImportTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS platform_tenant_bootstrap_product_import_batches (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                status varchar(32) NOT NULL,
                template_version varchar(64) NOT NULL,
                source_file_name varchar(260) NOT NULL,
                total_rows integer NOT NULL,
                valid_rows integer NOT NULL,
                invalid_rows integer NOT NULL,
                committed_rows integer NOT NULL DEFAULT 0,
                skipped_rows integer NOT NULL DEFAULT 0,
                idempotency_key_hash varchar(64) NULL,
                actor_platform_user_id uuid NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_bootstrap_import_batches_tenant_status
                ON platform_tenant_bootstrap_product_import_batches (tenant_id, status);

            CREATE TABLE IF NOT EXISTS platform_tenant_bootstrap_product_import_rows (
                id uuid PRIMARY KEY,
                import_batch_id uuid NOT NULL
                    REFERENCES platform_tenant_bootstrap_product_import_batches(id) ON DELETE CASCADE,
                tenant_id uuid NOT NULL,
                row_number integer NOT NULL,
                raw_row_json text NOT NULL,
                is_valid boolean NOT NULL,
                error_code varchar(100) NULL,
                error_detail varchar(500) NULL,
                committed_product_id uuid NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS uq_bootstrap_import_rows_batch_row
                ON platform_tenant_bootstrap_product_import_rows (import_batch_id, row_number);

            CREATE TABLE IF NOT EXISTS platform_tenant_bootstrap_idempotency_records (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                operation_type varchar(80) NOT NULL,
                idempotency_key_hash varchar(64) NOT NULL,
                request_hash varchar(64) NULL,
                response_json text NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT uq_bootstrap_idempotency_tenant_operation_key
                    UNIQUE (tenant_id, operation_type, idempotency_key_hash)
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS platform_tenant_bootstrap_idempotency_records;
            DROP TABLE IF EXISTS platform_tenant_bootstrap_product_import_rows;
            DROP TABLE IF EXISTS platform_tenant_bootstrap_product_import_batches;
            """);
    }
}
