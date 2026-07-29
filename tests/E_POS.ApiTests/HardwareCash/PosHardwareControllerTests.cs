using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.HardwareCash;

public sealed class PosHardwareControllerTests
{
    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(
            typeof(PosHardwareController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public async Task SaveConfiguration_WithTenantContext_ReturnsOk()
    {
        var service = new FakeService();
        var controller = Create(service);
        SetClaims(controller);
        var result = await controller.SaveConfiguration(
            new SavePosHardwareConfigurationRequest(
                Guid.NewGuid(), Guid.NewGuid(), null, "receiptPrinter",
                "localPrintAgent", "POS80", true, 0, null,
                new ReceiptPrinterSettingsDto(
                    "http://192.168.18.8:9101", "POSPrinter POS80",
                    "80mm", true, 5000, 5, true),
                null, null, null),
            CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(service.Context);
    }

    [Fact]
    public async Task SaveConfiguration_VersionConflict_ReturnsConflict()
    {
        var service = new FakeService
        {
            SaveResult = ApplicationResult<PosHardwareConfigurationDto>.Failure(
                new ApplicationError(
                    "pos_hardware.version_conflict",
                    "Hardware configuration changed elsewhere."))
        };
        var controller = Create(service);
        SetClaims(controller);
        var result = await controller.SaveConfiguration(
            new SavePosHardwareConfigurationRequest(
                Guid.NewGuid(), Guid.NewGuid(), null, "receiptPrinter",
                "localPrintAgent", "POS80", true, 1, null,
                null, null, null, null),
            CancellationToken.None);
        Assert.IsType<ConflictObjectResult>(result);
    }

    private static PosHardwareController Create(FakeService service)
    {
        var controller =
            new PosHardwareController(service, new TenantRequestContextFactory());
        controller.ControllerContext =
            new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetClaims(PosHardwareController controller)
    {
        controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("sub", Guid.NewGuid().ToString()),
                    new Claim("tenant_id", Guid.NewGuid().ToString()),
                    new Claim("permissions", PosPermissions.Hardware.Settings)
                ],
                "Test"));
    }

    private sealed class FakeService : IPosHardwareService
    {
        public TenantRequestContext? Context { get; private set; }
        public ApplicationResult<PosHardwareConfigurationDto> SaveResult { get; init; } =
            ApplicationResult<PosHardwareConfigurationDto>.Success(
                new PosHardwareConfigurationDto(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                    Guid.NewGuid(), null, "receiptPrinter", "localPrintAgent",
                    "POS80", true, 1, false, null, new { },
                    DateTimeOffset.UtcNow));

        public Task<ApplicationResult<IReadOnlyList<PosHardwareConfigurationDto>>>
            GetConfigurationsAsync(
                TenantRequestContext context,
                Guid posDeviceId,
                CancellationToken cancellationToken) =>
            Task.FromResult(
                ApplicationResult<IReadOnlyList<PosHardwareConfigurationDto>>
                    .Success([]));

        public Task<ApplicationResult<PosHardwareConfigurationDto>>
            SaveConfigurationAsync(
                TenantRequestContext context,
                SavePosHardwareConfigurationRequest request,
                CancellationToken cancellationToken)
        {
            Context = context;
            return Task.FromResult(SaveResult);
        }

        public Task<ApplicationResult<HardwareTestOperationDto>> CreateTestAsync(
            TenantRequestContext context,
            CreateHardwareTestRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<HardwareTestOperationDto>.Failure(
                new ApplicationError("unused", "unused")));

        public Task<ApplicationResult<HardwareTestOperationDto>> CompleteTestAsync(
            TenantRequestContext context,
            Guid testId,
            CompleteHardwareTestRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<HardwareTestOperationDto>.Failure(
                new ApplicationError("unused", "unused")));

        public Task<ApplicationResult<IReadOnlyList<HardwareTestOperationDto>>>
            GetTestHistoryAsync(
                TenantRequestContext context,
                Guid posDeviceId,
                int take,
                CancellationToken cancellationToken) =>
            Task.FromResult(
                ApplicationResult<IReadOnlyList<HardwareTestOperationDto>>
                    .Success([]));
    }
}
