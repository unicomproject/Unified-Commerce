using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.POSOperations;

public sealed class PosReceiptsControllerTests
{
    [Fact]
    public async Task RecordPrint_WithValidClaims_ReturnsOk()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 10, 11, 0, 0, TimeSpan.Zero);
        var print = new PosReceiptPrintResponseDto(
            saleId,
            receiptId,
            "RCP-000001",
            1,
            "printed",
            1,
            now);

        var service = new FakePosReceiptService
        {
            RecordPrintResult = ApplicationResult<PosReceiptPrintResponseDto>.Success(print),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, userId, ReceiptPermissions.Print);

        var result = await controller.RecordPrint(
            saleId,
            new PosReceiptPrintRequestDto("success", 1, null),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(saleId, service.SaleId);
    }

    [Fact]
    public async Task RecordPrint_WhenPermissionDenied_ReturnsForbidden()
    {
        var service = new FakePosReceiptService
        {
            RecordPrintResult = ApplicationResult<PosReceiptPrintResponseDto>.Failure(
                new ApplicationError("pos_receipts.permission_denied", "You do not have permission to print receipts.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "products.view");

        var result = await controller.RecordPrint(
            Guid.NewGuid(),
            new PosReceiptPrintRequestDto("success", 1, null),
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task RecordPrint_WhenReceiptNotFound_ReturnsNotFound()
    {
        var service = new FakePosReceiptService
        {
            RecordPrintResult = ApplicationResult<PosReceiptPrintResponseDto>.Failure(
                new ApplicationError("pos_receipts.receipt_not_found", "Receipt could not be found for the sale.")),
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), ReceiptPermissions.Print);

        var result = await controller.RecordPrint(
            Guid.NewGuid(),
            new PosReceiptPrintRequestDto("success", 1, null),
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PosReceiptsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static PosReceiptsController CreateController(FakePosReceiptService service)
    {
        var controller = new PosReceiptsController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static void SetTenantClaims(
        PosReceiptsController controller,
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

    private sealed class FakePosReceiptService : IPosReceiptService
    {
        public ApplicationResult<PosReceiptPrintResponseDto> RecordPrintResult { get; init; } =
            ApplicationResult<PosReceiptPrintResponseDto>.Failure(
                new ApplicationError("pos_receipts.print_failed", "Receipt print audit could not be recorded."));

        public TenantRequestContext? Context { get; private set; }
        public Guid? SaleId { get; private set; }

        public Task<ApplicationResult<PosReceiptPrintResponseDto>> RecordPrintAsync(
            TenantRequestContext context,
            Guid saleId,
            PosReceiptPrintRequestDto request,
            CancellationToken cancellationToken)
        {
            Context = context;
            SaleId = saleId;
            return Task.FromResult(RecordPrintResult);
        }
    }
}
