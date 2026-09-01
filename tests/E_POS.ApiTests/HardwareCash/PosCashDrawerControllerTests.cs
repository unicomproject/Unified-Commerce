using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.HardwareCash;

public sealed class PosCashDrawerControllerTests
{
    [Fact]
    public void Controllers_RequireTenantOnlyPolicy()
    {
        var drawerAuth = Assert.Single(
            typeof(PosCashDrawerController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", drawerAuth.Policy);

        var typesAuth = Assert.Single(
            typeof(PosCashMovementTypesController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", typesAuth.Policy);
    }

    [Fact]
    public async Task MovementTypes_Get_WhenSuccess_ReturnsOk()
    {
        var service = new FakeDrawerService();
        var controller = CreateMovementTypesController(service);
        SetClaims(controller);

        var result = await controller.Get("IN", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task MovementTypes_Get_WhenPermissionDenied_Returns403()
    {
        var service = new FakeDrawerService
        {
            MovementTypesResult = ApplicationResult<IReadOnlyList<PosCashMovementTypeDto>>.Failure(
                new ApplicationError("cash_drawer.permission_denied", "Permission denied."))
        };
        var controller = CreateMovementTypesController(service);
        SetClaims(controller);

        var result = await controller.Get("IN", CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
    }

    [Fact]
    public async Task CreateMovement_WhenSuccess_ReturnsOk()
    {
        var service = new FakeDrawerService();
        var controller = CreateDrawerController(service);
        SetClaims(controller);

        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000m, "Float");
        var result = await controller.CreateMovement(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateMovement_WhenPermissionDenied_Returns403()
    {
        var service = new FakeDrawerService
        {
            CreateMovementResult = ApplicationResult<PosCashDrawerMovementDto>.Failure(
                new ApplicationError("cash_drawer.permission_denied", "Permission denied."))
        };
        var controller = CreateDrawerController(service);
        SetClaims(controller);

        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000m, "Float");
        var result = await controller.CreateMovement(request, CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
    }

    [Fact]
    public async Task CreateMovement_WhenIdempotencyConflict_Returns409()
    {
        var service = new FakeDrawerService
        {
            CreateMovementResult = ApplicationResult<PosCashDrawerMovementDto>.Failure(
                new ApplicationError("cash_drawer.idempotency_conflict", "Conflict."))
        };
        var controller = CreateDrawerController(service);
        SetClaims(controller);

        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000m, "Float");
        var result = await controller.CreateMovement(request, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflict.Value);
    }

    [Fact]
    public async Task CreateMovement_WhenInvalidAmount_Returns400()
    {
        var service = new FakeDrawerService
        {
            CreateMovementResult = ApplicationResult<PosCashDrawerMovementDto>.Failure(
                new ApplicationError("cash_drawer.invalid_amount", "Amount must be greater than zero."))
        };
        var controller = CreateDrawerController(service);
        SetClaims(controller);

        var request = new CreatePosCashMovementRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, "Float");
        var result = await controller.CreateMovement(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static PosCashMovementTypesController CreateMovementTypesController(IPosDrawerService service)
    {
        var controller = new PosCashMovementTypesController(service, new TenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private static PosCashDrawerController CreateDrawerController(IPosDrawerService service)
    {
        var controller = new PosCashDrawerController(service, new TenantRequestContextFactory())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private static void SetClaims(ControllerBase controller)
    {
        controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("sub", Guid.NewGuid().ToString()),
                    new Claim("tenant_id", Guid.NewGuid().ToString()),
                    new Claim("permissions", "cash_drawer.view,cash_drawer.movement.create")
                ],
                "Test"));
    }

    private sealed class FakeDrawerService : IPosDrawerService
    {
        public ApplicationResult<IReadOnlyList<PosCashMovementTypeDto>> MovementTypesResult { get; init; } =
            ApplicationResult<IReadOnlyList<PosCashMovementTypeDto>>.Success(
                [new PosCashMovementTypeDto(Guid.NewGuid(), "FLOAT_ADDED", "Float Added", "IN", false, true)]);

        public ApplicationResult<PosCashDrawerMovementDto> CreateMovementResult { get; init; } =
            ApplicationResult<PosCashDrawerMovementDto>.Success(
                new PosCashDrawerMovementDto(
                    Guid.NewGuid(), "FLOAT_ADDED", "IN", 1000m, "LKR", "Float", null, "Cashier", DateTimeOffset.UtcNow));

        public Task<ApplicationResult<CashDrawerOperationDto>> RegisterOperationAsync(
            TenantRequestContext context, RegisterDrawerOperationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<CashDrawerOperationDto>> FinalizeOperationAsync(
            TenantRequestContext context, Guid operationId, FinalizeDrawerOperationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<CashDrawerOperationDto>> ManualOpenDrawerAsync(
            TenantRequestContext context, ManualOpenDrawerRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<IReadOnlyList<CashDrawerOperationDto>>> GetHistoryAsync(
            TenantRequestContext context, Guid posDeviceId, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<CashDrawerOperationDto>> GetOperationStatusAsync(
            TenantRequestContext context, Guid operationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<CashDrawerOperationDto>> GetOperationStatusByRequestIdAsync(
            TenantRequestContext context, Guid requestId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<PosCashDrawerSummaryDto>> GetFinancialSummaryAsync(
            TenantRequestContext context, Guid deviceId, CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PosCashDrawerSummaryDto>.Success(
                new PosCashDrawerSummaryDto(
                    Guid.NewGuid(), Guid.NewGuid(), "Till", "OPEN", "LKR", 1000m, 0m, 0m, 0m, 0m, 0m, 1000m, "Cashier", DateTimeOffset.UtcNow)));

        public Task<ApplicationResult<PosCashDrawerMovementPageDto>> GetFinancialMovementsAsync(
            TenantRequestContext context, Guid deviceId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PosCashDrawerMovementPageDto>.Success(
                new PosCashDrawerMovementPageDto([], 1, 25, 0, 0)));

        public Task<ApplicationResult<IReadOnlyList<PosCashMovementTypeDto>>> GetMovementTypesAsync(
            TenantRequestContext context, string direction, CancellationToken cancellationToken) =>
            Task.FromResult(MovementTypesResult);

        public Task<ApplicationResult<PosCashDrawerMovementDto>> CreateFinancialMovementAsync(
            TenantRequestContext context, CreatePosCashMovementRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(CreateMovementResult);
    }
}
