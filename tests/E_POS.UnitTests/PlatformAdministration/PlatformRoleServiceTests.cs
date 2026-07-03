using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Mappers;
using E_POS.Application.Modules.PlatformAdministration.Services;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformRoleServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 17, 0, 0, TimeSpan.Zero);
    private static readonly Guid SuperAdminRoleId = PlatformAdminSeedConstants.SuperAdministratorRoleId;

    [Fact]
    public async Task GetRolesAsync_WithPermission_ReturnsRoles()
    {
        var service = CreateService(
            new FakePlatformRoleRepository { ListResponse = CreateListResponse() },
            new FakePlatformPermissionCatalogRepository(),
            hasRolesView: true);

        var result = await service.GetRolesAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Roles);
    }

    [Fact]
    public async Task GetRolesAsync_WithoutPermission_ReturnsForbidden()
    {
        var service = CreateService(
            new FakePlatformRoleRepository { ListResponse = CreateListResponse() },
            new FakePlatformPermissionCatalogRepository(),
            hasRolesView: false);

        var result = await service.GetRolesAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_roles.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetRoleAsync_WhenRoleMissing_ReturnsNotFound()
    {
        var service = CreateService(
            new FakePlatformRoleRepository(),
            new FakePlatformPermissionCatalogRepository(),
            hasRolesView: true);

        var result = await service.GetRoleAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_roles.not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidRequest_ReturnsCreatedRole()
    {
        var repository = new FakePlatformRoleRepository();
        var service = CreateService(
            repository,
            new FakePlatformPermissionCatalogRepository(),
            hasRolesCreate: true);

        var result = await service.CreateRoleAsync(
            new CreatePlatformRoleRequest
            {
                RoleCode = "support_operator",
                Name = "Support Operator",
                Description = "Support team role."
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("support_operator", result.Value!.RoleCode);
        Assert.False(result.Value.IsProtected);
        Assert.True(repository.AddCalled);
    }

    [Fact]
    public async Task CreateRoleAsync_WithDuplicateCode_ReturnsConflict()
    {
        var service = CreateService(
            new FakePlatformRoleRepository { RoleCodeExists = true },
            new FakePlatformPermissionCatalogRepository(),
            hasRolesCreate: true);

        var result = await service.CreateRoleAsync(
            new CreatePlatformRoleRequest { RoleCode = "support_operator", Name = "Support Operator" },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_roles.conflict", result.Error.Code);
    }

    [Fact]
    public async Task CreateRoleAsync_WithMissingName_ReturnsValidationFailed()
    {
        var service = CreateService(
            new FakePlatformRoleRepository(),
            new FakePlatformPermissionCatalogRepository(),
            hasRolesCreate: true);

        var result = await service.CreateRoleAsync(
            new CreatePlatformRoleRequest { RoleCode = "support_operator", Name = "  " },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_roles.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task UpdateRoleAsync_ForProtectedRole_ReturnsSystemRoleProtected()
    {
        var repository = new FakePlatformRoleRepository
        {
            RoleEntity = PlatformRole.Create(
                SuperAdminRoleId,
                PlatformRoleCodes.SuperAdministrator,
                "Super Administrator",
                "Protected role.",
                PlatformAuthConstants.ActiveStatus,
                Now)
        };

        var service = CreateService(
            repository,
            new FakePlatformPermissionCatalogRepository(),
            hasRolesUpdate: true);

        var result = await service.UpdateRoleAsync(
            SuperAdminRoleId,
            new UpdatePlatformRoleRequest { Name = "Changed Name" },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_roles.system_role_protected", result.Error.Code);
    }

    [Fact]
    public async Task GetRolePermissionsAsync_WithPermission_ReturnsPermissions()
    {
        var permissionsResponse = new PlatformRolePermissionsResponse(
            Guid.NewGuid(),
            "support_operator",
            "Support Operator",
            [PlatformPermissionCodes.DashboardView],
            [],
            Now);

        var service = CreateService(
            new FakePlatformRoleRepository { RolePermissionsResponse = permissionsResponse },
            new FakePlatformPermissionCatalogRepository(),
            hasRolePermissionsView: true);

        var result = await service.GetRolePermissionsAsync(
            permissionsResponse.RoleId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AssignedPermissionCodes);
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_WithDashboardPermission_ReturnsUpdatedPermissions()
    {
        var roleId = Guid.NewGuid();
        var repository = new FakePlatformRoleRepository
        {
            RoleEntity = PlatformRole.Create(
                roleId,
                "support_operator",
                "Support Operator",
                null,
                PlatformAuthConstants.ActiveStatus,
                Now),
            PermissionIdMap = new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                [PlatformPermissionCodes.DashboardView] = Guid.Parse("62000000-0000-0000-0000-000000000001")
            },
            RolePermissionsResponse = new PlatformRolePermissionsResponse(
                roleId,
                "support_operator",
                "Support Operator",
                [PlatformPermissionCodes.DashboardView],
                [],
                Now)
        };

        var service = CreateService(
            repository,
            new FakePlatformPermissionCatalogRepository(),
            hasRolePermissionsUpdate: true);

        var result = await service.UpdateRolePermissionsAsync(
            roleId,
            new UpdatePlatformRolePermissionsRequest
            {
                PermissionCodes = [PlatformPermissionCodes.DashboardView]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.ReplaceCalled);
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_WithBootstrapPermission_ReturnsValidationFailed()
    {
        var roleId = Guid.NewGuid();
        var service = CreateService(
            new FakePlatformRoleRepository
            {
                RoleEntity = PlatformRole.Create(
                    roleId,
                    "support_operator",
                    "Support Operator",
                    null,
                    PlatformAuthConstants.ActiveStatus,
                    Now),
                PermissionIdMap = new Dictionary<string, Guid>(StringComparer.Ordinal)
            },
            new FakePlatformPermissionCatalogRepository(),
            hasRolePermissionsUpdate: true);

        var result = await service.UpdateRolePermissionsAsync(
            roleId,
            new UpdatePlatformRolePermissionsRequest
            {
                PermissionCodes = [PlatformBootstrapPermissionCodes.AdminAccess]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_roles.validation_failed", result.Error.Code);
        Assert.Contains("platform.admin.access", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_WithUnknownPermission_ReturnsValidationFailed()
    {
        var roleId = Guid.NewGuid();
        var service = CreateService(
            new FakePlatformRoleRepository
            {
                RoleEntity = PlatformRole.Create(
                    roleId,
                    "support_operator",
                    "Support Operator",
                    null,
                    PlatformAuthConstants.ActiveStatus,
                    Now),
                PermissionIdMap = new Dictionary<string, Guid>(StringComparer.Ordinal)
            },
            new FakePlatformPermissionCatalogRepository(),
            hasRolePermissionsUpdate: true);

        var result = await service.UpdateRolePermissionsAsync(
            roleId,
            new UpdatePlatformRolePermissionsRequest
            {
                PermissionCodes = ["platform.unknown.permission"]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_roles.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task UpdateRolePermissionsAsync_ForSuperAdministrator_ReturnsSystemRoleProtected()
    {
        var service = CreateService(
            new FakePlatformRoleRepository
            {
                RoleEntity = PlatformRole.Create(
                    SuperAdminRoleId,
                    PlatformRoleCodes.SuperAdministrator,
                    "Super Administrator",
                    null,
                    PlatformAuthConstants.ActiveStatus,
                    Now)
            },
            new FakePlatformPermissionCatalogRepository(),
            hasRolePermissionsUpdate: true);

        var result = await service.UpdateRolePermissionsAsync(
            SuperAdminRoleId,
            new UpdatePlatformRolePermissionsRequest
            {
                PermissionCodes = [PlatformPermissionCodes.DashboardView]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_roles.system_role_protected", result.Error.Code);
    }

    private static PlatformRoleService CreateService(
        FakePlatformRoleRepository repository,
        FakePlatformPermissionCatalogRepository permissionCatalogRepository,
        bool hasRolesView = false,
        bool hasRolesCreate = false,
        bool hasRolesUpdate = false,
        bool hasRolePermissionsView = false,
        bool hasRolePermissionsUpdate = false)
    {
        return new PlatformRoleService(
            repository,
            permissionCatalogRepository,
            new FakePlatformPermissionChecker(
                hasRolesView,
                hasRolesCreate,
                hasRolesUpdate,
                hasRolePermissionsView,
                hasRolePermissionsUpdate),
            new FakeDateTimeProvider());
    }

    private static PlatformRoleListResponse CreateListResponse()
    {
        return new PlatformRoleListResponse(
        [
            PlatformRoleMapper.ToListItem(
                SuperAdminRoleId,
                PlatformRoleCodes.SuperAdministrator,
                "Super Administrator",
                "Protected role.",
                PlatformAuthConstants.ActiveStatus,
                36,
                1,
                Now,
                Now)
        ]);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly bool _hasRolesView;
        private readonly bool _hasRolesCreate;
        private readonly bool _hasRolesUpdate;
        private readonly bool _hasRolePermissionsView;
        private readonly bool _hasRolePermissionsUpdate;

        public FakePlatformPermissionChecker(
            bool hasRolesView,
            bool hasRolesCreate,
            bool hasRolesUpdate,
            bool hasRolePermissionsView,
            bool hasRolePermissionsUpdate)
        {
            _hasRolesView = hasRolesView;
            _hasRolesCreate = hasRolesCreate;
            _hasRolesUpdate = hasRolesUpdate;
            _hasRolePermissionsView = hasRolePermissionsView;
            _hasRolePermissionsUpdate = hasRolePermissionsUpdate;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            var allowed = permissionCode switch
            {
                _ when string.Equals(permissionCode, PlatformPermissionCodes.RolesView, StringComparison.Ordinal) =>
                    _hasRolesView,
                _ when string.Equals(permissionCode, PlatformPermissionCodes.RolesCreate, StringComparison.Ordinal) =>
                    _hasRolesCreate,
                _ when string.Equals(permissionCode, PlatformPermissionCodes.RolesUpdate, StringComparison.Ordinal) =>
                    _hasRolesUpdate,
                _ when string.Equals(permissionCode, PlatformPermissionCodes.RolePermissionsView, StringComparison.Ordinal) =>
                    _hasRolePermissionsView,
                _ when string.Equals(permissionCode, PlatformPermissionCodes.RolePermissionsUpdate, StringComparison.Ordinal) =>
                    _hasRolePermissionsUpdate,
                _ => false
            };

            return Task.FromResult(allowed);
        }
    }

    private sealed class FakePlatformPermissionCatalogRepository : IPlatformPermissionCatalogRepository
    {
        public Task<IReadOnlyList<PlatformPermissionCatalogItem>> GetActiveBusinessPermissionsAsync(
            CancellationToken cancellationToken)
        {
            IReadOnlyList<PlatformPermissionCatalogItem> permissions =
                PlatformAdminPermissionsSeedData.Definitions
                    .Select(definition => new PlatformPermissionCatalogItem(
                        definition.Id,
                        definition.PermissionCode,
                        definition.Name,
                        definition.Description,
                        PlatformAuthConstants.ActiveStatus))
                    .ToList();

            return Task.FromResult(permissions);
        }
    }

    private sealed class FakePlatformRoleRepository : IPlatformRoleRepository
    {
        public PlatformRoleListResponse ListResponse { get; init; } = new([]);
        public PlatformRoleDetailResponse? DetailResponse { get; init; }
        public PlatformRole? RoleEntity { get; init; }
        public PlatformRolePermissionsResponse? RolePermissionsResponse { get; init; }
        public bool RoleCodeExists { get; init; }
        public IReadOnlyDictionary<string, Guid> PermissionIdMap { get; init; } =
            new Dictionary<string, Guid>(StringComparer.Ordinal);
        public bool AddCalled { get; private set; }
        public bool ReplaceCalled { get; private set; }
        private PlatformRole? _addedRole;

        public Task<PlatformRoleListResponse> GetRolesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ListResponse);
        }

        public Task<PlatformRoleDetailResponse?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken)
        {
            if (DetailResponse is not null)
            {
                return Task.FromResult<PlatformRoleDetailResponse?>(DetailResponse);
            }

            if (RoleEntity is not null && RoleEntity.Id == roleId)
            {
                return Task.FromResult<PlatformRoleDetailResponse?>(
                    PlatformRoleMapper.ToDetail(RoleEntity, 0, 0));
            }

            if (_addedRole is not null && _addedRole.Id == roleId)
            {
                return Task.FromResult<PlatformRoleDetailResponse?>(
                    PlatformRoleMapper.ToDetail(_addedRole, 0, 0));
            }

            return Task.FromResult<PlatformRoleDetailResponse?>(null);
        }

        public Task<PlatformRole?> GetRoleEntityByIdAsync(Guid roleId, CancellationToken cancellationToken)
        {
            return Task.FromResult(RoleEntity?.Id == roleId ? RoleEntity : null);
        }

        public Task<bool> RoleCodeExistsAsync(string roleCode, CancellationToken cancellationToken)
        {
            return Task.FromResult(RoleCodeExists);
        }

        public Task AddRoleAsync(PlatformRole role, CancellationToken cancellationToken)
        {
            AddCalled = true;
            _addedRole = role;
            return Task.CompletedTask;
        }

        public Task UpdateRoleAsync(PlatformRole role, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<PlatformRolePermissionsResponse?> GetRolePermissionsAsync(
            IReadOnlyList<PlatformPermissionDto> availablePermissions,
            Guid roleId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(RolePermissionsResponse);
        }

        public Task<IReadOnlyDictionary<string, Guid>> GetActiveBusinessPermissionIdMapAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(PermissionIdMap);
        }

        public Task ReplaceRolePermissionsAsync(
            Guid roleId,
            IReadOnlyList<Guid> permissionIds,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ReplaceCalled = true;
            return Task.CompletedTask;
        }
    }
}
