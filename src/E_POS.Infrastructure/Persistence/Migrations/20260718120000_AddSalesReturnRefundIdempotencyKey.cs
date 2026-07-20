using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace E_POS.Infrastructure.Persistence.Migrations;

/// <summary>
/// Durable refund completion idempotency on sales_returns (parity with sales_exchanges).
/// </summary>
[DbContext(typeof(EPosDbContext))]
[Migration("20260718120000_AddSalesReturnRefundIdempotencyKey")]
public partial class AddSalesReturnRefundIdempotencyKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE sales_returns
                ADD COLUMN IF NOT EXISTS idempotency_key varchar(120) NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS ux_sales_returns_tenant_idempotency_key
                ON sales_returns (tenant_id, idempotency_key)
                WHERE idempotency_key IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS ux_sales_returns_tenant_idempotency_key;

            ALTER TABLE sales_returns
                DROP COLUMN IF EXISTS idempotency_key;
            """);
    }
}
