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

public sealed class StockInControllerTests
{
    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(
            typeof(StockInController).GetCustomAttributes<AuthorizeAttribute>(true));
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public async Task StockIn_WithValidClaims_ReturnsCreated()
    {
        var response = new StockInResponse(Guid.NewGuid(), Guid.NewGuid(), "StockIn", null, [], DateTimeOffset.UtcNow);
        var service = new FakeCurrentStockService
        {
            StockInResult = ApplicationResult<StockInResponse>.Success(response)
        };
        var controller = CreateController(currentStockService: service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "tenant.stock.edit");

        var result = await controller.StockIn(new StockInRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(created.Value);
    }

    [Fact]
    public async Task StockIn_WithoutClaims_ReturnsUnauthorized()
    {
        var service = new FakeCurrentStockService();
        var controller = CreateController(currentStockService: service);

        var result = await controller.StockIn(new StockInRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    private static StockInController CreateController(FakeCurrentStockService currentStockService)
    {
        var controller = new StockInController(
            currentStockService,
            new FakeTenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static void SetTenantClaims(
        StockInController controller,
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
        public ApplicationResult<StockInResponse> StockInResult { get; init; } =
            ApplicationResult<StockInResponse>.Success(new StockInResponse(Guid.NewGuid(), Guid.NewGuid(), "StockIn", null, [], DateTimeOffset.UtcNow));

        public Task<ApplicationResult<CurrentStockSummaryResponse>> GetCurrentStockSummaryAsync(TenantRequestContext context, Guid? outletId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<CurrentStockListResponse>> GetCurrentStockAsync(TenantRequestContext context, CurrentStockQuery query, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<byte[]>> ExportCurrentStockAsync(TenantRequestContext context, CurrentStockQuery query, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<StockInResponse>> StockInAsync(TenantRequestContext context, StockInRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(StockInResult);
        }

        public Task<ApplicationResult<ProductStockDetailResponse>> GetProductStockDetailAsync(TenantRequestContext context, Guid productVariantId, Guid? outletId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ApplicationResult<StockMovementHistoryListResponse>> GetStockMovementHistoryAsync(TenantRequestContext context, StockMovementHistoryQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public Task<ApplicationResult<StockAdjustmentResponse>> AdjustStockAsync(TenantRequestContext context, StockAdjustmentRequest request, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<StockAdjustmentResponse>.Success(new StockAdjustmentResponse { StockMovementId = Guid.NewGuid(), OutletId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow }));
        public Task<ApplicationResult<StockTransferResponse>> TransferStockAsync(TenantRequestContext context, StockTransferRequest request, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<StockTransferResponse>.Success(new StockTransferResponse { StockMovementId = Guid.NewGuid(), SourceOutletId = Guid.NewGuid(), DestinationOutletId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow }));
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
