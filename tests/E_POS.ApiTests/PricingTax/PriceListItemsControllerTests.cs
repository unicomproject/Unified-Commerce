using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.PricingTax;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PricingTax.Contracts;
using E_POS.Application.Modules.PricingTax.Dtos;
using E_POS.Domain.Modules.PricingTax.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PricingTax;

public sealed class PriceListItemsControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_WithTenantClaims_Returns201Created()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var response = new PriceListItemResponse(
            itemId, Guid.NewGuid(), Guid.NewGuid(), null, null, 10m, 15m, 1m, null, null, "ACTIVE", Now, Now);

        var service = new FakePriceListItemsService
        {
            CreateResult = ApplicationResult<PriceListItemResponse>.Success(response)
        };

        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, PricingTaxConstants.UpdatePermission);

        var request = new PriceListItemCreateRequest(
            response.PriceListId, response.ProductId, null, null, 10m, 15m, 1m, null, null, "ACTIVE");

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PriceListItemsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static PriceListItemsController CreateController(IPriceListItemsService service)
    {
        return new PriceListItemsController(service, new TenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static void SetTenantClaims(ControllerBase controller, Guid tenantId, Guid userId, string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("permissions", permission)
        ], "Test"));
    }

    private sealed class FakePriceListItemsService : IPriceListItemsService
    {
        public TenantRequestContext? Context { get; private set; }
        public ApplicationResult<PriceListItemResponse> CreateResult { get; init; } = ApplicationResult<PriceListItemResponse>.Failure(new ApplicationError("pricing.price_list_item.not_configured", "Not configured."));

        public Task<ApplicationResult<PriceListItemResponse>> CreateAsync(TenantRequestContext context, PriceListItemCreateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<PriceListItemListResponse>> ListAsync(TenantRequestContext context, Guid priceListId, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult<PriceListItemListResponse>.Success(new PriceListItemListResponse([], pageNumber, pageSize, 0)));
        }

        public Task<ApplicationResult<PriceListItemResponse>> GetByIdAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<PriceListItemResponse>> UpdateAsync(TenantRequestContext context, Guid id, PriceListItemUpdateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<Guid>> DeleteAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult<Guid>.Success(id));
        }
    }
}
