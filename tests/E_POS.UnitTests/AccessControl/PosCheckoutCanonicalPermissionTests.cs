using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using E_POS.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class PosCheckoutCanonicalPermissionTests
{
    public static IEnumerable<object[]> CanonicalCodes()
    {
        yield return [SalesPermissions.Sale.Checkout];
        yield return [PaymentPermissions.AcceptCash];
        yield return [PaymentPermissions.AcceptCard];
        yield return [PaymentPermissions.AcceptQr];
        yield return [PaymentPermissions.AcceptSplit];
        yield return [PosPermissions.Notifications.View];
        yield return [CustomerPermissions.View];
        yield return [CustomerPermissions.Create];
        yield return [CustomerPermissions.Update];
    }

    [Theory]
    [MemberData(nameof(CanonicalCodes))]
    public void AffectedPermissionCode_HasExactlyFourSegments(string permissionCode)
    {
        Assert.Equal(4, permissionCode.Split('.').Length);
    }

    [Fact]
    public void CorrectiveMigration_UpdatesRowsInPlaceToPreservePermissionAssignments()
    {
        var migration = new CanonicalizePosCheckoutPermissionCodes();
        var sql = string.Join('\n', migration.UpOperations
            .OfType<SqlOperation>()
            .Select(operation => operation.Sql));

        Assert.Contains("UPDATE permission_definitions", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO permission_definitions", sql, StringComparison.Ordinal);
        Assert.Contains("'sales.checkout' THEN 'pos.sales.checkout.execute'", sql, StringComparison.Ordinal);
        Assert.Contains("'payments.cash.accept' THEN 'pos.payments.cash.accept'", sql, StringComparison.Ordinal);
        Assert.Contains("'notifications.view' THEN 'pos.notifications.alerts.view'", sql, StringComparison.Ordinal);
        Assert.Contains("'customers.create' THEN 'pos.customers.management.create'", sql, StringComparison.Ordinal);
    }
}
