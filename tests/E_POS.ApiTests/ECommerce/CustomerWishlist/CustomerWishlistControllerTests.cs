using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers.V1.ECommerce.CustomerWishlist;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerWishlist.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.ECommerce.CustomerWishlist;

public sealed class CustomerWishlistControllerTests
{
    [Fact]
    public async Task AddItem_AuthenticatedCustomer_UsesOnlyJwtTenantAndCustomerClaims()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var request = new AddCustomerWishlistItemRequest { ProductId = Guid.NewGuid() };
        var service = new FakeService
        {
            AddResult = ApplicationResult<CustomerWishlistReadModel>.Success(
                CreateWishlist(customerId))
        };
        var controller = CreateController(service, tenantId, customerId);

        var result = await controller.AddItem(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(customerId, service.CustomerId);
        Assert.Same(request, service.AddRequest);
    }

    [Fact]
    public async Task Get_MissingCustomerClaims_ReturnsUnauthorizedWithoutCallingService()
    {
        var service = new FakeService();
        var controller = CreateController(service);

        var result = await controller.Get(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(0, service.CallCount);
    }

    [Fact]
    public async Task RemoveItem_ItemNotFound_ReturnsNotFound()
    {
        var service = new FakeService
        {
            RemoveResult = ApplicationResult<CustomerWishlistReadModel>.Failure(
                new ApplicationError(
                    "customer_wishlist.item_not_found",
                    "Wishlist item not found or access denied."))
        };
        var controller = CreateController(service, Guid.NewGuid(), Guid.NewGuid());

        var result = await controller.RemoveItem(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void Controller_RequiresCustomerOnlyPolicyAndExpectedRoutes()
    {
        var authorize = Assert.Single(
            typeof(CustomerWishlistController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("CustomerOnly", authorize.Policy);

        var route = Assert.Single(
            typeof(CustomerWishlistController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/v1/ecommerce/storefront/wishlist", route.Template);
        Assert.Equal(
            "items",
            Assert.Single(typeof(CustomerWishlistController)
                .GetMethod(nameof(CustomerWishlistController.AddItem))!
                .GetCustomAttributes<HttpPostAttribute>()).Template);
        Assert.Equal(
            "items/{itemId:guid}",
            Assert.Single(typeof(CustomerWishlistController)
                .GetMethod(nameof(CustomerWishlistController.RemoveItem))!
                .GetCustomAttributes<HttpDeleteAttribute>()).Template);
    }

    private static CustomerWishlistController CreateController(
        FakeService service,
        Guid? tenantId = null,
        Guid? customerId = null)
    {
        var context = new DefaultHttpContext();
        if (tenantId.HasValue && customerId.HasValue)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("tenant_id", tenantId.Value.ToString()),
                new Claim("sub", customerId.Value.ToString())
            ], "Test"));
        }

        return new CustomerWishlistController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static CustomerWishlistReadModel CreateWishlist(Guid customerId) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        Name = "My Wishlist",
        Items = []
    };

    private sealed class FakeService : ICustomerWishlistService
    {
        public ApplicationResult<CustomerWishlistReadModel> GetResult { get; init; } =
            ApplicationResult<CustomerWishlistReadModel>.Success(new CustomerWishlistReadModel());
        public ApplicationResult<CustomerWishlistReadModel> AddResult { get; init; } =
            ApplicationResult<CustomerWishlistReadModel>.Success(new CustomerWishlistReadModel());
        public ApplicationResult<CustomerWishlistReadModel> RemoveResult { get; init; } =
            ApplicationResult<CustomerWishlistReadModel>.Success(new CustomerWishlistReadModel());
        public ApplicationResult<CustomerWishlistReadModel> ClearResult { get; init; } =
            ApplicationResult<CustomerWishlistReadModel>.Success(new CustomerWishlistReadModel());
        public int CallCount { get; private set; }
        public Guid? TenantId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public AddCustomerWishlistItemRequest? AddRequest { get; private set; }

        public Task<ApplicationResult<CustomerWishlistReadModel>> GetAsync(
            Guid tenantId,
            Guid customerId,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            return Task.FromResult(GetResult);
        }

        public Task<ApplicationResult<CustomerWishlistReadModel>> AddItemAsync(
            Guid tenantId,
            Guid customerId,
            AddCustomerWishlistItemRequest request,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            AddRequest = request;
            return Task.FromResult(AddResult);
        }

        public Task<ApplicationResult<CustomerWishlistReadModel>> RemoveItemAsync(
            Guid tenantId,
            Guid customerId,
            Guid itemId,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            return Task.FromResult(RemoveResult);
        }

        public Task<ApplicationResult<CustomerWishlistReadModel>> ClearAsync(
            Guid tenantId,
            Guid customerId,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            return Task.FromResult(ClearResult);
        }

        private void Capture(Guid tenantId, Guid customerId)
        {
            CallCount++;
            TenantId = tenantId;
            CustomerId = customerId;
        }
    }
}
