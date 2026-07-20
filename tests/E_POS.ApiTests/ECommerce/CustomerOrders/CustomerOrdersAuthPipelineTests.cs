using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace E_POS.ApiTests.ECommerce.CustomerOrders;

public sealed class CustomerOrdersAuthPipelineTests : IClassFixture<CustomerOrdersAuthPipelineTests.CustomerOrdersApiFactory>
{
    private const string Issuer = "TM-EPOS";
    private const string CustomerAudience = "TM-EPOS-Customer";
    private const string TenantAudience = "TM-EPOS-Tenant";
    private const string CustomerKey = "DEV_ONLY_CUSTOMER_JWT_SIGNING_KEY_32_CHARS_MINIMUM";
    private const string TenantKey = "DEV_ONLY_TENANT_JWT_SIGNING_KEY_32_CHARS_MINIMUM";
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid TenantUserId = Guid.NewGuid();

    private readonly CustomerOrdersApiFactory _factory;

    public CustomerOrdersAuthPipelineTests(CustomerOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CustomerOrders_WithValidCustomerJwt_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(CreateCustomerToken());

        var response = await client.GetAsync("/api/v1/ecommerce/storefront/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CustomerOrders_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/ecommerce/storefront/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CustomerOrders_WithTenantJwt_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(CreateTenantToken(["fulfillment.orders.manage"]));

        var response = await client.GetAsync("/api/v1/ecommerce/storefront/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CustomerOrders_WithExpiredCustomerJwt_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(CreateCustomerToken(expiresAt: DateTime.UtcNow.AddMinutes(-10)));

        var response = await client.GetAsync("/api/v1/ecommerce/storefront/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CustomerOrders_WithWrongAudience_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(CreateToken(
            CustomerKey,
            audience: "TM-EPOS-Unknown",
            identityType: "customer",
            subject: CustomerId,
            tenantId: TenantId));

        var response = await client.GetAsync("/api/v1/ecommerce/storefront/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CustomerOrderCancel_WithValidCustomerJwt_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(CreateCustomerToken());

        var response = await client.PostAsJsonAsync(
            $"/api/v1/ecommerce/storefront/orders/{Guid.NewGuid()}/cancel",
            new CustomerOrderCancelRequest { Reason = "Changed my mind" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CustomerOrderCancel_WithTenantJwt_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(CreateTenantToken(["fulfillment.orders.manage"]));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/ecommerce/storefront/orders/{Guid.NewGuid()}/cancel",
            new CustomerOrderCancelRequest { Reason = "Changed my mind" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TenantStatusUpdate_WithValidTenantJwtAndPermission_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(CreateTenantToken(["fulfillment.orders.manage"]));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tenant/ecommerce/click-collect/orders/{Guid.NewGuid()}/status",
            new ClickCollectOrderStatusUpdateRequest { Status = "ACCEPTED" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TenantStatusUpdate_WithCustomerJwt_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(CreateCustomerToken());

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tenant/ecommerce/click-collect/orders/{Guid.NewGuid()}/status",
            new ClickCollectOrderStatusUpdateRequest { Status = "ACCEPTED" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TenantStatusUpdate_WithTenantJwtMissingPermission_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = Bearer(CreateTenantToken(["tenant.dashboard.view"]));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tenant/ecommerce/click-collect/orders/{Guid.NewGuid()}/status",
            new ClickCollectOrderStatusUpdateRequest { Status = "ACCEPTED" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static AuthenticationHeaderValue Bearer(string token) =>
        new("Bearer", token);

    private static string CreateCustomerToken(DateTime? expiresAt = null) =>
        CreateToken(
            CustomerKey,
            CustomerAudience,
            "customer",
            CustomerId,
            TenantId,
            expiresAt: expiresAt);

    private static string CreateTenantToken(IReadOnlyCollection<string> permissions) =>
        CreateToken(
            TenantKey,
            TenantAudience,
            "tenant_user",
            TenantUserId,
            TenantId,
            permissions);

    private static string CreateToken(
        string signingKey,
        string audience,
        string identityType,
        Guid subject,
        Guid tenantId,
        IReadOnlyCollection<string>? permissions = null,
        DateTime? expiresAt = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject.ToString()),
            new("tenant_id", tenantId.ToString()),
            new("identity_type", identityType),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (permissions is not null)
        {
            claims.AddRange(permissions.Select(permission => new Claim("permissions", permission)));
        }

        var expires = expiresAt ?? DateTime.UtcNow.AddMinutes(15);
        var notBefore = expires <= DateTime.UtcNow
            ? expires.AddMinutes(-5)
            : DateTime.UtcNow.AddMinutes(-5);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public sealed class CustomerOrdersApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=ApiAuthPipelineTests;Username=postgres;Password=postgres",
                    ["PlatformJwt:Issuer"] = Issuer,
                    ["PlatformJwt:Audience"] = "TM-EPOS-Platform",
                    ["PlatformJwt:SigningKey"] = "DEV_ONLY_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
                    ["TenantJwt:Issuer"] = Issuer,
                    ["TenantJwt:Audience"] = TenantAudience,
                    ["TenantJwt:SigningKey"] = TenantKey,
                    ["CustomerJwt:Issuer"] = Issuer,
                    ["CustomerJwt:Audience"] = CustomerAudience,
                    ["CustomerJwt:SigningKey"] = CustomerKey
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAuthSessionValidator>();
                services.RemoveAll<ICustomerOrderService>();
                services.RemoveAll<IClickCollectOrderStatusService>();

                services.AddSingleton<IAuthSessionValidator, AlwaysActiveAuthSessionValidator>();
                services.AddSingleton<ICustomerOrderService, FakeCustomerOrderService>();
                services.AddSingleton<IClickCollectOrderStatusService, PermissionAwareStatusService>();
            });
        }
    }

    private sealed class AlwaysActiveAuthSessionValidator : IAuthSessionValidator
    {
        public Task<bool> IsCurrentSessionActiveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class FakeCustomerOrderService : ICustomerOrderService
    {
        public Task<ApplicationResult<CustomerOrderListReadModel>> GetAsync(
            Guid tenantId,
            Guid customerId,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<CustomerOrderListReadModel>.Success(new CustomerOrderListReadModel
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            }));

        public Task<ApplicationResult<CustomerOrderDetailReadModel>> GetDetailAsync(
            Guid tenantId,
            Guid customerId,
            Guid orderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<CustomerOrderDetailReadModel>.Success(new CustomerOrderDetailReadModel
            {
                Id = orderId,
                Status = "PENDING_CONFIRMATION",
                StatusLabel = "Pending Confirmation"
            }));

        public Task<ApplicationResult<CustomerOrderCancelResponse>> CancelAsync(
            Guid tenantId,
            Guid customerId,
            Guid orderId,
            CustomerOrderCancelRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<CustomerOrderCancelResponse>.Success(new CustomerOrderCancelResponse
            {
                Id = orderId,
                OrderNumber = "SO-WEB-1",
                Status = "CANCELLED",
                StatusLabel = "Cancelled",
                CancelledAt = DateTimeOffset.UtcNow,
                Message = "Order cancelled successfully."
            }));
    }

    private sealed class PermissionAwareStatusService : IClickCollectOrderStatusService
    {
        public Task<ApplicationResult<ClickCollectOrderStatusUpdateResponse>> UpdateStatusAsync(
            TenantRequestContext context,
            Guid orderId,
            ClickCollectOrderStatusUpdateRequest request,
            CancellationToken cancellationToken)
        {
            if (!context.HasPermission("fulfillment.orders.manage"))
            {
                return Task.FromResult(ApplicationResult<ClickCollectOrderStatusUpdateResponse>.Failure(
                    new ApplicationError("click_collect_orders.permission_denied", "Permission denied.")));
            }

            return Task.FromResult(ApplicationResult<ClickCollectOrderStatusUpdateResponse>.Success(
                new ClickCollectOrderStatusUpdateResponse
                {
                    Id = orderId,
                    OrderNumber = "SO-WEB-1",
                    Status = request.Status,
                    StatusLabel = request.Status,
                    FulfillmentStatus = request.Status,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    CollectionQrAvailable = true
                }));
        }
    }
}
