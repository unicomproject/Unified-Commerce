using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.AccessControl.Services;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.UnitTests.TestSupport;
using Xunit;

namespace E_POS.UnitTests.AccessControl;

public sealed class TenantAdminUserServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid RoleId = Guid.NewGuid();
    private static readonly Guid OutletId = Guid.NewGuid();
    private static readonly Guid TillId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);
    private const string IdempotencyKey = "create-user-test-key";

    [Fact]
    public async Task ListAsync_WithViewPermission_PreservesMasterDetailListFields()
    {
        var outlets = new[]
        {
            new OutletOptionResponse(OutletId, "Main Outlet", "MAIN", "Active"),
        };
        var repository = new FakeTenantAdminUserRepository
        {
            ListResponse = new TenantAdminUserListResponse(
                [new TenantAdminUserListItemResponse(
                    UserId,
                    "Jane Doe",
                    "jane.doe@example.com",
                    "+1234567890",
                    null,
                    RoleId,
                    "Store Manager",
                    "Main Outlet",
                    "Active",
                    Now,
                    "Manages store operations.",
                    outlets,
                    1)],
                1,
                10,
                1),
        };
        var service = CreateService(repository);

        var result = await service.ListAsync(
            CreateContext([TenantAdminUserPermissions.View]),
            "123456",
            "Active",
            null,
            null,
            1,
            10,
            "name",
            "asc",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("+1234567890", item.PhoneNumber);
        Assert.Equal("Manages store operations.", item.RoleDescription);
        Assert.Equal(1, item.OutletCount);
        Assert.Single(item.Outlets!);
    }

    [Fact]
    public async Task GetCreateOptionsAsync_DoesNotResolveInviteSecretProtector()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = new TenantAdminUserService(
            new FakeIdempotencyService(),
            repository,
            new FakeDateTimeProvider(),
            new FakePasswordHashService(),
            new PlatformPasswordPolicyValidator(),
            new AllowingTenantResourceLimitGuard(),
            new FakeStaffCodeService(),
            new FakeInvitationTokenService(),
            new Lazy<IInvitationDeliverySecretProtector>(() =>
                throw new InvalidOperationException("Protector should not be resolved for create options.")));

        var result = await service.GetCreateOptionsAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Roles);
        Assert.Empty(result.Value.Outlets);
        Assert.Empty(result.Value.PermissionGroups);
    }

    [Fact]
    public async Task GetCreateOptions_ReturnsSupportedStatuses()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.GetCreateOptionsAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantAdminUserCreateStatusPolicy.SupportedStatuses, result.Value!.SupportedStatuses);
    }

    [Fact]
    public async Task GetCreateOptions_ReturnsOnlyCreationAllowedStatuses()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.GetCreateOptionsAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value!.SupportedStatuses, status =>
            Assert.NotNull(TenantAdminUserCreateStatusPolicy.Normalize(status)));
    }

    [Fact]
    public async Task GetCreateOptions_ReturnsActive_WhenDirectPasswordCreateIsSupported()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.GetCreateOptionsAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(TenantUserConstants.StatusActive, result.Value!.SupportedStatuses);
    }

    [Fact]
    public async Task GetCreateOptions_ReturnsExplicitScopeCapabilitiesAndCatalogVersion()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.GetCreateOptionsAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(TenantUserAccessScopes.NoOutletAccess, result.Value!.SupportedOutletAccessScopes!);
        Assert.Contains(TenantUserAccessScopes.SelectedTills, result.Value.SupportedTillAccessScopes!);
        var capabilities = Assert.IsType<TenantAdminUserCreateCapabilitiesResponse>(result.Value.Capabilities);
        Assert.True(capabilities.SupportsUserPermissionOverrides);
        Assert.False(capabilities.SupportsPermissionDenies);
        Assert.True(capabilities.SupportsDirectActiveCreation);
        Assert.True(capabilities.SupportsTemporaryPassword);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.PermissionCatalogVersion));
    }

    [Fact]
    public async Task CreateAsync_WithoutCreateOrInvitePermission_ReturnsPermissionDenied()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.CreateAsync(CreateContext([]), CreateValidRequest(), CancellationToken.None, IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_denied", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithCreatePermission_ReturnsSuccess()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Equal(RoleId, repository.CreatedRoleId);
        Assert.Empty(repository.CreatedOutletIds);
    }

    [Fact]
    public async Task CreateAsync_WithSelectedOutletAndTill_PersistsExplicitScopesAndDefaults()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with
        {
            OutletAccessScope = TenantUserAccessScopes.SelectedOutlets,
            OutletIds = [OutletId],
            DefaultOutletId = OutletId,
            TillAccessScope = TenantUserAccessScopes.SelectedTills,
            TillIds = [TillId],
            DefaultTillId = TillId,
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantUserAccessScopes.SelectedOutlets, repository.CreatedOutletAccessScope);
        Assert.Equal([OutletId], repository.CreatedOutletIds);
        Assert.Equal([TillId], repository.CreatedTillIds);
        Assert.Equal(OutletId.ToString(), repository.CreatedUser!.DefaultOutletId);
        Assert.Equal(TillId, repository.CreatedUser.DefaultTillId);
    }

    [Fact]
    public async Task CreateAsync_WithNoOutletAccess_PersistsNoOutletAndNoTillScopes()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with
            {
                OutletAccessScope = TenantUserAccessScopes.NoOutletAccess,
                TillAccessScope = TenantUserAccessScopes.NoTillAccess,
            },
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantUserAccessScopes.NoOutletAccess, repository.CreatedOutletAccessScope);
        Assert.Equal(TenantUserAccessScopes.NoTillAccess, repository.CreatedUser!.TillAccessScope);
        Assert.Empty(repository.CreatedOutletIds);
        Assert.Empty(repository.CreatedTillIds);
    }

    [Fact]
    public async Task CreateAsync_WithNoOutletAccessAndTill_ReturnsConflictValidation()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with
            {
                OutletAccessScope = TenantUserAccessScopes.NoOutletAccess,
                TillAccessScope = TenantUserAccessScopes.SelectedTills,
                TillIds = [TillId],
            },
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.no_outlet_access_conflict", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithPermissionDenial_ReturnsUnsupportedError()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with { DeniedPermissionIds = [Guid.NewGuid()] },
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_denies_unsupported", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithStalePermissionCatalog_ReturnsCatalogMismatch()
    {
        var permissionId = Guid.NewGuid();
        var service = CreateService(new FakeTenantAdminUserRepository());

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create, TenantAdminUserPermissions.PermissionOverride]),
            CreateValidRequest() with
            {
                PermissionOverrideEnabled = true,
                OverriddenPermissionIds = [permissionId],
                PermissionCatalogVersion = "stale",
            },
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_catalog_mismatch", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithTillOutsideOutletScope_ReturnsTillScopeError()
    {
        var repository = new FakeTenantAdminUserRepository
        {
            TillValidation = TenantAdminUserAccessValidationResult.Invalid(
                TenantAdminUserAccessValidationFailure.TillOutsideOutletScope),
        };
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with
            {
                OutletAccessScope = TenantUserAccessScopes.SelectedOutlets,
                OutletIds = [OutletId],
                TillAccessScope = TenantUserAccessScopes.SelectedTills,
                TillIds = [TillId],
            },
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.till_outside_outlet_scope", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithMissingFullName_ReturnsValidationFailure()
    {
        var service = CreateService(new FakeTenantAdminUserRepository());
        var request = CreateValidRequest() with { FullName = "" };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

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
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenRoleNotInTenant_ReturnsRoleNotFound()
    {
        var service = CreateService(new FakeTenantAdminUserRepository
        {
            RoleValidation = TenantAdminUserAccessValidationResult.Invalid(
                TenantAdminUserAccessValidationFailure.RoleNotFound),
        });

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.role_not_found", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenOutletNotInTenant_ReturnsOutletNotFound()
    {
        var service = CreateService(new FakeTenantAdminUserRepository
        {
            OutletValidation = TenantAdminUserAccessValidationResult.Invalid(
                TenantAdminUserAccessValidationFailure.OutletNotFound),
        });
        var request = CreateValidRequest() with { OutletIds = [OutletId] };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

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
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.duplicate_email", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithSendInviteEmailTrue_CreatesInviteRecord()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with
        {
            SendInviteEmail = true,
            EmployeeId = " EMP-001 ",
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.CreatedInvite);
        Assert.Equal("hash-value-1", repository.CreatedInvite!.InviteTokenHash);
        Assert.NotEqual("raw-token", repository.CreatedInvite.InviteTokenHash);
        Assert.Equal(TenantUserConstants.StatusInvited, repository.CreatedUser?.AccountStatus);
        Assert.Equal(repository.CreatedUser?.Id, repository.CreatedInvite?.TenantUserId);
        Assert.Equal("USR-2026-00001", repository.CreatedUser?.StaffCode);
        Assert.Equal("EMP-001", repository.CreatedUser?.EmployeeId);
        Assert.Equal("USR-2026-00001", result.Value?.StaffCode);
        Assert.Equal("EMP-001", result.Value?.EmployeeId);
        Assert.Equal("Invited", result.Value?.Status);
        Assert.NotNull(repository.CreatedDeliverySecret);
        Assert.Equal(repository.CreatedUser?.Id, repository.CreatedDeliverySecret?.TenantUserId);
        Assert.Equal(repository.CreatedInvite?.Id, repository.CreatedDeliverySecret?.InviteId);
        Assert.Equal("cipher:raw-token-1", repository.CreatedDeliverySecret?.EncryptedToken);
        Assert.NotNull(repository.CreatedOutbox);
        Assert.Equal("tenant.user_invited", repository.CreatedOutbox?.MessageType);
        Assert.Contains(TenantId.ToString(), repository.CreatedOutbox!.PayloadJson);
        Assert.Contains(repository.CreatedUser!.Id.ToString(), repository.CreatedOutbox.PayloadJson);
        Assert.Contains(repository.CreatedInvite!.Id.ToString(), repository.CreatedOutbox.PayloadJson);
        Assert.Equal("hash-value-1", repository.CreatedInvite.InviteTokenHash);
        Assert.DoesNotContain("raw-token", repository.CreatedInvite!.InviteTokenHash);
        Assert.DoesNotContain("raw-token", repository.CreatedOutbox.PayloadJson);
        Assert.DoesNotContain("hash-value-1", repository.CreatedOutbox.PayloadJson);
        Assert.DoesNotContain("cipher:raw-token-1", repository.CreatedOutbox.PayloadJson);
        Assert.DoesNotContain("raw-token", string.Join("|", repository.CreatedAudits.Select(x => x.NewValues)));
        Assert.DoesNotContain("cipher:raw-token-1", string.Join("|", repository.CreatedAudits.Select(x => x.NewValues)));
        Assert.DoesNotContain("raw-token", result.Value!.Email);
        Assert.Contains(repository.CreatedAudits, x => x.Action == "user.invited");
        Assert.Contains(repository.CreatedAudits, x => x.Action == "user.created");
        Assert.Contains(repository.CreatedAudits, x => x.Action == "user.access_assigned");
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
            CancellationToken.None, IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Null(repository.CreatedInvite);
        Assert.Null(repository.CreatedDeliverySecret);
        Assert.Null(repository.CreatedOutbox);
        Assert.Equal(TenantUserConstants.StatusInactive, repository.CreatedUser?.AccountStatus);
        Assert.Equal(TenantUserConstants.PendingInvitePasswordHash, repository.CreatedUser?.EncryptedPassword);
        Assert.Equal("empty_salt", repository.CreatedUser?.PasswordSalt);
        Assert.Equal("Inactive", result.Value?.Status);
        Assert.Equal("USR-2026-00001", result.Value?.StaffCode);
        Assert.Contains(repository.CreatedAudits, x => x.Action == "user.created");
        Assert.Contains(repository.CreatedAudits, x => x.Action == "user.access_assigned");
        Assert.DoesNotContain(repository.CreatedAudits, x => x.Action == "user.invited");
    }

    [Fact]
    public async Task CreateAsync_WithValidProfileMedia_AttachesMediaAndAuditsSafeId()
    {
        var mediaAssetId = Guid.NewGuid();
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with { ProfileMediaAssetId = mediaAssetId };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Equal(mediaAssetId, repository.ValidatedProfileMediaAssetId);
        Assert.Null(repository.ValidatedProfileMediaTargetUserId);
        Assert.Equal(mediaAssetId, repository.CreatedUser?.ProfileImageUrl);
        var audit = Assert.Single(repository.CreatedAudits, x => x.Action == "user.profile_image_assigned");
        Assert.Contains(mediaAssetId.ToString(), audit.NewValues);
        Assert.DoesNotContain("storage-key", audit.NewValues);
    }

    [Theory]
    [InlineData(TenantAdminUserProfileMediaValidationFailure.WrongTenant, "user.profile_media_wrong_tenant")]
    [InlineData(TenantAdminUserProfileMediaValidationFailure.Deleted, "user.profile_media_deleted")]
    [InlineData(TenantAdminUserProfileMediaValidationFailure.Expired, "user.profile_media_expired")]
    [InlineData(TenantAdminUserProfileMediaValidationFailure.NotImage, "user.profile_media_not_image")]
    [InlineData(TenantAdminUserProfileMediaValidationFailure.IncompatibleOwner, "user.profile_media_in_use")]
    public async Task CreateAsync_WithInvalidProfileMedia_ReturnsControlledError(
        TenantAdminUserProfileMediaValidationFailure failure,
        string expectedCode)
    {
        var repository = new FakeTenantAdminUserRepository
        {
            ProfileMediaValidation = TenantAdminUserProfileMediaValidationResult.Invalid(failure),
        };
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with { ProfileMediaAssetId = Guid.NewGuid() },
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Null(repository.CreatedUser);
    }

    [Fact]
    public async Task CreateAsync_WithOverridePermissionsButNoOverrideGrant_ReturnsPermissionDenied()
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
            CancellationToken.None, IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_denied", result.Error.Code);
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
            CancellationToken.None, IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Contains(permissionId, repository.CreatedOverriddenPermissionIds);
        Assert.Contains(repository.CreatedAudits, x => x.Action == "user.permission_override_changed");
    }

    [Fact]
    public async Task CreateAsync_WithActiveStatusAndNoPassword_ReturnsPasswordFailure()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with { AccountStatus = TenantUserConstants.StatusActive };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.password_invalid", result.Error.Code);
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateUser_AcceptsInvited()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with
        {
            AccountStatus = TenantUserConstants.StatusInvited,
            SendInviteEmail = true,
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantUserConstants.StatusInvited, repository.CreatedUser?.AccountStatus);
        Assert.NotNull(repository.CreatedInvite);
    }

    [Fact]
    public async Task CreateUser_AcceptsInactive()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with
        {
            AccountStatus = TenantUserConstants.StatusInactive,
            SendInviteEmail = false,
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantUserConstants.StatusInactive, repository.CreatedUser?.AccountStatus);
        Assert.Null(repository.CreatedInvite);
    }

    [Fact]
    public async Task CreateUser_WithValidPassword_CreatesActiveLoginAccount()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with
        {
            AccountStatus = TenantUserConstants.StatusActive,
            Password = "SecurePass123",
            ConfirmPassword = "SecurePass123",
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantUserConstants.StatusActive, repository.CreatedUser?.AccountStatus);
        Assert.Equal("HASH:SecurePass123", repository.CreatedUser?.EncryptedPassword);
        Assert.Equal("pbkdf2_embedded", repository.CreatedUser?.PasswordSalt);
        Assert.Null(repository.CreatedInvite);
        Assert.Null(repository.CreatedDeliverySecret);
        Assert.Null(repository.CreatedOutbox);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateUser_WithPasswordMismatch_DoesNotPersistUser()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with
        {
            AccountStatus = TenantUserConstants.StatusActive,
            Password = "SecurePass123",
            ConfirmPassword = "DifferentPass123",
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.password_mismatch", result.Error.Code);
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithSpecificOutlet_PersistsOutletRoleAssignment()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with { OutletIds = [OutletId] };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Equal([OutletId], repository.CreatedOutletIds);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateOutletIds_DeduplicatesBeforePersistence()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with { OutletIds = [OutletId, OutletId] };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        var outletId = Assert.Single(repository.CreatedOutletIds);
        Assert.Equal(OutletId, outletId);
    }

    [Fact]
    public async Task CreateAsync_WithCrossTenantOutlet_ReturnsOutletWrongTenant()
    {
        var repository = new FakeTenantAdminUserRepository
        {
            OutletValidation = TenantAdminUserAccessValidationResult.Invalid(
                TenantAdminUserAccessValidationFailure.OutletWrongTenant),
        };
        var service = CreateService(repository);
        var request = CreateValidRequest() with { OutletIds = [OutletId] };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.outlet_wrong_tenant", result.Error.Code);
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithCrossTenantRole_ReturnsRoleWrongTenant()
    {
        var repository = new FakeTenantAdminUserRepository
        {
            RoleValidation = TenantAdminUserAccessValidationResult.Invalid(
                TenantAdminUserAccessValidationFailure.RoleWrongTenant),
        };
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.role_wrong_tenant", result.Error.Code);
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithOverrideOff_IgnoresStalePermissionIds()
    {
        var repository = new FakeTenantAdminUserRepository
        {
            PermissionValidation = TenantAdminUserAccessValidationResult.Valid,
        };
        var service = CreateService(repository);
        var stalePermissionId = Guid.NewGuid();
        var request = CreateValidRequest() with
        {
            PermissionOverrideEnabled = false,
            OverriddenPermissionIds = [stalePermissionId],
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            IdempotencyKey);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.ValidatedPermissionIds);
        Assert.Empty(repository.CreatedOverriddenPermissionIds);
    }

    [Theory]
    [InlineData(TenantAdminUserAccessValidationFailure.PermissionNotFound, "user.permission_not_found")]
    [InlineData(TenantAdminUserAccessValidationFailure.PermissionInactive, "user.permission_inactive")]
    [InlineData(TenantAdminUserAccessValidationFailure.ActorCannotDelegate, "user.permission_not_delegable")]
    [InlineData(TenantAdminUserAccessValidationFailure.PermissionNotAssignable, "user.permission_not_assignable")]
    [InlineData(TenantAdminUserAccessValidationFailure.InvalidScope, "user.permission_invalid_scope")]
    [InlineData(TenantAdminUserAccessValidationFailure.TenantEntitlementMissing, "user.tenant_entitlement_missing")]
    public async Task CreateAsync_WithInvalidPermissionOverride_ReturnsControlledError(
        TenantAdminUserAccessValidationFailure failure,
        string expectedCode)
    {
        var repository = new FakeTenantAdminUserRepository
        {
            PermissionValidation = TenantAdminUserAccessValidationResult.Invalid(failure),
        };
        var service = CreateService(repository);
        var request = CreateValidRequest() with
        {
            PermissionOverrideEnabled = true,
            OverriddenPermissionIds = [Guid.NewGuid()],
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create, TenantAdminUserPermissions.PermissionOverride]),
            request,
            CancellationToken.None, IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Equal(0, repository.CreateCallCount);
        Assert.Null(repository.CreatedUser);
    }

    [Fact]
    public async Task CreateAsync_WithMixedValidAndInvalidPermissions_FailsAtomically()
    {
        var repository = new FakeTenantAdminUserRepository
        {
            PermissionValidation = TenantAdminUserAccessValidationResult.Invalid(
                TenantAdminUserAccessValidationFailure.ActorCannotDelegate),
        };
        var service = CreateService(repository);
        var request = CreateValidRequest() with
        {
            PermissionOverrideEnabled = true,
            OverriddenPermissionIds = [Guid.NewGuid(), Guid.NewGuid()],
        };

        var result = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create, TenantAdminUserPermissions.PermissionOverride]),
            request,
            CancellationToken.None, IdempotencyKey);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_not_delegable", result.Error.Code);
        Assert.Equal(0, repository.CreateCallCount);
        Assert.Empty(repository.CreatedOverriddenPermissionIds);
    }

    [Fact]
    public async Task CreateAsync_WhenPersistenceFails_DoesNotReturnCreatedResponse()
    {
        var repository = new FakeTenantAdminUserRepository { ThrowOnCreate = true };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                CreateContext([TenantAdminUserPermissions.Create]),
                CreateValidRequest(),
                CancellationToken.None, IdempotencyKey));

        Assert.Equal(1, repository.CreateCallCount);
        Assert.Null(repository.CreatedUser);
        Assert.Null(repository.CreatedInvite);
        Assert.Null(repository.CreatedOutbox);
    }

    [Fact]
    public async Task CreateAsync_SameKeySameInactiveRequest_ReplaysOriginalUser()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with { SendInviteEmail = false };

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            "same-inactive");
        var second = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            request,
            CancellationToken.None,
            "same-inactive");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.UserId, second.Value!.UserId);
        Assert.Equal(first.Value.StaffCode, second.Value.StaffCode);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_SameKeySameInvitedRequest_ReplaysWithoutDuplicateInviteSideEffects()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var request = CreateValidRequest() with { SendInviteEmail = true };

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            request,
            CancellationToken.None,
            "same-invited");
        var second = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            request,
            CancellationToken.None,
            "same-invited");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.UserId, second.Value!.UserId);
        Assert.Equal(first.Value.StaffCode, second.Value.StaffCode);
        Assert.Equal(1, repository.CreateCallCount);
        Assert.NotNull(repository.CreatedInvite);
        Assert.NotNull(repository.CreatedDeliverySecret);
        Assert.NotNull(repository.CreatedOutbox);
    }

    [Fact]
    public async Task CreateAsync_SameKeyDifferentEmail_ReturnsIdempotencyConflict()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            "email-conflict");
        var second = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with { Email = "other@example.com" },
            CancellationToken.None,
            "email-conflict");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("user.idempotency_conflict", second.Error.Code);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_SameKeyDifferentRole_ReturnsIdempotencyConflict()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            "role-conflict");
        var second = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with { RoleId = Guid.NewGuid() },
            CancellationToken.None,
            "role-conflict");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("user.idempotency_conflict", second.Error.Code);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_OutletIdsInDifferentOrder_ReplaysOriginalUser()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var otherOutletId = Guid.NewGuid();

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with { OutletIds = [OutletId, otherOutletId] },
            CancellationToken.None,
            "outlet-order");
        var second = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with { OutletIds = [otherOutletId, OutletId] },
            CancellationToken.None,
            "outlet-order");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.UserId, second.Value!.UserId);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_DuplicateOutletIds_ReplaysCanonicalRequest()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with { OutletIds = [OutletId] },
            CancellationToken.None,
            "duplicate-outlets");
        var second = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with { OutletIds = [OutletId, OutletId] },
            CancellationToken.None,
            "duplicate-outlets");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.UserId, second.Value!.UserId);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_PermissionIdsInDifferentOrder_ReplaysOriginalUser()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);
        var firstPermissionId = Guid.NewGuid();
        var secondPermissionId = Guid.NewGuid();

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create, TenantAdminUserPermissions.PermissionOverride]),
            CreateValidRequest() with
            {
                PermissionOverrideEnabled = true,
                OverriddenPermissionIds = [firstPermissionId, secondPermissionId],
            },
            CancellationToken.None,
            "permission-order");
        var second = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create, TenantAdminUserPermissions.PermissionOverride]),
            CreateValidRequest() with
            {
                PermissionOverrideEnabled = true,
                OverriddenPermissionIds = [secondPermissionId, firstPermissionId],
            },
            CancellationToken.None,
            "permission-order");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.UserId, second.Value!.UserId);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_OverrideDisabledIgnoresStalePermissionIdsForReplay()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with
            {
                PermissionOverrideEnabled = false,
                OverriddenPermissionIds = [],
            },
            CancellationToken.None,
            "override-disabled");
        var second = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest() with
            {
                PermissionOverrideEnabled = false,
                OverriddenPermissionIds = [Guid.NewGuid()],
            },
            CancellationToken.None,
            "override-disabled");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.UserId, second.Value!.UserId);
        Assert.Equal(1, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_DifferentIdempotencyKeys_CanCreateSeparateUsers()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            "first-key");
        var second = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            "second-key");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value!.UserId, second.Value!.UserId);
        Assert.Equal(2, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_SameKeyAcrossTenants_IsIsolated()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            "tenant-scope");
        var second = await service.CreateAsync(
            new TenantRequestContext(Guid.NewGuid(), UserId, [TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            "tenant-scope");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_SameKeyAcrossActors_IsIsolated()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var first = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            "actor-scope");
        var second = await service.CreateAsync(
            new TenantRequestContext(TenantId, Guid.NewGuid(), [TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            "actor-scope");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, repository.CreateCallCount);
    }

    [Fact]
    public async Task CreateAsync_FailedCreate_DoesNotReturnFalseCompletedReplay()
    {
        var repository = new FakeTenantAdminUserRepository { ThrowOnCreateCount = 1 };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                CreateContext([TenantAdminUserPermissions.Create]),
                CreateValidRequest(),
                CancellationToken.None,
                "failed-retry"));

        var retry = await service.CreateAsync(
            CreateContext([TenantAdminUserPermissions.Create]),
            CreateValidRequest(),
            CancellationToken.None,
            "failed-retry");

        Assert.True(retry.IsSuccess);
        Assert.Equal(2, repository.CreateCallCount);
    }

    [Fact]
    public async Task ResendInviteAsync_WithInvitePermission_ReplacesInviteSideEffects()
    {
        var oldInvite = CreatePendingInvite(UserId, "old-hash");
        var oldSecret = TenantUserInviteDeliverySecret.Create(
            Guid.NewGuid(),
            TenantId,
            UserId,
            oldInvite.Id,
            "old-cipher",
            "old",
            oldInvite.ExpiresAt,
            Now);
        var repository = new FakeTenantAdminUserRepository
        {
            InviteMutationUser = TenantUser.Create(
                UserId,
                TenantId,
                "jane.doe@example.com",
                "Jane Doe",
                null,
                null,
                TenantUserConstants.PendingInvitePasswordHash,
                "empty_salt",
                TenantUserConstants.StatusInvited,
                "admin",
                "admin",
                null,
                Now),
            OpenInvites = [oldInvite],
            DeliverySecrets = [oldSecret],
        };
        var service = CreateService(repository);

        var result = await service.ResendInviteAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.ResendInviteCallCount);
        Assert.Equal(UserId, repository.InviteMutationUserId);
        Assert.Equal(UserInviteConstants.StatusRevoked, oldInvite.InviteStatus);
        Assert.NotNull(oldSecret.PurgedAt);
        Assert.NotNull(repository.ResentInvite);
        Assert.Equal(UserId, repository.ResentInvite!.TenantUserId);
        Assert.NotEqual("old-hash", repository.ResentInvite.InviteTokenHash);
        Assert.NotNull(repository.ResentDeliverySecret);
        Assert.Equal(repository.ResentInvite.Id, repository.ResentDeliverySecret!.InviteId);
        Assert.Equal("cipher:raw-token-1", repository.ResentDeliverySecret.EncryptedToken);
        Assert.NotNull(repository.ResentOutbox);
        Assert.Equal("tenant.user_invited", repository.ResentOutbox!.MessageType);
        Assert.Contains(repository.ResentInvite.Id.ToString(), repository.ResentOutbox.PayloadJson);
        Assert.Contains(repository.InviteAudits, audit => audit.Action == "user.invite_resent");
        var auditValues = string.Join("|", repository.InviteAudits.Select(audit => audit.NewValues));
        Assert.DoesNotContain("raw-token", auditValues);
        Assert.DoesNotContain("hash:", auditValues);
    }

    [Fact]
    public async Task ResendInviteAsync_WithoutInvitePermission_ReturnsPermissionDenied()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var result = await service.ResendInviteAsync(CreateContext([]), UserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.ResendInviteCallCount);
    }

    [Fact]
    public async Task ResendInviteAsync_NoUsableInvite_ReturnsInviteNotAvailable()
    {
        var repository = new FakeTenantAdminUserRepository
        {
            InviteMutationStatus = TenantAdminUserInviteMutationStatus.NoUsableInvite,
        };
        var service = CreateService(repository);

        var result = await service.ResendInviteAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            UserId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.invite_not_available", result.Error.Code);
    }

    [Fact]
    public async Task ResendInviteAsync_TargetsOnlyRequestedUser()
    {
        var otherUserId = Guid.NewGuid();
        var oldInvite = CreatePendingInvite(UserId, "old-hash");
        var otherInvite = CreatePendingInvite(otherUserId, "other-hash");
        var repository = new FakeTenantAdminUserRepository
        {
            InviteMutationUser = TenantUser.Create(
                UserId,
                TenantId,
                "jane.doe@example.com",
                "Jane Doe",
                null,
                null,
                TenantUserConstants.PendingInvitePasswordHash,
                "empty_salt",
                TenantUserConstants.StatusInvited,
                "admin",
                "admin",
                null,
                Now),
            OpenInvites = [oldInvite],
            OtherUserInvite = otherInvite,
        };
        var service = CreateService(repository);

        var result = await service.ResendInviteAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserInviteConstants.StatusRevoked, oldInvite.InviteStatus);
        Assert.Equal(UserInviteConstants.StatusPending, otherInvite.InviteStatus);
    }

    [Fact]
    public async Task RevokeInviteAsync_WithInvitePermission_RevokesAndPurgesSecret()
    {
        var invite = CreatePendingInvite(UserId, "hash");
        var secret = TenantUserInviteDeliverySecret.Create(
            Guid.NewGuid(),
            TenantId,
            UserId,
            invite.Id,
            "cipher",
            "test",
            invite.ExpiresAt,
            Now);
        var repository = new FakeTenantAdminUserRepository
        {
            InviteMutationUser = TenantUser.Create(
                UserId,
                TenantId,
                "jane.doe@example.com",
                "Jane Doe",
                null,
                null,
                TenantUserConstants.PendingInvitePasswordHash,
                "empty_salt",
                TenantUserConstants.StatusInvited,
                "admin",
                "admin",
                null,
                Now),
            OpenInvites = [invite],
            DeliverySecrets = [secret],
        };
        var service = CreateService(repository);

        var result = await service.RevokeInviteAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserInviteConstants.StatusRevoked, invite.InviteStatus);
        Assert.NotNull(secret.PurgedAt);
        Assert.Contains(repository.InviteAudits, audit => audit.Action == "user.invite_revoked");
    }

    [Fact]
    public async Task RevokeInviteAsync_WithoutInvitePermission_ReturnsPermissionDenied()
    {
        var repository = new FakeTenantAdminUserRepository();
        var service = CreateService(repository);

        var result = await service.RevokeInviteAsync(CreateContext([]), UserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.permission_denied", result.Error.Code);
        Assert.Equal(0, repository.RevokeInviteCallCount);
    }

    [Fact]
    public async Task RevokeInviteAsync_RepeatedRevoke_ReturnsSuccessWithoutNewAudit()
    {
        var repository = new FakeTenantAdminUserRepository
        {
            InviteMutationUser = TenantUser.Create(
                UserId,
                TenantId,
                "jane.doe@example.com",
                "Jane Doe",
                null,
                null,
                TenantUserConstants.PendingInvitePasswordHash,
                "empty_salt",
                TenantUserConstants.StatusInvited,
                "admin",
                "admin",
                null,
                Now),
            OpenInvites = [],
        };
        var service = CreateService(repository);

        var result = await service.RevokeInviteAsync(
            CreateContext([TenantAdminUserPermissions.Invite]),
            UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.InviteAudits);
    }

    [Fact]
    public async Task UpdateAsync_WithReplacementProfileMedia_ReplacesAndAudits()
    {
        var oldMediaAssetId = Guid.NewGuid();
        var newMediaAssetId = Guid.NewGuid();
        var user = TenantUser.Create(
            UserId, TenantId, "user@example.com", "User", null, null, "hash", "salt",
            TenantUserConstants.StatusActive, "admin", "admin", "HQ", Now);
        user.SetProfileMediaAsset(oldMediaAssetId, UserId, Now);
        var repository = new FakeTenantAdminUserRepository { EditableUser = user };
        var service = CreateService(repository);

        var result = await service.UpdateAsync(
            CreateContext([TenantAdminUserPermissions.Update]),
            UserId,
            CreateValidUpdateRequest() with { ProfileMediaAssetId = newMediaAssetId },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(newMediaAssetId, user.ProfileImageUrl);
        Assert.Equal(newMediaAssetId, repository.ValidatedProfileMediaAssetId);
        Assert.Equal(UserId, repository.ValidatedProfileMediaTargetUserId);
        var change = Assert.Single(repository.ProfileMediaChanges);
        Assert.Equal(oldMediaAssetId, change.PreviousMediaAssetId);
        Assert.Equal(newMediaAssetId, change.NextMediaAssetId);
        Assert.Equal("user.profile_image_replaced", change.AuditAction);
    }

    [Fact]
    public async Task UpdateAsync_WithRemoveProfileMedia_ClearsAndAudits()
    {
        var oldMediaAssetId = Guid.NewGuid();
        var user = TenantUser.Create(
            UserId, TenantId, "user@example.com", "User", null, null, "hash", "salt",
            TenantUserConstants.StatusActive, "admin", "admin", "HQ", Now);
        user.SetProfileMediaAsset(oldMediaAssetId, UserId, Now);
        var repository = new FakeTenantAdminUserRepository { EditableUser = user };
        var service = CreateService(repository);

        var result = await service.UpdateAsync(
            CreateContext([TenantAdminUserPermissions.Update]),
            UserId,
            CreateValidUpdateRequest() with { ProfileMediaAction = "REMOVE" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.ProfileImageUrl);
        var change = Assert.Single(repository.ProfileMediaChanges);
        Assert.Equal(oldMediaAssetId, change.PreviousMediaAssetId);
        Assert.Null(change.NextMediaAssetId);
        Assert.Equal("user.profile_image_removed", change.AuditAction);
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

    private static TenantAdminUserService CreateService(
        FakeTenantAdminUserRepository repository,
        FakeIdempotencyService? idempotencyService = null)
    {
        return new TenantAdminUserService(
            idempotencyService ?? new FakeIdempotencyService(),
            repository,
            new FakeDateTimeProvider(),
            new FakePasswordHashService(),
            new PlatformPasswordPolicyValidator(),
            new AllowingTenantResourceLimitGuard(),
            new FakeStaffCodeService(),
            new FakeInvitationTokenService(),
            new Lazy<IInvitationDeliverySecretProtector>(() => new FakeDeliverySecretProtector()));
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

    private static TenantAdminUserUpdateRequest CreateValidUpdateRequest()
    {
        return new TenantAdminUserUpdateRequest(
            "Jane Doe",
            "jane.doe@example.com",
            "+1234567890",
            RoleId,
            [],
            false,
            [],
            TenantUserConstants.StatusActive);
    }

    private static UserInvite CreatePendingInvite(Guid userId, string tokenHash)
    {
        return UserInvite.CreatePending(
            Guid.NewGuid(),
            TenantId,
            "jane.doe@example.com",
            "JANE.DOE@EXAMPLE.COM",
            RoleId,
            null,
            tokenHash,
            Now.AddDays(7),
            Now,
            userId);
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

    private sealed class FakeStaffCodeService : ITenantUserStaffCodeService
    {
        private int _nextValue = 1;

        public Task<string> GenerateAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            var value = _nextValue++;
            return Task.FromResult($"USR-{now:yyyy}-{value:00000}");
        }
    }

    private sealed class FakeIdempotencyService : IIdempotencyService
    {
        private readonly Dictionary<(Guid TenantId, Guid ActorUserId, string Operation, string Key), Entry> _entries = [];

        public async Task<ApplicationResult<T>> ExecuteAsync<T>(
            Guid tenantId,
            Guid actorUserId,
            string operation,
            string idempotencyKey,
            string requestHash,
            Func<CancellationToken, Task<ApplicationResult<T>>> operationFunc,
            CancellationToken cancellationToken)
        {
            var scope = (tenantId, actorUserId, operation, idempotencyKey.Trim());
            if (_entries.TryGetValue(scope, out var entry))
            {
                return string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal)
                    ? ApplicationResult<T>.Success((T)entry.Value)
                    : ApplicationResult<T>.Failure(new ApplicationError(
                        "user.idempotency_conflict",
                        "Idempotency key was already used for a different create-user request."));
            }

            var result = await operationFunc(cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                _entries.Add(scope, new Entry(requestHash, result.Value));
            }

            return result;
        }

        private sealed record Entry(string RequestHash, object Value);
    }

    private sealed class FakeInvitationTokenService : IInvitationTokenService
    {
        private int _nextToken = 1;

        public string GenerateToken() => $"raw-token-{_nextToken++}";
        public string HashToken(string rawToken) => rawToken.Replace("raw-token", "hash-value", StringComparison.Ordinal);
    }

    private sealed class FakeDeliverySecretProtector : IInvitationDeliverySecretProtector
    {
        public ProtectedInvitationDeliverySecret Protect(string rawToken) => new("cipher:" + rawToken, "test");
        public string Unprotect(string ciphertext, string keyVersion) => ciphertext[7..];
    }

    private sealed class FakeTenantAdminUserRepository : ITenantAdminUserRepository
    {
        public bool EmailExists { get; init; }
        public bool PermissionIdsExist { get; init; } = true;
        public TenantAdminUserAccessValidationResult RoleValidation { get; init; } =
            TenantAdminUserAccessValidationResult.Valid;
        public TenantAdminUserAccessValidationResult OutletValidation { get; init; } =
            TenantAdminUserAccessValidationResult.Valid;
        public TenantAdminUserAccessValidationResult TillValidation { get; init; } =
            TenantAdminUserAccessValidationResult.Valid;
        public TenantAdminUserAccessValidationResult PermissionValidation { get; init; } =
            TenantAdminUserAccessValidationResult.Valid;
        public TenantAdminUserProfileMediaValidationResult ProfileMediaValidation { get; init; } =
            TenantAdminUserProfileMediaValidationResult.Valid("https://cdn.example.test/profile.jpg");
        public TenantUser? EditableUser { get; init; }
        public bool HasActiveTillSession { get; init; }
        public bool HasSalesReferences { get; init; }
        public TenantAdminUserListResponse? ListResponse { get; init; }
        public bool ThrowOnCreate { get; init; }
        public bool ReturnEmptyPermissionCatalog { get; init; }
        public int ThrowOnCreateCount { get; init; }
        public TenantAdminUserInviteMutationStatus InviteMutationStatus { get; init; } =
            TenantAdminUserInviteMutationStatus.Success;
        public TenantUser? InviteMutationUser { get; init; }
        public IReadOnlyList<UserInvite> OpenInvites { get; init; } = [];
        public IReadOnlyList<TenantUserInviteDeliverySecret> DeliverySecrets { get; init; } = [];
        public UserInvite? OtherUserInvite { get; init; }
        private int _remainingCreateFailures;
        private bool _createFailuresInitialized;

        public TenantUser? CreatedUser { get; private set; }
        public UserInvite? CreatedInvite { get; private set; }
        public TenantUserInviteDeliverySecret? CreatedDeliverySecret { get; private set; }
        public IntegrationOutboxMessage? CreatedOutbox { get; private set; }
        public IReadOnlyCollection<AuditLog> CreatedAudits { get; private set; } = [];
        public Guid? CreatedRoleId { get; private set; }
        public IReadOnlyCollection<Guid> CreatedOutletIds { get; private set; } = [];
        public string? CreatedOutletAccessScope { get; private set; }
        public IReadOnlyCollection<Guid> CreatedTillIds { get; private set; } = [];
        public IReadOnlyCollection<Guid> CreatedOverriddenPermissionIds { get; private set; } = [];
        public IReadOnlyCollection<Guid> ValidatedPermissionIds { get; private set; } = [];
        public Guid? ValidatedProfileMediaAssetId { get; private set; }
        public Guid? ValidatedProfileMediaTargetUserId { get; private set; }
        public List<ProfileMediaChangeRecord> ProfileMediaChanges { get; } = [];
        public int CreateCallCount { get; private set; }
        public int ResendInviteCallCount { get; private set; }
        public int RevokeInviteCallCount { get; private set; }
        public Guid? InviteMutationUserId { get; private set; }
        public UserInvite? ResentInvite { get; private set; }
        public TenantUserInviteDeliverySecret? ResentDeliverySecret { get; private set; }
        public IntegrationOutboxMessage? ResentOutbox { get; private set; }
        public List<AuditLog> InviteAudits { get; } = [];

        public Task<TenantAdminUserListResponse> ListAsync(
            Guid tenantId, string? search, string? status, Guid? roleId, Guid? outletId,
            int page, int pageSize, string sortBy, string sortDirection, CancellationToken cancellationToken) =>
            Task.FromResult(ListResponse ?? new TenantAdminUserListResponse([], page, pageSize, 0));

        public Task<IReadOnlyList<RoleOptionResponse>> GetRoleOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RoleOptionResponse>>([]);

        public Task<IReadOnlyList<OutletOptionResponse>> GetOutletOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OutletOptionResponse>>([]);

        public Task<IReadOnlyList<TillOptionResponse>> GetTillOptionsAsync(Guid tenantId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TillOptionResponse>>([]);

        public Task<IReadOnlyList<PermissionGroupResponse>> GetPermissionGroupsAsync(
            Guid tenantId,
            IReadOnlyCollection<string> actorPermissionCodes,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<PermissionGroupResponse> groups =
                ReturnEmptyPermissionCatalog || ValidatedPermissionIds.Count == 0
                    ? []
                    :
                    [
                        new PermissionGroupResponse(
                            "Test Module",
                            ValidatedPermissionIds
                                .Select(id => new PermissionItemResponse(id, $"test.{id:N}", "view", "Test permission"))
                                .ToList())
                    ];
            return Task.FromResult(groups);
        }

        public Task<bool> RoleBelongsToTenantAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult(RoleValidation.IsValid);

        public Task<TenantAdminUserAccessValidationResult> ValidateRoleAssignmentAsync(
            Guid tenantId,
            Guid roleId,
            IReadOnlyCollection<string> actorPermissionCodes,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(RoleValidation);

        public Task<bool> OutletsBelongToTenantAsync(Guid tenantId, IReadOnlyCollection<Guid> outletIds, CancellationToken cancellationToken) =>
            Task.FromResult(outletIds.Count == 0 || OutletValidation.IsValid);

        public Task<TenantAdminUserAccessValidationResult> ValidateOutletSelectionAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> outletIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(OutletValidation);

        public Task<TenantAdminUserAccessValidationResult> ValidateTillSelectionAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> tillIds,
            IReadOnlyCollection<Guid> allowedOutletIds,
            bool allowAllTenantOutlets,
            CancellationToken cancellationToken) =>
            Task.FromResult(TillValidation);

        public Task<bool> EmailExistsForTenantAsync(Guid tenantId, string normalizedEmail, Guid? excludeUserId, CancellationToken cancellationToken) =>
            Task.FromResult(EmailExists);

        public Task<bool> PermissionIdsExistAsync(IReadOnlyCollection<Guid> permissionIds, CancellationToken cancellationToken) =>
            Task.FromResult(permissionIds.Count == 0 || PermissionIdsExist);

        public Task<TenantAdminUserAccessValidationResult> ValidatePermissionOverridesAsync(
            Guid tenantId,
            IReadOnlyCollection<Guid> permissionIds,
            IReadOnlyCollection<string> actorPermissionCodes,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ValidatedPermissionIds = permissionIds;
            return Task.FromResult(PermissionValidation);
        }

        public Task<TenantAdminUserProfileMediaValidationResult> ValidateProfileMediaAsync(
            Guid tenantId,
            Guid mediaAssetId,
            Guid? targetUserId,
            CancellationToken cancellationToken)
        {
            ValidatedProfileMediaAssetId = mediaAssetId;
            ValidatedProfileMediaTargetUserId = targetUserId;
            return Task.FromResult(ProfileMediaValidation);
        }

        public Task<Guid> CreateAsync(
            TenantUser user, Guid roleId, IReadOnlyCollection<Guid> outletIds,
            IReadOnlyCollection<Guid> overriddenPermissionIds, UserInvite? invite,
            TenantUserInviteDeliverySecret? deliverySecret, IntegrationOutboxMessage? outboxMessage,
            IReadOnlyCollection<AuditLog> auditLogs, DateTimeOffset now, CancellationToken cancellationToken)
        {
            CreateCallCount++;
            if (!_createFailuresInitialized)
            {
                _remainingCreateFailures = ThrowOnCreateCount;
                _createFailuresInitialized = true;
            }

            if (ThrowOnCreate || _remainingCreateFailures-- > 0)
            {
                throw new InvalidOperationException("Persistence failed.");
            }

            CreatedUser = user;
            CreatedInvite = invite;
            CreatedDeliverySecret = deliverySecret;
            CreatedOutbox = outboxMessage;
            CreatedAudits = auditLogs;
            CreatedRoleId = roleId;
            CreatedOutletIds = outletIds;
            CreatedOverriddenPermissionIds = overriddenPermissionIds;
            return Task.FromResult(user.Id);
        }

        public Task<Guid> CreateAsync(
            TenantUser user,
            Guid roleId,
            string outletAccessScope,
            IReadOnlyCollection<Guid> outletIds,
            IReadOnlyCollection<Guid> overriddenPermissionIds,
            IReadOnlyCollection<Guid> tillIds,
            UserInvite? invite,
            TenantUserInviteDeliverySecret? deliverySecret,
            IntegrationOutboxMessage? outboxMessage,
            IReadOnlyCollection<AuditLog> auditLogs,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CreatedOutletAccessScope = outletAccessScope;
            CreatedTillIds = tillIds;
            return CreateAsync(
                user,
                roleId,
                outletIds,
                overriddenPermissionIds,
                invite,
                deliverySecret,
                outboxMessage,
                auditLogs,
                now,
                cancellationToken);
        }

        public Task<TenantAdminUserInviteMutationResult> ResendInviteAsync(
            Guid tenantId,
            Guid actorUserId,
            Guid userId,
            string inviteTokenHash,
            string encryptedToken,
            string keyVersion,
            DateTimeOffset expiresAt,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ResendInviteCallCount++;
            InviteMutationUserId = userId;
            if (InviteMutationStatus != TenantAdminUserInviteMutationStatus.Success)
            {
                return Task.FromResult(new TenantAdminUserInviteMutationResult(InviteMutationStatus));
            }

            var user = InviteMutationUser;
            if (user is null || user.TenantId != tenantId || user.Id != userId)
            {
                return Task.FromResult(new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotFound));
            }

            if (!string.Equals(user.AccountStatus, TenantUserConstants.StatusInvited, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotEligible));
            }

            if (!OpenInvites.Any(invite => invite.IsUsableAt(now)))
            {
                return Task.FromResult(new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NoUsableInvite));
            }

            foreach (var invite in OpenInvites)
            {
                invite.Revoke(now);
            }

            foreach (var secret in DeliverySecrets)
            {
                secret.Purge(now);
            }

            ResentInvite = UserInvite.CreatePending(
                Guid.NewGuid(),
                tenantId,
                user.Email,
                TenantUser.NormalizeEmail(user.Email),
                RoleId,
                null,
                inviteTokenHash,
                expiresAt,
                now,
                userId);
            ResentDeliverySecret = TenantUserInviteDeliverySecret.Create(
                Guid.NewGuid(),
                tenantId,
                userId,
                ResentInvite.Id,
                encryptedToken,
                keyVersion,
                expiresAt,
                now);
            ResentOutbox = IntegrationOutboxMessage.Create(
                Guid.NewGuid(),
                "tenant.user_invited",
                "TENANT_USER",
                userId,
                1,
                tenantId,
                Guid.NewGuid(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new { tenantId, tenantUserId = userId, inviteId = ResentInvite.Id }),
                $"tenant.user_invited:{ResentInvite.Id:N}",
                now);
            InviteAudits.Add(new AuditLog
            {
                TenantId = tenantId,
                ActorUserId = actorUserId,
                ActorType = "TENANT_USER",
                EntityType = "TENANT_USER",
                EntityId = userId,
                Action = "user.invite_resent",
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { inviteId = ResentInvite.Id }),
                CreatedAt = now
            });

            return Task.FromResult(TenantAdminUserInviteMutationResult.Success(GetDetail(userId), ResentInvite.Id));
        }

        public Task<TenantAdminUserInviteMutationResult> RevokeInviteAsync(
            Guid tenantId,
            Guid actorUserId,
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            RevokeInviteCallCount++;
            InviteMutationUserId = userId;
            if (InviteMutationStatus != TenantAdminUserInviteMutationStatus.Success)
            {
                return Task.FromResult(new TenantAdminUserInviteMutationResult(InviteMutationStatus));
            }

            var user = InviteMutationUser;
            if (user is null || user.TenantId != tenantId || user.Id != userId)
            {
                return Task.FromResult(new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotFound));
            }

            if (!string.Equals(user.AccountStatus, TenantUserConstants.StatusInvited, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new TenantAdminUserInviteMutationResult(TenantAdminUserInviteMutationStatus.NotEligible));
            }

            if (OpenInvites.Count > 0)
            {
                foreach (var invite in OpenInvites)
                {
                    invite.Revoke(now);
                }

                foreach (var secret in DeliverySecrets)
                {
                    secret.Purge(now);
                }

                InviteAudits.Add(new AuditLog
                {
                    TenantId = tenantId,
                    ActorUserId = actorUserId,
                    ActorType = "TENANT_USER",
                    EntityType = "TENANT_USER",
                    EntityId = userId,
                    Action = "user.invite_revoked",
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new { inviteId = OpenInvites[0].Id }),
                    CreatedAt = now
                });
            }

            return Task.FromResult(TenantAdminUserInviteMutationResult.Success(GetDetail(userId)));
        }

        public Task<TenantAdminUserDetailResponse?> GetDetailAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<TenantAdminUserDetailResponse?>(GetDetail(userId));
        }

        private TenantAdminUserDetailResponse GetDetail(Guid userId)
        {
            var user = CreatedUser ?? InviteMutationUser;
            return new TenantAdminUserDetailResponse(
                userId,
                user?.FullName ?? "Jane Doe",
                user?.Email ?? "JANE.DOE@EXAMPLE.COM",
                user?.UnmaskedPhone ?? user?.Phone ?? "+1234567890",
                RoleId,
                "Store Manager",
                CreatedOutletIds.Select(id => new OutletOptionResponse(id, "Main Outlet", "MAIN", "ACTIVE")).ToList(),
                FormatStatus(user?.AccountStatus ?? TenantUserConstants.StatusActive),
                CreatedOverriddenPermissionIds.Count > 0,
                CreatedOverriddenPermissionIds.ToList(),
                null,
                user?.CreatedAt ?? Now,
                null,
                "Manages store operations.",
                CreatedOutletIds.Count,
                new TenantAdminUserAccessSummaryResponse(CreatedOutletIds.Count, 0, CreatedOverriddenPermissionIds.Count),
                user?.EmployeeId,
                user?.StaffCode);
        }

        public Task<TenantUser?> GetEditableAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<TenantUser?>(EditableUser ?? TenantUser.Create(
                userId, tenantId, "user@example.com", "User", null, null, "hash", "salt",
                TenantUserConstants.StatusActive, "admin", "admin", "HQ", Now));

        public Task ReplaceAssignmentsAsync(
            Guid tenantId, Guid userId, Guid roleId, IReadOnlyCollection<Guid> outletIds,
            bool permissionOverrideEnabled, IReadOnlyCollection<Guid> overriddenPermissionIds,
            Guid actingUserId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ReplaceAssignmentsAsync(
            Guid tenantId,
            Guid userId,
            Guid roleId,
            string outletAccessScope,
            IReadOnlyCollection<Guid> outletIds,
            bool permissionOverrideEnabled,
            IReadOnlyCollection<Guid> overriddenPermissionIds,
            IReadOnlyCollection<Guid> tillIds,
            Guid actingUserId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CreatedOutletAccessScope = outletAccessScope;
            CreatedOutletIds = outletIds;
            CreatedTillIds = tillIds;
            return Task.CompletedTask;
        }

        public Task ApplyProfileMediaChangeAsync(
            Guid tenantId,
            Guid userId,
            Guid actorUserId,
            Guid? previousMediaAssetId,
            Guid? nextMediaAssetId,
            string auditAction,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ProfileMediaChanges.Add(new ProfileMediaChangeRecord(
                previousMediaAssetId,
                nextMediaAssetId,
                auditAction));
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> HasSalesReferencesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(HasSalesReferences);

        public Task<bool> HasActiveTillSessionAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(HasActiveTillSession);

        private static string FormatStatus(string status)
        {
            return status.Trim().ToUpperInvariant() switch
            {
                TenantUserConstants.StatusActive => "Active",
                TenantUserConstants.StatusInactive => "Inactive",
                TenantUserConstants.StatusInvited => "Invited",
                _ => status,
            };
        }

        public sealed record ProfileMediaChangeRecord(
            Guid? PreviousMediaAssetId,
            Guid? NextMediaAssetId,
            string AuditAction);
    }
}
