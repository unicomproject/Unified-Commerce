using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Controllers;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformReturnPolicyTemplatesApiTests
{
    private static readonly Guid TemplateId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PlatformUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Controller_HasAuthorizePlatformOnlyPolicy()
    {
        var authAttr = typeof(PlatformReturnPolicyTemplatesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("PlatformOnly", authAttr.Policy);

        var routeAttr = typeof(PlatformReturnPolicyTemplatesController).GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(routeAttr);
        Assert.Equal("api/v1/platform/return-policy-templates", routeAttr.Template);
    }

    [Fact]
    public async Task List_AuthenticatedPlatformUser_ReturnsOk()
    {
        var service = new FakeReturnPolicyTemplateService();
        service.ListResult = ApplicationResult<ReturnPolicyTemplateListResponse>.Success(
            new ReturnPolicyTemplateListResponse([], 1, 50, 0));
        var controller = CreateController(service, PlatformUserId);

        var result = await controller.List(1, 50, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<ReturnPolicyTemplateListResponse>>(ok.Value);
        Assert.True(payload.Success);
    }

    [Fact]
    public async Task List_Unauthenticated_ReturnsUnauthorized()
    {
        var service = new FakeReturnPolicyTemplateService();
        var controller = CreateController(service, platformUserId: null);

        var result = await controller.List(1, 50, null, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var service = new FakeReturnPolicyTemplateService();
        service.GetByIdResult = ApplicationResult<ReturnPolicyTemplateResponse>.Failure(
            new ApplicationError("return_policy_templates.not_found", "Template not found"));
        var controller = CreateController(service, PlatformUserId);

        var result = await controller.GetById(TemplateId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        var service = new FakeReturnPolicyTemplateService();
        var response = CreateResponse(TemplateId, "DRAFT");
        service.CreateResult = ApplicationResult<ReturnPolicyTemplateResponse>.Success(response);
        var controller = CreateController(service, PlatformUserId);

        var request = new ReturnPolicyTemplateCreateRequest("STD", "Standard", null, 14, 14, true, true, false, false, "ACTIVE");
        var result = await controller.Create(request, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
    }

    [Fact]
    public async Task Update_PublishedImmutable_ReturnsBadRequest()
    {
        var service = new FakeReturnPolicyTemplateService();
        service.UpdateResult = ApplicationResult<ReturnPolicyTemplateResponse>.Failure(
            new ApplicationError("return_policy_templates.immutable", "Published templates are immutable."));
        var controller = CreateController(service, PlatformUserId);

        var request = new ReturnPolicyTemplateUpdateRequest("STD", "STD Updated", null, 30, 30, true, true, false, false, "ACTIVE");
        var result = await controller.Update(TemplateId, request, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }

    [Fact]
    public async Task Publish_ValidDraft_ReturnsOk()
    {
        var service = new FakeReturnPolicyTemplateService();
        service.PublishResult = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(TemplateId, "PUBLISHED"));
        var controller = CreateController(service, PlatformUserId);

        var result = await controller.Publish(TemplateId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<ReturnPolicyTemplateResponse>>(ok.Value);
        Assert.Equal("PUBLISHED", payload.Data.LifecycleStatus);
    }

    [Fact]
    public async Task Archive_Published_ReturnsOk()
    {
        var service = new FakeReturnPolicyTemplateService();
        service.ArchiveResult = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(TemplateId, "ARCHIVED"));
        var controller = CreateController(service, PlatformUserId);

        var result = await controller.Archive(TemplateId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<ReturnPolicyTemplateResponse>>(ok.Value);
        Assert.Equal("ARCHIVED", payload.Data.LifecycleStatus);
    }

    [Fact]
    public async Task SetDefault_ValidPublished_ReturnsOk()
    {
        var service = new FakeReturnPolicyTemplateService();
        var response = CreateResponse(TemplateId, "PUBLISHED") with { IsPlatformDefault = true };
        service.SetDefaultResult = ApplicationResult<ReturnPolicyTemplateResponse>.Success(response);
        var controller = CreateController(service, PlatformUserId);

        var result = await controller.SetDefault(TemplateId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<LegacyApiResponse<ReturnPolicyTemplateResponse>>(ok.Value);
        Assert.True(payload.Data.IsPlatformDefault);
    }

    [Fact]
    public async Task Duplicate_ValidTemplate_ReturnsCreated()
    {
        var service = new FakeReturnPolicyTemplateService();
        var duplicatedId = Guid.NewGuid();
        service.DuplicateResult = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(duplicatedId, "DRAFT"));
        var controller = CreateController(service, PlatformUserId);

        var result = await controller.Duplicate(TemplateId, new PlatformReturnPolicyTemplatesController.DuplicateReturnPolicyTemplateRequest("DUP", "Duplicate"), CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
    }

    [Fact]
    public async Task Delete_ValidDraft_ReturnsNoContent()
    {
        var service = new FakeReturnPolicyTemplateService();
        service.DeleteResult = ApplicationResult.Success();
        var controller = CreateController(service, PlatformUserId);

        var result = await controller.Delete(TemplateId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    private static PlatformReturnPolicyTemplatesController CreateController(IReturnPolicyTemplateService service, Guid? platformUserId)
    {
        var claims = new List<Claim>();
        if (platformUserId.HasValue)
        {
            claims.Add(new Claim("sub", platformUserId.Value.ToString()));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, platformUserId.Value.ToString()));
        }

        var identity = claims.Count > 0
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();

        return new PlatformReturnPolicyTemplatesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static ReturnPolicyTemplateResponse CreateResponse(Guid id, string lifecycleStatus) =>
        new(
            Id: id,
            TemplateCode: "TMPL",
            Name: "Template",
            Description: null,
            ReturnWindowDays: 14,
            ExchangeWindowDays: 14,
            RequiresReceipt: true,
            AllowDefectiveReturn: true,
            RequiresManagerApproval: false,
            IsPlatformDefault: false,
            VersionNumber: 1,
            LifecycleStatus: lifecycleStatus,
            ConcurrencyToken: Guid.NewGuid(),
            Status: "ACTIVE",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);

    private sealed class FakeReturnPolicyTemplateService : IReturnPolicyTemplateService
    {
        public ApplicationResult<ReturnPolicyTemplateListResponse> ListResult { get; set; } = ApplicationResult<ReturnPolicyTemplateListResponse>.Success(new([], 1, 50, 0));
        public ApplicationResult<ReturnPolicyTemplateResponse> GetByIdResult { get; set; } = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(Guid.NewGuid(), "DRAFT"));
        public ApplicationResult<ReturnPolicyTemplateResponse> CreateResult { get; set; } = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(Guid.NewGuid(), "DRAFT"));
        public ApplicationResult<ReturnPolicyTemplateResponse> UpdateResult { get; set; } = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(Guid.NewGuid(), "DRAFT"));
        public ApplicationResult<ReturnPolicyTemplateResponse> PublishResult { get; set; } = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(Guid.NewGuid(), "PUBLISHED"));
        public ApplicationResult<ReturnPolicyTemplateResponse> ArchiveResult { get; set; } = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(Guid.NewGuid(), "ARCHIVED"));
        public ApplicationResult<ReturnPolicyTemplateResponse> SetDefaultResult { get; set; } = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(Guid.NewGuid(), "PUBLISHED"));
        public ApplicationResult<ReturnPolicyTemplateResponse> DuplicateResult { get; set; } = ApplicationResult<ReturnPolicyTemplateResponse>.Success(CreateResponse(Guid.NewGuid(), "DRAFT"));
        public ApplicationResult DeleteResult { get; set; } = ApplicationResult.Success();

        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> CreateAsync(Guid actorPlatformUserId, ReturnPolicyTemplateCreateRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(CreateResult);

        public Task<ApplicationResult<ReturnPolicyTemplateListResponse>> ListAsync(Guid actorPlatformUserId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) =>
            Task.FromResult(ListResult);

        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> GetByIdAsync(Guid actorPlatformUserId, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(GetByIdResult);

        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> UpdateAsync(Guid actorPlatformUserId, Guid id, ReturnPolicyTemplateUpdateRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(UpdateResult);

        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> PublishAsync(Guid actorPlatformUserId, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(PublishResult);

        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> ArchiveAsync(Guid actorPlatformUserId, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(ArchiveResult);

        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> SetDefaultAsync(Guid actorPlatformUserId, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(SetDefaultResult);

        public Task<ApplicationResult<ReturnPolicyTemplateResponse>> DuplicateAsync(Guid actorPlatformUserId, Guid id, string? newCode, string? newName, CancellationToken cancellationToken) =>
            Task.FromResult(DuplicateResult);

        public Task<ApplicationResult> DeleteAsync(Guid actorPlatformUserId, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(DeleteResult);
    }
}
