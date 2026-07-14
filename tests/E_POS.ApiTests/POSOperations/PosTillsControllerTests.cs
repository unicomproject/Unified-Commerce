using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.POSOperations;

public sealed class PosTillsControllerTests
{
    [Fact]
    public async Task GetCurrentSession_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var response = CreateResponse(deviceId);

        var service = new FakePosTillSessionService
        {
            GetCurrentSessionResult = ApplicationResult<CurrentTillSessionResponseDto>.Success(response),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "pos.till.open");

        var result = await controller.GetCurrentSession(deviceId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(deviceId, service.DeviceId);
    }

    [Fact]
    public async Task GetCurrentSession_WhenNoOpenSession_ReturnsNotFound()
    {
        var service = new FakePosTillSessionService
        {
            GetCurrentSessionResult = ApplicationResult<CurrentTillSessionResponseDto>.Failure(
                new ApplicationError("till_session.not_found", "No open till session was found for this device.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "pos.till.open");

        var result = await controller.GetCurrentSession(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task OpenTill_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var response = CreateResponse(deviceId);
        var service = new FakePosTillSessionService
        {
            OpenTillResult = ApplicationResult<CurrentTillSessionResponseDto>.Success(response),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "pos.till.open");

        var result = await controller.OpenTill(
            new OpenTillRequest(deviceId, tillId, 150m, "Morning shift"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(150m, service.OpenTillRequest?.OpeningFloat);
    }

    [Fact]
    public async Task OpenTill_WhenSessionAlreadyOpen_ReturnsConflict()
    {
        var service = new FakePosTillSessionService
        {
            OpenTillResult = ApplicationResult<CurrentTillSessionResponseDto>.Failure(
                new ApplicationError("till_session.already_open", "An open till session already exists for this till.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "pos.till.open");

        var result = await controller.OpenTill(
            new OpenTillRequest(Guid.NewGuid(), Guid.NewGuid(), 0m, null),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task OpenTill_WhenPermissionDenied_ReturnsForbidden()
    {
        var service = new FakePosTillSessionService
        {
            OpenTillResult = ApplicationResult<CurrentTillSessionResponseDto>.Failure(
                new ApplicationError("till_session.permission_denied", "Permission denied.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "pos.home.view");

        var result = await controller.OpenTill(
            new OpenTillRequest(Guid.NewGuid(), Guid.NewGuid(), 0m, null),
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetCurrentSession_WhenPermissionDenied_ReturnsForbidden()
    {
        var service = new FakePosTillSessionService
        {
            GetCurrentSessionResult = ApplicationResult<CurrentTillSessionResponseDto>.Failure(
                new ApplicationError("till_session.permission_denied", "Permission denied.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "pos.home.view");

        var result = await controller.GetCurrentSession(Guid.NewGuid(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task CloseTill_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var response = CreateCloseResponse(tillId);
        var service = new FakePosTillSessionService
        {
            CloseTillResult = ApplicationResult<CloseTillResponseDto>.Success(response),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, "pos.till.close");

        var result = await controller.CloseTill(
            new CloseTillRequest(deviceId, tillId, 500m, 480m, null, "End of shift"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(500m, service.CloseTillRequest?.CountedCash);
    }

    [Fact]
    public async Task CloseTill_WhenNoOpenSession_ReturnsNotFound()
    {
        var service = new FakePosTillSessionService
        {
            CloseTillResult = ApplicationResult<CloseTillResponseDto>.Failure(
                new ApplicationError("till_session.not_open", "No open till session was found to close.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "pos.till.close");

        var result = await controller.CloseTill(
            new CloseTillRequest(Guid.NewGuid(), Guid.NewGuid(), 100m, 100m, null, null),
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PosTillsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static PosTillsController CreateController(FakePosTillSessionService service)
    {
        var controller = new PosTillsController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetTenantClaims(PosTillsController controller, Guid tenantId, Guid userId, string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim("permissions", permission)
            ],
            "Test"));
    }

    private static CurrentTillSessionResponseDto CreateResponse(Guid deviceId) =>
        new(new CurrentTillSessionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            deviceId,
            150m,
            "open",
            DateTimeOffset.UtcNow,
            "Morning shift"));

    private static CloseTillResponseDto CreateCloseResponse(Guid tillId) =>
        new(new ClosedTillSessionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            tillId,
            150m,
            480m,
            500m,
            20m,
            "closed",
            DateTimeOffset.UtcNow.AddHours(-8),
            DateTimeOffset.UtcNow,
            "End of shift"));

    private sealed class FakePosTillSessionService : IPosTillSessionService
    {
        public ApplicationResult<CurrentTillSessionResponseDto> GetCurrentSessionResult { get; init; } =
            ApplicationResult<CurrentTillSessionResponseDto>.Failure(
                new ApplicationError("till_session.not_found", "No open till session was found for this device."));

        public ApplicationResult<CurrentTillSessionResponseDto> OpenTillResult { get; init; } =
            ApplicationResult<CurrentTillSessionResponseDto>.Failure(
                new ApplicationError("till_session.open_failed", "Till could not be opened."));

        public ApplicationResult<CloseTillResponseDto> CloseTillResult { get; init; } =
            ApplicationResult<CloseTillResponseDto>.Failure(
                new ApplicationError("till_session.close_failed", "Till could not be closed."));

        public TenantRequestContext? Context { get; private set; }
        public Guid? DeviceId { get; private set; }
        public OpenTillRequest? OpenTillRequest { get; private set; }
        public CloseTillRequest? CloseTillRequest { get; private set; }

        public Task<ApplicationResult<CurrentTillSessionResponseDto>> GetCurrentSessionAsync(
            TenantRequestContext context,
            Guid? deviceId,
            CancellationToken cancellationToken)
        {
            Context = context;
            DeviceId = deviceId;
            return Task.FromResult(GetCurrentSessionResult);
        }

        public Task<ApplicationResult<CurrentTillSessionResponseDto>> OpenTillAsync(
            TenantRequestContext context,
            OpenTillRequest request,
            CancellationToken cancellationToken)
        {
            Context = context;
            OpenTillRequest = request;
            return Task.FromResult(OpenTillResult);
        }

        public Task<ApplicationResult<CloseTillResponseDto>> CloseTillAsync(
            TenantRequestContext context,
            CloseTillRequest request,
            CancellationToken cancellationToken)
        {
            Context = context;
            CloseTillRequest = request;
            return Task.FromResult(CloseTillResult);
        }
    }
}
