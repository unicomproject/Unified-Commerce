using System.Text.Json;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.SensitiveData;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.HardwareCash.Constants;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class CashierPosChunk7SensitiveFilteringTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static TenantRequestContext Ctx(params string[] permissions) =>
        new(Guid.NewGuid(), Guid.NewGuid(), permissions);

    private static PosCustomerListItemResponseDto SampleCustomer() =>
        new(
            Guid.NewGuid(),
            "Ada Lovelace",
            "+15551212",
            "ada@example.com",
            "ACTIVE",
            "CUS000001",
            "POS",
            DateTimeOffset.UtcNow,
            3,
            150.50m,
            "USD",
            DateTimeOffset.UtcNow,
            false);

    [Fact]
    public void Catalog_SensitiveRoleAssignableCount_IsSixty()
    {
        var count = CashierPosCanonicalPermissionCatalog.All
            .Count(x => x.IsRoleAssignable && x.IsSensitive);
        Assert.Equal(60, count);
    }

    [Fact]
    public void Customer_ViewPlusName_HidesPhoneEmailSpend()
    {
        var filtered = PosSensitiveResponseFilter.FilterCustomer(
            Ctx(CustomerPermissions.View),
            SampleCustomer());

        Assert.Equal("Ada Lovelace", filtered.FullName);
        Assert.Null(filtered.Phone);
        Assert.Null(filtered.Email);
        Assert.Null(filtered.TotalSpentAmount);
    }

    [Fact]
    public void Customer_PhoneGranted_EmailDenied()
    {
        var filtered = PosSensitiveResponseFilter.FilterCustomer(
            Ctx(CustomerPermissions.View, CashierPosSensitiveFieldCodes.CustomerPhone),
            SampleCustomer());

        Assert.Equal("+15551212", filtered.Phone);
        Assert.Null(filtered.Email);
    }

    [Fact]
    public void CustomerList_PhoneDenied_NoRowLeaksPhone()
    {
        var list = new PosCustomerListResponseDto(
            [SampleCustomer(), SampleCustomer() with { FullName = "Grace Hopper", Phone = "+1999" }],
            1,
            25,
            2,
            1);

        var filtered = PosSensitiveResponseFilter.FilterCustomerList(
            Ctx(CustomerPermissions.View),
            list);

        Assert.All(filtered.Items, item => Assert.Null(item.Phone));
    }

    [Fact]
    public void CustomerOrders_HistoryDenied_ReturnsEmpty()
    {
        var orders = new PosCustomerOrdersResponseDto(
            [
                new PosCustomerOrderItemDto(
                    Guid.NewGuid(),
                    "SO-1",
                    DateTimeOffset.UtcNow,
                    42m,
                    "USD",
                    "COMPLETED",
                    "Main")
            ],
            1,
            25,
            1,
            1);

        var filtered = PosSensitiveResponseFilter.FilterCustomerOrders(
            Ctx(CustomerPermissions.View),
            orders);

        Assert.Empty(filtered.Items);
        Assert.Equal(0, filtered.TotalCount);
    }

    [Fact]
    public void CustomerOrders_HistoryGranted_AmountsDenied_NullsAmounts()
    {
        var orders = new PosCustomerOrdersResponseDto(
            [
                new PosCustomerOrderItemDto(
                    Guid.NewGuid(),
                    "SO-1",
                    DateTimeOffset.UtcNow,
                    42m,
                    "USD",
                    "COMPLETED",
                    "Main")
            ],
            1,
            25,
            1,
            1);

        var filtered = PosSensitiveResponseFilter.FilterCustomerOrders(
            Ctx(
                CustomerPermissions.View,
                CashierPosSensitiveFieldCodes.CustomerPurchaseHistory),
            orders);

        Assert.Single(filtered.Items);
        Assert.Null(filtered.Items[0].TotalAmount);
    }

    [Fact]
    public void Drawer_ExpectedCashDenied_IsNull()
    {
        var summary = new PosCashDrawerSummaryDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Till 1",
            "open",
            "USD",
            100m,
            50m,
            0m,
            10m,
            5m,
            20m,
            135m,
            "Cashier",
            DateTimeOffset.UtcNow);

        var filtered = PosSensitiveResponseFilter.FilterDrawerSummary(
            Ctx(CashDrawerPermissions.Canonical.PositionView),
            summary);

        Assert.Null(filtered.OpeningCash);
        Assert.Null(filtered.CashSales);
        Assert.Null(filtered.CurrentExpectedCash);
        Assert.Equal("Till 1", filtered.TillName);
    }

    [Fact]
    public void Drawer_OpeningDenied_CashSalesGranted()
    {
        var summary = new PosCashDrawerSummaryDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Till 1",
            "open",
            "USD",
            100m,
            50m,
            0m,
            10m,
            5m,
            20m,
            135m,
            "Cashier",
            DateTimeOffset.UtcNow);

        var filtered = PosSensitiveResponseFilter.FilterDrawerSummary(
            Ctx(
                CashDrawerPermissions.Canonical.PositionView,
                CashierPosSensitiveFieldCodes.DrawerCashSales),
            summary);

        Assert.Null(filtered.OpeningCash);
        Assert.Equal(50m, filtered.CashSales);
        Assert.Null(filtered.CurrentExpectedCash);
    }

    [Fact]
    public void DrawerMovements_AmountDenied_NoRowLeaksAmount()
    {
        var page = new PosCashDrawerMovementPageDto(
            [
                new PosCashDrawerMovementDto(
                    Guid.NewGuid(),
                    "CASH_IN",
                    "IN",
                    25m,
                    "USD",
                    null,
                    null,
                    "Cashier",
                    DateTimeOffset.UtcNow,
                    CurrentExpectedCash: 125m),
                new PosCashDrawerMovementDto(
                    Guid.NewGuid(),
                    "CASH_DROP",
                    "OUT",
                    10m,
                    "USD",
                    null,
                    null,
                    "Cashier",
                    DateTimeOffset.UtcNow,
                    CurrentExpectedCash: 115m)
            ],
            1,
            25,
            2,
            1);

        var filtered = PosSensitiveResponseFilter.FilterDrawerMovements(
            Ctx(CashDrawerPermissions.Canonical.PositionView),
            page);

        Assert.All(filtered.Items, item => Assert.Null(item.Amount));
    }

    [Fact]
    public void CloseTill_ExpectedCashDenied_DifferenceDenied()
    {
        var closed = new ClosedTillSessionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            150m,
            148m,
            -2m,
            "closed",
            DateTimeOffset.UtcNow.AddHours(-8),
            DateTimeOffset.UtcNow,
            null);

        var filtered = PosSensitiveResponseFilter.FilterClosedTillSession(
            Ctx(PosPermissions.Till.SessionClose),
            closed);

        Assert.Null(filtered.ExpectedCash);
        Assert.Null(filtered.CashDifference);
        Assert.Null(filtered.CountedCash);
    }

    [Fact]
    public void CloseTill_ExpectedCashDenied_InPrimaryAndSummaryAliases()
    {
        var closed = new ClosedTillSessionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            150m,
            148m,
            -2m,
            "closed",
            DateTimeOffset.UtcNow.AddHours(-8),
            DateTimeOffset.UtcNow,
            null);

        var withPrimaryOnly = PosSensitiveResponseFilter.FilterClosedTillSession(
            Ctx(
                PosPermissions.Till.SessionClose,
                CashierPosSensitiveFieldCodes.TillClosingExpectedCash),
            closed);
        Assert.Equal(150m, withPrimaryOnly.ExpectedCash);

        var withSummaryOnly = PosSensitiveResponseFilter.FilterClosedTillSession(
            Ctx(
                PosPermissions.Till.SessionClose,
                CashierPosSensitiveFieldCodes.TillClosingExpectedCashSummary),
            closed);
        Assert.Equal(150m, withSummaryOnly.ExpectedCash);

        var denied = PosSensitiveResponseFilter.FilterClosedTillSession(
            Ctx(PosPermissions.Till.SessionClose),
            closed);
        Assert.Null(denied.ExpectedCash);
    }

    [Fact]
    public void Payment_ChangeDueDenied_StillReturnsOtherFields()
    {
        var payment = new PosCheckoutStartPaymentResponseDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "S-1",
            "R-1",
            "R-1",
            100,
            5,
            10,
            105,
            120,
            15,
            "CASH",
            "USD",
            "COMPLETED",
            "NONE",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Array.Empty<PosCheckoutStartPaymentLineResponseDto>());

        var filtered = PosSensitiveResponseFilter.FilterStartPayment(
            Ctx(
                SalesPermissions.Sale.Checkout,
                CashierPosSensitiveFieldCodes.CashPaymentDiscount,
                CashierPosSensitiveFieldCodes.CashPaymentTotalDue,
                CashierPosSensitiveFieldCodes.CashPaymentAmountReceivedView),
            payment);

        Assert.Equal(5, filtered.DiscountTotal);
        Assert.Equal(105, filtered.GrandTotal);
        Assert.Equal(120, filtered.CashReceived);
        Assert.Null(filtered.ChangeDue);
    }

    [Fact]
    public void Receipt_SensitiveFields_NullIndependently()
    {
        var detail = new PosReceiptDetailDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "R-1",
            "S-1",
            "SALE",
            "ISSUED",
            DateTimeOffset.UtcNow,
            "Cashier",
            Guid.NewGuid(),
            "Till 1",
            Guid.NewGuid(),
            "Outlet",
            Guid.NewGuid(),
            "CASH",
            "USD",
            100m,
            5m,
            10m,
            0m,
            0m,
            105m,
            120m,
            15m,
            Array.Empty<PosReceiptDetailLineDto>(),
            0,
            null);

        var filtered = PosSensitiveResponseFilter.FilterReceiptDetail(
            Ctx(
                ReceiptPermissions.View,
                CashierPosSensitiveFieldCodes.ReceiptTotal),
            detail);

        Assert.Equal(105m, filtered.TotalAmount);
        Assert.Null(filtered.DiscountAmount);
        Assert.Null(filtered.PaidAmount);
        Assert.Null(filtered.ChangeAmount);
        Assert.Null(filtered.PaymentMethod);
    }

    [Fact]
    public void ParentOnly_DoesNotExposeSensitiveChild()
    {
        var customer = PosSensitiveResponseFilter.FilterCustomer(
            Ctx(CustomerPermissions.View),
            SampleCustomer());
        Assert.Null(customer.Phone);

        var drawer = PosSensitiveResponseFilter.FilterDrawerSummary(
            Ctx(CashDrawerPermissions.Canonical.PositionView),
            new PosCashDrawerSummaryDto(
                Guid.NewGuid(), Guid.NewGuid(), "T", "open", "USD",
                1m, 2m, 0m, 0m, 0m, 0m, 3m, "C", DateTimeOffset.UtcNow));
        Assert.Null(drawer.CurrentExpectedCash);
    }

    [Fact]
    public void ParentPlusOneChild_OnlyThatChildExposed()
    {
        var filtered = PosSensitiveResponseFilter.FilterCustomer(
            Ctx(CustomerPermissions.View, CashierPosSensitiveFieldCodes.CustomerEmail),
            SampleCustomer());

        Assert.Equal("ada@example.com", filtered.Email);
        Assert.Null(filtered.Phone);
        Assert.Null(filtered.TotalSpentAmount);
    }

    [Fact]
    public void FullAccess_PreservesSensitiveValues()
    {
        var customer = SampleCustomer();
        var filtered = PosSensitiveResponseFilter.FilterCustomer(
            Ctx(
                CustomerPermissions.View,
                CashierPosSensitiveFieldCodes.CustomerPhone,
                CashierPosSensitiveFieldCodes.CustomerEmail,
                CashierPosSensitiveFieldCodes.CustomerTotalSpend),
            customer);

        Assert.Equal(customer.Phone, filtered.Phone);
        Assert.Equal(customer.Email, filtered.Email);
        Assert.Equal(customer.TotalSpentAmount, filtered.TotalSpentAmount);
    }

    [Fact]
    public void SerializedJson_DeniedPhoneIsNull()
    {
        var filtered = PosSensitiveResponseFilter.FilterCustomer(
            Ctx(CustomerPermissions.View),
            SampleCustomer());

        var json = JsonSerializer.Serialize(filtered, JsonOptions);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("phone").ValueKind);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("email").ValueKind);
        Assert.False(doc.RootElement.GetProperty("fullName").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public void Filtering_DoesNotMutateSourceRecordValues()
    {
        var source = SampleCustomer();
        _ = PosSensitiveResponseFilter.FilterCustomer(Ctx(CustomerPermissions.View), source);
        Assert.Equal("+15551212", source.Phone);
        Assert.Equal(150.50m, source.TotalSpentAmount);
    }

    [Fact]
    public void HoldList_ValueDenied_NullsRowTotals()
    {
        var hold = new PosHoldListItemDto(
            Guid.NewGuid(),
            "H-1",
            Guid.NewGuid(),
            "S-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            "HELD",
            2,
            100,
            0,
            10,
            110,
            "USD",
            DateTimeOffset.UtcNow,
            null,
            [
                new PosHoldLineDto(Guid.NewGuid(), Guid.NewGuid(), "Item", null, "SKU", 1, 100, 100)
            ]);

        var filtered = PosSensitiveResponseFilter.FilterHoldList(
            Ctx(SalesPermissions.HeldSales.View),
            new PosHoldListResponseDto([hold], 1, 110, "USD", 1, 25));

        Assert.Null(filtered.TotalValue);
        Assert.Null(filtered.Holds[0].Total);
        Assert.Null(filtered.Holds[0].Lines[0].UnitPrice);
    }

    [Fact]
    public void SensitiveFieldCodes_AreAllPresentInCatalog()
    {
        var codes = typeof(CashierPosSensitiveFieldCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();

        var catalog = CashierPosCanonicalPermissionCatalog.All.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
        Assert.All(codes, code => Assert.Contains(code, catalog));
        Assert.All(codes, code =>
        {
            var def = CashierPosCanonicalPermissionCatalog.All.Single(x => x.Code == code);
            Assert.True(def.IsSensitive);
            Assert.True(def.IsRoleAssignable);
        });
    }
}
