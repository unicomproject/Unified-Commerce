using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.AccessControl.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class TenantAdminRoleServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ActorUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_IgnoresClientRoleCode_AndGeneratesBackendControlledCode()
    {
        var repository = new FakeTenantAdminRoleRepository();
        var idempotency = new FakeIdempotencyService();
        var service = CreateService(repository, idempotency);
        var context = Context(TenantAdminUserPermissions.RolesCreate);

        var result = await service.CreateAsync(
            context,
            new TenantAdminRoleCreateRequest("Floor Manager", "platform.super_admin", "Manages floor"),
            CancellationToken.None,
            "role-create-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("FLOOR_MANAGER", repository.CreatedRole?.RoleCode);
        Assert.False((repository.CreatedRole?.RoleCode ?? string.Empty).StartsWith("platform.", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, idempotency.ExecuteCallCount);
    }

    [Fact]
    public async Task Create_WithoutRoleCreatePermission_DoesNotEnterIdempotencyPipeline()
    {
        var repository = new FakeTenantAdminRoleRepository();
        var idempotency = new FakeIdempotencyService();
        var service = CreateService(repository, idempotency);

        var result = await service.CreateAsync(
            Context(),
            new TenantAdminRoleCreateRequest("Manager", "MANAGER", null),
            CancellationToken.None,
            "role-create-2");

        Assert.True(result.IsFailure);
        Assert.Equal("tenant_roles.permission_denied", result.Error.Code);
        Assert.Equal(0, idempotency.ExecuteCallCount);
        Assert.Null(repository.CreatedRole);
    }

    [Fact]
    public async Task ReplacePermissions_WhenActorCannotDelegateRequestedPermission_ReturnsForbiddenContract()
    {
        var role = ExistingRole(updatedAt: Now);
        var repository = new FakeTenantAdminRoleRepository { EditableRole = role };
        var service = CreateService(repository);

        var result = await service.ReplacePermissionsAsync(
            Context(TenantAdminUserPermissions.RolesPermissionsUpdate),
            role.Id,
            new TenantRolePermissionsUpdateRequest(["tenant.users.manage"], Now),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("tenant_roles.delegation_ceiling_exceeded", result.Error.Code);
        Assert.Equal(0, repository.ReplacePermissionsCallCount);
    }

    [Fact]
    public async Task ReplaceAssignments_WhenPostStateRemovesFinalAdmin_ReturnsLastAdminProtection()
    {
        var role = ExistingRole(updatedAt: Now);
        var repository = new FakeTenantAdminRoleRepository
        {
            EditableRole = role,
            AssignmentReplacementRemovesLastAdmin = true
        };
        var service = CreateService(repository);

        var result = await service.ReplaceAssignmentsAsync(
            Context(TenantAdminUserPermissions.RolesAssignmentsUpdate),
            role.Id,
            new TenantRoleAssignmentsUpdateRequest(
                [new TenantAdminRoleAssignmentRequest(Guid.NewGuid(), "TENANT_WIDE")],
                Now),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("tenant_roles.last_admin_protected", result.Error.Code);
        Assert.Equal(0, repository.ReplaceAssignmentsCallCount);
    }

    [Fact]
    public async Task Update_WithStaleExpectedUpdatedAt_ReturnsConcurrencyConflict()
    {
        var role = ExistingRole(updatedAt: Now);
        var repository = new FakeTenantAdminRoleRepository { EditableRole = role };
        var service = CreateService(repository);

        var result = await service.UpdateAsync(
            Context(TenantAdminUserPermissions.RolesUpdate),
            role.Id,
            new TenantAdminRoleUpdateRequest("Manager", "IGNORED", null, Now.AddSeconds(-1)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("tenant_roles.concurrency_conflict", result.Error.Code);
    }

    private static TenantAdminRoleService CreateService(
        FakeTenantAdminRoleRepository repository,
        FakeIdempotencyService? idempotency = null) =>
        new(repository, idempotency ?? new FakeIdempotencyService(), new FakeDateTimeProvider());

    private static TenantRequestContext Context(params string[] permissions) =>
        new(TenantId, ActorUserId, permissions);

    private static TenantRole ExistingRole(DateTimeOffset updatedAt)
    {
        return TenantRole.Create(
            Guid.NewGuid(),
            TenantId,
            null,
            null,
            "MANAGER",
            "Manager",
            null,
            true,
            true,
            ActorUserId,
            updatedAt);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeIdempotencyService : IIdempotencyService
    {
        public int ExecuteCallCount { get; private set; }

        public async Task<ApplicationResult<T>> ExecuteAsync<T>(
            Guid tenantId,
            Guid actorUserId,
            string operation,
            string idempotencyKey,
            string requestHash,
            Func<CancellationToken, Task<ApplicationResult<T>>> operationFunc,
            CancellationToken cancellationToken)
        {
            ExecuteCallCount++;
            return await operationFunc(cancellationToken);
        }
    }

    private sealed class FakeTenantAdminRoleRepository : ITenantAdminRoleRepository
    {
        public TenantRole? EditableRole { get; init; }
        public TenantRole? CreatedRole { get; private set; }
        public bool AssignmentReplacementRemovesLastAdmin { get; init; }
        public int ReplacePermissionsCallCount { get; private set; }
        public int ReplaceAssignmentsCallCount { get; private set; }

        public Task<TenantAdminRoleListResponse> ListAsync(Guid tenantId, string? search, string? status, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(new TenantAdminRoleListResponse([], page, pageSize, 0, 0));

        public Task<TenantAdminRoleDetailResponse?> GetDetailAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
        {
            var role = CreatedRole?.Id == roleId ? CreatedRole : EditableRole;
            return Task.FromResult(role is null ? null : ToDetail(role));
        }

        public Task<TenantRole?> GetEditableAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult(EditableRole?.Id == roleId ? EditableRole : null);

        public Task<bool> RoleCodeExistsAsync(Guid tenantId, string roleCode, Guid? excludeRoleId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> RoleNameExistsAsync(Guid tenantId, string roleName, Guid? excludeRoleId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddAsync(TenantRole role, CancellationToken cancellationToken)
        {
            CreatedRole = role;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PermissionDefinition>> GetAssignablePermissionsByCodeAsync(
            Guid tenantId,
            IReadOnlyCollection<string> permissionCodes,
            IReadOnlyCollection<string> actorPermissionCodes,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            var permissions = permissionCodes
                .Where(code => actorPermissionCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
                .Select(code => PermissionDefinition.Create(Guid.NewGuid(), code, Guid.NewGuid(), Guid.NewGuid(), "view", null, false, true, now))
                .ToArray();
            return Task.FromResult<IReadOnlyList<PermissionDefinition>>(permissions);
        }

        public Task<TenantPermissionCatalogResponse> GetPermissionCatalogAsync(Guid tenantId, IReadOnlyCollection<string> actorPermissionCodes, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(new TenantPermissionCatalogResponse([]));

        public Task<TenantRolePermissionsResponse?> GetPermissionsAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult<TenantRolePermissionsResponse?>(new TenantRolePermissionsResponse(roleId, "MANAGER", "Manager", "TENANT", false, [], [], Now));

        public Task ReplacePermissionsAsync(Guid tenantId, Guid roleId, IReadOnlyCollection<Guid> permissionIds, Guid actorUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            ReplacePermissionsCallCount++;
            return Task.CompletedTask;
        }

        public Task<TenantRoleAssignmentsResponse?> GetAssignmentsAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult<TenantRoleAssignmentsResponse?>(new TenantRoleAssignmentsResponse(roleId, "MANAGER", "Manager", false, [], Now));

        public Task<RoleAssignmentValidationResult> ValidateAssignmentsAsync(Guid tenantId, IReadOnlyCollection<TenantAdminRoleAssignmentRequest> assignments, CancellationToken cancellationToken) =>
            Task.FromResult(RoleAssignmentValidationResult.Valid);

        public Task ReplaceAssignmentsAsync(Guid tenantId, Guid roleId, IReadOnlyCollection<TenantAdminRoleAssignmentRequest> assignments, Guid actorUserId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            ReplaceAssignmentsCallCount++;
            return Task.CompletedTask;
        }

        public Task<bool> WouldRemoveLastAdminAsync(Guid tenantId, Guid roleId, IReadOnlyCollection<Guid>? replacementPermissionIds, bool? replacementIsActive, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> WouldReplaceAssignmentsRemoveLastAdminAsync(Guid tenantId, Guid roleId, IReadOnlyCollection<TenantAdminRoleAssignmentRequest> replacementAssignments, CancellationToken cancellationToken) =>
            Task.FromResult(AssignmentReplacementRemovesLastAdmin);

        public Task AddAuditAsync(Guid tenantId, Guid actorUserId, Guid roleId, string action, object? payload, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        private static TenantAdminRoleDetailResponse ToDetail(TenantRole role) =>
            new(role.Id, role.RoleCode, role.RoleName, role.RoleDescription, role.IsActive, role.IsCustom != true, 0, 0, role.CreatedAt, role.UpdatedAt ?? role.CreatedAt);
    }
}
