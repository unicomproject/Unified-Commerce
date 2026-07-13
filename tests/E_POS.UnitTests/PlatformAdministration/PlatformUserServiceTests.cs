using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformUserServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid SuperAdminUserId = PlatformAdminSeedConstants.DevelopmentPlatformUserId;
    private static readonly Guid SupportRoleId = Guid.Parse("52000000-0000-0000-0000-000000000099");

    [Fact]
    public async Task GetUsersAsync_WithPermission_ReturnsUsers()
    {
        var service = CreateService(
            new FakePlatformUserRepository { ListResponse = CreateListResponse() },
            hasUsersView: true);

        var result = await service.GetUsersAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Users);
    }

    [Fact]
    public async Task GetUsersAsync_WithoutPermission_ReturnsForbidden()
    {
        var service = CreateService(
            new FakePlatformUserRepository { ListResponse = CreateListResponse() },
            hasUsersView: false);

        var result = await service.GetUsersAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task GetUserAsync_WhenMissing_ReturnsNotFound()
    {
        var service = CreateService(new FakePlatformUserRepository(), hasUsersView: true);

        var result = await service.GetUserAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidRequest_ReturnsCreatedUser()
    {
        var repository = new FakePlatformUserRepository
        {
            ResolvedRoles =
            [
                new ResolvedPlatformRole(SupportRoleId, "support_operator", "Support Operator")
            ]
        };

        var service = CreateService(repository, hasUsersCreate: true);

        var result = await service.CreateUserAsync(
            new CreatePlatformUserRequest
            {
                Email = "support.user@example.com",
                RoleCodes = ["support_operator"]
            },
            SuperAdminUserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("support.user@example.com", result.Value!.Email);
        Assert.True(result.Value.InvitePending);
        Assert.True(repository.AddCalled);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_ReturnsConflict()
    {
        var service = CreateService(
            new FakePlatformUserRepository
            {
                EmailExists = true,
                ResolvedRoles =
                [
                    new ResolvedPlatformRole(SupportRoleId, "support_operator", "Support Operator")
                ]
            },
            hasUsersCreate: true);

        var result = await service.CreateUserAsync(
            new CreatePlatformUserRequest
            {
                Email = "duplicate@example.com",
                RoleCodes = ["support_operator"]
            },
            SuperAdminUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.conflict", result.Error.Code);
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidRole_ReturnsValidationFailed()
    {
        var service = CreateService(
            new FakePlatformUserRepository(),
            hasUsersCreate: true);

        var result = await service.CreateUserAsync(
            new CreatePlatformUserRequest
            {
                Email = "support.user@example.com",
                RoleCodes = ["unknown_role"]
            },
            SuperAdminUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateUserAsync_WithoutPermission_ReturnsForbidden()
    {
        var service = CreateService(new FakePlatformUserRepository(), hasUsersCreate: false);

        var result = await service.CreateUserAsync(
            new CreatePlatformUserRequest
            {
                Email = "support.user@example.com",
                RoleCodes = ["support_operator"]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.access_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateUserAsync_WithProtectedRoleWithoutSuperAdmin_ReturnsProtectedRoleDenied()
    {
        var service = CreateService(
            new FakePlatformUserRepository
            {
                ResolvedRoles =
                [
                    new ResolvedPlatformRole(
                        PlatformAdminSeedConstants.SuperAdministratorRoleId,
                        PlatformRoleCodes.SuperAdministrator,
                        "Super Administrator")
                ],
                RequesterHasSuperAdminRole = false
            },
            hasUsersCreate: true);

        var result = await service.CreateUserAsync(
            new CreatePlatformUserRequest
            {
                Email = "new.admin@example.com",
                RoleCodes = [PlatformRoleCodes.SuperAdministrator]
            },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.protected_role_denied", result.Error.Code);
    }

    [Fact]
    public async Task UpdateUserAsync_WithValidStatus_ReturnsUpdatedUser()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePlatformUserRepository
        {
            UserEntity = PlatformUser.CreatePendingInvite(userId, "support.user@example.com", Now),
            DetailResponse = CreateDetailResponse(userId, "support.user@example.com")
        };

        var service = CreateService(repository, hasUsersUpdate: true);

        var result = await service.UpdateUserAsync(
            userId,
            new UpdatePlatformUserRequest { Status = PlatformAuthConstants.ActiveStatus },
            SuperAdminUserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformAuthConstants.ActiveStatus, repository.UserEntity!.Status);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenMissing_ReturnsNotFound()
    {
        var service = CreateService(new FakePlatformUserRepository(), hasUsersUpdate: true);

        var result = await service.UpdateUserAsync(
            Guid.NewGuid(),
            new UpdatePlatformUserRequest { Status = PlatformAuthConstants.InactiveStatus },
            SuperAdminUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.not_found", result.Error.Code);
    }

    [Fact]
    public async Task UpdateUserAsync_LastSuperAdminSelfDeactivation_ReturnsLockout()
    {
        var repository = new FakePlatformUserRepository
        {
            UserEntity = PlatformUser.Create(
                SuperAdminUserId,
                "admin@example.com",
                "hash",
                PlatformAuthConstants.ActiveStatus,
                Now),
            UserRoleCodes = [PlatformRoleCodes.SuperAdministrator],
            ActiveSuperAdministratorsExcludingTarget = 0
        };

        var service = CreateService(repository, hasUsersUpdate: true);

        var result = await service.UpdateUserAsync(
            SuperAdminUserId,
            new UpdatePlatformUserRequest { Status = PlatformAuthConstants.InactiveStatus },
            SuperAdminUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.super_admin_lockout", result.Error.Code);
    }

    [Fact]
    public async Task AssignRolesAsync_WithValidRoles_ReturnsUpdatedUser()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePlatformUserRepository
        {
            UserEntity = PlatformUser.CreatePendingInvite(userId, "support.user@example.com", Now),
            ResolvedRoles =
            [
                new ResolvedPlatformRole(SupportRoleId, "support_operator", "Support Operator")
            ],
            DetailResponse = CreateDetailResponse(userId, "support.user@example.com")
        };

        var service = CreateService(repository, hasUsersRolesAssign: true);

        var result = await service.AssignRolesAsync(
            userId,
            new AssignPlatformUserRolesRequest { RoleCodes = ["support_operator"] },
            SuperAdminUserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.ReplaceRolesCalled);
    }

    [Fact]
    public async Task AssignRolesAsync_WithInvalidRole_ReturnsValidationFailed()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePlatformUserRepository
        {
            UserEntity = PlatformUser.CreatePendingInvite(userId, "support.user@example.com", Now)
        };

        var service = CreateService(repository, hasUsersRolesAssign: true);

        var result = await service.AssignRolesAsync(
            userId,
            new AssignPlatformUserRolesRequest { RoleCodes = ["unknown_role"] },
            SuperAdminUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task AssignRolesAsync_RemovingLastSuperAdmin_ReturnsLockout()
    {
        var repository = new FakePlatformUserRepository
        {
            UserEntity = PlatformUser.Create(
                SuperAdminUserId,
                "admin@example.com",
                "hash",
                PlatformAuthConstants.ActiveStatus,
                Now),
            UserRoleCodes = [PlatformRoleCodes.SuperAdministrator],
            ActiveSuperAdministratorsExcludingTarget = 0,
            ResolvedRoles =
            [
                new ResolvedPlatformRole(SupportRoleId, "support_operator", "Support Operator")
            ]
        };

        var service = CreateService(repository, hasUsersRolesAssign: true);

        var result = await service.AssignRolesAsync(
            SuperAdminUserId,
            new AssignPlatformUserRolesRequest { RoleCodes = ["support_operator"] },
            SuperAdminUserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.super_admin_lockout", result.Error.Code);
    }

    [Fact]
    public async Task AssignRolesAsync_WithoutPermission_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePlatformUserRepository
        {
            UserEntity = PlatformUser.CreatePendingInvite(userId, "support.user@example.com", Now)
        };

        var service = CreateService(repository, hasUsersRolesAssign: false);

        var result = await service.AssignRolesAsync(
            userId,
            new AssignPlatformUserRolesRequest { RoleCodes = ["support_operator"] },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("platform_users.access_denied", result.Error.Code);
    }

    private static PlatformUserService CreateService(
        FakePlatformUserRepository repository,
        bool hasUsersView = false,
        bool hasUsersCreate = false,
        bool hasUsersUpdate = false,
        bool hasUsersRolesAssign = false)
    {
        return new PlatformUserService(
            repository,
            new FakePlatformPermissionChecker(
                hasUsersView,
                hasUsersCreate,
                hasUsersUpdate,
                hasUsersRolesAssign),
            new FakeDateTimeProvider());
    }

    private static PlatformUserListResponse CreateListResponse()
    {
        return new PlatformUserListResponse(
        [
            new PlatformUserListItemDto(
                SuperAdminUserId,
                "admin@example.com",
                "admin",
                PlatformAuthConstants.ActiveStatus,
                [PlatformRoleCodes.SuperAdministrator],
                ["Super Administrator"],
                36,
                Now,
                Now,
                Now)
        ]);
    }

    private static PlatformUserDetailResponse CreateDetailResponse(Guid userId, string email)
    {
        return new PlatformUserDetailResponse(
            userId,
            email,
            "support.user",
            PlatformAuthConstants.InactiveStatus,
            true,
            ["support_operator"],
            ["Support Operator"],
            1,
            null,
            Now,
            Now);
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePlatformPermissionChecker : IPlatformPermissionChecker
    {
        private readonly bool _hasUsersView;
        private readonly bool _hasUsersCreate;
        private readonly bool _hasUsersUpdate;
        private readonly bool _hasUsersRolesAssign;

        public FakePlatformPermissionChecker(
            bool hasUsersView,
            bool hasUsersCreate,
            bool hasUsersUpdate,
            bool hasUsersRolesAssign)
        {
            _hasUsersView = hasUsersView;
            _hasUsersCreate = hasUsersCreate;
            _hasUsersUpdate = hasUsersUpdate;
            _hasUsersRolesAssign = hasUsersRolesAssign;
        }

        public Task<bool> HasPermissionAsync(
            Guid platformUserId,
            string permissionCode,
            CancellationToken cancellationToken)
        {
            var allowed = permissionCode switch
            {
                _ when string.Equals(permissionCode, PlatformPermissionCodes.UsersView, StringComparison.Ordinal) =>
                    _hasUsersView,
                _ when string.Equals(permissionCode, PlatformPermissionCodes.UsersCreate, StringComparison.Ordinal) =>
                    _hasUsersCreate,
                _ when string.Equals(permissionCode, PlatformPermissionCodes.UsersUpdate, StringComparison.Ordinal) =>
                    _hasUsersUpdate,
                _ when string.Equals(permissionCode, PlatformPermissionCodes.UsersRolesAssign, StringComparison.Ordinal) =>
                    _hasUsersRolesAssign,
                _ => false
            };

            return Task.FromResult(allowed);
        }
    }

    private sealed class FakePlatformUserRepository : IPlatformUserRepository
    {
        public PlatformUserListResponse ListResponse { get; init; } = new([]);
        public PlatformUserDetailResponse? DetailResponse { get; init; }
        public PlatformUser? UserEntity { get; set; }
        public bool EmailExists { get; init; }
        public IReadOnlyList<ResolvedPlatformRole> ResolvedRoles { get; init; } = [];
        public IReadOnlyList<string> UserRoleCodes { get; init; } = [];
        public int ActiveSuperAdministratorsExcludingTarget { get; init; } = 1;
        public bool RequesterHasSuperAdminRole { get; init; } = true;
        public bool AddCalled { get; private set; }
        public bool ReplaceRolesCalled { get; private set; }

        public Task<PlatformUserListResponse> GetUsersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ListResponse);
        }

        public Task<PlatformUserDetailResponse?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            if (DetailResponse is not null && DetailResponse.Id == userId)
            {
                return Task.FromResult<PlatformUserDetailResponse?>(DetailResponse);
            }

            if (UserEntity is not null && UserEntity.Id == userId)
            {
                return Task.FromResult<PlatformUserDetailResponse?>(
                    CreateDetailResponse(userId, UserEntity.Email));
            }

            return Task.FromResult<PlatformUserDetailResponse?>(null);
        }

        public Task<PlatformUser?> GetUserEntityByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(UserEntity?.Id == userId ? UserEntity : null);
        }

        public Task<bool> EmailExistsAsync(
            string normalizedEmail,
            Guid? excludingUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(EmailExists);
        }

        public Task AddUserWithRolesAsync(
            PlatformUser user,
            IReadOnlyList<Guid> roleIds,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            AddCalled = true;
            UserEntity = user;
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(PlatformUser user, CancellationToken cancellationToken)
        {
            UserEntity = user;
            return Task.CompletedTask;
        }

        public Task ReplaceUserRolesAsync(
            Guid userId,
            IReadOnlyList<Guid> roleIds,
            DateTimeOffset now,
            Guid? actorPlatformUserId,
            CancellationToken cancellationToken)
        {
            ReplaceRolesCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ResolvedPlatformRole>> ResolveActiveRolesAsync(
            IReadOnlyList<Guid>? roleIds,
            IReadOnlyList<string>? roleCodes,
            CancellationToken cancellationToken)
        {
            if (roleCodes?.Contains("unknown_role", StringComparer.Ordinal) == true ||
                roleCodes?.Contains("unknown", StringComparer.Ordinal) == true)
            {
                return Task.FromResult<IReadOnlyList<ResolvedPlatformRole>>([]);
            }

            return Task.FromResult(ResolvedRoles);
        }

        public Task<bool> UserHasActiveRoleCodeAsync(
            Guid userId,
            string roleCode,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                RequesterHasSuperAdminRole &&
                string.Equals(roleCode, PlatformRoleCodes.SuperAdministrator, StringComparison.Ordinal));
        }

        public Task<int> CountActiveSuperAdministratorsAsync(
            Guid? excludingUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ActiveSuperAdministratorsExcludingTarget);
        }

        public Task<IReadOnlyList<string>> GetUserActiveRoleCodesAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(UserRoleCodes);
        }
    }
}


