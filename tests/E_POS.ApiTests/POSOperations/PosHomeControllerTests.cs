using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.POSOperations;

public sealed class PosHomeControllerTests
{
    [Fact]
    public async Task GetPosHome_ForwardsDeviceContextAndCancellationToken()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var outletId = Guid.NewGuid();
        var tillId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        const string deviceFingerprint = "  browser-installation-fingerprint  ";
        using var cancellationTokenSource = new CancellationTokenSource();
        var service = new CapturingPosHomeDashboardService();
        var controller = CreateController(service, tenantId, userId);

        await controller.GetPosHome(
            outletId,
            tillId,
            deviceId,
            deviceFingerprint,
            cancellationTokenSource.Token);

        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
        Assert.Equal(outletId, service.OutletId);
        Assert.Equal(tillId, service.TillId);
        Assert.Equal(deviceId, service.DeviceId);
        Assert.Equal(deviceFingerprint, service.DeviceFingerprint);
        Assert.Equal(cancellationTokenSource.Token, service.CancellationToken);
    }

    private static PosHomeController CreateController(
        CapturingPosHomeDashboardService service,
        Guid tenantId,
        Guid userId)
    {
        var controller = new PosHomeController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("sub", userId.ToString()),
                        new Claim("tenant_id", tenantId.ToString())
                    ],
                    "Test"))
            }
        };
        return controller;
    }

    private sealed class CapturingPosHomeDashboardService : IPosHomeDashboardService
    {
        public TenantRequestContext? Context { get; private set; }
        public Guid? OutletId { get; private set; }
        public Guid? TillId { get; private set; }
        public Guid? DeviceId { get; private set; }
        public string? DeviceFingerprint { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<ApplicationResult<PosHomeDashboardResponseDto>> GetPosHomeAsync(
            TenantRequestContext context,
            Guid? outletId,
            Guid? tillId,
            Guid? deviceId,
            string? deviceFingerprint,
            CancellationToken cancellationToken)
        {
            Context = context;
            OutletId = outletId;
            TillId = tillId;
            DeviceId = deviceId;
            DeviceFingerprint = deviceFingerprint;
            CancellationToken = cancellationToken;

            return Task.FromResult(ApplicationResult<PosHomeDashboardResponseDto>.Failure(
                new ApplicationError("pos_home.test", "Test response.")));
        }
    }
}
