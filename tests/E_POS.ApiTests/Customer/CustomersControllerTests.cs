using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.Customer.Contracts;
using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.Customer;

public sealed class CustomersControllerTests
{
    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedCustomer()
    {
        var deviceId = Guid.NewGuid();
        var service = new FakeCustomerService
        {
            CreateResult = ApplicationResult<PosCustomerListItemResponseDto>.Success(
                new PosCustomerListItemResponseDto(
                    Guid.NewGuid(),
                    "Kamal Perera",
                    "+94771234567",
                    "kamal@example.com",
                    "ACTIVE"))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "customers.create");

        var result = await controller.Create(
            deviceId,
            new PosCustomerCreateRequestDto(
                "Kamal Perera",
                "+94771234567",
                "kamal@example.com"),
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(deviceId, service.CreateDeviceId);
        Assert.Equal("Kamal Perera", service.CreateRequest?.FullName);
    }

    [Fact]
    public async Task List_WithValidTenantContext_ReturnsCustomers()
    {
        var tenantId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var service = new FakeCustomerService
        {
            Result = ApplicationResult<PosCustomerListResponseDto>.Success(
                new PosCustomerListResponseDto(
                    [new PosCustomerListItemResponseDto(
                        Guid.NewGuid(),
                        "Kamal Perera",
                        "+94771234567",
                        "kamal@example.com",
                        "ACTIVE")],
                    1,
                    20,
                    1,
                    1))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, tenantId, Guid.NewGuid(), "customers.view");

        var result = await controller.List(
            deviceId,
            "Kamal",
            null,
            null,
            1,
            20,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(deviceId, service.DeviceId);
        Assert.Equal("Kamal", service.Search);
        Assert.Equal(1, service.Page);
        Assert.Equal(20, service.PageSize);
    }

    [Fact]
    public async Task List_WhenPermissionDenied_ReturnsForbidden()
    {
        var service = new FakeCustomerService
        {
            Result = ApplicationResult<PosCustomerListResponseDto>.Failure(
                new ApplicationError(
                    "pos_customers.permission_denied",
                    "You do not have permission to view customers."))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "pos.home.view");

        var result = await controller.List(
            Guid.NewGuid(),
            null,
            null,
            null,
            1,
            20,
            CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Summary_WithValidContext_ReturnsOk()
    {
        var service = new FakeCustomerService
        {
            SummaryResult = ApplicationResult<PosCustomerSummaryResponseDto>.Success(
                new PosCustomerSummaryResponseDto(10, 8, 5, 2, "UTC"))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "customers.view");

        var result = await controller.Summary(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOk()
    {
        var customerId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var service = new FakeCustomerService
        {
            UpdateResult = ApplicationResult<PosCustomerListItemResponseDto>.Success(
                new PosCustomerListItemResponseDto(
                    customerId,
                    "Updated Name",
                    "+94771234567",
                    null,
                    "ACTIVE",
                    "CUS000001",
                    "POS"))
        };
        var controller = CreateController(service);
        SetTenantClaims(controller, Guid.NewGuid(), Guid.NewGuid(), "customers.update");

        var result = await controller.Update(
            customerId,
            deviceId,
            new PosCustomerUpdateRequestDto("Updated Name", "+94771234567", null, "ACTIVE"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(customerId, service.UpdateCustomerId);
        Assert.Equal("Updated Name", service.UpdateRequest?.FullName);
    }

    [Fact]
    public void Controller_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(CustomersController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    private static CustomersController CreateController(FakeCustomerService service)
    {
        var controller = new CustomersController(service, new TenantRequestContextFactory());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static void SetTenantClaims(
        CustomersController controller,
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

    private sealed class FakeCustomerService : IPosCustomerService
    {
        public ApplicationResult<PosCustomerListItemResponseDto> CreateResult { get; init; } =
            ApplicationResult<PosCustomerListItemResponseDto>.Failure(
                new ApplicationError("pos_customers.create_failed", "Customer could not be created."));

        public ApplicationResult<PosCustomerListResponseDto> Result { get; init; } =
            ApplicationResult<PosCustomerListResponseDto>.Failure(
                new ApplicationError("pos_customers.list_failed", "Customers could not be loaded."));

        public ApplicationResult<PosCustomerSummaryResponseDto> SummaryResult { get; init; } =
            ApplicationResult<PosCustomerSummaryResponseDto>.Failure(
                new ApplicationError("pos_customers.summary_failed", "Summary unavailable."));

        public TenantRequestContext? Context { get; private set; }
        public Guid? DeviceId { get; private set; }
        public string? Search { get; private set; }
        public int Page { get; private set; }
        public int PageSize { get; private set; }
        public Guid? CreateDeviceId { get; private set; }
        public PosCustomerCreateRequestDto? CreateRequest { get; private set; }

        public Task<ApplicationResult<PosCustomerListItemResponseDto>> CreateAsync(
            TenantRequestContext context,
            Guid? deviceId,
            PosCustomerCreateRequestDto request,
            CancellationToken cancellationToken)
        {
            Context = context;
            CreateDeviceId = deviceId;
            CreateRequest = request;
            return Task.FromResult(CreateResult);
        }

        public Task<ApplicationResult<PosCustomerListResponseDto>> ListAsync(
            TenantRequestContext context,
            Guid? deviceId,
            string? search,
            string? status,
            string? source,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            Context = context;
            DeviceId = deviceId;
            Search = search;
            Page = page;
            PageSize = pageSize;
            return Task.FromResult(Result);
        }

        public Task<ApplicationResult<PosCustomerSummaryResponseDto>> GetSummaryAsync(
            TenantRequestContext context,
            Guid? deviceId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SummaryResult);

        public Task<ApplicationResult<PosCustomerListItemResponseDto>> GetByIdAsync(
            TenantRequestContext context,
            Guid? deviceId,
            Guid customerId,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreateResult);

        public Task<ApplicationResult<PosCustomerOrdersResponseDto>> GetOrdersAsync(
            TenantRequestContext context,
            Guid? deviceId,
            Guid customerId,
            int page,
            int pageSize,
            DateTimeOffset? fromDate,
            DateTimeOffset? toDate,
            string? status,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PosCustomerOrdersResponseDto>.Success(
                new PosCustomerOrdersResponseDto([], page, pageSize, 0, 0)));

        public Task<ApplicationResult<PosCustomerAttachToSaleResponseDto>> AttachToSaleAsync(
            TenantRequestContext context,
            Guid? deviceId,
            Guid customerId,
            PosCustomerAttachToSaleRequestDto request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PosCustomerAttachToSaleResponseDto>.Failure(
                new ApplicationError("pos_customers.attach_failed", "Attach failed.")));

        public ApplicationResult<PosCustomerListItemResponseDto> UpdateResult { get; init; } =
            ApplicationResult<PosCustomerListItemResponseDto>.Failure(
                new ApplicationError("pos_customers.update_failed", "Customer could not be updated."));

        public Guid? UpdateCustomerId { get; private set; }
        public PosCustomerUpdateRequestDto? UpdateRequest { get; private set; }

        public Task<ApplicationResult<PosCustomerListItemResponseDto>> UpdateAsync(
            TenantRequestContext context,
            Guid? deviceId,
            Guid customerId,
            PosCustomerUpdateRequestDto request,
            CancellationToken cancellationToken)
        {
            Context = context;
            DeviceId = deviceId;
            UpdateCustomerId = customerId;
            UpdateRequest = request;
            return Task.FromResult(UpdateResult);
        }
    }
}
