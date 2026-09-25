using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using E_POS.Domain.Modules.Tenant.Payment.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.POSOperations;

public sealed class PosCheckoutControllerTests
{
    [Theory]
    [InlineData("pos_checkout.permission_denied", 403, 403)]
    [InlineData("pos_checkout.payment_permission_denied", 400, 403)]
    [InlineData("pos_checkout.device_not_found", 404, 404)]
    [InlineData("pos_checkout.customer_not_found", 404, 404)]
    [InlineData("pos_checkout.variant_not_found", 404, 404)]
    [InlineData("pos_checkout.customer_inactive", 422, 422)]
    [InlineData("pos_checkout.customer_blocked", 422, 422)]
    [InlineData("pos_checkout.customer_deleted", 422, 422)]
    [InlineData("pos_checkout.customer_not_eligible", 422, 422)]
    [InlineData("pos_checkout.discount_application_not_found", 404, 404)]
    [InlineData("pos_checkout.discount_approval_required", 409, 409)]
    [InlineData("pos_checkout.discount_application_expired", 409, 409)]
    [InlineData("pos_checkout.discount_application_invalid", 409, 409)]
    [InlineData("pos_checkout.discount_context_mismatch", 409, 409)]
    [InlineData("pos_checkout.discount_policy_inactive", 409, 409)]
    [InlineData("pos_checkout.discount_cart_changed", 409, 409)]
    [InlineData("pos_checkout.idempotency_conflict", 400, 409)]
    [InlineData("pos_checkout.stock_conflict", 400, 409)]
    [InlineData("pos_checkout.persistence_failed", 400, 500)]
    [InlineData("pos_checkout.till_session_not_open", 400, 400)]
    [InlineData("pos_checkout.invalid_payment_method", 400, 400)]
    [InlineData("pos_checkout.cash_received_required", 400, 400)]
    [InlineData("pos_checkout.insufficient_cash", 400, 400)]
    [InlineData("pos_checkout.insufficient_stock", 400, 400)]
    [InlineData("pos_checkout.price_not_configured", 400, 400)]
    [InlineData("pos_checkout.invalid_lines", 400, 400)]
    [InlineData("pos_checkout.invalid_idempotency_key", 400, 400)]
    [InlineData("pos_checkout.payment_provider_required", 400, 400)]
    [InlineData("pos_checkout.invalid_tenant_context", 401, 401)]
    [InlineData("pos_checkout.unrecognized", 400, 400)]
    public async Task ErrorMappings_PreserveEndpointStatusAndEnvelope(string code, int commonStatus, int startStatus)
    {
        var error = new ApplicationError(code, "Safe failure");
        var service = new FakePosCheckoutService
        {
            GetSummaryResult = ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(error),
            StartPaymentResult = ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(error),
            PaymentStatusResult = ApplicationResult<PosCheckoutPaymentStatusDto>.Failure(error)
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Sale.Checkout);
        controller.HttpContext.TraceIdentifier = "checkout-test";
        var before = DateTimeOffset.UtcNow;
        var results = new[]
        {
            (await controller.GetSummary(new(Guid.NewGuid(), "NewSale", null, []), CancellationToken.None), commonStatus),
            (await controller.StartPayment(new(Guid.NewGuid(), "NewSale", null, [], "cash", 0), CancellationToken.None), startStatus),
            (await controller.GetPaymentStatus(new("key"), CancellationToken.None), commonStatus)
        };
        foreach (var (result, status) in results)
        {
            var response = Assert.IsAssignableFrom<ObjectResult>(result);
            Assert.Equal(status, response.StatusCode);
            var body = System.Text.Json.JsonSerializer.SerializeToElement(response.Value);
            Assert.Equal(5, body.EnumerateObject().Count());
            Assert.Equal(code, body.GetProperty("code").GetString());
            Assert.Equal("Safe failure", body.GetProperty("message").GetString());
            Assert.Equal(0, body.GetProperty("details").GetArrayLength());
            Assert.Equal("checkout-test", body.GetProperty("traceId").GetString());
            Assert.InRange(body.GetProperty("timestamp").GetDateTimeOffset(), before, DateTimeOffset.UtcNow);
        }
    }

    [Fact]
    public void PaymentStatus_DeclaresDocumentedResponses()
    {
        var attributes = typeof(PosCheckoutController).GetMethod(nameof(PosCheckoutController.GetPaymentStatus))!
            .GetCustomAttributes<ProducesResponseTypeAttribute>().ToArray();
        Assert.Equal(new[] { 200, 400, 401, 403 }, attributes.Select(a => a.StatusCode).OrderBy(s => s));
        Assert.Equal(typeof(PosCheckoutPaymentStatusDto), attributes.Single(a => a.StatusCode == 200).Type);
    }

    [Fact]
    public async Task PaymentStatus_RequiresTenantContext()
    {
        var controller = CreateController(new FakePosCheckoutService());
        Assert.IsType<UnauthorizedObjectResult>(await controller.GetPaymentStatus(new("key"), CancellationToken.None));
    }

    [Fact]
    public async Task PaymentStatus_ReturnsReadOnlyUnknownEnvelope()
    {
        var controller = CreateController(new FakePosCheckoutService());
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Sale.Checkout);
        var result = await controller.GetPaymentStatus(new("key"), CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("unknown", System.Text.Json.JsonSerializer.Serialize(ok.Value));
    }

    [Fact]
    public async Task GetSummary_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var summary = new PosCheckoutSummaryResponseDto(
            new PosCheckoutBillingSummaryDto(2, 2500, 0, 0, 2500, "LKR"),
            new PosCheckoutSaleDetailsDto("New Sale", 2, now, "Cashier 001"),
            ["cash", "card"],
            []);

        var service = new FakePosCheckoutService
        {
            GetSummaryResult = ApplicationResult<PosCheckoutSummaryResponseDto>.Success(summary),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, SalesPermissions.Sale.Checkout);

        var request = new PosCheckoutSummaryRequestDto(
            deviceId,
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 2)]);

        var result = await controller.GetSummary(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(deviceId, service.Request?.DeviceId);
    }

    [Fact]
    public async Task GetSummary_WhenPermissionDenied_ReturnsForbidden()
    {
        var service = new FakePosCheckoutService
        {
            GetSummaryResult = ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(
                new ApplicationError("pos_checkout.permission_denied", "You do not have permission to checkout POS sales.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "products.view");

        var request = new PosCheckoutSummaryRequestDto(
            Guid.NewGuid(),
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)]);

        var result = await controller.GetSummary(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WhenTillSessionNotOpen_ReturnsBadRequest()
    {
        var service = new FakePosCheckoutService
        {
            GetSummaryResult = ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(
                new ApplicationError("pos_checkout.till_session_not_open", "An open till session is required before checkout.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Sale.Checkout);

        var request = new PosCheckoutSummaryRequestDto(
            Guid.NewGuid(),
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)]);

        var result = await controller.GetSummary(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetSummary_WhenVariantNotFound_ReturnsNotFound()
    {
        var service = new FakePosCheckoutService
        {
            GetSummaryResult = ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(
                new ApplicationError("pos_checkout.variant_not_found", "One or more product variants could not be found.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Sale.Checkout);

        var request = new PosCheckoutSummaryRequestDto(
            Guid.NewGuid(),
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)]);

        var result = await controller.GetSummary(request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task StartPayment_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var payment = new PosCheckoutStartPaymentResponseDto(
            saleId,
            saleId,
            "SO-000001",
            "RCP-000001",
            "RCP-000001",
            2500,
            0,
            0,
            2500,
            3000,
            500,
            "cash",
            "LKR",
            "completed",
            "completed",
            now,
            paymentId,
            [new PosCheckoutStartPaymentLineResponseDto("Team Jersey", 2, 1250, 2500, "JER-SKU")],
            ReceiptDataJson: "{}");

        var service = new FakePosCheckoutService
        {
            StartPaymentResult = ApplicationResult<PosCheckoutStartPaymentResponseDto>.Success(payment),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, SalesPermissions.Sale.Checkout, PaymentPermissions.AcceptCash);

        var request = new PosCheckoutStartPaymentRequestDto(
            deviceId,
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 2)],
            "cash",
            3000);

        var result = await controller.StartPayment(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(deviceId, service.StartPaymentRequest?.DeviceId);
        Assert.Equal("cash", service.StartPaymentRequest?.PaymentMethod);
    }

    [Fact]
    public async Task StartPayment_WhenInsufficientCash_ReturnsBadRequest()
    {
        var service = new FakePosCheckoutService
        {
            StartPaymentResult = ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(
                new ApplicationError("pos_checkout.insufficient_cash", "Cash received is less than the amount due.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Sale.Checkout, PaymentPermissions.AcceptCash);

        var request = new PosCheckoutStartPaymentRequestDto(
            Guid.NewGuid(),
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)],
            "cash",
            100);

        var result = await controller.StartPayment(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StartPayment_WhenPaymentPermissionDenied_ReturnsForbidden()
    {
        var service = new FakePosCheckoutService
        {
            StartPaymentResult = ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(
                new ApplicationError("pos_checkout.payment_permission_denied", "You do not have permission to accept this payment method.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Sale.Checkout);

        var request = new PosCheckoutStartPaymentRequestDto(
            Guid.NewGuid(),
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)],
            "card",
            null);

        var result = await controller.StartPayment(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PosCheckoutController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static PosCheckoutController CreateController(FakePosCheckoutService service)
    {
        var controller = new PosCheckoutController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetTenantClaims(
        PosCheckoutController controller,
        Guid tenantId,
        Guid userId,
        params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("tenant_id", tenantId.ToString())
        };
        claims.AddRange(permissions.Select(permission => new Claim("permissions", permission)));

        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            "Test"));
    }

    private sealed class FakePosCheckoutService : IPosCheckoutService
    {
        public ApplicationResult<PosCheckoutPaymentStatusDto> PaymentStatusResult { get; init; } =
            ApplicationResult<PosCheckoutPaymentStatusDto>.Success(new("unknown", null));

        public Task<ApplicationResult<PosCheckoutPaymentStatusDto>> GetPaymentStatusAsync(
            TenantRequestContext context, string key, CancellationToken cancellationToken)
            => Task.FromResult(PaymentStatusResult);

        public ApplicationResult<PosCheckoutSummaryResponseDto> GetSummaryResult { get; init; } =
            ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(
                new ApplicationError("pos_checkout.summary_failed", "Checkout summary could not be calculated."));

        public ApplicationResult<PosCheckoutStartPaymentResponseDto> StartPaymentResult { get; init; } =
            ApplicationResult<PosCheckoutStartPaymentResponseDto>.Failure(
                new ApplicationError("pos_checkout.start_payment_failed", "Checkout payment could not be started."));

        public TenantRequestContext? Context { get; private set; }
        public PosCheckoutSummaryRequestDto? Request { get; private set; }
        public PosCheckoutStartPaymentRequestDto? StartPaymentRequest { get; private set; }

        public Task<ApplicationResult<PosCheckoutSummaryResponseDto>> CalculateCartAsync(
            TenantRequestContext context,
            PosCheckoutSummaryRequestDto request,
            CancellationToken cancellationToken)
        {
            Context = context;
            Request = request;
            return Task.FromResult(GetSummaryResult);
        }

        public Task<ApplicationResult<PosCheckoutSummaryResponseDto>> GetSummaryAsync(
            TenantRequestContext context,
            PosCheckoutSummaryRequestDto request,
            CancellationToken cancellationToken)
        {
            Context = context;
            Request = request;
            return Task.FromResult(GetSummaryResult);
        }

        public Task<ApplicationResult<PosCheckoutStartPaymentResponseDto>> StartPaymentAsync(
            TenantRequestContext context,
            PosCheckoutStartPaymentRequestDto request,
            CancellationToken cancellationToken)
        {
            Context = context;
            StartPaymentRequest = request;
            return Task.FromResult(StartPaymentResult);
        }
    }
}
