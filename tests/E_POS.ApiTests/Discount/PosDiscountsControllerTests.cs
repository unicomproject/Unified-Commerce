using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Application.Modules.Tenant.Discount.Dtos;
using E_POS.Domain.Modules.Tenant.Orders.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.Discount;

public sealed class PosDiscountsControllerTests
{
    [Fact]
    public async Task List_WithValidContext_ReturnsCatalogAndAuthority()
    {
        var catalog = new PosDiscountCatalogResponseDto(
            new PosDiscountAuthorityDto(10, 1000, "LKR"), []);
        var service = new FakeService { Catalog = ApplicationResult<PosDiscountCatalogResponseDto>.Success(catalog) };
        var controller = Create(service, SalesPermissions.Discount.Apply);

        var result = await controller.List(
            new PosDiscountCatalogQueryDto(Guid.NewGuid(), null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Apply_WhenIdempotencyConflict_ReturnsConflict()
    {
        var service = new FakeService
        {
            Apply = ApplicationResult<PosDiscountApplyResponseDto>.Failure(
                new ApplicationError("pos_discounts.idempotency_conflict", "Conflict."))
        };
        var controller = Create(service, SalesPermissions.Discount.Apply);

        var result = await controller.Apply(Request(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Decide_WhenManagerHasPermission_ReturnsOk()
    {
        var now = DateTimeOffset.UtcNow;
        var decision = new PosDiscountDecisionResponseDto(
            Guid.NewGuid(), Guid.NewGuid(), "APPROVED", Guid.NewGuid(), now, null, 500, 4500, "hash");
        var service = new FakeService
        {
            Decision = ApplicationResult<PosDiscountDecisionResponseDto>.Success(decision)
        };
        var controller = Create(service, SalesPermissions.Discount.Approve);

        var result = await controller.Decide(
            decision.ApplicationId, new PosDiscountDecisionRequestDto("APPROVE", null), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenApplicationExists_ReturnsOk()
    {
        var now = DateTimeOffset.UtcNow;
        var cancellation = new PosDiscountCancelResponseDto(Guid.NewGuid(), "cancelled", now);
        var service = new FakeService
        {
            Cancellation = ApplicationResult<PosDiscountCancelResponseDto>.Success(cancellation)
        };
        var controller = Create(service, SalesPermissions.Discount.Apply);

        var result = await controller.Cancel(
            cancellation.ApplicationId,
            new PosDiscountCancelRequestDto(Guid.NewGuid(), "Customer removed discount"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    private static PosDiscountValidationRequestDto Request() => new(
        Guid.NewGuid(), Guid.NewGuid(), "MANUAL", 5, "ORDER", null, null,
        "NewSale", null, [new(Guid.NewGuid(), 1)], "key", "PERCENTAGE");

    private static PosDiscountsController Create(FakeService service, string permission)
    {
        var controller = new PosDiscountsController(service, new TenantRequestContextFactory());
        var claims = new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("tenant_id", Guid.NewGuid().ToString()),
            new Claim("permissions", permission)
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        return controller;
    }

    private sealed class FakeService : IPosDiscountService
    {
        public ApplicationResult<PosDiscountCatalogResponseDto> Catalog { get; init; } =
            ApplicationResult<PosDiscountCatalogResponseDto>.Failure(new("not_configured", "Not configured."));
        public ApplicationResult<PosDiscountValidationResponseDto> Validation { get; init; } =
            ApplicationResult<PosDiscountValidationResponseDto>.Failure(new("not_configured", "Not configured."));
        public ApplicationResult<PosDiscountApplyResponseDto> Apply { get; init; } =
            ApplicationResult<PosDiscountApplyResponseDto>.Failure(new("not_configured", "Not configured."));
        public ApplicationResult<PosDiscountDecisionResponseDto> Decision { get; init; } =
            ApplicationResult<PosDiscountDecisionResponseDto>.Failure(new("not_configured", "Not configured."));
        public ApplicationResult<PosDiscountCancelResponseDto> Cancellation { get; init; } =
            ApplicationResult<PosDiscountCancelResponseDto>.Failure(new("not_configured", "Not configured."));

        public Task<ApplicationResult<PosDiscountCatalogResponseDto>> ListAvailableAsync(
            TenantRequestContext context, PosDiscountCatalogQueryDto query, CancellationToken cancellationToken) => Task.FromResult(Catalog);
        public Task<ApplicationResult<PosDiscountValidationResponseDto>> ValidateAsync(
            TenantRequestContext context, PosDiscountValidationRequestDto request, CancellationToken cancellationToken) => Task.FromResult(Validation);
        public Task<ApplicationResult<PosDiscountApplyResponseDto>> ApplyAsync(
            TenantRequestContext context, PosDiscountValidationRequestDto request, CancellationToken cancellationToken) => Task.FromResult(Apply);
        public Task<ApplicationResult<PosDiscountDecisionResponseDto>> DecideAsync(
            TenantRequestContext context, Guid applicationId, PosDiscountDecisionRequestDto request, CancellationToken cancellationToken) => Task.FromResult(Decision);
        public Task<ApplicationResult<PosDiscountCancelResponseDto>> CancelAsync(
            TenantRequestContext context, Guid applicationId, PosDiscountCancelRequestDto request,
            CancellationToken cancellationToken) => Task.FromResult(Cancellation);
    }
}
