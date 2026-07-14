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

public sealed class PosReturnsControllerTests
{
    [Fact]
    public async Task CompleteReturn_WithValidContext_ReturnsOkAndForwardsRequest()
    {
        var saleId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var request = new PosReturnCompleteRequestDto(
            "DAMAGED",
            "CASH_REFUND",
            null,
            [new PosReturnCreditPreviewLineRequestDto(Guid.NewGuid(), 1)]);
        var service = new FakeReturnService
        {
            CompleteResult = ApplicationResult<PosReturnReceiptDto>.Success(
                CreateReturnReceipt(saleId))
        };
        var controller = CreateController(service);
        SetClaims(controller, Guid.NewGuid(), Guid.NewGuid(), ReturnsPermissions.CreateRefund);

        var result = await controller.CompleteReturn(
            saleId,
            deviceId,
            request,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(saleId, service.CompleteSaleId);
        Assert.Equal(deviceId, service.CompleteDeviceId);
        Assert.Same(request, service.CompleteRequest);
    }

    [Fact]
    public async Task PreviewCredit_WithValidContext_ReturnsOkAndForwardsRequest()
    {
        var saleId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var request = new PosReturnCreditPreviewRequestDto(
            "DAMAGED",
            [new PosReturnCreditPreviewLineRequestDto(lineId, 1)]);
        var service = new FakeReturnService
        {
            CreditResult = ApplicationResult<PosReturnCreditPreviewDto>.Success(
                CreateCreditPreview(saleId, lineId))
        };
        var controller = CreateController(service);
        SetClaims(controller, Guid.NewGuid(), Guid.NewGuid(), ReturnsPermissions.CreateRefund);

        var result = await controller.PreviewCredit(
            saleId,
            deviceId,
            request,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(saleId, service.CreditSaleId);
        Assert.Equal(deviceId, service.CreditDeviceId);
        Assert.Same(request, service.CreditRequest);
    }

    [Fact]
    public async Task GetSaleEligibility_WithValidContext_ReturnsOkAndForwardsIds()
    {
        var saleId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var service = new FakeReturnService
        {
            EligibilityResult = ApplicationResult<PosReturnSaleEligibilityDto>.Success(
                new PosReturnSaleEligibilityDto(
                    saleId,
                    "RCP-001",
                    null,
                    "Walk-in Customer",
                    DateTimeOffset.UtcNow,
                    "Cash",
                    string.Empty,
                    "LKR",
                    [],
                    []))
        };
        var controller = CreateController(service);
        SetClaims(controller, Guid.NewGuid(), Guid.NewGuid(), ReturnsPermissions.ViewReturns);

        var result = await controller.GetSaleEligibility(
            saleId,
            deviceId,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(saleId, service.EligibilitySaleId);
        Assert.Equal(deviceId, service.EligibilityDeviceId);
    }

    [Fact]
    public async Task SearchOriginalSales_WithValidContext_ReturnsOkAndForwardsQuery()
    {
        var deviceId = Guid.NewGuid();
        var service = new FakeReturnService
        {
            Result = ApplicationResult<PosReturnSaleSearchPageDto>.Success(
                new PosReturnSaleSearchPageDto([], 1, 20, 0))
        };
        var controller = CreateController(service);
        SetClaims(controller, Guid.NewGuid(), Guid.NewGuid(), ReturnsPermissions.ViewReturns);

        var result = await controller.SearchOriginalSales(
            deviceId,
            "invoice",
            "RCP-001",
            1,
            20,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(deviceId, service.DeviceId);
        Assert.Equal("invoice", service.SearchType);
        Assert.Equal("RCP-001", service.Search);
    }

    [Fact]
    public void Controller_UsesExpectedRouteAndTenantPolicy()
    {
        var route = Assert.Single(typeof(PosReturnsController).GetCustomAttributes<RouteAttribute>());
        Assert.Equal("api/v1/pos/returns", route.Template);
        var authorize = Assert.Single(typeof(PosReturnsController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
        var actionRoute = typeof(PosReturnsController)
            .GetMethod(nameof(PosReturnsController.SearchOriginalSales))!
            .GetCustomAttribute<HttpGetAttribute>();
        Assert.Equal("sales/search", actionRoute?.Template);
    }

    private static PosReturnsController CreateController(FakeReturnService service)
    {
        var controller = new PosReturnsController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static void SetClaims(
        PosReturnsController controller,
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

    private static PosReturnCreditPreviewDto CreateCreditPreview(Guid saleId, Guid lineId) =>
        new(
            saleId,
            "RCP-001",
            null,
            "Walk-in Customer",
            string.Empty,
            DateTimeOffset.UtcNow,
            "Cash",
            string.Empty,
            "LKR",
            1000m,
            1,
            "DAMAGED",
            "Damaged",
            [new PosReturnCreditPreviewItemDto(
                lineId,
                "Product",
                "SKU-1",
                string.Empty,
                null,
                1,
                1000m,
                1000m)],
            new PosReturnCreditCalculationDto(
                1000m,
                "Discount adjustment",
                0,
                "Tax refund",
                0,
                1000m),
            "PREVIEW-RCP-001",
            0,
            null,
            1);

    private static PosReturnReceiptDto CreateReturnReceipt(Guid saleId) =>
        new(
            saleId,
            "RRF-000001",
            "RCP-000001",
            1,
            "CASH_REFUND",
            "Cash Refund",
            "Cash",
            "Cash Refund completed",
            "LKR",
            1000m,
            0,
            DateTimeOffset.UtcNow,
            "COMPLETED",
            "Walk-in Customer",
            "Cashier",
            "Till 1",
            "NOT_REQUIRED",
            "NOT_CAPTURED");

    private sealed class FakeReturnService : IPosReturnService
    {
        public ApplicationResult<PosReturnReceiptDto> CompleteResult { get; init; } =
            ApplicationResult<PosReturnReceiptDto>.Failure(
                new ApplicationError("pos_returns.failed", "Failed."));
        public Guid CompleteSaleId { get; private set; }
        public Guid? CompleteDeviceId { get; private set; }
        public PosReturnCompleteRequestDto? CompleteRequest { get; private set; }
        public ApplicationResult<PosReturnCreditPreviewDto> CreditResult { get; init; } =
            ApplicationResult<PosReturnCreditPreviewDto>.Failure(
                new ApplicationError("pos_returns.failed", "Failed."));
        public ApplicationResult<PosReturnSaleEligibilityDto> EligibilityResult { get; init; } =
            ApplicationResult<PosReturnSaleEligibilityDto>.Failure(
                new ApplicationError("pos_returns.failed", "Failed."));
        public ApplicationResult<PosReturnSaleSearchPageDto> Result { get; init; } =
            ApplicationResult<PosReturnSaleSearchPageDto>.Failure(
                new ApplicationError("pos_returns.failed", "Failed."));
        public Guid? DeviceId { get; private set; }
        public string? SearchType { get; private set; }
        public string? Search { get; private set; }
        public Guid EligibilitySaleId { get; private set; }
        public Guid? EligibilityDeviceId { get; private set; }
        public Guid CreditSaleId { get; private set; }
        public Guid? CreditDeviceId { get; private set; }
        public PosReturnCreditPreviewRequestDto? CreditRequest { get; private set; }

        public Task<ApplicationResult<PosReturnReceiptDto>> CompleteReturnAsync(
            TenantRequestContext context,
            Guid saleId,
            Guid? deviceId,
            PosReturnCompleteRequestDto request,
            CancellationToken cancellationToken)
        {
            CompleteSaleId = saleId;
            CompleteDeviceId = deviceId;
            CompleteRequest = request;
            return Task.FromResult(CompleteResult);
        }

        public Task<ApplicationResult<PosReturnCreditPreviewDto>> PreviewCreditAsync(
            TenantRequestContext context,
            Guid saleId,
            Guid? deviceId,
            PosReturnCreditPreviewRequestDto request,
            CancellationToken cancellationToken)
        {
            CreditSaleId = saleId;
            CreditDeviceId = deviceId;
            CreditRequest = request;
            return Task.FromResult(CreditResult);
        }

        public Task<ApplicationResult<PosReturnSaleEligibilityDto>> GetSaleEligibilityAsync(
            TenantRequestContext context,
            Guid saleId,
            Guid? deviceId,
            CancellationToken cancellationToken)
        {
            EligibilitySaleId = saleId;
            EligibilityDeviceId = deviceId;
            return Task.FromResult(EligibilityResult);
        }

        public Task<ApplicationResult<PosReturnSaleSearchPageDto>> SearchOriginalSalesAsync(
            TenantRequestContext context,
            Guid? deviceId,
            string? searchType,
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            DeviceId = deviceId;
            SearchType = searchType;
            Search = search;
            return Task.FromResult(Result);
        }
    }
}
