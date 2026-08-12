using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using E_POS.Api.Middleware;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformTenantBootstrapHttpPipelineTests
    : IClassFixture<PlatformTenantBootstrapHttpPipelineTests.BootstrapApiFactory>
{
    private const string Issuer = "TM-EPOS";
    private const string PlatformAudience = "TM-EPOS-Platform";
    private const string PlatformKey = "DEV_ONLY_PLATFORM_JWT_SIGNING_KEY_32_CHARS_MINIMUM";
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PlatformUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly BootstrapApiFactory _factory;

    public PlatformTenantBootstrapHttpPipelineTests(BootstrapApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSummary_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/platform-admin/tenants/{TenantId}/bootstrap/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WithPlatformJwt_Returns200()
    {
        var client = CreateAuthorizedClient();

        var response = await client.GetAsync($"/api/v1/platform-admin/tenants/{TenantId}/bootstrap/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains(CorrelationIdMiddleware.HeaderName));
        Assert.False(string.IsNullOrWhiteSpace(
            response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).FirstOrDefault()));
    }

    [Fact]
    public async Task CreateOutlet_WithoutIdempotencyKey_Returns400()
    {
        var client = CreateAuthorizedClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/platform-admin/tenants/{TenantId}/bootstrap/outlets",
            new PlatformTenantBootstrapOutletCreateRequest { OutletName = "Main" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorEnvelopeHasCorrelationAsync(response);
    }

    [Fact]
    public async Task CreateOutlet_WithAuthAndIdempotencyAndCorrelation_Returns201()
    {
        var client = CreateAuthorizedClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "my-corr-123");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "outlet-http-key-1");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/platform-admin/tenants/{TenantId}/bootstrap/outlets",
            new PlatformTenantBootstrapOutletCreateRequest { OutletName = "Main Store" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            "my-corr-123",
            response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task CreateTill_WhenServiceReturnsDependencyMissing_Returns409()
    {
        _factory.BootstrapService.TillResult = ApplicationResult<PlatformTenantBootstrapTillResponse>.Failure(
            new ApplicationError(
                "platform_tenants.bootstrap.dependency_missing",
                "Selected-tenant bootstrap dependency is missing."));

        var client = CreateAuthorizedClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "till-http-key-1");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/platform-admin/tenants/{TenantId}/bootstrap/tills",
            new PlatformTenantBootstrapTillCreateRequest { OutletId = Guid.NewGuid(), TillName = "Till 1" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CommitImport_WithoutIdempotencyKey_Returns400()
    {
        var client = CreateAuthorizedClient();

        var response = await client.PostAsync(
            $"/api/v1/platform-admin/tenants/{TenantId}/bootstrap/products/import/{Guid.NewGuid()}/commit",
            null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorEnvelopeHasCorrelationAsync(response);
    }

    [Fact]
    public async Task GetImportTemplate_WithAuth_Returns200Csv()
    {
        var client = CreateAuthorizedClient();

        var response = await client.GetAsync(
            $"/api/v1/platform-admin/tenants/{TenantId}/bootstrap/products/import/template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("product_name", body, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateAuthorizedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreatePlatformToken());
        return client;
    }

    private static async Task AssertErrorEnvelopeHasCorrelationAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("correlationId", out var correlationId));
        Assert.False(string.IsNullOrWhiteSpace(correlationId.GetString()));
        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    private static string CreatePlatformToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PlatformKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, PlatformUserId.ToString()),
            new("identity_type", "platform_user"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("sid", Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: PlatformAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public sealed class BootstrapApiFactory : WebApplicationFactory<Program>
    {
        public FakeBootstrapService BootstrapService { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Port=5432;Database=BootstrapHttpPipelineTests;Username=postgres;Password=postgres",
                    ["PlatformJwt:Issuer"] = Issuer,
                    ["PlatformJwt:Audience"] = PlatformAudience,
                    ["PlatformJwt:SigningKey"] = PlatformKey,
                    ["TenantJwt:Issuer"] = Issuer,
                    ["TenantJwt:Audience"] = "TM-EPOS-Tenant",
                    ["TenantJwt:SigningKey"] = "DEV_ONLY_TENANT_JWT_SIGNING_KEY_32_CHARS_MINIMUM",
                    ["CustomerJwt:Issuer"] = Issuer,
                    ["CustomerJwt:Audience"] = "TM-EPOS-Customer",
                    ["CustomerJwt:SigningKey"] = "DEV_ONLY_CUSTOMER_JWT_SIGNING_KEY_32_CHARS_MINIMUM"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
                services.RemoveAll<IAuthSessionValidator>();
                services.RemoveAll<IPlatformTenantBootstrapService>();

                services.AddSingleton<IAuthSessionValidator, AlwaysActiveAuthSessionValidator>();
                services.AddSingleton<IPlatformTenantBootstrapService>(BootstrapService);
            });
        }
    }

    private sealed class AlwaysActiveAuthSessionValidator : IAuthSessionValidator
    {
        public Task<bool> IsCurrentSessionActiveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    public sealed class FakeBootstrapService : IPlatformTenantBootstrapService
    {
        public ApplicationResult<PlatformTenantBootstrapTillResponse>? TillResult { get; set; }

        public Task<ApplicationResult<PlatformTenantBootstrapSummaryResponse>> GetSummaryAsync(
            Guid tenantId,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapSummaryResponse>.Success(
                new PlatformTenantBootstrapSummaryResponse(
                    new PlatformTenantBootstrapTenantSummaryDto(
                        tenantId, "Tenant", "TEN-001", "ACTIVE", "Starter"),
                    PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
                        new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                            true, true, true, 0, 0, 0, 1, 0, false, true, true, true, true, true,
                            OnlineStoreEntitled: false, OnlineStoreStatus: null, CanManageOnlineStore: false)))));

        public Task<ApplicationResult<PlatformTenantBootstrapOutletResponse>> CreateOutletAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapOutletCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapOutletResponse>.Success(
                new PlatformTenantBootstrapOutletResponse(
                    Guid.NewGuid(),
                    request.OutletName,
                    "OUT-001",
                    "STORE",
                    "ACTIVE",
                    "Asia/Colombo")));

        public Task<ApplicationResult<PlatformTenantBootstrapTillResponse>> CreateTillAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapTillCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(TillResult ?? ApplicationResult<PlatformTenantBootstrapTillResponse>.Success(
                new PlatformTenantBootstrapTillResponse(
                    Guid.NewGuid(), "Till 1", "TILL-1", request.OutletId, "ACTIVE", "UNBOUND")));

        public Task<ApplicationResult<PlatformTenantBootstrapRoleResponse>> CreateRoleAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapRoleCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapRoleResponse>.Success(
                new PlatformTenantBootstrapRoleResponse(Guid.NewGuid(), "Role", "ROLE", [])));

        public Task<ApplicationResult<PlatformTenantBootstrapUserResponse>> CreateUserAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapUserCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapUserResponse>.Success(
                new PlatformTenantBootstrapUserResponse(
                    Guid.NewGuid(), request.DisplayName, request.Email, "PENDING", "PENDING")));

        public Task<ApplicationResult<PlatformTenantBootstrapProductResponse>> CreateProductAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapProductCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapProductResponse>.Success(
                new PlatformTenantBootstrapProductResponse(
                    Guid.NewGuid(), request.ProductName, request.Sku, "ACTIVE")));

        public Task<ApplicationResult<byte[]>> GetProductImportTemplateAsync(
            Guid tenantId,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<byte[]>.Success(
                Encoding.UTF8.GetBytes("product_name,sku,selling_price\r\n")));

        public Task<ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>> ValidateProductImportAsync(
            Guid tenantId,
            Guid platformUserId,
            Stream csvStream,
            string fileName,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>.Success(
                new PlatformTenantBootstrapProductImportValidateResponse(Guid.NewGuid(), 0, 0, 0, [])));

        public Task<ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>> CommitProductImportAsync(
            Guid tenantId,
            Guid platformUserId,
            Guid importId,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>.Success(
                new PlatformTenantBootstrapProductImportCommitResponse(importId, 0, 0)));

        public Task<ApplicationResult<byte[]>> GetProductImportErrorsCsvAsync(
            Guid tenantId,
            Guid platformUserId,
            Guid importId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<byte[]>.Success([]));

        public Task<ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>> GetOnlineStoreAsync(
            Guid tenantId,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Success(
                new PlatformTenantBootstrapOnlineStoreResponse(
                    true, "DRAFT", "MATCH_TENANT", false, false, null)));

        public Task<ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>> UpsertOnlineStoreAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapOnlineStoreUpsertRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Success(
                new PlatformTenantBootstrapOnlineStoreResponse(
                    true,
                    request.StoreStatus,
                    request.TaxDisplayMode ?? "MATCH_TENANT",
                    false,
                    false,
                    null)));
    }
}
