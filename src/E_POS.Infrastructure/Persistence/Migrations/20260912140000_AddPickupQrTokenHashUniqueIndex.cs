using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260912140000_AddPickupQrTokenHashUniqueIndex")]
public sealed class AddPickupQrTokenHashUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS ux_pickup_orders_qr_token_hash
            ON pickup_orders (tenant_id, pickup_qr_token_hash)
            WHERE pickup_qr_token_hash IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS ux_pickup_orders_qr_token_hash;
            """);
    }
}
