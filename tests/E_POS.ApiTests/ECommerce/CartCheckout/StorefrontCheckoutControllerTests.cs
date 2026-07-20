using System.Security.Claims;
using E_POS.Api.Controllers.V1.ECommerce.Storefront;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.ECommerce.CartCheckout;

public sealed class StorefrontCheckoutControllerTests
{
    [Fact]
    public async Task CreateFromCart_ReadsCustomerClaimsAndCartSessionHeader()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var service = new FakeService();
        var controller = CreateController(service, tenantId, customerId, "cart-session");
        var request = new CreateStorefrontCheckoutFromCartRequest
        {
            SelectedOutletId = Guid.NewGuid()
        };

        var result = await controller.CreateFromCart(request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(customerId, service.CustomerId);
        Assert.Equal("cart-session", service.CartSessionId);
        Assert.Same(request, service.CreateRequest);
    }

    [Fact]
    public async Task Get_MissingCustomerClaims_ReturnsUnauthorizedWithoutCallingService()
    {
        var service = new FakeService();
        var controller = new StorefrontCheckoutController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(service.TenantId);
    }

    [Fact]
    public async Task Confirm_ExpiredSession_ReturnsConflictAndForwardsIdempotencyKey()
    {
        var service = new FakeService
        {
            Result = ApplicationResult<StorefrontCheckoutReadModel>.Failure(
                new ApplicationError("storefront_checkout.session_expired", "Expired."))
        };
        var controller = CreateController(service, Guid.NewGuid(), Guid.NewGuid(), "cart-session");

        var result = await controller.Confirm(
            Guid.NewGuid(), "confirm-key", CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("confirm-key", service.IdempotencyKey);
    }

    [Fact]
    public async Task UpdateCollection_ReadsCustomerClaimsAndForwardsRequest()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var service = new FakeService();
        var controller = CreateController(service, tenantId, customerId, "cart-session");
        var request = new UpdateStorefrontCheckoutCollectionRequest
        {
            SelectedOutletId = Guid.NewGuid(),
            RequestedCollectionAt = DateTimeOffset.UtcNow.AddHours(2)
        };

        var result = await controller.UpdateCollection(
            Guid.NewGuid(), request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal(customerId, service.CustomerId);
        Assert.Same(request, service.CollectionRequest);
    }

    private static StorefrontCheckoutController CreateController(
        FakeService service,
        Guid tenantId,
        Guid customerId,
        string cartSessionId)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("sub", customerId.ToString()),
                new Claim("identity_type", "customer")
            ], "test"))
        };
        httpContext.Request.Headers["X-Cart-Session-Id"] = cartSessionId;
        return new StorefrontCheckoutController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private sealed class FakeService : IStorefrontCheckoutService
    {
        public ApplicationResult<StorefrontCheckoutReadModel> Result { get; init; } =
            ApplicationResult<StorefrontCheckoutReadModel>.Success(new StorefrontCheckoutReadModel
            {
                Id = Guid.NewGuid()
            });
        public Guid? TenantId { get; private set; }
        public Guid? CustomerId { get; private set; }
        public string? CartSessionId { get; private set; }
        public CreateStorefrontCheckoutFromCartRequest? CreateRequest { get; private set; }
        public UpdateStorefrontCheckoutCollectionRequest? CollectionRequest { get; private set; }
        public string? IdempotencyKey { get; private set; }

        public Task<ApplicationResult<StorefrontCheckoutReadModel>> CreateFromCartAsync(
            Guid tenantId,
            Guid customerId,
            string? cartSessionId,
            CreateStorefrontCheckoutFromCartRequest request,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            CartSessionId = cartSessionId;
            CreateRequest = request;
            return Task.FromResult(Result);
        }

        public Task<ApplicationResult<StorefrontCheckoutReadModel>> GetAsync(
            Guid tenantId,
            Guid customerId,
            Guid checkoutSessionId,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            return Task.FromResult(Result);
        }

        public Task<ApplicationResult<StorefrontCheckoutReadModel>> UpdateCollectionAsync(
            Guid tenantId,
            Guid customerId,
            Guid checkoutSessionId,
            UpdateStorefrontCheckoutCollectionRequest request,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            CollectionRequest = request;
            return Task.FromResult(Result);
        }

        public Task<ApplicationResult<StorefrontCheckoutReadModel>> ConfirmAsync(
            Guid tenantId,
            Guid customerId,
            Guid checkoutSessionId,
            string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            Capture(tenantId, customerId);
            IdempotencyKey = idempotencyKey;
            return Task.FromResult(Result);
        }

        private void Capture(Guid tenantId, Guid customerId)
        {
            TenantId = tenantId;
            CustomerId = customerId;
        }
    }
}
