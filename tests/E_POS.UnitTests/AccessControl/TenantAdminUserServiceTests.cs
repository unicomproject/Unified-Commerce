using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.AccessControl.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class TenantAdminUserServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid RoleId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_WithoutCreateOrInvitePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.CreateAsync(CreateContext([]), CreateValidRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithCreatePermission_ReturnsSuccess()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_WithMissingFullName_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());
        var request = CreateValidRequest() with { FullName = "" };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidEmail_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());
        var request = CreateValidRequest() with { Email = "not-an-email" };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenRoleNotInTenant_ReturnsRoleNotFound()
    {
        var service = CreateService(new FakeTenantAdminUserRepository { RoleBelongsToTenant = false });

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.role_not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenOutletNotInTenant_ReturnsOutletNotFound()
    {
        var service = CreateService(new FakeTenantAdminUserRepository { OutletsBelongToTenant = false });
        var request = CreateValidRequest() with { OutletIds = [OutletId] };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.outlet_not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsDuplicateEmail()
    {
        var service = CreateService(new FakeTenantAdminUserRepository { EmailExists = true });

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.duplicate_email", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithSendInviteEmailTrue_CreatesInviteRecord()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with { SendInviteEmail = true };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.CreatedInvite);
        Assert.Equal(TenantUserConstants.StatusInvited, repository.CreatedUser?.AccountStatus);
    }

    [Fact]
    public async Task CreateAsync_WithSendInviteEmailFalse_CreatesDraftUserWithoutInvite()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with { SendInviteEmail = false };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(repository.CreatedInvite);
        Assert.Equal(TenantUserConstants.StatusInactive, repository.CreatedUser?.AccountStatus);
    }

    [Fact]
    public async Task CreateAsync_WithOverridePermissionsButNoOverrideGrant_IgnoresOverrides()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var permissionId = Guid.NewGuid();
        var request = CreateValidRequest() with
        {
            PermissionOverrideEnabled = true,
            OverriddenPermissionIds = [permissionId],
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.CreatedOverriddenPermissionIds);
    }

    [Fact]
    public async Task CreateAsync_WithOverridePermissionsAndOverrideGrant_PersistsOverrides()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var permissionId = Guid.NewGuid();
        var request = CreateValidRequest() with
        {
            PermissionOverrideEnabled = true,
            OverriddenPermissionIds = [permissionId],
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create, TenantAdminUserPermissions.PermissionOverride]),
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(permissionId, repository.CreatedOverriddenPermissionIds);
    }

    [Fact]
    public async Task DeleteAsync_WhenTargetingSelf_ReturnsCannotDeleteSelf()
    {
        var user = TenantUser.Create(
            UserId, TenantId, "user@example.com", "User", null, null, "hash", "salt",
            TenantUserConstants.StatusActive, "admin", "admin", "HQ", Now);
        var repository = new FakeTenantAdminUserRepository { EditableUser = user };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminUserPermissions.Delete]),
            UserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.cannot_delete_self", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WithActiveTillSession_ReturnsDeleteConflict()
    {
        var otherUserId = Guid.NewGuid();
        var user = TenantUser.Create(
            otherUserId, TenantId, "user@example.com", "User", null, null, "hash", "salt",
            TenantUserConstants.StatusActive, "admin", "admin", "HQ", Now);
        var repository = new FakeTenantAdminUserRepository { EditableUser = user, HasActiveTillSession = true };
        var service = CreateService(repository);

        var result = await service.DeleteAsync(
            CreateContext([TenantAdminUserPermissions.Delete]),
            otherUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.delete_conflict", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WithoutDeletePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.DeleteAsync(CreateContext([]), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_denied", result.Error.Code);
    }

    private static TenantAdminUserService CreateService(FakeTenantAdminUserRepository repository)
    {
        return new TenantAdminUserService(repository, new FakeDateTimeProvider(), new FakePasswordHashService());
    }

    private static TenantRequestContext CreateContext(IReadOnlyCollection<string>? permissions = null)
    {
        return new TenantRequestContext(TenantId, UserId, permissions ?? [TenantAdminUserPermissions.Manage]);
    }

    private static TenantAdminUserCreateRequest CreateValidRequest()
    {
        return new TenantAdminUserCreateRequest(
            "Jane Doe",
            "jane.doe@example.com",
            "+1234567890",
            RoleId,
            [],
            false,
            [],
            false);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => $"HASH:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"HASH:{password}";
    }

    private sealed class FakeTenantAdminUserRepository : ITenantAdminUserRepository
    {
        public bool RoleBelongsToTenant { get; init; } = true;
        public bool OutletsBelongToTenant { get; init; } = true;
        public bool EmailExists { get; init; }
        public bool PermissionIdsExist { get; init; } = true;
        public TenantUser? EditableUser { get; init; }
        public bool HasActiveTillSession { get; init; }
        public bool HasSalesReferences { get; init; }

        public TenantUser? CreatedUser { get; private set; }
        public UserInvite? CreatedInvite { get; private set; }
        public IReadOnlyCollection<Guid> CreatedOverriddenPermissionIds { get; private set; } = [];

        public Task<TenantAdminUserListResponse> ListAsync(
            Guid tenantId, string? search, string? status, Guid? roleId, Guid? outletId,
            int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken) =>
            Task.FromResult(new TenantAdminUserListResponse([], page, pageSize, 0));

        public Task<IReadOnlyList<RoleOptionResponse>> GetRoleOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RoleOptionResponse>>([]);

        public Task<IReadOnlyList<OutletOptionResponse>> GetOutletOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OutletOptionResponse>>([]);

        public Task<IReadOnlyList<PermissionGroupResponse>> GetPermissionGroupsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PermissionGroupResponse>>([]);

        public Task<bool> RoleBelongsToTenantAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult(RoleBelongsToTenant);

        public Task<bool> OutletsBelongToTenantAsync(Guid tenantId, IReadOnlyCollection<Guid> outletIds, CancellationToken cancellationToken) =>
            Task.FromResult(outletIds.Count == 0 || OutletsBelongToTenant);

        public Task<bool> EmailExistsForTenantAsync(Guid tenantId, string normalizedEmail, Guid? excludeUserId, CancellationToken cancellationToken) =>
            Task.FromResult(EmailExists);

        public Task<bool> PermissionIdsExistAsync(IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken) =>
            Task.FromResult(permissionIds.Count == 0 || PermissionIdsExist);

        public Task<Guid> CreateAsync(
            TenantUser user, Guid roleId, IReadOnlyCollection<Guid> outletIds,
            IReadOnlyCollection<Guid> overriddenPermissionIds, UserInvite? invite,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            CreatedUser = user;
            CreatedInvite = invite;
            CreatedOverriddenPermissionIds = overriddenPermissionIds;
            return Task.FromResult(user.Id);
        }

        public Task<TenantAdminUserDetailResponse?> GetDetailAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<TenantAdminUserDetailResponse?>(new TenantAdminUserDetailResponse(
                userId, "Jane Doe", "JANE.DOE@EXAMPLE.COM", "+1234567890", RoleId, "Store Manager",
                [], "Active", false, [], null, Now, null));

        public Task<TenantUser?> GetEditableAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(EditableUser ?? TenantUser.Create(
                userId, tenantId, "user@example.com", "User", null, null, "hash", "salt",
                TenantUserConstants.StatusActive, "admin", "admin", "HQ", Now));

        public Task ReplaceAssignmentsAsync(
            Guid tenantId, Guid userId, Guid roleId, IReadOnlyCollection<Guid> outletIds,
            bool permissionOverrideEnabled, IReadOnlyCollection<Guid> overriddenPermissionIds,
            Guid actingUserId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> HasSalesReferencesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(HasSalesReferences);

        public Task<bool> HasActiveTillSessionAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(HasActiveTillSession);
    }
}
