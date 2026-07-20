using E_POS.Api.Controllers.V1.ECommerce.Storefront;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CartCheckout.Contracts;
using E_POS.Application.Modules.ECommerce.CartCheckout.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.ECommerce.CartCheckout;

public sealed class StorefrontCartControllerTests
{
    [Fact]
    public async Task AddItem_ReadsTenantAndSessionHeadersAndReturnsDataEnvelope()
    {
        var tenantId = Guid.NewGuid();
        var service = new FakeService();
        var controller = CreateController(service, tenantId, "guest-session");
        var request = new AddStorefrontCartItemRequest { ProductId = Guid.NewGuid(), Quantity = 1m };

        var result = await controller.AddItem(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.TenantId);
        Assert.Equal("guest-session", service.SessionId);
        Assert.Same(request, service.AddRequest);
    }

    [Fact]
    public async Task UpdateItem_InsufficientStock_ReturnsConflict()
    {
        var service = new FakeService
        {
            Result = ApplicationResult<StorefrontCartReadModel>.Failure(
                new ApplicationError("storefront_cart.insufficient_stock", "Not enough stock."))
        };
        var controller = CreateController(service, Guid.NewGuid(), "guest-session");

        var result = await controller.UpdateItem(
            Guid.NewGuid(),
            new UpdateStorefrontCartItemRequest { Quantity = 100m },
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    private static StorefrontCartController CreateController(
        FakeService service, Guid tenantId, string sessionId)
    {
        var controller = new StorefrontCartController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers["X-Tenant-Id"] = tenantId.ToString();
        controller.Request.Headers["X-Cart-Session-Id"] = sessionId;
        return controller;
    }

    private sealed class FakeService : IStorefrontCartService
    {
        public ApplicationResult<StorefrontCartReadModel> Result { get; init; } =
            ApplicationResult<StorefrontCartReadModel>.Success(new StorefrontCartReadModel());
        public Guid? TenantId { get; private set; }
        public string? SessionId { get; private set; }
        public AddStorefrontCartItemRequest? AddRequest { get; private set; }

        public Task<ApplicationResult<StorefrontCartReadModel>> GetAsync(Guid tenantId, string? sessionId, CancellationToken cancellationToken)
            => Capture(tenantId, sessionId);

        public Task<ApplicationResult<StorefrontCartReadModel>> AddItemAsync(Guid tenantId, string? sessionId, AddStorefrontCartItemRequest request, CancellationToken cancellationToken)
        {
            AddRequest = request;
            return Capture(tenantId, sessionId);
        }

        public Task<ApplicationResult<StorefrontCartReadModel>> UpdateItemAsync(Guid tenantId, string? sessionId, Guid itemId, UpdateStorefrontCartItemRequest request, CancellationToken cancellationToken)
            => Capture(tenantId, sessionId);

        public Task<ApplicationResult<StorefrontCartReadModel>> RemoveItemAsync(Guid tenantId, string? sessionId, Guid itemId, CancellationToken cancellationToken)
            => Capture(tenantId, sessionId);

        public Task<ApplicationResult<StorefrontCartReadModel>> ClearAsync(Guid tenantId, string? sessionId, CancellationToken cancellationToken)
            => Capture(tenantId, sessionId);

        private Task<ApplicationResult<StorefrontCartReadModel>> Capture(Guid tenantId, string? sessionId)
        {
            TenantId = tenantId;
            SessionId = sessionId;
            return Task.FromResult(Result);
        }
    }
}
