using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Application.Modules.CatalogProduct.Services;
using E_POS.Application.Modules.CatalogProduct.Validators;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ReturnPolicyServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TemplateCreateAsync_WithoutPermission_ReturnsAccessDenied()
    {
        var service = new ReturnPolicyTemplateService(new FakeTemplateRepository(), new ReturnPolicyTemplateRequestValidator(), new FakePlatformPermissionChecker([]), new FakeDateTimeProvider());

        var result = await service.CreateAsync(Guid.NewGuid(), new ReturnPolicyTemplateCreateRequest("7days", "7 Days", 7, ReturnPolicyTemplateConstants.ActiveStatus), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("return_policy_templates.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task TemplateCreateAsync_WithPermission_NormalizesCodeAndPersists()
    {
        var repository = new FakeTemplateRepository();
        var service = new ReturnPolicyTemplateService(repository, new ReturnPolicyTemplateRequestValidator(), new FakePlatformPermissionChecker([PlatformPermissionCodes.ReturnPolicyTemplatesCreate]), new FakeDateTimeProvider());

        var result = await service.CreateAsync(Guid.NewGuid(), new ReturnPolicyTemplateCreateRequest(" 7days ", "7 Days", 7, ReturnPolicyTemplateConstants.ActiveStatus), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("7DAYS", repository.AddedTemplate?.TemplateCode);
    }

    [Fact]
    public async Task ReturnPolicyCreateAsync_WithPermission_NormalizesCodeAndPersists()
    {
        var repository = new FakePolicyRepository();
        var service = new ReturnPolicyService(repository, new ReturnPolicyRequestValidator(), new FakeDateTimeProvider());

        var result = await service.CreateAsync(CreateContext([ReturnPolicyConstants.CreatePermission]), new ReturnPolicyCreateRequest(" no_return ", "No Return", 0, ReturnPolicyConstants.ActiveStatus), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("NO_RETURN", repository.AddedPolicy?.PolicyCode);
        Assert.Equal(TenantId, repository.AddedPolicy?.TenantId);
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string> permissions) => new(TenantId, UserId, permissions);

    private sealed class FakeDateTimeProvider : IDateTimeProvider { public DateTimeOffset UtcNow => Now; }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly IReadOnlyCollection<string> _permissions;
        public FakePlatformPermissionChecker(IReadOnlyCollection<string> permissions) => _permissions = permissions;
        public Task<bool> HasPermissionAsync(Guid platformUserId, string permissionCode, CancellationToken cancellationToken) => Task.FromResult(_permissions.Contains(permissionCode));
    }

    private sealed class FakeTemplateRepository : IReturnPolicyTemplateRepository
    {
        public ReturnPolicyTemplate? AddedTemplate { get; private set; }
        public Task<bool> TemplateCodeExistsAsync(string templateCode, Guid? excludeTemplateId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<ReturnPolicyTemplateListResponse> ListAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(new ReturnPolicyTemplateListResponse([], pageNumber, pageSize, 0));
        public Task<ReturnPolicyTemplateResponse?> GetByIdAsync(Guid templateId, bool includeDeleted, CancellationToken cancellationToken) => Task.FromResult<ReturnPolicyTemplateResponse?>(new ReturnPolicyTemplateResponse(templateId, AddedTemplate!.TemplateCode, AddedTemplate.Name, AddedTemplate.ReturnWindowDays, AddedTemplate.Status, AddedTemplate.CreatedAt, AddedTemplate.UpdatedAt));
        public Task<ReturnPolicyTemplate?> GetEditableAsync(Guid templateId, CancellationToken cancellationToken) => Task.FromResult<ReturnPolicyTemplate?>(AddedTemplate);
        public Task AddAsync(ReturnPolicyTemplate template, CancellationToken cancellationToken) { AddedTemplate = template; return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePolicyRepository : IReturnPolicyRepository
    {
        public ReturnPolicy? AddedPolicy { get; private set; }
        public Task<bool> PolicyCodeExistsAsync(Guid tenantId, string policyCode, Guid? excludePolicyId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<ReturnPolicyListResponse> ListAsync(Guid tenantId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken) => Task.FromResult(new ReturnPolicyListResponse([], pageNumber, pageSize, 0));
        public Task<ReturnPolicyResponse?> GetByIdAsync(Guid tenantId, Guid policyId, bool includeDeleted, CancellationToken cancellationToken) => Task.FromResult<ReturnPolicyResponse?>(new ReturnPolicyResponse(policyId, AddedPolicy!.PolicyCode, AddedPolicy.Name, AddedPolicy.ReturnWindowDays, AddedPolicy.Status, AddedPolicy.CreatedAt, AddedPolicy.UpdatedAt));
        public Task<ReturnPolicy?> GetEditableAsync(Guid tenantId, Guid policyId, CancellationToken cancellationToken) => Task.FromResult<ReturnPolicy?>(AddedPolicy);
        public Task AddAsync(ReturnPolicy policy, CancellationToken cancellationToken) { AddedPolicy = policy; return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}