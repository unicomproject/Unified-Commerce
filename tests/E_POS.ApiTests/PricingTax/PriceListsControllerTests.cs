using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.PricingTax;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.PricingTax.Contracts;
using E_POS.Application.Modules.Tenant.PricingTax.Dtos;
using E_POS.Domain.Modules.Tenant.PricingTax.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PricingTax;

public sealed class PriceListsControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_WithTenantClaims_Returns201Created()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();

        var response = new PriceListResponse(
            priceListId, "PL-1", "Standard List", "POS", "USD", false, true, 0, null, null, "ACTIVE", [], [], Now, Now);

        var service = new FakePriceListService
        {
            CreateResult = ApplicationResult<PriceListResponse>.Success(response)
        };

        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, PricingTaxConstants.CreatePermission);

        var request = new PriceListCreateRequest(
            "PL-1", "Standard List", "POS", "USD", false, true, 0, null, null, "ACTIVE", null, null);

        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PriceListsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static PriceListsController CreateController(IPriceListService service)
    {
        return new PriceListsController(service, new TenantRequestContextFactory())
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

    private sealed class FakePriceListService : IPriceListService
    {
        public TenantRequestContext? Context { get; private set; }
        public ApplicationResult<PriceListResponse> CreateResult { get; init; } = ApplicationResult<PriceListResponse>.Failure(new ApplicationError("pricing.price_list.not_configured", "Not configured."));

        public Task<ApplicationResult<PriceListResponse>> CreateAsync(TenantRequestContext context, PriceListCreateRequest request, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<PriceListListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(ApplicationResult<PriceListListResponse>.Success(new PriceListListResponse([], pageNumber, pageSize, 0)));
        }

        public Task<ApplicationResult<PriceListResponse>> GetByIdAsync(TenantRequestContext context, Guid id, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<PriceListResponse>> UpdateAsync(TenantRequestContext context, Guid id, PriceListUpdateRequest request, CancellationToken cancellationToken)
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



