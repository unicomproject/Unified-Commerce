using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;

namespace E_POS.Application.Modules.Tenant.AccessControl.SensitiveData;

/// <summary>
/// Chunk 7: permission-aware response shaping. Uses Chunk 5 effective permissions
/// already present on <see cref="TenantRequestContext"/>. Fail-closed: absent permission → null.
/// Does not invent redacted/fake numeric values (no 0 / "REDACTED" substitutes).
/// </summary>
public static class PosSensitiveResponseFilter
{
    public static PosCustomerListItemResponseDto FilterCustomer(
        TenantRequestContext context,
        PosCustomerListItemResponseDto customer)
    {
        var allowSpend = Can(context, CashierPosSensitiveFieldCodes.CustomerTotalSpend);
        return customer with
        {
            Phone = Can(context, CashierPosSensitiveFieldCodes.CustomerPhone) ? customer.Phone : null,
            Email = Can(context, CashierPosSensitiveFieldCodes.CustomerEmail) ? customer.Email : null,
            TotalSpentAmount = allowSpend ? customer.TotalSpentAmount : null,
            IsMixedCurrencySpend = allowSpend && customer.IsMixedCurrencySpend,
            CurrencyCode = allowSpend ? customer.CurrencyCode : null,
        };
    }

    public static PosCustomerListResponseDto FilterCustomerList(
        TenantRequestContext context,
        PosCustomerListResponseDto response) =>
        response with
        {
            Items = response.Items.Select(item => FilterCustomer(context, item)).ToArray()
        };

    public static PosCustomerAttachToSaleResponseDto FilterAttachedCustomer(
        TenantRequestContext context,
        PosCustomerAttachToSaleResponseDto customer) =>
        customer with
        {
            Phone = Can(context, CashierPosSensitiveFieldCodes.CustomerPhone) ? customer.Phone : null,
            Email = Can(context, CashierPosSensitiveFieldCodes.CustomerEmail) ? customer.Email : null,
        };

    public static PosCustomerOrdersResponseDto FilterCustomerOrders(
        TenantRequestContext context,
        PosCustomerOrdersResponseDto response)
    {
        var allowHistory = Can(context, CashierPosSensitiveFieldCodes.CustomerPurchaseHistory)
            || Can(context, CashierPosSensitiveFieldCodes.CustomerRecentPurchases);
        if (!allowHistory)
        {
            return response with
            {
                Items = Array.Empty<PosCustomerOrderItemDto>(),
                TotalCount = 0,
                TotalPages = 0,
            };
        }

        var allowAmounts = Can(context, CashierPosSensitiveFieldCodes.CustomerPurchaseAmounts);
        return response with
        {
            Items = response.Items.Select(item => item with
            {
                TotalAmount = allowAmounts ? item.TotalAmount : null
            }).ToArray()
        };
    }

    public static PosCashDrawerSummaryDto FilterDrawerSummary(
        TenantRequestContext context,
        PosCashDrawerSummaryDto summary)
    {
        // Shared summary DTO backs Cash Drawer + Cash In/Out/Drop screens.
        var allowOpening = Can(context, CashierPosSensitiveFieldCodes.DrawerOpeningCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashInAvailableCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashOutAvailableCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashDropAvailableCash);
        var allowExpected = Can(context, CashierPosSensitiveFieldCodes.DrawerExpectedCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashInExpectedCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashOutExpectedCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashDropExpectedCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashInResultingBalance)
            || Can(context, CashierPosSensitiveFieldCodes.CashOutResultingBalance)
            || Can(context, CashierPosSensitiveFieldCodes.CashDropResultingBalance);

        return summary with
        {
            OpeningCash = allowOpening ? summary.OpeningCash : null,
            CashSales = Can(context, CashierPosSensitiveFieldCodes.DrawerCashSales)
                ? summary.CashSales
                : null,
            CurrentExpectedCash = allowExpected ? summary.CurrentExpectedCash : null,
        };
    }

    public static PosCashDrawerMovementDto FilterDrawerMovement(
        TenantRequestContext context,
        PosCashDrawerMovementDto movement,
        string? movementTypeCode = null)
    {
        var allowAmount = Can(context, CashierPosSensitiveFieldCodes.DrawerMovementAmount);
        var allowExpected = Can(context, CashierPosSensitiveFieldCodes.DrawerExpectedCash)
            || CanResultingOrExpectedForMovement(context, movementTypeCode);

        return movement with
        {
            Amount = allowAmount ? movement.Amount : null,
            CurrentExpectedCash = allowExpected ? movement.CurrentExpectedCash : null,
        };
    }

    public static PosCashDrawerMovementPageDto FilterDrawerMovements(
        TenantRequestContext context,
        PosCashDrawerMovementPageDto page) =>
        page with
        {
            Items = page.Items.Select(item => FilterDrawerMovement(context, item)).ToArray()
        };

    public static CurrentTillSessionDto FilterCurrentTillSession(
        TenantRequestContext context,
        CurrentTillSessionDto session) =>
        session with
        {
            OpeningFloat = Can(context, CashierPosSensitiveFieldCodes.TillStartingCashView)
                ? session.OpeningFloat
                : null,
            ExpectedCash = Can(context, CashierPosSensitiveFieldCodes.TillClosingExpectedCash)
                || Can(context, CashierPosSensitiveFieldCodes.TillClosingExpectedCashSummary)
                || Can(context, CashierPosSensitiveFieldCodes.DrawerExpectedCash)
                ? session.ExpectedCash
                : null,
        };

    public static ClosedTillSessionDto FilterClosedTillSession(
        TenantRequestContext context,
        ClosedTillSessionDto session) =>
        session with
        {
            OpeningFloat = Can(context, CashierPosSensitiveFieldCodes.TillStartingCashView)
                ? session.OpeningFloat
                : null,
            ExpectedCash = Can(context, CashierPosSensitiveFieldCodes.TillClosingExpectedCash)
                || Can(context, CashierPosSensitiveFieldCodes.TillClosingExpectedCashSummary)
                ? session.ExpectedCash
                : null,
            CountedCash = Can(context, CashierPosSensitiveFieldCodes.TillClosingCountedCashSummary)
                ? session.CountedCash
                : null,
            CashDifference = Can(context, CashierPosSensitiveFieldCodes.TillClosingDifference)
                || Can(context, CashierPosSensitiveFieldCodes.TillClosingDifferenceSummary)
                ? session.CashDifference
                : null,
        };

    public static CurrentTillSessionResponseDto FilterCurrentTillSessionResponse(
        TenantRequestContext context,
        CurrentTillSessionResponseDto response) =>
        response with { TillSession = FilterCurrentTillSession(context, response.TillSession) };

    public static CloseTillResponseDto FilterCloseTillResponse(
        TenantRequestContext context,
        CloseTillResponseDto response) =>
        response with { TillSession = FilterClosedTillSession(context, response.TillSession) };

    public static PosHoldListItemDto FilterHoldItem(
        TenantRequestContext context,
        PosHoldListItemDto hold)
    {
        var allowValue = Can(context, CashierPosSensitiveFieldCodes.HeldSalesValue);
        return hold with
        {
            Subtotal = allowValue ? hold.Subtotal : null,
            Discount = allowValue ? hold.Discount : null,
            Tax = allowValue ? hold.Tax : null,
            Total = allowValue ? hold.Total : null,
            Lines = allowValue
                ? hold.Lines
                : hold.Lines.Select(line => line with
                {
                    UnitPrice = null,
                    LineTotal = null,
                }).ToArray(),
        };
    }

    public static PosHoldListResponseDto FilterHoldList(
        TenantRequestContext context,
        PosHoldListResponseDto response) =>
        response with
        {
            Holds = response.Holds.Select(item => FilterHoldItem(context, item)).ToArray(),
            TotalValue = Can(context, CashierPosSensitiveFieldCodes.HeldSalesSummary)
                ? response.TotalValue
                : null,
        };

    public static PosRecallHoldResponseDto FilterRecallHold(
        TenantRequestContext context,
        PosRecallHoldResponseDto recall) =>
        recall with
        {
            CheckoutSummary = FilterCheckoutSummary(context, recall.CheckoutSummary, isCartContext: false)
        };

    public static PosCheckoutBillingSummaryDto FilterCheckoutBilling(
        TenantRequestContext context,
        PosCheckoutBillingSummaryDto billing,
        bool isCartContext)
    {
        var discountCode = isCartContext
            ? CashierPosSensitiveFieldCodes.CartDiscount
            : CashierPosSensitiveFieldCodes.CheckoutDiscount;
        var totalCode = isCartContext
            ? CashierPosSensitiveFieldCodes.CartTotal
            : CashierPosSensitiveFieldCodes.CheckoutTotal;

        var allowDiscount = Can(context, discountCode)
            || Can(context, CashierPosSensitiveFieldCodes.CashPaymentDiscount);
        var allowTotal = Can(context, totalCode)
            || Can(context, CashierPosSensitiveFieldCodes.CashPaymentTotalDue)
            || Can(context, CashierPosSensitiveFieldCodes.CashPaymentDueAmount);

        return billing with
        {
            Discount = allowDiscount ? billing.Discount : null,
            AutomaticDiscount = allowDiscount ? billing.AutomaticDiscount : null,
            ManualDiscount = allowDiscount ? billing.ManualDiscount : null,
            TotalPayable = allowTotal ? billing.TotalPayable : null,
        };
    }

    public static PosCheckoutSummaryResponseDto FilterCheckoutSummary(
        TenantRequestContext context,
        PosCheckoutSummaryResponseDto summary,
        bool isCartContext) =>
        summary with
        {
            BillingSummary = FilterCheckoutBilling(context, summary.BillingSummary, isCartContext)
        };

    public static PosCheckoutStartPaymentResponseDto FilterStartPayment(
        TenantRequestContext context,
        PosCheckoutStartPaymentResponseDto payment)
    {
        var allowCustomer = Can(context, CashierPosSensitiveFieldCodes.SaleCompleteCustomer)
            || Can(context, CashierPosSensitiveFieldCodes.ReceiptCustomer);
        var allowDiscount = Can(context, CashierPosSensitiveFieldCodes.CashPaymentDiscount)
            || Can(context, CashierPosSensitiveFieldCodes.CheckoutDiscount)
            || Can(context, CashierPosSensitiveFieldCodes.ReceiptDiscount)
            || Can(context, CashierPosSensitiveFieldCodes.CartDiscount);
        var allowTotal = Can(context, CashierPosSensitiveFieldCodes.SaleCompleteTotalPaid)
            || Can(context, CashierPosSensitiveFieldCodes.CashPaymentTotalDue)
            || Can(context, CashierPosSensitiveFieldCodes.CashPaymentDueAmount)
            || Can(context, CashierPosSensitiveFieldCodes.CheckoutTotal)
            || Can(context, CashierPosSensitiveFieldCodes.ReceiptTotal)
            || Can(context, CashierPosSensitiveFieldCodes.CartTotal);
        var allowCashReceived = Can(context, CashierPosSensitiveFieldCodes.SaleCompleteCashReceived)
            || Can(context, CashierPosSensitiveFieldCodes.CashPaymentAmountReceivedView)
            || Can(context, CashierPosSensitiveFieldCodes.ReceiptPaidAmount);
        var allowChange = Can(context, CashierPosSensitiveFieldCodes.SaleCompleteChangeDue)
            || Can(context, CashierPosSensitiveFieldCodes.CashPaymentChangeDue)
            || Can(context, CashierPosSensitiveFieldCodes.ReceiptChangeDue);
        var allowPaymentMethod = Can(context, CashierPosSensitiveFieldCodes.ReceiptPaymentMethod);

        IReadOnlyList<PosReceiptTenderLineDto>? tenders = payment.Tenders;
        if (tenders is not null)
        {
            if (!allowPaymentMethod && !allowTotal && !allowCashReceived && !allowChange)
            {
                tenders = null;
            }
            else
            {
                tenders = tenders.Select(tender => tender with
                {
                    MethodCode = allowPaymentMethod ? tender.MethodCode : null,
                    MethodName = allowPaymentMethod ? tender.MethodName : null,
                    Amount = allowTotal || allowCashReceived ? tender.Amount : null,
                    AmountTendered = allowCashReceived ? tender.AmountTendered : null,
                    ChangeAmount = allowChange ? tender.ChangeAmount : null,
                }).ToArray();
            }
        }

        return payment with
        {
            DiscountTotal = allowDiscount ? payment.DiscountTotal : null,
            GrandTotal = allowTotal ? payment.GrandTotal : null,
            CashReceived = allowCashReceived ? payment.CashReceived : null,
            ChangeDue = allowChange ? payment.ChangeDue : null,
            PaymentMethod = allowPaymentMethod ? payment.PaymentMethod : null,
            CustomerId = allowCustomer ? payment.CustomerId : null,
            CustomerName = allowCustomer ? payment.CustomerName : null,
            CustomerPhone = allowCustomer
                && Can(context, CashierPosSensitiveFieldCodes.CustomerPhone)
                    ? payment.CustomerPhone
                    : null,
            Tenders = tenders,
            DiscountLines = allowDiscount ? payment.DiscountLines : null,
            ReceiptDataJson = ShouldStripReceiptPayload(
                allowDiscount, allowTotal, allowCashReceived, allowChange, allowPaymentMethod, allowCustomer)
                ? null
                : payment.ReceiptDataJson,
        };
    }

    public static PosReceiptDetailDto FilterReceiptDetail(
        TenantRequestContext context,
        PosReceiptDetailDto detail)
    {
        var allowDiscount = Can(context, CashierPosSensitiveFieldCodes.ReceiptDiscount);
        var allowTotal = Can(context, CashierPosSensitiveFieldCodes.ReceiptTotal);
        var allowPaid = Can(context, CashierPosSensitiveFieldCodes.ReceiptPaidAmount);
        var allowChange = Can(context, CashierPosSensitiveFieldCodes.ReceiptChangeDue);
        var allowPaymentMethod = Can(context, CashierPosSensitiveFieldCodes.ReceiptPaymentMethod);

        IReadOnlyList<PosReceiptTenderLineDto>? tenders = detail.Tenders;
        if (tenders is not null)
        {
            if (!allowPaymentMethod && !allowTotal && !allowPaid && !allowChange)
            {
                tenders = null;
            }
            else
            {
                tenders = tenders.Select(tender => tender with
                {
                    MethodCode = allowPaymentMethod ? tender.MethodCode : null,
                    MethodName = allowPaymentMethod ? tender.MethodName : null,
                    Amount = allowPaid || allowTotal ? tender.Amount : null,
                    AmountTendered = allowPaid ? tender.AmountTendered : null,
                    ChangeAmount = allowChange ? tender.ChangeAmount : null,
                }).ToArray();
            }
        }

        var stripPayload = ShouldStripReceiptPayload(
            allowDiscount, allowTotal, allowPaid, allowChange, allowPaymentMethod, allowCustomer: true);

        return detail with
        {
            PaymentMethod = allowPaymentMethod ? detail.PaymentMethod : null,
            DiscountAmount = allowDiscount ? detail.DiscountAmount : null,
            TotalAmount = allowTotal ? detail.TotalAmount : null,
            PaidAmount = allowPaid ? detail.PaidAmount : null,
            ChangeAmount = allowChange ? detail.ChangeAmount : null,
            DiscountLines = allowDiscount ? detail.DiscountLines : null,
            Tenders = tenders,
            ReceiptDataJson = stripPayload ? null : detail.ReceiptDataJson,
            HistoricalSnapshot = stripPayload ? null : detail.HistoricalSnapshot,
        };
    }

    public static PosReceiptSearchResponseDto FilterReceiptSearch(
        TenantRequestContext context,
        PosReceiptSearchResponseDto response)
    {
        var allowTotal = Can(context, CashierPosSensitiveFieldCodes.ReceiptTotal);
        var allowPaymentMethod = Can(context, CashierPosSensitiveFieldCodes.ReceiptPaymentMethod);
        return response with
        {
            Items = response.Items.Select(item => item with
            {
                PaymentMethod = allowPaymentMethod ? item.PaymentMethod : null,
                TotalAmount = allowTotal ? item.TotalAmount : null,
            }).ToArray()
        };
    }

    private static bool CanResultingOrExpectedForMovement(
        TenantRequestContext context,
        string? movementTypeCode)
    {
        if (string.Equals(movementTypeCode, "CASH_DROP", StringComparison.OrdinalIgnoreCase))
        {
            return Can(context, CashierPosSensitiveFieldCodes.CashDropExpectedCash)
                || Can(context, CashierPosSensitiveFieldCodes.CashDropResultingBalance)
                || Can(context, CashierPosSensitiveFieldCodes.CashDropAvailableCash);
        }

        // Direction-based fallback when only Direction is known on list items.
        return Can(context, CashierPosSensitiveFieldCodes.CashInExpectedCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashInResultingBalance)
            || Can(context, CashierPosSensitiveFieldCodes.CashInAvailableCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashOutExpectedCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashOutResultingBalance)
            || Can(context, CashierPosSensitiveFieldCodes.CashOutAvailableCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashDropExpectedCash)
            || Can(context, CashierPosSensitiveFieldCodes.CashDropResultingBalance)
            || Can(context, CashierPosSensitiveFieldCodes.CashDropAvailableCash);
    }

    private static bool ShouldStripReceiptPayload(
        bool allowDiscount,
        bool allowTotal,
        bool allowPaidOrCash,
        bool allowChange,
        bool allowPaymentMethod,
        bool allowCustomer) =>
        !(allowDiscount && allowTotal && allowPaidOrCash && allowChange && allowPaymentMethod && allowCustomer);

    private static bool Can(TenantRequestContext context, string permissionCode) =>
        context.HasPermission(permissionCode);
}
