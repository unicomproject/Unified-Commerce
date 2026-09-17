using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Application.Modules.Tenant.POSOperations.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Catalog.CashierPos;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using Moq;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosCheckoutServiceTests
{
    [Theory]
    [InlineData(0, "pos_checkout.permission_denied")]
    [InlineData(1, "pos_checkout.invalid_device_id")]
    [InlineData(2, "pos_checkout.invalid_lines")]
    [InlineData(3, "pos_cart.line_note_too_long")]
    [InlineData(4, "pos_cart.duplicate_client_line_id")]
    [InlineData(5, "pos_checkout.invalid_sale_type")]
    public async Task Summary_ValidationPrecedence(int stage, string code)
    {
        var lineId = Guid.NewGuid();
        var request = new PosCheckoutSummaryRequestDto(stage <= 1 ? Guid.Empty : Guid.NewGuid(), "invalid", null,
            stage <= 2 ? [] : [
                new(Guid.NewGuid(), 1, LineNote: stage == 3 ? new string('x', 501) : null, ClientLineId: lineId),
                new(Guid.NewGuid(), 1, ClientLineId: stage <= 4 ? lineId : Guid.NewGuid())]);
        var result = await Service.GetSummaryAsync(stage == 0 ? context with { Permissions = [] } : context,
            request, CancellationToken.None);
        Assert.Equal(code, result.Error.Code);
        Assert.Empty(repository.Invocations);
    }

    [Fact]
    public async Task Reconciliation_MissingCashPermission_DoesNotCallRepository()
    {
        var result = await Service.GetPaymentStatusAsync(context with { Permissions = [SalesPermissions.Sale.Checkout] }, "key", CancellationToken.None);
        Assert.Equal("pos_checkout.permission_denied", result.Error.Code);
        Assert.Equal("You do not have permission to checkout POS sales.", result.Error.Message);
        Assert.Empty(repository.Invocations);
    }

    [Fact]
    public async Task HundredCharacterTrimmedKey_IsAcceptedByBothOperations()
    {
        var key = new string('x', 100);
        var request = Request with { IdempotencyKey = " " + key + " " };
        SetupPayment(request, null, Payment);
        repository.Setup(x => x.ReconcileCashPaymentAsync(context.TenantId, context.UserId, key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PosCheckoutPaymentStatusDto("unknown", null));
        Assert.True((await Service.StartPaymentAsync(context, request, CancellationToken.None)).IsSuccess);
        Assert.True((await Service.GetPaymentStatusAsync(context, " " + key + " ", CancellationToken.None)).IsSuccess);
        Assert.Equal(2, repository.Invocations.Count);
    }

    [Theory]
    [InlineData("summary", "pos_checkout.device_not_found", "pos_checkout.device_not_found", "POS device could not be found.")]
    [InlineData("summary", "till_session.not_found", "pos_checkout.till_session_not_open", "An open till session is required before checkout.")]
    [InlineData("summary", "till_session.device_not_trusted", "pos_checkout.till_session_not_open", "An open till session is required before checkout.")]
    [InlineData("summary", "till_session.till_not_assigned", "pos_checkout.till_session_not_open", "An open till session is required before checkout.")]
    [InlineData("summary", "pos_checkout.till_session_not_open", "pos_checkout.till_session_not_open", "An open till session is required before checkout.")]
    [InlineData("summary", "pos_checkout.variant_not_found", "pos_checkout.variant_not_found", "One or more product variants could not be found.")]
    [InlineData("summary", "pos_checkout.customer_not_found", "pos_checkout.customer_not_found", "The selected customer could not be found.")]
    [InlineData("summary", "pos_checkout.customer_inactive", "pos_checkout.customer_inactive", "Inactive customers cannot be used at checkout.")]
    [InlineData("summary", "pos_checkout.customer_blocked", "pos_checkout.customer_blocked", "Blocked customers cannot be used at checkout.")]
    [InlineData("summary", "pos_checkout.customer_deleted", "pos_checkout.customer_deleted", "Deleted customers cannot be used at checkout.")]
    [InlineData("summary", "pos_checkout.customer_not_eligible", "pos_checkout.customer_not_eligible", "The selected customer is not eligible for checkout.")]
    [InlineData("summary", "pos_checkout.invalid_lines", "pos_checkout.invalid_lines", "Checkout requires at least one cart line.")]
    [InlineData("summary", "pos_checkout.invalid_sale_type", "pos_checkout.invalid_sale_type", "Sale type must be NewSale.")]
    [InlineData("summary", "pos_checkout.discount_cart_changed", "pos_checkout.discount_cart_changed", "The discount no longer matches this cart or customer. Re-apply the discount.")]
    [InlineData("summary", "pos_checkout.discount_context_mismatch", "pos_checkout.discount_context_mismatch", "The discount belongs to a different till session or device. Re-apply the discount.")]
    [InlineData("summary", "pos_checkout.discount_application_not_found", "pos_checkout.discount_application_not_found", "The applied discount could not be found. Re-apply the discount.")]
    [InlineData("summary", "pos_checkout.discount_application_expired", "pos_checkout.discount_application_expired", "The applied discount is no longer valid. Re-apply the discount.")]
    [InlineData("summary", "pos_checkout.discount_application_invalid", "pos_checkout.discount_application_invalid", "The applied discount is no longer valid. Re-apply the discount.")]
    [InlineData("summary", "pos_checkout.discount_approval_required", "pos_checkout.discount_approval_required", "Manager approval is required before this discount can be used.")]
    [InlineData("summary", "pos_checkout.discount_policy_inactive", "pos_checkout.discount_policy_inactive", "The discount policy is no longer active.")]
    [InlineData("payment", "pos_checkout.device_not_found", "pos_checkout.device_not_found", "POS device could not be found.")]
    [InlineData("payment", "till_session.not_found", "pos_checkout.till_session_not_open", "An open till session is required before checkout.")]
    [InlineData("payment", "till_session.device_not_trusted", "pos_checkout.till_session_not_open", "An open till session is required before checkout.")]
    [InlineData("payment", "till_session.till_not_assigned", "pos_checkout.till_session_not_open", "An open till session is required before checkout.")]
    [InlineData("payment", "pos_checkout.till_session_not_open", "pos_checkout.till_session_not_open", "An open till session is required before checkout.")]
    [InlineData("payment", "pos_checkout.variant_not_found", "pos_checkout.variant_not_found", "One or more product variants could not be found.")]
    [InlineData("payment", "pos_checkout.customer_not_found", "pos_checkout.customer_not_found", "The selected customer could not be found.")]
    [InlineData("payment", "pos_checkout.customer_inactive", "pos_checkout.customer_inactive", "Inactive customers cannot be used at checkout.")]
    [InlineData("payment", "pos_checkout.customer_blocked", "pos_checkout.customer_blocked", "Blocked customers cannot be used at checkout.")]
    [InlineData("payment", "pos_checkout.customer_deleted", "pos_checkout.customer_deleted", "Deleted customers cannot be used at checkout.")]
    [InlineData("payment", "pos_checkout.customer_not_eligible", "pos_checkout.customer_not_eligible", "The selected customer is not eligible for checkout.")]
    [InlineData("payment", "pos_checkout.invalid_lines", "pos_checkout.invalid_lines", "Checkout requires at least one cart line.")]
    [InlineData("payment", "pos_checkout.invalid_sale_type", "pos_checkout.invalid_sale_type", "Sale type must be NewSale.")]
    [InlineData("payment", "pos_checkout.invalid_payment_method", "pos_checkout.invalid_payment_method", "The selected payment method is not supported.")]
    [InlineData("payment", "pos_checkout.payment_permission_denied", "pos_checkout.payment_permission_denied", "You do not have permission to accept this payment method.")]
    [InlineData("payment", "pos_checkout.price_not_configured", "pos_checkout.price_not_configured", "One or more cart lines do not have a configured price.")]
    [InlineData("payment", "pos_checkout.insufficient_stock", "pos_checkout.insufficient_stock", "One or more cart lines do not have enough stock.")]
    [InlineData("payment", "pos_checkout.cash_received_required", "pos_checkout.cash_received_required", "Cash received is required for cash payments.")]
    [InlineData("payment", "pos_checkout.insufficient_cash", "pos_checkout.insufficient_cash", "Cash received is less than the amount due.")]
    [InlineData("payment", "pos_checkout.payment_method_not_found", "pos_checkout.payment_method_not_found", "The selected payment method could not be found.")]
    [InlineData("payment", "pos_checkout.payment_provider_required", "pos_checkout.payment_provider_required", "This payment method requires its provider confirmation flow.")]
    [InlineData("payment", "pos_checkout.idempotency_conflict", "pos_checkout.idempotency_conflict", "The idempotency key was already used for a different checkout request.")]
    [InlineData("payment", "pos_checkout.existing_order_not_found", "pos_checkout.existing_order_not_found", "The existing sales order was not found for this tenant.")]
    [InlineData("payment", "pos_checkout.existing_order_no_balance", "pos_checkout.existing_order_no_balance", "The existing sales order has no outstanding balance.")]
    [InlineData("payment", "pos_checkout.stock_conflict", "pos_checkout.stock_conflict", "Stock changed while the payment was being completed. Recalculate and retry.")]
    public async Task RepositoryErrors_TranslateExactCodeAndMessage(string endpoint, string input, string code, string message)
    {
        ApplicationError error;
        if (endpoint == "summary")
        {
            SetupSummary(input, null);
            var result = await Service.GetSummaryAsync(context,
                new(Guid.NewGuid(), "NewSale", null, [new(Guid.NewGuid(), 1)]), CancellationToken.None);
            Assert.False(result.IsSuccess);
            error = result.Error;
        }
        else
        {
            var request = Request;
            SetupPayment(request, input, null);
            var result = await Service.StartPaymentAsync(context, request, CancellationToken.None);
            Assert.False(result.IsSuccess);
            error = result.Error;
        }
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
        Assert.Single(repository.Invocations);
        Assert.Empty(drawer.Invocations);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unrecognized")]
    public async Task EndpointFallbacks_RemainIndependent(string? input)
    {
        SetupSummary(input, null);
        var request = Request;
        SetupPayment(request, input, null);
        var summary = await Service.GetSummaryAsync(context,
            new(request.DeviceId, request.SaleType, null, request.Lines), CancellationToken.None);
        var payment = await Service.StartPaymentAsync(context, request, CancellationToken.None);
        Assert.Equal(input ?? "pos_checkout.summary_failed", summary.Error.Code);
        Assert.Equal("Checkout summary could not be calculated.", summary.Error.Message);
        Assert.Equal(input ?? "pos_checkout.start_payment_failed", payment.Error.Code);
        Assert.Equal("Checkout payment could not be started.", payment.Error.Message);
    }
    private readonly Mock<IPosCheckoutRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IPosDrawerService> drawer = new(MockBehavior.Strict);
    private readonly Mock<IPosDrawerRepository> settings = new(MockBehavior.Strict);
    private readonly Mock<IDateTimeProvider> clock = new();
    private readonly TenantRequestContext context = new(Guid.NewGuid(), Guid.NewGuid(),
        [SalesPermissions.Sale.Checkout, SalesPermissions.Cart.UpdateItem, PaymentPermissions.AcceptCash]);
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-13T00:00:00Z");
    private PosCheckoutService Service => new(repository.Object, clock.Object, drawer.Object, settings.Object);
    private static PosCheckoutStartPaymentRequestDto Request =>
        new(Guid.NewGuid(), "NewSale", null, [new(Guid.NewGuid(), 1)], "CARD", 100, IdempotencyKey: "key");
    private static PosCheckoutStartPaymentResponseDto Payment => new(
        Guid.NewGuid(), Guid.NewGuid(), "sale", "receipt", "barcode", 100, 10, 0, 90, 100, 10,
        "cash", "LKR", "completed", "receipt", Now, Guid.NewGuid(), [], CustomerName: "Private customer");
    private static PosCheckoutSummaryResponseDto Summary => new(
        new(1, 100, 10, 0, 90, "LKR"), new("NewSale", 1, Now, "Cashier"), ["cash"], []);

    public PosCheckoutServiceTests() => clock.SetupGet(x => x.UtcNow).Returns(Now);

    private void SetupPayment(PosCheckoutStartPaymentRequestDto request, string? error, PosCheckoutStartPaymentResponseDto? payment) =>
        repository.Setup(x => x.StartPaymentAsync(context.TenantId, context.UserId,
            It.IsAny<IReadOnlyCollection<string>>(), request, Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PosCheckoutStartPaymentResult(error, payment));

    private void SetupSummary(string? error, PosCheckoutSummaryResponseDto? summary) =>
        repository.Setup(x => x.CalculateSummaryAsync(context.TenantId, context.UserId,
            It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<PosCheckoutSummaryRequestDto>(), Now,
            It.IsAny<CancellationToken>())).ReturnsAsync(new PosCheckoutCalculationResult(error, summary));

    [Theory]
    [InlineData("CASH", "cashSale", "Cash Sale Open")]
    [InlineData("SPLIT_CASH", "splitPaymentCash", "Split Payment Cash Open")]
    public async Task SuccessfulPayment_RegistersStableDrawerAndFiltersResponse(string method, string purpose, string label)
    {
        var request = Request with { PaymentMethod = method };
        var payment = Payment;
        SetupPayment(request, null, payment);
        var operationId = Guid.NewGuid();
        var requestId = CashDrawerStableRequestId.ForBusinessReference(payment.SaleId, purpose);
        RegisterDrawerOperationRequest? captured = null;
        drawer.Setup(x => x.RegisterOperationAsync(context, It.IsAny<RegisterDrawerOperationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TenantRequestContext, RegisterDrawerOperationRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(ApplicationResult<CashDrawerOperationDto>.Success(Operation(operationId, requestId, request.DeviceId, purpose)));
        var config = new CashDrawerSettingsDto(null, "0", 50, 50, "cash");
        settings.Setup(x => x.GetActiveDrawerSettingsAsync(context.TenantId, request.DeviceId, It.IsAny<CancellationToken>())).ReturnsAsync(config);
        var result = await Service.StartPaymentAsync(context, request, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(new RegisterDrawerOperationRequest(requestId, request.DeviceId, null, purpose, label, "SALE", payment.SaleId), captured);
        Assert.Equal(operationId, result.Value!.DrawerOperationId);
        Assert.Equal(requestId, result.Value.DrawerRequestId);
        Assert.Same(config, result.Value.CashDrawerSettings);
        Assert.Null(result.Value.CustomerName);
        Assert.Null(result.Value.GrandTotal);
        Assert.Equal(payment.SaleId, result.Value.SaleId);
        drawer.Verify(x => x.RegisterOperationAsync(context, It.IsAny<RegisterDrawerOperationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        settings.Verify(x => x.GetActiveDrawerSettingsAsync(context.TenantId, request.DeviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("registration-failure")]
    [InlineData("null-settings")]
    [InlineData("settings-exception")]
    public async Task DrawerFailure_PreservesCurrentPostPaymentBehavior(string scenario)
    {
        var request = Request with { PaymentMethod = "CASH" };
        var payment = Payment;
        SetupPayment(request, null, payment);
        var operationId = Guid.NewGuid();
        var requestId = CashDrawerStableRequestId.ForBusinessReference(payment.SaleId, "cashSale");
        drawer.Setup(x => x.RegisterOperationAsync(context, It.IsAny<RegisterDrawerOperationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario == "registration-failure"
                ? ApplicationResult<CashDrawerOperationDto>.Failure(new("pos_drawer.permission_denied", "Denied"))
                : ApplicationResult<CashDrawerOperationDto>.Success(Operation(operationId, requestId, request.DeviceId, "cashSale")));
        var failure = new InvalidOperationException("Settings unavailable");
        if (scenario == "null-settings")
            settings.Setup(x => x.GetActiveDrawerSettingsAsync(context.TenantId, request.DeviceId, It.IsAny<CancellationToken>())).ReturnsAsync((CashDrawerSettingsDto?)null);
        if (scenario == "settings-exception")
            settings.Setup(x => x.GetActiveDrawerSettingsAsync(context.TenantId, request.DeviceId, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        if (scenario == "settings-exception")
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => Service.StartPaymentAsync(context, request, CancellationToken.None)));
        else
        {
            var result = await Service.StartPaymentAsync(context, request, CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.Equal(payment.SaleId, result.Value!.SaleId);
            Assert.Null(result.Value.CashDrawerSettings);
            Assert.Equal(scenario == "registration-failure" ? null : (Guid?)operationId, result.Value.DrawerOperationId);
            Assert.Equal(scenario == "registration-failure" ? null : (Guid?)requestId, result.Value.DrawerRequestId);
        }
        Assert.Equal(scenario == "registration-failure" ? 0 : 1, settings.Invocations.Count);
        Assert.Single(repository.Invocations);
        Assert.Single(drawer.Invocations);
    }

    private CashDrawerOperationDto Operation(Guid id, Guid requestId, Guid deviceId, string purpose) => new(
        id, context.TenantId, Guid.NewGuid(), null, deviceId, Guid.NewGuid(), Guid.NewGuid(), context.UserId,
        null, requestId, purpose, null, "SALE", null, null, 1, "0", 50, 50, "pending", null, null, false, null, Now, null);

    [Theory]
    [InlineData(0, "pos_checkout.permission_denied")]
    [InlineData(1, "pos_checkout.invalid_device_id")]
    [InlineData(2, "pos_checkout.invalid_lines")]
    [InlineData(3, "pos_cart.line_note_too_long")]
    [InlineData(4, "pos_checkout.invalid_sale_type")]
    [InlineData(5, "pos_checkout.invalid_payment_method")]
    public async Task StartPayment_ValidationPrecedence(int stage, string code)
    {
        var request = Request with
        {
            DeviceId = stage <= 1 ? Guid.Empty : Guid.NewGuid(),
            Lines = stage <= 2 ? [] : [new(Guid.NewGuid(), 1, LineNote: stage == 3 ? new string('x', 501) : null)],
            SaleType = stage <= 4 ? "invalid" : "NewSale",
            PaymentMethod = "",
            IdempotencyKey = null
        };
        var caller = stage == 0 ? context with { Permissions = [] } : context;
        var result = await Service.StartPaymentAsync(caller, request, CancellationToken.None);
        Assert.Equal(code, result.Error.Code);
        Assert.Empty(repository.Invocations);
        Assert.Empty(drawer.Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingOrder_AlonePermitsEmptyLines(bool existing)
    {
        var request = Request with { Lines = [], ExistingSalesOrderId = existing ? Guid.NewGuid() : null };
        if (existing) SetupPayment(request, "pos_checkout.existing_order_not_found", null);
        var result = await Service.StartPaymentAsync(context, request, CancellationToken.None);
        Assert.Equal(existing ? "pos_checkout.existing_order_not_found" : "pos_checkout.invalid_lines", result.Error.Code);
        Assert.Equal(existing ? 1 : 0, repository.Invocations.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("oversized")]
    public async Task InvalidKeys_PreserveSeparateMessages(string? input)
    {
        var key = input == "oversized" ? new string('x', 101) : input;
        var start = await Service.StartPaymentAsync(context, Request with { IdempotencyKey = key }, CancellationToken.None);
        var status = await Service.GetPaymentStatusAsync(context, key!, CancellationToken.None);
        Assert.Equal("pos_checkout.invalid_idempotency_key", start.Error.Code);
        Assert.Equal("pos_checkout.invalid_idempotency_key", status.Error.Code);
        Assert.Equal("A valid idempotency key of at most 100 characters is required.", start.Error.Message);
        Assert.Equal("A valid idempotency key is required.", status.Error.Message);
        Assert.Empty(repository.Invocations);
    }

    [Fact]
    public async Task Reconciliation_MissingCheckoutPermission_RejectsBeforeKeyValidation()
    {
        var result = await Service.GetPaymentStatusAsync(context with { Permissions = [PaymentPermissions.AcceptCash] }, "", CancellationToken.None);
        Assert.Equal("pos_checkout.permission_denied", result.Error.Code);
        Assert.Equal("You do not have permission to checkout POS sales.", result.Error.Message);
        Assert.Empty(repository.Invocations);
    }

    [Fact]
    public async Task ValidKeys_ForwardingAndReconciledPaymentFilteringRemainDistinct()
    {
        var request = Request with { IdempotencyKey = "  key  " };
        SetupPayment(request, null, Payment);
        Assert.True((await Service.StartPaymentAsync(context, request, CancellationToken.None)).IsSuccess);
        repository.Setup(x => x.ReconcileCashPaymentAsync(context.TenantId, context.UserId, "key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PosCheckoutPaymentStatusDto("succeeded", Payment));
        var result = await Service.GetPaymentStatusAsync(context, "  key  ", CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("succeeded", result.Value!.Status);
        Assert.NotNull(result.Value.Payment);
        Assert.Null(result.Value.Payment.CustomerName);
        Assert.Null(result.Value.Payment.GrandTotal);
        Assert.Equal(2, repository.Invocations.Count);
        Assert.Empty(drawer.Invocations);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SummaryFiltering_UsesCartVersusCheckoutPermission(bool cartPermission)
    {
        var caller = context with
        {
            Permissions = context.Permissions.Append(cartPermission
            ? CashierPosSensitiveFieldCodes.CartTotal : CashierPosSensitiveFieldCodes.CheckoutTotal).ToArray()
        };
        SetupSummary(null, Summary);
        var request = new PosCheckoutSummaryRequestDto(Guid.NewGuid(), "NewSale", null, [new(Guid.NewGuid(), 1)]);
        var cart = await Service.CalculateCartAsync(caller, request, CancellationToken.None);
        var checkout = await Service.GetSummaryAsync(caller, request, CancellationToken.None);
        Assert.True(cart.IsSuccess);
        Assert.True(checkout.IsSuccess);
        Assert.Equal(cartPermission ? 90 : (int?)null, cart.Value!.BillingSummary.TotalPayable);
        Assert.Equal(cartPermission ? null : (int?)90, checkout.Value!.BillingSummary.TotalPayable);
        Assert.Null(cart.Value.BillingSummary.Discount);
        Assert.Null(checkout.Value.BillingSummary.Discount);
    }
}
