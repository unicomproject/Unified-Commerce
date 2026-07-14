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

public sealed class PosHoldsControllerTests
{
    [Fact]
    public async Task CancelHold_WhenSuccessful_ReturnsNoContent()
    {
        var service = new FakePosHoldService
        {
            CancelResult = ApplicationResult<bool>.Success(true)
        };
        var controller = CreateController(service);
        SetClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Park.Create);

        var result = await controller.CancelHold(
            Guid.NewGuid(), "Customer left", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task CancelHold_WhenAlreadyRecalled_ReturnsConflict()
    {
        var service = new FakePosHoldService
        {
            CancelResult = ApplicationResult<bool>.Failure(
                new ApplicationError("pos_holds.not_cancellable", "Not cancellable."))
        };
        var controller = CreateController(service);
        SetClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Park.Create);

        var result = await controller.CancelHold(
            Guid.NewGuid(), null, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task RecallHold_WithValidClaims_ReturnsOk()
    {
        var holdId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var service = new FakePosHoldService
        {
            RecallResult = ApplicationResult<PosRecallHoldResponseDto>.Success(
                new PosRecallHoldResponseDto(
                    holdId, Guid.NewGuid(), "HOLD-000001", deviceId, null, null,
                    "NewSale", null, DateTimeOffset.UtcNow, [],
                    new PosCheckoutSummaryResponseDto(
                        new PosCheckoutBillingSummaryDto(0, 0, 0, 0, 0, "LKR"),
                        new PosCheckoutSaleDetailsDto("New Sale", 0, DateTimeOffset.UtcNow, "Cashier"),
                        [], [])))
        };
        var controller = CreateController(service);
        SetClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Park.Recall);

        var result = await controller.RecallHold(
            holdId, new PosRecallHoldRequestDto(deviceId), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateHold_WithValidClaims_ReturnsCreated()
    {
        var hold = CreateHoldItem();
        var service = new FakePosHoldService
        {
            CreateResult = ApplicationResult<PosHoldListItemDto>.Success(hold)
        };
        var controller = CreateController(service);
        SetClaims(controller, Guid.NewGuid(), Guid.NewGuid(), SalesPermissions.Park.Create);

        var result = await controller.CreateHold(
            new PosCreateHoldRequestDto(
                Guid.NewGuid(), "NewSale", null,
                [new PosCheckoutLineRequestDto(Guid.NewGuid(), 1)],
                "Customer will return", null, "hold-key"),
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
    }

    [Fact]
    public async Task GetHolds_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new FakePosHoldService
        {
            Result = ApplicationResult<PosHoldListResponseDto>.Success(
                new PosHoldListResponseDto(Array.Empty<PosHoldListItemDto>(), 0))
        };
        var controller = CreateController(service);
        SetClaims(controller, tenantId, userId, SalesPermissions.Park.View);

        var result = await controller.GetHolds(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
    }

    [Fact]
    public async Task GetHolds_WhenPermissionDenied_ReturnsForbidden()
    {
        var service = new FakePosHoldService
        {
            Result = ApplicationResult<PosHoldListResponseDto>.Failure(
                new ApplicationError("pos_holds.permission_denied", "Denied."))
        };
        var controller = CreateController(service);
        SetClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "products.view");

        var result = await controller.GetHolds(CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task GetHolds_WhenTenantClaimsMissing_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakePosHoldService());

        var result = await controller.GetHolds(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Controller_UsesExpectedRouteAndTenantPolicy()
    {
        var route = Assert.Single(typeof(PosHoldsController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/v1/pos/holds", route.Template);
        var authorize = Assert.Single(typeof(PosHoldsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static PosHoldsController CreateController(FakePosHoldService service)
    {
        var controller = new PosHoldsController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetClaims(
        PosHoldsController controller,
        Guid tenantId,
        Guid userId,
        string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission)
            ],
            "Test"));
    }

    private sealed class FakePosHoldService : IPosHoldService
    {
        public ApplicationResult<bool> CancelResult { get; init; } =
            ApplicationResult<bool>.Failure(
                new ApplicationError("pos_holds.cancel_failed", "Failed."));
        public ApplicationResult<PosRecallHoldResponseDto> RecallResult { get; init; } =
            ApplicationResult<PosRecallHoldResponseDto>.Failure(
                new ApplicationError("pos_holds.recall_failed", "Failed."));
        public ApplicationResult<PosHoldListItemDto> CreateResult { get; init; } =
            ApplicationResult<PosHoldListItemDto>.Failure(
                new ApplicationError("pos_holds.failed", "Failed."));
        public ApplicationResult<PosHoldListResponseDto> Result { get; init; } =
            ApplicationResult<PosHoldListResponseDto>.Failure(
                new ApplicationError("pos_holds.failed", "Failed."));
        public TenantRequestContext? Context { get; private set; }

        public Task<ApplicationResult<bool>> CancelHoldAsync(
            TenantRequestContext context,
            Guid holdId,
            string? reason,
            CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CancelResult);
        }

        public Task<ApplicationResult<PosRecallHoldResponseDto>> RecallHoldAsync(
            TenantRequestContext context,
            Guid holdId,
            PosRecallHoldRequestDto request,
            CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(RecallResult);
        }

        public Task<ApplicationResult<PosHoldListItemDto>> CreateHoldAsync(
            TenantRequestContext context,
            PosCreateHoldRequestDto request,
            CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<PosHoldListResponseDto>> GetHoldsAsync(
            TenantRequestContext context,
            CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(Result);
        }
    }

    private static PosHoldListItemDto CreateHoldItem() => new(
        Guid.NewGuid(), "HOLD-000001", Guid.NewGuid(), "SO-000001",
        Guid.NewGuid(), Guid.NewGuid(), null, null, "Customer will return",
        "held", 1, 100, 0, 0, 100, "LKR", DateTimeOffset.UtcNow, null,
        Array.Empty<PosHoldLineDto>());
}
