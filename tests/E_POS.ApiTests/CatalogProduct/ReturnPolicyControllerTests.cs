using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.CatalogProduct;

public sealed class ReturnPolicyControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReturnPolicyCreate_WithTenantClaims_PassesServerTenantContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = new ReturnPolicyResponse(Guid.NewGuid(), "7DAYS", "7 Days", 7, ReturnPolicyConstants.ActiveStatus, Now, Now);
        var service = new FakeReturnPolicyService { CreateResult = ApplicationResult<ReturnPolicyResponse>.Success(response) };
        var controller = CreateReturnPolicyController(service);
        SetTenantClaims(controller, tenantId, userId, ReturnPolicyConstants.CreatePermission);

        var result = await controller.Create(new ReturnPolicyCreateRequest("7DAYS", "7 Days", 7, ReturnPolicyConstants.ActiveStatus), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Same(response, created.Value);
        Assert.Equal(tenantId, service.Context?.TenantId);
        Assert.Equal(userId, service.Context?.UserId);
    }

    [Fact]
    public async Task TemplateList_WithoutPlatformUserClaim_ReturnsUnauthorized()
    {
        var controller = new PlatformReturnPolicyTemplatesController(new FakeTemplateService()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var result = await controller.List(cancellationToken: CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void ReturnPolicyController_RequiresTenantOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(ReturnPoliciesController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("TenantOnly", authorize.Policy);
    }

    [Fact]
    public void PlatformTemplateController_RequiresPlatformOnlyPolicy()
    {
        var authorize = Assert.Single(typeof(PlatformReturnPolicyTemplatesController).GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("PlatformOnly", authorize.Policy);
    }

    private static ReturnPoliciesController CreateReturnPolicyController(FakeReturnPolicyService service)
    {
        return new ReturnPoliciesController(service, new TenantRequestContextFactory()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    }

    private static void SetTenantClaims(ControllerBase controller, Guid tenantId, Guid userId, string permission)
    {
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("permissions", permission)
        ], "Test"));
    }

    private sealed class FakeReturnPolicyService : IReturnPolicyService
    {
        public TenantRequestContext? Context { get; private set; }
        public ApplicationResult<ReturnPolicyResponse> CreateResult { get; init; } = ApplicationResult<ReturnPolicyResponse>.Failure(new ApplicationError("return_policies.not_configured", "Not configured."));
        public Task<ApplicationResult<ReturnPolicyResponse>> CreateAsync(TenantRequestContext context, ReturnPolicyCreateRequest request, CancellationToken cancellationToken) { Context = context; return Task.FromResult(CreateResult); }
        public Task<ApplicationResult<ReturnPolicyListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) { Context = context; return Task.FromResult(ApplicationResult<ReturnPolicyListResponse>.Success(new ReturnPolicyListResponse([], pageNumber, pageSize, 0))); }
        public Task<ApplicationResult<ReturnPolicyResponse>> GetByIdAsync(TenantRequestContext context, Guid policyId, CancellationToken cancellationToken) { Context = context; return Task.FromResult(CreateResult); }
        public Task<ApplicationResult<ReturnPolicyResponse>> UpdateAsync(TenantRequestContext context, Guid policyId, ReturnPolicyUpdateRequest request, CancellationToken cancellationToken) { Context = context; return Task.FromResult(CreateResult); }
        public Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid policyId, CancellationToken cancellationToken) { Context = context; return Task.FromResult(ApplicationResult.Success()); }
    }

    private sealed class FakeTemplateService : IReturnPolicyTemplateService
    {
        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> CreateAsync(Guid platformUserId, ReturnPolicyTemplateCreateRequest request, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<ReturnPolicyTemplateResponse>.Failure(new ApplicationError("return_policy_templates.not_configured", "Not configured.")));
        public Task<ApplicationResult<ReturnPolicyTemplateListResponse>> ListAsync(Guid platformUserId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<ReturnPolicyTemplateListResponse>.Success(new ReturnPolicyTemplateListResponse([], pageNumber, pageSize, 0)));
        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> GetByIdAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<ReturnPolicyTemplateResponse>.Failure(new ApplicationError("return_policy_templates.not_configured", "Not configured.")));
        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> UpdateAsync(Guid platformUserId, Guid templateId, ReturnPolicyTemplateUpdateRequest request, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult<ReturnPolicyTemplateResponse>.Failure(new ApplicationError("return_policy_templates.not_configured", "Not configured.")));
        public Task<ApplicationResult> DeleteAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken) => Task.FromResult(ApplicationResult.Success());
    }
}