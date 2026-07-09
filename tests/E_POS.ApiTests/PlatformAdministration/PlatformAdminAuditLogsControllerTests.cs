using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformAdminAuditLogsControllerTests
{
    private static readonly Guid PlatformUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAuditLogs_WithAuthenticatedUserAndPermission_ReturnsLegacyApiResponse()
    {
        var controller = CreateController(
            new FakePlatformAuditLogService(
                ApplicationResult<PlatformAuditLogListResponse>.Success(CreateResponse())),
            PlatformUserId);

        var result = await controller.GetAuditLogs(new PlatformAuditLogListQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformAuditLogListResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("platform_login_security", payload.Data.AuditScope);
        Assert.Single(payload.Data.Items);
        Assert.Null(payload.Data.Items[0].IpAddress);
        Assert.Null(payload.Data.Items[0].UserAgent);
        Assert.Equal("platform.login.success", payload.Data.Items[0].Action);
        Assert.Equal("Platform login succeeded.", payload.Data.Items[0].Summary);
    }

    [Fact]
    public async Task GetAuditLogs_WithoutPermission_ReturnsForbidden()
    {
        var controller = CreateController(
            new FakePlatformAuditLogService(
                ApplicationResult<PlatformAuditLogListResponse>.Failure(new ApplicationError(
                    "platform_audit.access_denied",
                    "Platform audit log access denied."))),
            PlatformUserId);

        var result = await controller.GetAuditLogs(new PlatformAuditLogListQuery(), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_ResponseShape_RemainsUnchanged()
    {
        var response = CreateResponse();
        var controller = CreateController(
            new FakePlatformAuditLogService(ApplicationResult<PlatformAuditLogListResponse>.Success(response)),
            PlatformUserId);

        var result = await controller.GetAuditLogs(new PlatformAuditLogListQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformAuditLogListResponse>>(ok.Value);

        Assert.Equal(response.AuditScope, payload.Data.AuditScope);
        Assert.Equal(response.AuditScopeDescription, payload.Data.AuditScopeDescription);
        Assert.Equal(response.PageNumber, payload.Data.PageNumber);
        Assert.Equal(response.PageSize, payload.Data.PageSize);
        Assert.Equal(response.TotalCount, payload.Data.TotalCount);
        Assert.Equal(response.TotalPages, payload.Data.TotalPages);

        var item = payload.Data.Items[0];
        var expected = response.Items[0];
        Assert.Equal(expected.Id, item.Id);
        Assert.Equal(expected.OccurredAt, item.OccurredAt);
        Assert.Equal(expected.Actor.PlatformUserId, item.Actor.PlatformUserId);
        Assert.Equal(expected.Actor.Email, item.Actor.Email);
        Assert.Equal(expected.Action, item.Action);
        Assert.Equal(expected.Area, item.Area);
        Assert.Equal(expected.EntityType, item.EntityType);
        Assert.Equal(expected.EntityId, item.EntityId);
        Assert.Equal(expected.Summary, item.Summary);
        Assert.Null(item.IpAddress);
        Assert.Null(item.UserAgent);
    }

    [Fact]
    public async Task GetAuditLogs_WithInvalidDateRange_ReturnsBadRequest()
    {
        var controller = CreateController(
            new FakePlatformAuditLogService(
                ApplicationResult<PlatformAuditLogListResponse>.Failure(new ApplicationError(
                    "platform_audit.validation_failed",
                    "The audit log date range is invalid."))),
            PlatformUserId);

        var result = await controller.GetAuditLogs(
            new PlatformAuditLogListQuery
            {
                From = Now,
                To = Now.AddDays(-1)
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new FakePlatformAuditLogService(
                ApplicationResult<PlatformAuditLogListResponse>.Success(CreateResponse())),
            platformUserId: null);

        var result = await controller.GetAuditLogs(new PlatformAuditLogListQuery(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void AuditLogsEndpoint_RequiresPlatformOnlyPolicy()
    {
        var controllerAuthorize = typeof(PlatformAdminAuditLogsController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("PlatformOnly", controllerAuthorize!.Policy);
    }

    private static PlatformAdminAuditLogsController CreateController(
        FakePlatformAuditLogService service,
        Guid? platformUserId)
    {
        var controller = new PlatformAdminAuditLogsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (platformUserId is not null)
        {
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", platformUserId.Value.ToString())],
                "Test"));
        }

        return controller;
    }

    private static PlatformAuditLogListResponse CreateResponse()
    {
        return new PlatformAuditLogListResponse(
            "platform_login_security",
            "Platform login and authentication security events from platform_login_audits. Generic business audit logs are not available in Release 1.",
            [
                new PlatformAuditLogListItemDto(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Now,
                    new PlatformAuditLogActorDto(PlatformUserId, "admin@nytroz.local"),
                    "platform.login.success",
                    "platform_auth",
                    "platform_user",
                    PlatformUserId,
                    "Platform login succeeded.",
                    null,
                    null)
            ],
            1,
            20,
            1,
            1);
    }

    private sealed class FakePlatformAuditLogService : IPlatformAuditLogService
    {
        private readonly ApplicationResult<PlatformAuditLogListResponse> _result;

        public FakePlatformAuditLogService(ApplicationResult<PlatformAuditLogListResponse> result)
        {
            _result = result;
        }

        public Task<ApplicationResult<PlatformAuditLogListResponse>> GetAuditLogsAsync(
            PlatformAuditLogListQuery query,
            Guid platformUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }
}

