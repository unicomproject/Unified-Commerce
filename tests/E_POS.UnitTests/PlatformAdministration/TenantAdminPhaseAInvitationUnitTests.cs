using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Email;
using E_POS.Application.Modules.Platform.PlatformAdmin.Services;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Dtos;
using E_POS.Application.Modules.Tenant.TenantAuth;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Platform.Subscription.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Modules.Shared.Integration.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class TenantAdminPhaseAInvitationUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid PlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Unit_SecureToken_GeneratedCryptographically_AndUrlSafe()
    {
        var hashService = new TokenHashService();
        var jwtOptions = Options.Create(new TenantJwtOptions { SigningKey = "DEV_ONLY_TENANT_JWT_SIGNING_KEY_32_CHARS_MINIMUM" });
        var tokenService = new InvitationTokenService(hashService, jwtOptions);

        var token1 = tokenService.GenerateToken();
        var token2 = tokenService.GenerateToken();

        Assert.False(string.IsNullOrWhiteSpace(token1));
        Assert.False(string.IsNullOrWhiteSpace(token2));
        Assert.NotEqual(token1, token2);
        Assert.True(token1.Length >= 40); // 32 bytes base64 url-safe is 43 chars
        Assert.DoesNotContain("+", token1);
        Assert.DoesNotContain("/", token1);
        Assert.DoesNotContain("=", token1);
    }

    [Fact]
    public void Unit_HashOnlyPersistence_HashDiffersFromRawToken_AndIsDeterministic()
    {
        var hashService = new TokenHashService();
        var jwtOptions = Options.Create(new TenantJwtOptions { SigningKey = "DEV_ONLY_TENANT_JWT_SIGNING_KEY_32_CHARS_MINIMUM" });
        var tokenService = new InvitationTokenService(hashService, jwtOptions);

        var rawToken = tokenService.GenerateToken();
        var hash1 = tokenService.HashToken(rawToken);
        var hash2 = tokenService.HashToken(rawToken);

        Assert.NotEqual(rawToken, hash1);
        Assert.Equal(hash1, hash2);

        var anotherRawToken = tokenService.GenerateToken();
        var anotherHash = tokenService.HashToken(anotherRawToken);
        Assert.NotEqual(hash1, anotherHash);
    }

    [Fact]
    public void Unit_InvitedState_TenantAdminCreatedAsInvited_WithPasswordNotSet()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var adminUser = TenantUser.CreatePendingInvite(
            userId,
            tenantId,
            "admin@example.com",
            "Admin User",
            "+1234567890",
            "+1234567890",
            Now,
            "STAFF-001");

        Assert.Equal(TenantUserConstants.StatusInvited, adminUser.AccountStatus);
        Assert.Null(adminUser.EncryptedPassword);
        Assert.Null(adminUser.PasswordSalt);
        Assert.Equal("ADMIN@EXAMPLE.COM", adminUser.Email);
        Assert.Equal("Admin User", adminUser.FullName);
    }

    [Fact]
    public void Unit_Expiry_SetsConfiguredDuration_GreaterThanIssuedTime()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var hash = "hashed-representation-of-token";
        var expiresAt = Now.AddHours(24);

        var invite = UserInvite.CreatePending(
            Guid.NewGuid(),
            tenantId,
            "admin@example.com",
            "admin@example.com",
            Guid.NewGuid(),
            Guid.NewGuid(),
            hash,
            expiresAt,
            Now,
            tenantUserId: userId);

        Assert.Equal(expiresAt, invite.ExpiresAt);
        Assert.True(invite.ExpiresAt > Now);
        Assert.Equal(TimeSpan.FromHours(24), invite.ExpiresAt - Now);
        Assert.Equal(UserInviteConstants.StatusPending, invite.InviteStatus);
        Assert.Null(invite.AcceptedAt);
    }

    [Fact]
    public void Unit_EmailContent_ContainsAllRequiredFields_AndNoPassword()
    {
        var toAddress = "admin@mycompany.com";
        var tenantName = "Acme Retail Corp";
        var tenantCode = "ACME-01";
        var activationUrl = "https://admin.oneverz.com/tenant-admin/setup/raw_token_xyz";
        var loginUrl = "https://admin.oneverz.com";
        var expiresAt = Now.AddHours(24);

        var message = TenantAdminInvitationEmailComposer.Compose(
            toAddress,
            tenantName,
            tenantCode,
            toAddress,
            activationUrl,
            loginUrl,
            expiresAt,
            "corr-123");

        Assert.Equal(TenantAdminInvitationEmailComposer.Subject, message.Subject);
        Assert.Equal(toAddress, message.ToAddress);

        // Required assertions
        Assert.Contains(tenantName, message.HtmlBody);
        Assert.Contains(tenantCode, message.HtmlBody);
        Assert.Contains(toAddress, message.HtmlBody);
        Assert.Contains(activationUrl, message.HtmlBody);
        Assert.Contains(loginUrl, message.HtmlBody);
        Assert.Contains("Activate Account", message.HtmlBody);
        Assert.Contains(TenantAdminInvitationEmailComposer.SecurityStatement, message.HtmlBody);
        Assert.Contains(expiresAt.ToString("u"), message.HtmlBody);

        Assert.Contains(tenantName, message.PlainTextBody);
        Assert.Contains(tenantCode, message.PlainTextBody);
        Assert.Contains(toAddress, message.PlainTextBody);
        Assert.Contains(activationUrl, message.PlainTextBody);
        Assert.Contains(loginUrl, message.PlainTextBody);
        Assert.Contains(TenantAdminInvitationEmailComposer.SecurityStatement, message.PlainTextBody);

        // Absence of forbidden secrets
        Assert.DoesNotContain("Temporary Password", message.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Permanent Password", message.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password:", message.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Temporary Password", message.PlainTextBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password:", message.PlainTextBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unit_ApiSecretSafety_TenantCreationResponse_DoesNotSerializeSecrets()
    {
        var response = new PlatformTenantDetailResponse(
            Guid.NewGuid(),
            "TEN-001",
            "Tenant One",
            "active",
            "PAID",
            "UNIFIED_EPOS",
            "LKR",
            "Asia/Colombo",
            "en-LK",
            "retail",
            null,
            null,
            null,
            UserCount: 1,
            OutletCount: 0,
            TillCount: 0,
            OnlineStoreEnabled: false,
            ClickCollectEnabled: false,
            OfflineEnabled: false,
            EnabledFeatureIds: [],
            EnabledFeatureCodes: [],
            CreatedAt: Now,
            UpdatedAt: null,
            LastActivityAt: null,
            CanUpdate: true,
            CanActivate: false,
            CanSuspend: true,
            CanManageEntitlements: true,
            LifecycleStatus: "ACTIVE");

        var json = JsonSerializer.Serialize(response);

        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Integration_Provisioning_InsideTransaction_CreatesTenantAdminUser_Invite_AndDispatchesEmail()
    {
        var repo = new FakeTenantProvisioningRepository();
        var tokenService = new FakeInvitationTokenService();
        var deliveryService = new FakeInvitationDeliveryService();

        var service = CreatePlatformTenantService(repo, tokenService, deliveryService);

        var request = new CreatePlatformTenantRequest
        {
            Code = "TA-E2E-01",
            Name = "Tenant Alpha",
            SubscriptionPlanId = PlanId,
            BillingStatus = "paid",
            Subscription = new CreatePlatformTenantSubscriptionDetailsRequest
            {
                SubscriptionType = "PAID",
                BillingCycle = "MONTHLY"
            },
            TenantAdmin = new CreatePlatformTenantAdminRequest
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.doe@alpha.test",
                SendInvite = true
            }
        };

        var result = await service.CreateTenantAsync(request, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? $"{result.Error.Code}: {result.Error.Message} ({string.Join("; ", result.Error.FieldErrors?.Select(f => $"{f.Field}: {f.Message}") ?? [])})" : null);
        Assert.True(repo.CreateWizardCalled);
        var writeModel = repo.LastWriteModel;
        Assert.NotNull(writeModel);

        // Tenant Admin User assertions
        Assert.NotNull(writeModel.TenantAdminUser);
        Assert.Equal("JANE.DOE@ALPHA.TEST", writeModel.TenantAdminUser.Email);
        Assert.Equal(TenantUserConstants.StatusInvited, writeModel.TenantAdminUser.AccountStatus);
        Assert.Null(writeModel.TenantAdminUser.EncryptedPassword);
        Assert.Null(writeModel.TenantAdminUser.PasswordSalt);

        // Tenant Admin Invite assertions
        Assert.NotNull(writeModel.TenantAdminInvite);
        Assert.Equal(tokenService.ExpectedHash, writeModel.TenantAdminInvite.InviteTokenHash);
        Assert.Equal("PENDING", writeModel.TenantAdminInvite.InviteStatus);
        Assert.Equal(writeModel.TenantAdminUser.Id, writeModel.TenantAdminInvite.TenantUserId);

        // Email delivery assertions
        Assert.Single(deliveryService.DeliveredRequests);
        var delivered = deliveryService.DeliveredRequests[0];
        Assert.Equal("jane.doe@alpha.test", delivered.AdminEmail);
        Assert.Equal(tokenService.ExpectedRawToken, delivered.RawToken);
        Assert.Equal(writeModel.TenantAdminInvite.Id, repo.LastSentInviteId);
    }

    [Fact]
    public async Task Integration_Provisioning_WhenEmailFails_BootstrapRemainsCommitted()
    {
        var repo = new FakeTenantProvisioningRepository();
        var tokenService = new FakeInvitationTokenService();
        var failingDeliveryService = new FakeInvitationDeliveryService { ShouldSucceed = false };

        var service = CreatePlatformTenantService(repo, tokenService, failingDeliveryService);

        var request = new CreatePlatformTenantRequest
        {
            Code = "TA-E2E-FAIL",
            Name = "Tenant Fail Test",
            SubscriptionPlanId = PlanId,
            BillingStatus = "paid",
            Subscription = new CreatePlatformTenantSubscriptionDetailsRequest
            {
                SubscriptionType = "PAID",
                BillingCycle = "MONTHLY"
            },
            TenantAdmin = new CreatePlatformTenantAdminRequest
            {
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@fail.test",
                SendInvite = true
            }
        };

        var result = await service.CreateTenantAsync(request, Guid.NewGuid(), CancellationToken.None);

        // Creation succeeds despite email delivery failure
        Assert.True(result.IsSuccess, result.IsFailure ? $"{result.Error.Code}: {result.Error.Message} ({string.Join("; ", result.Error.FieldErrors?.Select(f => $"{f.Field}: {f.Message}") ?? [])})" : null);
        Assert.True(repo.CreateWizardCalled);
        var writeModel = repo.LastWriteModel;
        Assert.NotNull(writeModel);
        Assert.NotNull(writeModel.TenantAdminInvite);
        Assert.Null(writeModel.TenantAdminUser!.EncryptedPassword);

        // Invite was attempted but not marked sent
        Assert.Single(failingDeliveryService.DeliveredRequests);
        Assert.Null(repo.LastSentInviteId); // MarkTenantAdminInviteSentAsync was not called
    }

    private static PlatformTenantService CreatePlatformTenantService(
        FakeTenantProvisioningRepository repo,
        IInvitationTokenService tokenService,
        ITenantAdminInvitationDeliveryService deliveryService)
    {
        var permissions = new HashSet<string>
        {
            PlatformPermissionCodes.TenantsCreate,
            PlatformPermissionCodes.TenantsView
        };

        var planRepo = new FakeSubscriptionPlanRepository();
        var permChecker = new FakePermissionChecker(permissions);
        var permRepo = new FakePermissionRepository(permissions);
        var clock = new FakeDateTimeProvider();
        var passwordHash = new FakePasswordHashService();
        var counterService = new FakeTenantUsageCounterService();
        var settingsProvider = new FakeDefaultTenantSettingsProvider();

        return new PlatformTenantService(
            repo,
            planRepo,
            permChecker,
            permRepo,
            clock,
            passwordHash,
            counterService,
            settingsProvider,
            tokenService,
            deliveryService,
            NullLogger<PlatformTenantService>.Instance);
    }

    private sealed class FakeInvitationTokenService : IInvitationTokenService
    {
        public string ExpectedRawToken { get; set; } = "raw_sec_token_url_safe_test_256bits_entropy";
        public string ExpectedHash { get; set; } = "hash_token_hmac_sha256_abcdef";

        public string GenerateToken() => ExpectedRawToken;
        public string HashToken(string rawToken) => ExpectedHash;
    }

    private sealed class FakeInvitationDeliveryService : ITenantAdminInvitationDeliveryService
    {
        public List<TenantAdminInvitationDeliveryRequest> DeliveredRequests { get; } = [];
        public bool ShouldSucceed { get; set; } = true;

        public Task<TenantAdminInvitationDeliveryResult> DeliverAsync(
            TenantAdminInvitationDeliveryRequest request,
            CancellationToken cancellationToken)
        {
            DeliveredRequests.Add(request);
            if (ShouldSucceed)
            {
                return Task.FromResult(new TenantAdminInvitationDeliveryResult(true));
            }

            return Task.FromResult(new TenantAdminInvitationDeliveryResult(false, "email.provider_failed", "Transport failed."));
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakePasswordHashService : IPasswordHashService
    {
        public string HashPassword(string password) => "hashed_pass";
        public bool VerifyPassword(string password, string passwordHash) => true;
    }

    private sealed class FakePermissionChecker : IPlatformPermissionChecker
    {
        private readonly IReadOnlySet<string> _permissions;
        public FakePermissionChecker(IReadOnlySet<string> permissions) => _permissions = permissions;
        public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken ct) =>
            Task.FromResult(_permissions.Contains(permissionCode));
    }

    private sealed class FakePermissionRepository : IPlatformPermissionRepository
    {
        private readonly IReadOnlySet<string> _permissions;
        public FakePermissionRepository(IReadOnlySet<string> permissions) => _permissions = permissions;
        public Task<IReadOnlySet<string>> GetActivePermissionCodesAsync(Guid platformUserId, CancellationToken ct) =>
            Task.FromResult(_permissions);
    }

    private sealed class FakeSubscriptionPlanRepository : IPlatformSubscriptionPlanRepository
    {
        public Task<SubscriptionPlan?> GetPlanEntityByIdAsync(Guid planId, CancellationToken ct)
        {
            var plan = SubscriptionPlan.Create(
                PlanId,
                "GROWTH",
                "Growth Plan",
                SubscriptionPlanConstants.Status.Active,
                SubscriptionPlanConstants.BillingInterval.Monthly,
                100m,
                Now,
                maxOutlets: 10,
                maxUsers: 10,
                maxTills: 10);
            return Task.FromResult<SubscriptionPlan?>(plan);
        }

        public Task<SubscriptionPlanListResponse> GetPlansAsync(SubscriptionPlanListQuery query, SubscriptionPlanPermissionFlags permissionFlags, CancellationToken ct) => throw new NotImplementedException();
        public Task<SubscriptionPlanCatalogResponse> GetCatalogAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> PlanCodeExistsAsync(string planCode, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> PlanCodeExistsAsync(string planCode, Guid excludingPlanId, CancellationToken ct) => throw new NotImplementedException();
        public Task<SubscriptionPlanMutationResponse?> GetPlanByIdAsync(Guid planId, SubscriptionPlanPermissionFlags permissionFlags, CancellationToken ct) => throw new NotImplementedException();
        public Task<SubscriptionPlanDetailResponse?> GetPlanDetailByIdAsync(Guid planId, SubscriptionPlanPermissionFlags permissionFlags, CancellationToken ct) => throw new NotImplementedException();
        public Task AddPlanAsync(SubscriptionPlan plan, CancellationToken ct) => throw new NotImplementedException();
        public Task SaveChangesAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task ReplacePlanFeaturesAsync(Guid planId, IReadOnlyList<Guid> featureIds, DateTimeOffset now, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlySet<Guid>> GetActiveFeatureIdsAsync(IReadOnlyCollection<Guid> featureIds, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<PlanTechnicalFeatureLookupDto>> GetActiveTenantFeaturesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<PlanTechnicalFeatureLookupDto>>([]);
        public Task<int> GetFeatureCountAsync(Guid planId, CancellationToken ct) => throw new NotImplementedException();
        public Task UpsertLegacyPlanLimitsAsync(Guid planId, int? maxOutlets, int? maxUsers, int? maxTills, DateTimeOffset now, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<string, decimal?>> GetPlanLimitValuesByKeyAsync(Guid planId, CancellationToken ct) => throw new NotImplementedException();
        public Task<int> CountPlanAssignmentsAsync(Guid planId, CancellationToken ct) => throw new NotImplementedException();
        public Task<string?> GetPlanCodeByIdAsync(Guid planId, CancellationToken ct) => throw new NotImplementedException();
        public Task RemovePlanAsync(SubscriptionPlan plan, CancellationToken ct) => throw new NotImplementedException();
        public Task CopyPlanConfigurationAsync(Guid sourcePlanId, Guid targetPlanId, DateTimeOffset now, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeDefaultTenantSettingsProvider : IDefaultTenantSettingsProvider
    {
        public Task<DefaultTenantSettingsProvisionResult> BuildAsync(DefaultTenantSettingsProvisionRequest request, CancellationToken ct)
        {
            return Task.FromResult(new DefaultTenantSettingsProvisionResult(
                [],
                "LKR",
                "Asia/Colombo",
                "en-LK",
                [],
                []));
        }
    }

    private sealed class FakeTenantProvisioningRepository : IPlatformTenantRepository
    {
        public bool CreateWizardCalled { get; private set; }
        public PlatformTenantCreateWriteModel? LastWriteModel { get; private set; }
        public Guid? LastSentInviteId { get; private set; }

        public Task<bool> TenantCodeExistsAsync(string tenantCode, CancellationToken ct) => Task.FromResult(false);

        public Task<PlatformTenantCreateOptionsResponse> GetCreateOptionsAsync(CancellationToken ct) =>
            Task.FromResult(new PlatformTenantCreateOptionsResponse([], [], [], [], [], [], [], [], [], [], [], [], []));

        public Task<IReadOnlyList<ResolvedTenantFeature>> ResolveActiveFeaturesAsync(
            IReadOnlyList<Guid>? featureIds, IReadOnlyList<string>? featureCodes, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ResolvedTenantFeature>>([]);

        public Task<IReadOnlySet<Guid>> GetIncludedFeatureIdsForPlanAsync(Guid planId, CancellationToken ct) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task<Guid?> GetActiveBusinessTypeIdByCodeAsync(string code, CancellationToken ct) =>
            Task.FromResult<Guid?>(null);

        public Task<IReadOnlyDictionary<string, Guid>> GetActivePermissionIdMapByCodesAsync(
            IReadOnlyList<string> codes, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, Guid>>(
                codes.ToDictionary(c => c, _ => Guid.NewGuid()));

        public Task<bool> TenantUserEmailExistsAsync(string email, CancellationToken ct) => Task.FromResult(false);

        public Task CreateTenantWizardAsync(PlatformTenantCreateWriteModel model, CancellationToken ct)
        {
            CreateWizardCalled = true;
            LastWriteModel = model;
            return Task.CompletedTask;
        }

        public Task MarkTenantAdminInviteSentAsync(Guid inviteId, DateTimeOffset sentAt, CancellationToken ct)
        {
            LastSentInviteId = inviteId;
            return Task.CompletedTask;
        }

        public Task AddAuditLogAsync(Guid tenantId, Guid? userId, string action, string summary, string? reason, DateTimeOffset now, CancellationToken ct) =>
            Task.CompletedTask;

        public Task AddAuditLogAsync(
            Guid tenantId,
            Guid? platformUserId,
            string action,
            string summary,
            string? reason,
            DateTimeOffset now,
            string? entityType,
            Guid? entityId,
            object? before,
            object? after,
            string? correlationId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<PlatformTenantDetailResponse?> GetTenantDetailAsync(Guid tenantId, CancellationToken ct) =>
            Task.FromResult<PlatformTenantDetailResponse?>(new PlatformTenantDetailResponse(
                tenantId, "CODE", "Name", "active", "PAID", "UNIFIED_EPOS", "LKR", "Asia/Colombo", "en-LK",
                null, null, null, null, 1, 0, 0, false, false, false, [], [], Now, null, null, true, false, true, true, "ACTIVE"));

        public Task<PlatformTenantListResponse> GetTenantsAsync(PlatformTenantListQuery query, CancellationToken ct) => throw new NotImplementedException();
        public Task<PlatformTenantSummaryResponse> GetSummaryAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<PlatformTenantFilterOptionsResponse> GetFilterOptionsAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<PlatformTenantEntitlementOptionsResponse?> GetEntitlementOptionsAsync(Guid tenantId, CancellationToken ct) => throw new NotImplementedException();
        public Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId, CancellationToken ct) => throw new NotImplementedException();
        public Task AddTenantWithSubscriptionAndEntitlementsAsync(Tenant tenant, TenantSubscription subscription, IReadOnlyList<Guid> enabledFeatureIds, DateTimeOffset now, CancellationToken ct) => throw new NotImplementedException();
        public Task UpdateTenantAsync(Tenant tenant, CancellationToken ct) => throw new NotImplementedException();
        public Task<TenantSubscription?> GetCurrentTenantSubscriptionEntityAsync(Guid tenantId, CancellationToken ct) => throw new NotImplementedException();
        public Task UpdateTenantSubscriptionAsync(TenantSubscription subscription, CancellationToken ct) => throw new NotImplementedException();
        public Task ReplaceTenantEntitlementsAsync(Guid tenantId, IReadOnlyList<Guid> enabledFeatureIds, DateTimeOffset now, Guid? actorPlatformUserId, string? revokedReason, CancellationToken ct) => throw new NotImplementedException();
        public Task ReplaceTenantEntitlementsAsync(Guid tenantId, IReadOnlyList<Guid> enabledFeatureIds, DateTimeOffset now, Guid? actorPlatformUserId, string? revokedReason, string sourceType, string? overrideReason, DateTimeOffset? effectiveFrom, DateTimeOffset? effectiveUntil, CancellationToken ct) => throw new NotImplementedException();
        public Task RestoreTenantPlanEntitlementsAsync(Guid tenantId, Guid subscriptionPlanId, DateTimeOffset now, Guid? actorPlatformUserId, CancellationToken ct) => throw new NotImplementedException();
        public Task<TenantProfile?> GetTenantProfileEntityByTenantIdAsync(Guid tenantId, CancellationToken ct) => throw new NotImplementedException();
        public Task UpsertTenantProfileAsync(TenantProfile profile, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> HasVerifiedPaidInvoiceAsync(Guid tenantId, CancellationToken ct) => throw new NotImplementedException();
        public Task<PlatformTenantActivationRuntimeResult> ActivateTenantRuntimeAsync(Guid tenantId, Guid actorPlatformUserId, DateTimeOffset now, CancellationToken ct) => throw new NotImplementedException();
        public Task<PlatformTenantAuditLogListResponse> GetTenantAuditLogsAsync(Guid tenantId, int pageNumber, int pageSize, CancellationToken ct) => throw new NotImplementedException();
    }
}
