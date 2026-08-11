using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers.V1.Tenant.Inventory;
using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Contracts.Services;
using E_POS.Application.Modules.Tenant.Inventory.CurrentStock.Dtos;
using E_POS.Application.Modules.Tenant.Inventory.StockIn.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.Inventory;

public sealed class CurrentStockControllerTests
{
    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(
            typeof(CurrentStockController).GetCustomAttributes<AuthorizeAttribute>(true));
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public async Task GetCurrentStockSummary_WithValidClaims_ReturnsOk()
    {
        var summary = new CurrentStockSummaryResponse(10, 5, 2, 5000m);
        var service = new FakeCurrentStockService
        {
            SummaryResult = ApplicationResult<CurrentStockSummaryResponse>.Success(summary)
        };
        var controller = CreateController(currentStockService: service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.stock.view");

        var result = await controller.GetCurrentStockSummary(null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetCurrentStock_WithoutClaims_ReturnsUnauthorized()
    {
        var service = new FakeCurrentStockService();
        var controller = CreateController(currentStockService: service);

        var result = await controller.GetCurrentStock(new CurrentStockQuery(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    private static CurrentStockController CreateController(FakeCurrentStockService currentStockService)
    {
        var controller = new CurrentStockController(
            currentStockService,
            new FakeTenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static void SetTenantClaims(
        CurrentStockController controller,
        Guid tenantId,
        Guid userId,
        string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission),
            ],
            "Test"));
    }

    private sealed class FakeCurrentStockService : ICurrentStockService
    {
        public ApplicationResult<CurrentStockSummaryResponse> SummaryResult { get; init; } =
            ApplicationResult<CurrentStockSummaryResponse>.Success(new CurrentStockSummaryResponse(0, 0, 0, 0));

        public Task<ApplicationResult<CurrentStockSummaryResponse>> GetCurrentStockSummaryAsync(TenantRequestContext context, Guid? outletId, CancellationToken cancellationToken)
        {
            return Task.FromResult(SummaryResult);
        }

        public Task<ApplicationResult<CurrentStockListResponse>> GetCurrentStockAsync(TenantRequestContext context, CurrentStockQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<CurrentStockListResponse>.Success(new CurrentStockListResponse([], 0, 1, 10)));
        }

        public Task<ApplicationResult<byte[]>> ExportCurrentStockAsync(TenantRequestContext context, CurrentStockQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<byte[]>.Success(Array.Empty<byte>()));
        }

        public Task<ApplicationResult<ProductStockDetailResponse>> GetProductStockDetailAsync(TenantRequestContext context, Guid productVariantId, Guid? outletId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationResult<StockInResponse>> StockInAsync(TenantRequestContext context, StockInRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationResult<StockMovementHistoryListResponse>> GetStockMovementHistoryAsync(TenantRequestContext context, StockMovementHistoryQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeTenantRequestContextFactory : ITenantRequestContextFactory
    {
        public bool TryCreate(ClaimsPrincipal user, out TenantRequestContext context)
        {
            var tenantUserIdValue = user.FindFirstValue("sub");
            var tenantIdValue = user.FindFirstValue("tenant_id");
            var hasTenantUserId = Guid.TryParse(tenantUserIdValue, out var tenantUserId);
            var hasTenantId = Guid.TryParse(tenantIdValue, out var tenantId);

            if (!hasTenantUserId || !hasTenantId)
            {
                context = new TenantRequestContext(Guid.Empty, Guid.Empty, []);
                return false;
            }

            var permissions = user.FindAll("permissions")
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            context = new TenantRequestContext(tenantId, tenantUserId, permissions);
            return true;
        }
    }
}
