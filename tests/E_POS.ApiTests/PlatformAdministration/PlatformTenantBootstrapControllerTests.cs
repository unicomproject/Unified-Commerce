using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformTenantBootstrapControllerTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PlatformUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task GetSummary_WithAuthenticatedUser_ReturnsOk()
    {
        var summary = CreateSummary();
        var controller = CreateController(new FakeBootstrapService
        {
            SummaryResult = ApplicationResult<PlatformTenantBootstrapSummaryResponse>.Success(summary)
        });
        SetPlatformClaims(controller);

        var result = await controller.GetSummary(TenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformTenantBootstrapSummaryResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Same(summary, payload.Data);
    }

    [Fact]
    public async Task GetSummary_WithoutUser_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeBootstrapService());

        var result = await controller.GetSummary(TenantId, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CreateOutlet_WithSuccess_ReturnsCreated()
    {
        var response = new PlatformTenantBootstrapOutletResponse(
            Guid.NewGuid(), "Main Store", "OUT-001", "STORE", "ACTIVE", "Asia/Colombo");
        var service = new FakeBootstrapService
        {
            OutletResult = ApplicationResult<PlatformTenantBootstrapOutletResponse>.Success(response)
        };
        var controller = CreateController(service);
        SetPlatformClaims(controller);
        controller.Request.Headers["Idempotency-Key"] = "outlet-key-1";

        var result = await controller.CreateOutlet(
            TenantId,
            new PlatformTenantBootstrapOutletCreateRequest { OutletName = "Main Store" },
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal("outlet-key-1", service.LastIdempotencyKey);
    }

    [Fact]
    public async Task CreateOutlet_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        var controller = CreateController(new FakeBootstrapService());
        SetPlatformClaims(controller);

        var result = await controller.CreateOutlet(
            TenantId,
            new PlatformTenantBootstrapOutletCreateRequest(),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateTill_WithDependencyMissing_ReturnsConflict()
    {
        var controller = CreateController(new FakeBootstrapService
        {
            TillResult = ApplicationResult<PlatformTenantBootstrapTillResponse>.Failure(new ApplicationError(
                "platform_tenants.bootstrap.dependency_missing",
                "Selected-tenant bootstrap dependency is missing."))
        });
        SetPlatformClaims(controller);
        controller.Request.Headers["Idempotency-Key"] = "till-key-1";

        var result = await controller.CreateTill(
            TenantId,
            new PlatformTenantBootstrapTillCreateRequest(),
            CancellationToken.None);

        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task CreateRole_WithAccessDenied_ReturnsForbidden()
    {
        var controller = CreateController(new FakeBootstrapService
        {
            RoleResult = ApplicationResult<PlatformTenantBootstrapRoleResponse>.Failure(new ApplicationError(
                "platform_tenants.bootstrap.access_denied",
                "Selected-tenant bootstrap access denied."))
        });
        SetPlatformClaims(controller);
        controller.Request.Headers["Idempotency-Key"] = "role-key-1";

        var result = await controller.CreateRole(
            TenantId,
            new PlatformTenantBootstrapRoleCreateRequest(),
            CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithSuccess_ReturnsCreated()
    {
        var response = new PlatformTenantBootstrapUserResponse(
            Guid.NewGuid(), "Jane Doe", "jane@example.com", "PENDING", "PENDING");
        var controller = CreateController(new FakeBootstrapService
        {
            UserResult = ApplicationResult<PlatformTenantBootstrapUserResponse>.Success(response)
        });
        SetPlatformClaims(controller);
        controller.Request.Headers["Idempotency-Key"] = "user-key-1";

        var result = await controller.CreateUser(
            TenantId,
            new PlatformTenantBootstrapUserCreateRequest(),
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithIdempotencyConflict_ReturnsConflict()
    {
        var controller = CreateController(new FakeBootstrapService
        {
            ProductResult = ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(new ApplicationError(
                "platform_tenants.bootstrap.conflict",
                "Idempotency key was reused with a different request."))
        });
        SetPlatformClaims(controller);
        controller.Request.Headers["Idempotency-Key"] = "product-key-1";

        var result = await controller.CreateProduct(
            TenantId,
            new PlatformTenantBootstrapProductCreateRequest { ProductName = "Rice", Sku = "RICE-1", SellingPrice = 10m },
            CancellationToken.None);

        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task CommitProductImport_WithoutUser_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeBootstrapService());
        controller.Request.Headers["Idempotency-Key"] = "commit-key-1";

        var result = await controller.CommitProductImport(TenantId, Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ValidateProductImport_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        var controller = CreateController(new FakeBootstrapService());
        SetPlatformClaims(controller);

        var result = await controller.ValidateProductImport(
            TenantId,
            new FormFile(new MemoryStream([]), 0, 0, "file", "products.csv"),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Controller_RequiresPlatformOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PlatformTenantBootstrapController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("PlatformOnly", authorize.Policy);
    }

    [Fact]
    public async Task CreateOutlet_WithoutUser_ReturnsUnauthorized()
    {
        var controller = CreateController(new FakeBootstrapService());
        controller.Request.Headers["Idempotency-Key"] = "outlet-key-1";

        var result = await controller.CreateOutlet(
            TenantId,
            new PlatformTenantBootstrapOutletCreateRequest(),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetOnlineStore_WithSuccess_ReturnsOk()
    {
        var response = new PlatformTenantBootstrapOnlineStoreResponse(
            true, "DRAFT", "MATCH_TENANT", false, false, null);
        var controller = CreateController(new FakeBootstrapService
        {
            OnlineStoreResult = ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Success(response)
        });
        SetPlatformClaims(controller);

        var result = await controller.GetOnlineStore(TenantId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<PlatformTenantBootstrapOnlineStoreResponse>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Same(response, payload.Data);
    }

    [Fact]
    public async Task UpsertOnlineStore_WithSuccess_ReturnsOk()
    {
        var response = new PlatformTenantBootstrapOnlineStoreResponse(
            true, "ACTIVE", "MATCH_TENANT", true, false,
            "Click & Collect is entitled but collection points are not configured yet. That remains a Tenant Admin task. Online Store readiness can still be saved.");
        var service = new FakeBootstrapService
        {
            OnlineStoreResult = ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Success(response)
        };
        var controller = CreateController(service);
        SetPlatformClaims(controller);
        controller.Request.Headers["Idempotency-Key"] = "os-key-1";

        var result = await controller.UpsertOnlineStore(
            TenantId,
            new PlatformTenantBootstrapOnlineStoreUpsertRequest { StoreStatus = "ACTIVE" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("os-key-1", service.LastIdempotencyKey);
        var payload = Assert.IsType<LegacyApiResponse<PlatformTenantBootstrapOnlineStoreResponse>>(ok.Value);
        Assert.True(payload.Success);
    }

    [Fact]
    public async Task UpsertOnlineStore_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        var controller = CreateController(new FakeBootstrapService());
        SetPlatformClaims(controller);

        var result = await controller.UpsertOnlineStore(
            TenantId,
            new PlatformTenantBootstrapOnlineStoreUpsertRequest { StoreStatus = "ACTIVE" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static PlatformTenantBootstrapSummaryResponse CreateSummary() =>
        new(
            new PlatformTenantBootstrapTenantSummaryDto(TenantId, "Tenant", "TEN-001", "ACTIVE", "Starter"),
            PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
                new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                    true, true, true, 0, 0, 0, 1, 0, false, true, true, true, true, true,
                    OnlineStoreEntitled: false, OnlineStoreStatus: null, CanManageOnlineStore: false)));

    private static PlatformTenantBootstrapController CreateController(FakeBootstrapService service)
    {
        return new PlatformTenantBootstrapController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static void SetPlatformClaims(PlatformTenantBootstrapController controller)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", PlatformUserId.ToString())],
            "Test"));
    }

    private sealed class FakeBootstrapService : IPlatformTenantBootstrapService
    {
        public ApplicationResult<PlatformTenantBootstrapSummaryResponse>? SummaryResult { get; init; }
        public ApplicationResult<PlatformTenantBootstrapOutletResponse>? OutletResult { get; init; }
        public ApplicationResult<PlatformTenantBootstrapTillResponse>? TillResult { get; init; }
        public ApplicationResult<PlatformTenantBootstrapRoleResponse>? RoleResult { get; init; }
        public ApplicationResult<PlatformTenantBootstrapUserResponse>? UserResult { get; init; }
        public ApplicationResult<PlatformTenantBootstrapProductResponse>? ProductResult { get; init; }
        public ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>? OnlineStoreResult { get; init; }

        public string? LastIdempotencyKey { get; private set; }

        public Task<ApplicationResult<PlatformTenantBootstrapSummaryResponse>> GetSummaryAsync(
            Guid tenantId,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(SummaryResult ?? ApplicationResult<PlatformTenantBootstrapSummaryResponse>.Failure(
                new ApplicationError("platform_tenants.not_found", "Tenant not found.")));

        public Task<ApplicationResult<PlatformTenantBootstrapOutletResponse>> CreateOutletAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapOutletCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(OutletResult ?? ApplicationResult<PlatformTenantBootstrapOutletResponse>.Failure(
                new ApplicationError("platform_tenants.validation_failed", "Validation failed.")));
        }

        public Task<ApplicationResult<PlatformTenantBootstrapTillResponse>> CreateTillAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapTillCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(TillResult ?? ApplicationResult<PlatformTenantBootstrapTillResponse>.Failure(
                new ApplicationError("platform_tenants.validation_failed", "Validation failed.")));
        }

        public Task<ApplicationResult<PlatformTenantBootstrapRoleResponse>> CreateRoleAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapRoleCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(RoleResult ?? ApplicationResult<PlatformTenantBootstrapRoleResponse>.Failure(
                new ApplicationError("platform_tenants.validation_failed", "Validation failed.")));
        }

        public Task<ApplicationResult<PlatformTenantBootstrapUserResponse>> CreateUserAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapUserCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(UserResult ?? ApplicationResult<PlatformTenantBootstrapUserResponse>.Failure(
                new ApplicationError("platform_tenants.validation_failed", "Validation failed.")));
        }

        public Task<ApplicationResult<PlatformTenantBootstrapProductResponse>> CreateProductAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapProductCreateRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(ProductResult ?? ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(
                new ApplicationError("platform_tenants.validation_failed", "Validation failed.")));
        }

        public Task<ApplicationResult<byte[]>> GetProductImportTemplateAsync(
            Guid tenantId,
            Guid platformUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<byte[]>.Success([]));

        public Task<ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>> ValidateProductImportAsync(
            Guid tenantId,
            Guid platformUserId,
            Stream csvStream,
            string fileName,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>.Failure(
                new ApplicationError("platform_tenants.validation_failed", "Validation failed.")));

        public Task<ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>> CommitProductImportAsync(
            Guid tenantId,
            Guid platformUserId,
            Guid importId,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>.Failure(
                new ApplicationError("import.not_found", "Import not found.")));

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
            Task.FromResult(OnlineStoreResult ?? ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Failure(
                new ApplicationError("platform_tenants.bootstrap.not_entitled", "Tenant is not entitled for this bootstrap module.")));

        public Task<ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>> UpsertOnlineStoreAsync(
            Guid tenantId,
            Guid platformUserId,
            PlatformTenantBootstrapOnlineStoreUpsertRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(OnlineStoreResult ?? ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Failure(
                new ApplicationError("platform_tenants.validation_failed", "Validation failed.")));
        }
    }
}