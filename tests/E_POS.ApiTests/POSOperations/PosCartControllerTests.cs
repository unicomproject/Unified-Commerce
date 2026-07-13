using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.POSOperations;

public sealed class PosCartControllerTests
{
    [Fact]
    public async Task Calculate_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var summary = new PosCheckoutSummaryResponseDto(
            new PosCheckoutBillingSummaryDto(1, 1200, 0, 0, 1200, "LKR"),
            new PosCheckoutSaleDetailsDto("New Sale", 1, DateTimeOffset.UtcNow, "Cashier"),
            ["cash"],
            []);
        var service = new FakePosCheckoutService(
            ApplicationResult<PosCheckoutSummaryResponseDto>.Success(summary));
        var controller = CreateController(service, tenantId, SalesPermissions.Cart.UpdateItem);
        var request = new PosCheckoutSummaryRequestDto(
            deviceId,
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)]);

        var result = await controller.Calculate(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(deviceId, service.Request?.DeviceId);
    }

    [Fact]
    public async Task Calculate_WhenPermissionDenied_ReturnsForbidden()
    {
        var service = new FakePosCheckoutService(
            ApplicationResult<PosCheckoutSummaryResponseDto>.Failure(
                new ApplicationError("pos_cart.permission_denied", "Permission denied.")));
        var controller = CreateController(service, Guid.NewGuid(), "products.view");
        var request = new PosCheckoutSummaryRequestDto(
            Guid.NewGuid(),
            "NewSale",
            null,
            [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)]);

        var result = await controller.Calculate(request, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PosCartController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static PosCartController CreateController(
        FakePosCheckoutService service,
        Guid tenantId,
        params string[] permissions)
    {
        var controller = new PosCartController(service, new TenantRequestContextFactory());
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("tenant_id", tenantId.ToString())
        };
        claims.AddRange(permissions.Select(x => new Claim("permissions", x)));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        return controller;
    }

    private sealed class FakePosCheckoutService : IPosCheckoutService
    {
        private readonly ApplicationResult<PosCheckoutSummaryResponseDto> _result;

        public FakePosCheckoutService(ApplicationResult<PosCheckoutSummaryResponseDto> result) =>
            _result = result;

        public TenantRequestContext? Context { get; private set; }
        public PosCheckoutSummaryRequestDto? Request { get; private set; }

        public Task<ApplicationResult<PosCheckoutSummaryResponseDto>> CalculateCartAsync(
            TenantRequestContext context,
            PosCheckoutSummaryRequestDto request,
            CancellationToken cancellationToken)
        {
            Context = context;
            Request = request;
            return Task.FromResult(_result);
        }

        public Task<ApplicationResult<PosCheckoutSummaryResponseDto>> GetSummaryAsync(
            TenantRequestContext context,
            PosCheckoutSummaryRequestDto request,
            CancellationToken cancellationToken) => Task.FromResult(_result);

        public Task<ApplicationResult<PosCheckoutStartPaymentResponseDto>> StartPaymentAsync(
            TenantRequestContext context,
            PosCheckoutStartPaymentRequestDto request,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
