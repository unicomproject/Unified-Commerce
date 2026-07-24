using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers.V1.ECommerce.CustomerOrders;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.ECommerce.CustomerOrders;

public sealed class CustomerOrdersControllerTests
{
    [Fact]
    public async Task Get_AuthenticatedCustomer_UsesOnlyJwtTenantAndCustomerClaims()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var service = new FakeCustomerOrderService();
        var controller = CreateController(service, tenantId, customerId);

        var result = await controller.Get("accepted", 2, 25, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(customerId, service.CustomerId);
        Assert.Equal("accepted", service.Status);
        Assert.Equal(2, service.Page);
        Assert.Equal(25, service.PageSize);
    }

    [Fact]
    public async Task GetDetail_AuthenticatedCustomer_ForwardsOrderIdAndJwtContext()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var service = new FakeCustomerOrderService();
        var controller = CreateController(service, tenantId, customerId);

        var result = await controller.GetDetail(orderId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(customerId, service.CustomerId);
        Assert.Equal(orderId, service.OrderId);
    }

    [Fact]
    public async Task Cancel_AuthenticatedCustomer_ForwardsOrderIdReasonAndJwtContext()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var service = new FakeCustomerOrderService();
        var controller = CreateController(service, tenantId, customerId);

        var result = await controller.Cancel(
            orderId,
            new CustomerOrderCancelRequest { Reason = "Changed my mind" },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(customerId, service.CustomerId);
        Assert.Equal(orderId, service.CancelOrderId);
        Assert.Equal("Changed my mind", service.CancelReason);
    }

    [Fact]
    public async Task Get_MissingCustomerClaims_ReturnsUnauthorizedWithoutCallingService()
    {
        var service = new FakeCustomerOrderService();
        var controller = CreateController(service);

        var result = await controller.Get(null, 1, 10, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task GetDetail_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        var service = new FakeCustomerOrderService
        {
            DetailResult = ApplicationResult<CustomerOrderDetailReadModel>.Failure(
                new ApplicationError("customer_orders.not_found", "Order was not found."))
        };
        var controller = CreateController(service, Guid.NewGuid(), Guid.NewGuid());

        var result = await controller.GetDetail(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenServiceReturnsInvalidTransition_ReturnsConflict()
    {
        var service = new FakeCustomerOrderService
        {
            CancelResult = ApplicationResult<CustomerOrderCancelResponse>.Failure(
                new ApplicationError("customer_orders.invalid_transition", "Order cannot be cancelled."))
        };
        var controller = CreateController(service, Guid.NewGuid(), Guid.NewGuid());

        var result = await controller.Cancel(Guid.NewGuid(), new CustomerOrderCancelRequest(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
    }

    [Fact]
    public void Controller_RequiresCustomerOnlyPolicyAndExpectedRoutes()
    {
        var authorize = Assert.Single(
            typeof(CustomerOrdersController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("CustomerOnly", authorize.Policy);

        var route = Assert.Single(
            typeof(CustomerOrdersController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/v1/ecommerce/storefront/orders", route.Template);

        Assert.Single(typeof(CustomerOrdersController)
            .GetMethod(nameof(CustomerOrdersController.Get))!
            .GetCustomAttributes<HttpGetAttribute>());
        Assert.Equal(
            "{orderId:guid}",
            Assert.Single(typeof(CustomerOrdersController)
                .GetMethod(nameof(CustomerOrdersController.GetDetail))!
                .GetCustomAttributes<HttpGetAttribute>()).Template);
        Assert.Equal(
            "{orderId:guid}/cancel",
            Assert.Single(typeof(CustomerOrdersController)
                .GetMethod(nameof(CustomerOrdersController.Cancel))!
                .GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    private static CustomerOrdersController CreateController(
        FakeCustomerOrderService service,
        Guid? tenantId = null,
        Guid? customerId = null)
    {
        var context = new DefaultHttpContext();
        if (tenantId.HasValue && customerId.HasValue)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("tenant_id", tenantId.Value.ToString()),
                new Claim("sub", customerId.Value.ToString()),
                new Claim("identity_type", "customer")
            ], "Test"));
        }

        return new CustomerOrdersController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private sealed class FakeCustomerOrderService : ICustomerOrderService
    {
        public ApplicationResult<CustomerOrderListReadModel> ListResult { get; init; } =
            ApplicationResult<CustomerOrderListReadModel>.Success(new CustomerOrderListReadModel());
        public ApplicationResult<CustomerOrderDetailReadModel> DetailResult { get; init; } =
            ApplicationResult<CustomerOrderDetailReadModel>.Success(new CustomerOrderDetailReadModel());
        public ApplicationResult<CustomerOrderCancelResponse> CancelResult { get; init; } =
            ApplicationResult<CustomerOrderCancelResponse>.Success(new CustomerOrderCancelResponse());
        public int CallCount { get; private set; }
        public Guid? TenantId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public Guid? OrderId { get; private set; }
        public Guid? CancelOrderId { get; private set; }
        public string? CancelReason { get; private set; }
        public string? Status { get; private set; }
        public int? Page { get; private set; }
        public int? PageSize { get; private set; }

        public Task<ApplicationResult<CustomerOrderListReadModel>> GetAsync(
            Guid tenantId,
            Guid customerId,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            Status = status;
            Page = page;
            PageSize = pageSize;
            return Task.FromResult(ListResult);
        }

        public Task<ApplicationResult<CustomerOrderDetailReadModel>> GetDetailAsync(
            Guid tenantId,
            Guid customerId,
            Guid orderId,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            OrderId = orderId;
            return Task.FromResult(DetailResult);
        }

        public Task<ApplicationResult<CustomerOrderCancelResponse>> CancelAsync(
            Guid tenantId,
            Guid customerId,
            Guid orderId,
            CustomerOrderCancelRequest request,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            CancelOrderId = orderId;
            CancelReason = request.Reason;
            return Task.FromResult(CancelResult);
        }

        private void Capture(Guid tenantId, Guid customerId)
        {
            CallCount++;
            TenantId = tenantId;
            CustomerId = customerId;
        }
    }
}
