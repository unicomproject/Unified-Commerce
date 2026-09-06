using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPosDbContext))]
[Migration("20260903120000_CanonicalizePosCheckoutPermissionCodes")]
public partial class CanonicalizePosCheckoutPermissionCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE permission_definitions
            SET permission_code = CASE permission_code
                    WHEN 'sales.checkout' THEN 'pos.sales.checkout.execute'
                    WHEN 'payments.cash.accept' THEN 'pos.payments.cash.accept'
                    WHEN 'payments.card.accept' THEN 'pos.payments.card.accept'
                    WHEN 'payments.qr.accept' THEN 'pos.payments.qr.accept'
                    WHEN 'payments.split.accept' THEN 'pos.payments.split.accept'
                    WHEN 'notifications.view' THEN 'pos.notifications.alerts.view'
                    WHEN 'customers.view' THEN 'pos.customers.management.view'
                    WHEN 'customers.create' THEN 'pos.customers.management.create'
                    WHEN 'customers.update' THEN 'pos.customers.management.update'
                END,
                updated_at = now()
            WHERE permission_code IN (
                'sales.checkout',
                'payments.cash.accept',
                'payments.card.accept',
                'payments.qr.accept',
                'payments.split.accept',
                'notifications.view',
                'customers.view',
                'customers.create',
                'customers.update');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE permission_definitions
            SET permission_code = CASE permission_code
                    WHEN 'pos.sales.checkout.execute' THEN 'sales.checkout'
                    WHEN 'pos.payments.cash.accept' THEN 'payments.cash.accept'
                    WHEN 'pos.payments.card.accept' THEN 'payments.card.accept'
                    WHEN 'pos.payments.qr.accept' THEN 'payments.qr.accept'
                    WHEN 'pos.payments.split.accept' THEN 'payments.split.accept'
                    WHEN 'pos.notifications.alerts.view' THEN 'notifications.view'
                    WHEN 'pos.customers.management.view' THEN 'customers.view'
                    WHEN 'pos.customers.management.create' THEN 'customers.create'
                    WHEN 'pos.customers.management.update' THEN 'customers.update'
                END,
                updated_at = now()
            WHERE permission_code IN (
                'pos.sales.checkout.execute',
                'pos.payments.cash.accept',
                'pos.payments.card.accept',
                'pos.payments.qr.accept',
                'pos.payments.split.accept',
                'pos.notifications.alerts.view',
                'pos.customers.management.view',
                'pos.customers.management.create',
                'pos.customers.management.update');
            """);
    }
}
