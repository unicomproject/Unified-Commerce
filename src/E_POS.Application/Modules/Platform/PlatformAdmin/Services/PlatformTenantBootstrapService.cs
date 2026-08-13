using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Domain.Modules.Shared.Integration.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.TenantAuth.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformTenantBootstrapService : IPlatformTenantBootstrapService
{
    private const string OutletCodePrefix = "OUT";
    private const string OutletCodeSequenceKey = "OUTLET_CODE";
    private const int GeneratedCodePaddingLength = 3;
    private const int MaxCodeGenerationAttempts = 5;
    private const string OutletCreateOperation = "outlet_create";
    private const string TillCreateOperation = "till_create";
    private const string RoleCreateOperation = "role_create";
    private const string UserCreateOperation = "user_create";
    private const string ProductImportCommitOperation = "products_import_commit";
    private const string ProductCreateOperation = "product_create";
    private const string OnlineStoreUpsertOperation = "online_store_upsert";
    private const string StoreStatusDraft = "DRAFT";
    private const string StoreStatusActive = "ACTIVE";
    private const string TaxDisplayModeMatchTenant = "MATCH_TENANT";
    private const string ClickCollectDependencyNotice =
        "Click & Collect is entitled but collection points are not configured yet. That remains a Tenant Admin task. Online Store readiness can still be saved.";

    private static readonly JsonSerializerOptions IdempotencyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly ApplicationError ValidationFailed = new(
        "platform_tenants.validation_failed",
        "Selected-tenant bootstrap validation failed.");

    private static readonly ApplicationError Conflict = new(
        "platform_tenants.bootstrap.conflict",
        "Selected-tenant bootstrap conflict.");

    private static readonly ApplicationError DependencyMissing = new(
        "platform_tenants.bootstrap.dependency_missing",
        "Selected-tenant bootstrap dependency is missing.");

    private static readonly ApplicationError LimitReached = new(
        "platform_tenants.bootstrap.limit_reached",
        "Selected-tenant bootstrap subscription limit reached.");

    private static readonly ApplicationError NotEntitled = new(
        "platform_tenants.bootstrap.not_entitled",
        "Tenant is not entitled for this bootstrap module.");

    private static readonly ApplicationError ImportBatchInProgress = new(
        "import.batch_in_progress",
        "A bootstrap product import batch is already in progress for this tenant.");

    private static readonly ApplicationError ImportNotFound = new(
        "import.not_found",
        "Bootstrap product import batch was not found.");

    private readonly PlatformSelectedTenantAccessPolicy _accessPolicy;
    private readonly IPlatformTenantBootstrapRepository _bootstrapRepository;
    private readonly IPlatformTenantRepository _tenantRepository;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly ITenantFeatureEntitlementEvaluator _featureEntitlementEvaluator;
    private readonly IOutletRepository _outletRepository;
    private readonly IOutletRequestValidator _outletRequestValidator;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly ITenantResourceLimitGuard _resourceLimitGuard;
    private readonly ITenantAdminTillRepository _tillRepository;
    private readonly ITenantAdminUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly ITenantAdminProductRepository _tenantAdminProductRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestCorrelationAccessor _correlationAccessor;
    private readonly IInvitationTokenService _invitationTokenService;
    private readonly Lazy<IInvitationDeliverySecretProtector> _deliverySecretProtector;
    private readonly ITenantUserStaffCodeService _staffCodeService;

    public PlatformTenantBootstrapService(
        PlatformSelectedTenantAccessPolicy accessPolicy,
        IPlatformTenantBootstrapRepository bootstrapRepository,
        IPlatformTenantRepository tenantRepository,
        IPlatformPermissionChecker permissionChecker,
        ITenantFeatureEntitlementEvaluator featureEntitlementEvaluator,
        IOutletRepository outletRepository,
        IOutletRequestValidator outletRequestValidator,
        ICodeSequenceRepository codeSequenceRepository,
        ITenantResourceLimitGuard resourceLimitGuard,
        ITenantAdminTillRepository tillRepository,
        ITenantAdminUserRepository userRepository,
        IProductRepository productRepository,
        ITenantAdminProductRepository tenantAdminProductRepository,
        IDateTimeProvider dateTimeProvider,
        IRequestCorrelationAccessor correlationAccessor,
        IInvitationTokenService invitationTokenService,
        Lazy<IInvitationDeliverySecretProtector> deliverySecretProtector,
        ITenantUserStaffCodeService staffCodeService)
    {
        _accessPolicy = accessPolicy;
        _bootstrapRepository = bootstrapRepository;
        _tenantRepository = tenantRepository;
        _permissionChecker = permissionChecker;
        _featureEntitlementEvaluator = featureEntitlementEvaluator;
        _outletRepository = outletRepository;
        _outletRequestValidator = outletRequestValidator;
        _codeSequenceRepository = codeSequenceRepository;
        _resourceLimitGuard = resourceLimitGuard;
        _tillRepository = tillRepository;
        _userRepository = userRepository;
        _productRepository = productRepository;
        _tenantAdminProductRepository = tenantAdminProductRepository;
        _dateTimeProvider = dateTimeProvider;
        _correlationAccessor = correlationAccessor;
        _invitationTokenService = invitationTokenService;
        _deliverySecretProtector = deliverySecretProtector;
        _staffCodeService = staffCodeService;
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapSummaryResponse>> GetSummaryAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var access = await _accessPolicy.AuthorizeReadAsync(platformUserId, tenantId, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapSummaryResponse>.Failure(access.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        var counts = await _bootstrapRepository.GetFootprintCountsAsync(tenantId, cancellationToken);
        var outletEntitled = await _featureEntitlementEvaluator.IsEnabledAsync(
            tenantId, PlatformTenantFeatureCodes.OutletManagement, now, cancellationToken);
        var tillEntitled = await _featureEntitlementEvaluator.IsEnabledAsync(
            tenantId, PlatformTenantFeatureCodes.TillManagement, now, cancellationToken);
        var productEntitled = await _featureEntitlementEvaluator.IsEnabledAsync(
            tenantId, PlatformTenantFeatureCodes.ProductCatalog, now, cancellationToken);
        var onlineStoreEntitled = await _featureEntitlementEvaluator.IsEnabledAsync(
            tenantId, PlatformTenantFeatureCodes.OnlineStore, now, cancellationToken);
        var onlineStoreDefaults = ParseOnlineStoreDefaults(
            await _bootstrapRepository.GetOnlineStoreDefaultsJsonAsync(tenantId, cancellationToken));

        var modules = PlatformSelectedTenantSetupHubStatusEvaluator.Evaluate(
            new PlatformSelectedTenantSetupHubStatusEvaluator.Input(
                outletEntitled,
                tillEntitled,
                productEntitled,
                counts.ActiveOutletCount,
                counts.ActiveTillCount,
                counts.CustomRoleCount,
                counts.TenantUserCount,
                counts.ActiveOrDraftProductCount,
                string.Equals(access.Value!.Snapshot.LifecycleStatus, TenantStatusConstants.Suspended, StringComparison.OrdinalIgnoreCase),
                await _permissionChecker.HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsBootstrapOutletsManage, cancellationToken),
                await _permissionChecker.HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsBootstrapTillsManage, cancellationToken),
                await _permissionChecker.HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsBootstrapRolesManage, cancellationToken),
                await _permissionChecker.HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsBootstrapUsersManage, cancellationToken),
                await _permissionChecker.HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsBootstrapProductsManage, cancellationToken),
                onlineStoreEntitled,
                onlineStoreDefaults.StoreStatus,
                await _permissionChecker.HasPermissionAsync(platformUserId, PlatformPermissionCodes.TenantsBootstrapOnlineStoreManage, cancellationToken)));

        return ApplicationResult<PlatformTenantBootstrapSummaryResponse>.Success(
            new PlatformTenantBootstrapSummaryResponse(
                new PlatformTenantBootstrapTenantSummaryDto(
                    access.Value.Snapshot.TenantId,
                    access.Value.Snapshot.TenantName,
                    access.Value.Snapshot.TenantCode,
                    access.Value.Snapshot.LifecycleStatus,
                    access.Value.Snapshot.PlanName),
                modules));
    }

    public async Task<ApplicationResult<IReadOnlyList<PlatformTenantBootstrapOutletOptionDto>>> GetOutletOptionsAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var access = await _accessPolicy.AuthorizeReadAsync(platformUserId, tenantId, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<IReadOnlyList<PlatformTenantBootstrapOutletOptionDto>>.Failure(access.Error);
        }

        var items = await _bootstrapRepository.ListOutletOptionsAsync(tenantId, cancellationToken);
        return ApplicationResult<IReadOnlyList<PlatformTenantBootstrapOutletOptionDto>>.Success(items);
    }

    public async Task<ApplicationResult<IReadOnlyList<PlatformTenantBootstrapRoleOptionDto>>> GetRoleOptionsAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var access = await _accessPolicy.AuthorizeReadAsync(platformUserId, tenantId, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<IReadOnlyList<PlatformTenantBootstrapRoleOptionDto>>.Failure(access.Error);
        }

        var items = await _bootstrapRepository.ListRoleOptionsAsync(tenantId, cancellationToken);
        return ApplicationResult<IReadOnlyList<PlatformTenantBootstrapRoleOptionDto>>.Success(items);
    }

    public async Task<ApplicationResult<IReadOnlyList<PlatformTenantBootstrapPermissionOptionDto>>> GetPermissionOptionsAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var access = await _accessPolicy.AuthorizeReadAsync(platformUserId, tenantId, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<IReadOnlyList<PlatformTenantBootstrapPermissionOptionDto>>.Failure(access.Error);
        }

        var codes = await ListEntitledTenantPermissionCodesAsync(tenantId, cancellationToken);
        var items = codes
            .Select(code => new PlatformTenantBootstrapPermissionOptionDto(code))
            .ToList();
        return ApplicationResult<IReadOnlyList<PlatformTenantBootstrapPermissionOptionDto>>.Success(items);
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapOutletResponse>> CreateOutletAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapOutletCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestHash = HashRequest(request);
        var replay = await TryReplayOrConflictAsync<PlatformTenantBootstrapOutletResponse>(
            tenantId, OutletCreateOperation, idempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var access = await _accessPolicy.AuthorizeMutationAsync(
            platformUserId, tenantId, PlatformPermissionCodes.TenantsBootstrapOutletsManage, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapOutletResponse>.Failure(access.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        if (!await _featureEntitlementEvaluator.IsEnabledAsync(
                tenantId, PlatformTenantFeatureCodes.OutletManagement, now, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapOutletResponse>.Failure(NotEntitled);
        }

        var outletRequest = new OutletCreateRequest(
            request.OutletName,
            request.Status,
            request.OutletType,
            request.Timezone,
            IsDefaultOutlet: false,
            request.Phone,
            request.Email,
            new OutletAddressRequest(
                request.Address.AddressLine1,
                null,
                request.Address.City,
                request.Address.StateOrProvince,
                request.Address.PostalCode,
                request.Address.CountryCode,
                null,
                null,
                null),
            BusinessHours: null,
            CollectionEnabled: false);

        var validationError = _outletRequestValidator.ValidateCreate(outletRequest);
        if (validationError is not null)
        {
            return ApplicationResult<PlatformTenantBootstrapOutletResponse>.Failure(validationError);
        }

        var guarded = await _resourceLimitGuard.ExecuteWithinCapacityAsync(
            tenantId,
            TenantSubscriptionLimitKeys.MaxOutlets,
            requestedIncrease: 1,
            async ct =>
            {
                var created = await CreateOutletInternalAsync(tenantId, outletRequest, ct);
                return created.IsSuccess
                    ? TenantResourceCapacityOperationResult<ApplicationResult<PlatformTenantBootstrapOutletResponse>>.Succeeded(created)
                    : TenantResourceCapacityOperationResult<ApplicationResult<PlatformTenantBootstrapOutletResponse>>.Aborted(created);
            },
            cancellationToken);

        if (!guarded.Allowed)
        {
            return ApplicationResult<PlatformTenantBootstrapOutletResponse>.Failure(
                guarded.Evaluation.ToApplicationError() ?? LimitReached);
        }

        if (guarded.Value!.IsSuccess)
        {
            var outlet = guarded.Value.Value!;
            await _tenantRepository.AddAuditLogAsync(
                tenantId,
                platformUserId,
                "platform.tenant_bootstrap.outlet_created",
                $"Bootstrap outlet '{outlet.OutletCode}' created.",
                null,
                now,
                "Outlet",
                outlet.OutletId,
                before: null,
                after: outlet,
                _correlationAccessor.CorrelationId,
                cancellationToken);

            await SaveIdempotencyAsync(
                tenantId,
                OutletCreateOperation,
                idempotencyKey,
                requestHash,
                outlet,
                now,
                cancellationToken);
        }

        return guarded.Value!;
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapTillResponse>> CreateTillAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapTillCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestHash = HashRequest(request);
        var replay = await TryReplayOrConflictAsync<PlatformTenantBootstrapTillResponse>(
            tenantId, TillCreateOperation, idempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var access = await _accessPolicy.AuthorizeMutationAsync(
            platformUserId, tenantId, PlatformPermissionCodes.TenantsBootstrapTillsManage, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapTillResponse>.Failure(access.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        if (!await _featureEntitlementEvaluator.IsEnabledAsync(
                tenantId, PlatformTenantFeatureCodes.TillManagement, now, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapTillResponse>.Failure(NotEntitled);
        }

        if (string.IsNullOrWhiteSpace(request.TillName) || string.IsNullOrWhiteSpace(request.TillCode))
        {
            return ApplicationResult<PlatformTenantBootstrapTillResponse>.Failure(
                ValidationFailed with { Message = "Till name and till code are required." });
        }

        if (!await _bootstrapRepository.OutletBelongsToTenantAsync(tenantId, request.OutletId, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapTillResponse>.Failure(DependencyMissing);
        }

        var normalizedTillCode = TillConstants.NormalizeTillCode(request.TillCode);
        if (await _tillRepository.TillCodeExistsForTenantAsync(tenantId, normalizedTillCode, null, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapTillResponse>.Failure(Conflict with { Message = "Till code already exists for this tenant." });
        }

        var tillId = Guid.NewGuid();
        var tillAreaName = request.TillName.Trim();
        var tillNumber = await _tillRepository.GetNextTillNumberAsync(
            tenantId, request.OutletId, tillAreaName, cancellationToken);

        var till = Till.Create(
            tillId,
            tenantId,
            request.OutletId,
            request.TillName.Trim(),
            tillAreaName,
            tillNumber,
            normalizedTillCode,
            TillConstants.StandardTillType,
            defaultOpeningFloatAmount: 0m,
            TillConstants.DefaultCurrencyCode,
            isCashManaged: true,
            OutletConstants.ActiveStatus,
            createdByTenantUserId: null,
            now);

        var guarded = await _resourceLimitGuard.ExecuteWithinCapacityAsync(
            tenantId,
            TenantSubscriptionLimitKeys.MaxTills,
            requestedIncrease: 1,
            async ct =>
            {
                await _tillRepository.ExecuteInTransactionAsync(async () =>
                {
                    await _tillRepository.AddAsync(till, ct);
                }, ct);

                return TenantResourceCapacityOperationResult<ApplicationResult<PlatformTenantBootstrapTillResponse>>.Succeeded(
                    ApplicationResult<PlatformTenantBootstrapTillResponse>.Success(
                        new PlatformTenantBootstrapTillResponse(
                            tillId,
                            till.TillName,
                            till.TillCode,
                            request.OutletId,
                            till.Status,
                            "PENDING")));
            },
            cancellationToken);

        if (!guarded.Allowed)
        {
            return ApplicationResult<PlatformTenantBootstrapTillResponse>.Failure(
                guarded.Evaluation.ToApplicationError() ?? LimitReached);
        }

        await _tenantRepository.AddAuditLogAsync(
            tenantId,
            platformUserId,
            "platform.tenant_bootstrap.till_created",
            $"Bootstrap till '{normalizedTillCode}' created.",
            null,
            now,
            "Till",
            guarded.Value!.Value!.TillId,
            before: null,
            after: guarded.Value.Value,
            _correlationAccessor.CorrelationId,
            cancellationToken);

        if (guarded.Value!.IsSuccess)
        {
            await SaveIdempotencyAsync(
                tenantId,
                TillCreateOperation,
                idempotencyKey,
                requestHash,
                guarded.Value.Value!,
                now,
                cancellationToken);
        }

        return guarded.Value!;
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapRoleResponse>> CreateRoleAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapRoleCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestHash = HashRequest(request);
        var replay = await TryReplayOrConflictAsync<PlatformTenantBootstrapRoleResponse>(
            tenantId, RoleCreateOperation, idempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var access = await _accessPolicy.AuthorizeMutationAsync(
            platformUserId, tenantId, PlatformPermissionCodes.TenantsBootstrapRolesManage, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapRoleResponse>.Failure(access.Error);
        }

        if (string.IsNullOrWhiteSpace(request.RoleName))
        {
            return ApplicationResult<PlatformTenantBootstrapRoleResponse>.Failure(
                ValidationFailed with { Message = "Role name is required." });
        }

        var permissionCodes = request.PermissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (permissionCodes.Count == 0)
        {
            return ApplicationResult<PlatformTenantBootstrapRoleResponse>.Failure(
                ValidationFailed with { Message = "At least one permission code is required." });
        }

        var entitledCodes = await ResolveEntitledTenantPermissionCodesAsync(tenantId, permissionCodes, cancellationToken);
        if (entitledCodes.Count != permissionCodes.Count)
        {
            return ApplicationResult<PlatformTenantBootstrapRoleResponse>.Failure(NotEntitled);
        }

        var permissionMap = await _bootstrapRepository.GetActivePermissionIdMapByCodesAsync(permissionCodes, cancellationToken);
        if (permissionMap.Count != permissionCodes.Count)
        {
            return ApplicationResult<PlatformTenantBootstrapRoleResponse>.Failure(
                ValidationFailed with { Message = "One or more permission codes are invalid." });
        }

        var now = _dateTimeProvider.UtcNow;
        var roleId = await _bootstrapRepository.CreateCustomRoleAsync(
            tenantId,
            request.RoleName,
            request.Description,
            permissionMap.Values.ToList(),
            now,
            cancellationToken);

        var response = new PlatformTenantBootstrapRoleResponse(roleId, request.RoleName.Trim(), $"CUSTOM_{roleId:N}"[..20], permissionCodes);
        await _tenantRepository.AddAuditLogAsync(
            tenantId,
            platformUserId,
            "platform.tenant_bootstrap.role_created",
            $"Bootstrap role '{request.RoleName.Trim()}' created.",
            null,
            now,
            "Role",
            roleId,
            before: null,
            after: response,
            _correlationAccessor.CorrelationId,
            cancellationToken);

        await SaveIdempotencyAsync(
            tenantId,
            RoleCreateOperation,
            idempotencyKey,
            requestHash,
            response,
            now,
            cancellationToken);

        return ApplicationResult<PlatformTenantBootstrapRoleResponse>.Success(response);
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapUserResponse>> CreateUserAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapUserCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestHash = HashRequest(request);
        var replay = await TryReplayOrConflictAsync<PlatformTenantBootstrapUserResponse>(
            tenantId, UserCreateOperation, idempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var access = await _accessPolicy.AuthorizeMutationAsync(
            platformUserId, tenantId, PlatformPermissionCodes.TenantsBootstrapUsersManage, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapUserResponse>.Failure(access.Error);
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Email))
        {
            return ApplicationResult<PlatformTenantBootstrapUserResponse>.Failure(
                ValidationFailed with { Message = "Display name and email are required." });
        }

        if (!await _bootstrapRepository.RoleBelongsToTenantAsync(tenantId, request.RoleId, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapUserResponse>.Failure(DependencyMissing);
        }

        var outletIds = request.OutletIds?.ToArray() ?? [];
        if (outletIds.Length > 0 &&
            !await _bootstrapRepository.OutletsBelongToTenantAsync(tenantId, outletIds, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapUserResponse>.Failure(DependencyMissing);
        }

        var normalizedEmail = TenantUser.NormalizeEmail(request.Email);
        if (await _bootstrapRepository.EmailExistsForTenantAsync(tenantId, normalizedEmail, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapUserResponse>.Failure(
                Conflict with { Message = "A user with this email already exists for this tenant." });
        }

        var now = _dateTimeProvider.UtcNow;
        var userId = Guid.NewGuid();
        var staffCode = await _staffCodeService.GenerateAsync(tenantId, now, cancellationToken);
        var user = TenantUser.CreatePendingInvite(
            userId,
            tenantId,
            request.Email.Trim(),
            request.DisplayName.Trim(),
            request.Phone?.Trim(),
            request.Phone?.Trim(),
            now,
            staffCode);

        var rawToken = _invitationTokenService.GenerateToken();
        var protectedToken = _deliverySecretProtector.Value.Protect(rawToken);
        var inviteTokenHash = _invitationTokenService.HashToken(rawToken);
        var invite = UserInvite.CreatePending(
            Guid.NewGuid(),
            tenantId,
            request.Email.Trim(),
            normalizedEmail,
            request.RoleId,
            platformUserId,
            inviteTokenHash,
            now.AddDays(7),
            now,
            userId);
        var deliverySecret = TenantUserInviteDeliverySecret.Create(
            Guid.NewGuid(),
            tenantId,
            userId,
            invite.Id,
            protectedToken.Ciphertext,
            protectedToken.KeyVersion,
            invite.ExpiresAt,
            now);
        var outbox = IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            "tenant.user_invited",
            "TENANT_USER",
            userId,
            1,
            tenantId,
            Guid.NewGuid(),
            null,
            JsonSerializer.Serialize(new { tenantId, tenantUserId = userId, inviteId = invite.Id }),
            $"tenant.user_invited:{invite.Id:N}",
            now);
        IReadOnlyCollection<AuditLog> auditLogs = [];

        var guarded = await _resourceLimitGuard.ExecuteWithinCapacityAsync(
            tenantId,
            TenantSubscriptionLimitKeys.MaxUsers,
            requestedIncrease: 1,
            async ct =>
            {
                await _userRepository.CreateAsync(
                    user,
                    request.RoleId,
                    outletIds,
                    [],
                    invite,
                    deliverySecret,
                    outbox,
                    auditLogs,
                    now,
                    ct);
                return TenantResourceCapacityOperationResult<ApplicationResult<PlatformTenantBootstrapUserResponse>>.Succeeded(
                    ApplicationResult<PlatformTenantBootstrapUserResponse>.Success(
                        new PlatformTenantBootstrapUserResponse(
                            userId,
                            request.DisplayName.Trim(),
                            request.Email.Trim(),
                            user.AccountStatus,
                            "PENDING")));
            },
            cancellationToken);

        if (!guarded.Allowed)
        {
            return ApplicationResult<PlatformTenantBootstrapUserResponse>.Failure(
                guarded.Evaluation.ToApplicationError() ?? LimitReached);
        }

        await _tenantRepository.AddAuditLogAsync(
            tenantId,
            platformUserId,
            "platform.tenant_bootstrap.user_created",
            $"Bootstrap user '{request.Email.Trim()}' invited.",
            null,
            now,
            "TenantUser",
            userId,
            before: null,
            after: guarded.Value!.Value!,
            _correlationAccessor.CorrelationId,
            cancellationToken);

        if (guarded.Value!.IsSuccess)
        {
            await SaveIdempotencyAsync(
                tenantId,
                UserCreateOperation,
                idempotencyKey,
                requestHash,
                guarded.Value.Value!,
                now,
                cancellationToken);
        }

        return guarded.Value!;
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapProductResponse>> CreateProductAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapProductCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestHash = HashRequest(request);
        var replay = await TryReplayOrConflictAsync<PlatformTenantBootstrapProductResponse>(
            tenantId, ProductCreateOperation, idempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var access = await _accessPolicy.AuthorizeMutationAsync(
            platformUserId, tenantId, PlatformPermissionCodes.TenantsBootstrapProductsManage, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(access.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        if (!await _featureEntitlementEvaluator.IsEnabledAsync(
                tenantId, PlatformTenantFeatureCodes.ProductCatalog, now, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(NotEntitled);
        }

        var validationError = ValidateBootstrapProductRequest(request);
        if (validationError is not null)
        {
            return ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(validationError);
        }

        if (await _productRepository.SkuExistsAsync(tenantId, request.Sku.Trim(), null, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(
                Conflict with { Message = "SKU already exists for this tenant." });
        }

        if (!string.IsNullOrWhiteSpace(request.Barcode) &&
            await _productRepository.BarcodeExistsAsync(tenantId, request.Barcode.Trim(), null, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(
                Conflict with { Message = "Barcode already exists for this tenant." });
        }

        if (request.OpeningStockQuantity.GetValueOrDefault() > 0 &&
            (!request.OutletId.HasValue ||
             !await _bootstrapRepository.OutletBelongsToTenantAsync(tenantId, request.OutletId.Value, cancellationToken)))
        {
            return ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(DependencyMissing);
        }

        var categoryId = request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty
            ? request.CategoryId.Value
            : Guid.Empty;

        if (categoryId != Guid.Empty &&
            !await _tenantAdminProductRepository.CategoryBelongsToTenantAsync(
                tenantId,
                categoryId,
                parentCategoryId: null,
                cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(DependencyMissing);
        }

        var unitId = await _tenantAdminProductRepository.ResolveUnitIdAsync(tenantId, "EA", cancellationToken);
        if (!unitId.HasValue)
        {
            return ApplicationResult<PlatformTenantBootstrapProductResponse>.Failure(
                ValidationFailed with { Message = "Default unit EA is not configured for this tenant." });
        }

        var mappedRequest = new TenantAdminProductCreateRequest
        {
            ProductName = request.ProductName.Trim(),
            Sku = request.Sku.Trim(),
            Barcode = request.Barcode?.Trim(),
            CategoryId = categoryId,
            UnitType = "EA",
            SellingPrice = request.SellingPrice,
            TrackInventory = request.TrackInventory ?? true,
            OpeningStockQuantity = request.OpeningStockQuantity,
            OutletIds = request.OutletId.HasValue ? [request.OutletId.Value] : null,
            HasVariants = false,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "ACTIVE" : request.Status.Trim().ToUpperInvariant()
        };

        // Platform actor must not be written into tenant_users FK columns (published_by / created_by).
        var created = await _tenantAdminProductRepository.CreateProductAsync(
            tenantId,
            userId: null,
            mappedRequest,
            unitId.Value,
            now,
            cancellationToken);

        var response = new PlatformTenantBootstrapProductResponse(
            created.ProductId,
            created.ProductName,
            created.Sku,
            created.Status);

        await _tenantRepository.AddAuditLogAsync(
            tenantId,
            platformUserId,
            "platform.tenant_bootstrap.product_created",
            $"Bootstrap product '{created.Sku}' created.",
            null,
            now,
            "Product",
            created.ProductId,
            before: null,
            after: response,
            _correlationAccessor.CorrelationId,
            cancellationToken);

        await SaveIdempotencyAsync(
            tenantId,
            ProductCreateOperation,
            idempotencyKey,
            requestHash,
            response,
            now,
            cancellationToken);

        return ApplicationResult<PlatformTenantBootstrapProductResponse>.Success(response);
    }

    public async Task<ApplicationResult<byte[]>> GetProductImportTemplateAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var access = await _accessPolicy.AuthorizeMutationAsync(
            platformUserId, tenantId, PlatformPermissionCodes.TenantsBootstrapProductsImport, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<byte[]>.Failure(access.Error);
        }

        return ApplicationResult<byte[]>.Success(
            System.Text.Encoding.UTF8.GetBytes(PlatformTenantBootstrapProductImportParser.BuildTemplateCsv()));
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>> ValidateProductImportAsync(
        Guid tenantId,
        Guid platformUserId,
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        var access = await _accessPolicy.AuthorizeMutationAsync(
            platformUserId, tenantId, PlatformPermissionCodes.TenantsBootstrapProductsImport, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>.Failure(access.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        if (!await _featureEntitlementEvaluator.IsEnabledAsync(
                tenantId, PlatformTenantFeatureCodes.ProductCatalog, now, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>.Failure(NotEntitled);
        }

        if (await _bootstrapRepository.HasInFlightImportBatchAsync(tenantId, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>.Failure(ImportBatchInProgress);
        }

        var parsed = PlatformTenantBootstrapProductImportParser.Parse(csvStream, fileName);
        if (!parsed.IsSuccess)
        {
            return ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>.Failure(
                new ApplicationError(parsed.ErrorCode!, parsed.ErrorMessage!));
        }

        var validatedRows = await PlatformTenantBootstrapProductImportValidator.ValidateRowsAsync(
            tenantId,
            parsed.Rows,
            _bootstrapRepository,
            _productRepository,
            cancellationToken);

        var importId = Guid.NewGuid();
        var batch = PlatformTenantBootstrapProductImportBatch.CreateValidated(
            importId,
            tenantId,
            fileName,
            validatedRows.Count,
            validatedRows.Count(row => row.IsValid),
            validatedRows.Count(row => !row.IsValid),
            platformUserId,
            now);

        var rows = validatedRows.Select(row =>
            PlatformTenantBootstrapProductImportRow.Create(
                Guid.NewGuid(),
                importId,
                tenantId,
                row.RowNumber,
                row.IsValid && row.ProductRequest is not null
                    ? JsonSerializer.Serialize(row.ProductRequest)
                    : row.RawRowJson,
                row.IsValid,
                row.ErrorCode,
                row.ErrorDetail,
                now)).ToList();

        await _bootstrapRepository.SaveImportBatchAsync(batch, rows, cancellationToken);

        var preview = validatedRows
            .Where(row => !row.IsValid)
            .Take(20)
            .Select(row => new PlatformTenantBootstrapProductImportPreviewInvalidRow(
                row.RowNumber,
                row.ErrorCode ?? "import.invalid_row",
                row.ErrorDetail ?? "Row failed validation."))
            .ToList();

        return ApplicationResult<PlatformTenantBootstrapProductImportValidateResponse>.Success(
            new PlatformTenantBootstrapProductImportValidateResponse(
                importId,
                validatedRows.Count,
                validatedRows.Count(row => row.IsValid),
                validatedRows.Count(row => !row.IsValid),
                preview));
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>> CommitProductImportAsync(
        Guid tenantId,
        Guid platformUserId,
        Guid importId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var access = await _accessPolicy.AuthorizeMutationAsync(
            platformUserId, tenantId, PlatformPermissionCodes.TenantsBootstrapProductsImport, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>.Failure(access.Error);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>.Failure(
                ValidationFailed with { Message = "Idempotency-Key header is required." });
        }

        var commitRequest = new { importId };
        var requestHash = HashRequest(commitRequest);
        var replay = await TryReplayOrConflictAsync<PlatformTenantBootstrapProductImportCommitResponse>(
            tenantId, ProductImportCommitOperation, idempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var batch = await _bootstrapRepository.GetImportBatchAsync(tenantId, importId, cancellationToken);
        if (batch is null)
        {
            return ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>.Failure(ImportNotFound);
        }

        if (batch.Status == "COMMITTED")
        {
            var committed = new PlatformTenantBootstrapProductImportCommitResponse(
                importId, batch.CommittedRows, batch.SkippedRows);
            return ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>.Success(committed);
        }

        if (batch.Status == "COMMITTING")
        {
            return ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>.Failure(ImportBatchInProgress);
        }

        var now = _dateTimeProvider.UtcNow;
        batch.MarkCommitting(now);
        await _bootstrapRepository.UpdateImportBatchAsync(batch, cancellationToken);

        var rows = await _bootstrapRepository.GetImportRowsAsync(importId, cancellationToken);
        var committedRows = 0;
        var skippedRows = 0;

        foreach (var row in rows.Where(item => item.IsValid))
        {
            var productRequest = JsonSerializer.Deserialize<PlatformTenantBootstrapProductCreateRequest>(row.RawRowJson);
            if (productRequest is null)
            {
                skippedRows++;
                continue;
            }

            var createResult = await CreateProductAsync(
                tenantId,
                platformUserId,
                productRequest,
                idempotencyKey: $"{idempotencyKey}:row:{row.RowNumber}",
                cancellationToken);

            if (createResult.IsSuccess)
            {
                committedRows++;
                row.MarkCommitted(createResult.Value!.ProductId, now);
            }
            else
            {
                skippedRows++;
            }
        }

        skippedRows += rows.Count(item => !item.IsValid);
        await _bootstrapRepository.UpdateImportRowsAsync(rows, cancellationToken);
        batch.MarkCommitted(committedRows, skippedRows, idempotencyKey, now);
        await _bootstrapRepository.UpdateImportBatchAsync(batch, cancellationToken);

        var response = new PlatformTenantBootstrapProductImportCommitResponse(importId, committedRows, skippedRows);
        await SaveIdempotencyAsync(
            tenantId,
            ProductImportCommitOperation,
            idempotencyKey,
            requestHash,
            response,
            now,
            cancellationToken);

        await _tenantRepository.AddAuditLogAsync(
            tenantId,
            platformUserId,
            "platform.tenant_bootstrap.products_imported",
            $"Bootstrap product import {importId:D} committed {committedRows} rows, skipped {skippedRows}.",
            null,
            now,
            "ProductImportBatch",
            importId,
            before: null,
            after: response,
            _correlationAccessor.CorrelationId,
            cancellationToken);

        return ApplicationResult<PlatformTenantBootstrapProductImportCommitResponse>.Success(response);
    }

    public async Task<ApplicationResult<byte[]>> GetProductImportErrorsCsvAsync(
        Guid tenantId,
        Guid platformUserId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var access = await _accessPolicy.AuthorizeReadAsync(platformUserId, tenantId, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<byte[]>.Failure(access.Error);
        }

        if (!await _permissionChecker.HasPermissionAsync(
                platformUserId, PlatformPermissionCodes.TenantsBootstrapProductsImport, cancellationToken))
        {
            return ApplicationResult<byte[]>.Failure(PlatformSelectedTenantAccessPolicy.AccessDenied);
        }

        var batch = await _bootstrapRepository.GetImportBatchAsync(tenantId, importId, cancellationToken);
        if (batch is null)
        {
            return ApplicationResult<byte[]>.Failure(ImportNotFound);
        }

        var rows = await _bootstrapRepository.GetImportRowsAsync(importId, cancellationToken);
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("row_number,error_code,error_detail,raw_row");
        foreach (var row in rows.Where(item => !item.IsValid))
        {
            builder.Append(row.RowNumber);
            builder.Append(',');
            builder.Append(CsvEscape(row.ErrorCode));
            builder.Append(',');
            builder.Append(CsvEscape(row.ErrorDetail));
            builder.Append(',');
            builder.Append(CsvEscape(row.RawRowJson));
            builder.AppendLine();
        }

        return ApplicationResult<byte[]>.Success(System.Text.Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private async Task<ApplicationResult<PlatformTenantBootstrapOutletResponse>> CreateOutletInternalAsync(
        Guid tenantId,
        OutletCreateRequest request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCodeGenerationAttempts; attempt++)
        {
            var now = _dateTimeProvider.UtcNow;
            var outletCode = await _codeSequenceRepository.GetNextCodeAsync(
                tenantId, OutletCodeSequenceKey, OutletCodePrefix, GeneratedCodePaddingLength, now, cancellationToken);

            if (await _outletRepository.OutletCodeExistsAsync(tenantId, outletCode, null, cancellationToken))
            {
                continue;
            }

            var outletId = Guid.NewGuid();
            var outlet = Outlet.Create(
                outletId,
                tenantId,
                request.OutletName,
                outletCode,
                request.Status,
                request.OutletType,
                request.Timezone,
                request.IsDefaultOutlet,
                request.Phone,
                request.Email,
                createdByTenantUserId: null,
                now);

            var address = OutletAddress.Create(
                Guid.NewGuid(),
                tenantId,
                outletId,
                request.Address.AddressLine1,
                request.Address.AddressLine2,
                request.Address.City,
                request.Address.StateOrProvince,
                request.Address.PostalCode,
                request.Address.CountryCode,
                request.Address.ContactName,
                request.Address.ContactPhone,
                request.Address.ContactEmail,
                createdByTenantUserId: null,
                now);

            if (!await _outletRepository.AddAsync(outlet, address, [], null, cancellationToken))
            {
                continue;
            }

            return ApplicationResult<PlatformTenantBootstrapOutletResponse>.Success(
                new PlatformTenantBootstrapOutletResponse(
                    outletId,
                    outlet.OutletName,
                    outlet.OutletCode,
                    outlet.OutletType,
                    outlet.Status,
                    outlet.Timezone));
        }

        return ApplicationResult<PlatformTenantBootstrapOutletResponse>.Failure(
            Conflict with { Message = "Unable to generate a unique outlet code." });
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>> GetOnlineStoreAsync(
        Guid tenantId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        var access = await _accessPolicy.AuthorizeReadAsync(platformUserId, tenantId, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Failure(access.Error);
        }

        if (!await _permissionChecker.HasPermissionAsync(
                platformUserId, PlatformPermissionCodes.TenantsBootstrapOnlineStoreManage, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Failure(
                PlatformSelectedTenantAccessPolicy.AccessDenied);
        }

        var now = _dateTimeProvider.UtcNow;
        if (!await _featureEntitlementEvaluator.IsEnabledAsync(
                tenantId, PlatformTenantFeatureCodes.OnlineStore, now, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Failure(NotEntitled);
        }

        return ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Success(
            await BuildOnlineStoreResponseAsync(tenantId, entitled: true, cancellationToken));
    }

    public async Task<ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>> UpsertOnlineStoreAsync(
        Guid tenantId,
        Guid platformUserId,
        PlatformTenantBootstrapOnlineStoreUpsertRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var requestHash = HashRequest(request);
        var replay = await TryReplayOrConflictAsync<PlatformTenantBootstrapOnlineStoreResponse>(
            tenantId, OnlineStoreUpsertOperation, idempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var access = await _accessPolicy.AuthorizeMutationAsync(
            platformUserId, tenantId, PlatformPermissionCodes.TenantsBootstrapOnlineStoreManage, cancellationToken);
        if (access.IsFailure)
        {
            return ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Failure(access.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        if (!await _featureEntitlementEvaluator.IsEnabledAsync(
                tenantId, PlatformTenantFeatureCodes.OnlineStore, now, cancellationToken))
        {
            return ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Failure(NotEntitled);
        }

        var validationError = ValidateOnlineStoreUpsertRequest(request);
        if (validationError is not null)
        {
            return ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Failure(validationError);
        }

        var storeStatus = request.StoreStatus.Trim();
        var taxDisplayMode = string.IsNullOrWhiteSpace(request.TaxDisplayMode)
            ? TaxDisplayModeMatchTenant
            : request.TaxDisplayMode.Trim();

        var defaultsJson = JsonSerializer.Serialize(
            new OnlineStoreDefaultsPayload(storeStatus, taxDisplayMode),
            IdempotencyJsonOptions);

        await _bootstrapRepository.UpsertOnlineStoreDefaultsAsync(
            tenantId,
            defaultsJson,
            platformUserId,
            now,
            cancellationToken);

        var response = await BuildOnlineStoreResponseAsync(tenantId, entitled: true, cancellationToken);

        await _tenantRepository.AddAuditLogAsync(
            tenantId,
            platformUserId,
            "platform.tenant_bootstrap.online_store_configured",
            $"Bootstrap online store configured with status '{storeStatus}'.",
            null,
            now,
            "OnlineStoreSettings",
            null,
            before: null,
            after: response,
            _correlationAccessor.CorrelationId,
            cancellationToken);

        await SaveIdempotencyAsync(
            tenantId,
            OnlineStoreUpsertOperation,
            idempotencyKey,
            requestHash,
            response,
            now,
            cancellationToken);

        return ApplicationResult<PlatformTenantBootstrapOnlineStoreResponse>.Success(response);
    }

    private async Task<PlatformTenantBootstrapOnlineStoreResponse> BuildOnlineStoreResponseAsync(
        Guid tenantId,
        bool entitled,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var defaults = ParseOnlineStoreDefaults(
            await _bootstrapRepository.GetOnlineStoreDefaultsJsonAsync(tenantId, cancellationToken));
        var clickCollectEntitled = await _featureEntitlementEvaluator.IsEnabledAsync(
            tenantId, PlatformTenantFeatureCodes.ClickCollect, now, cancellationToken);
        var clickCollectConfigured = clickCollectEntitled &&
            await _bootstrapRepository.HasClickCollectCollectionConfiguredAsync(tenantId, cancellationToken);
        var dependencyNotice = clickCollectEntitled && !clickCollectConfigured
            ? ClickCollectDependencyNotice
            : null;

        return new PlatformTenantBootstrapOnlineStoreResponse(
            entitled,
            defaults.StoreStatus,
            defaults.TaxDisplayMode,
            clickCollectEntitled,
            clickCollectConfigured,
            dependencyNotice);
    }

    private static ApplicationError? ValidateOnlineStoreUpsertRequest(PlatformTenantBootstrapOnlineStoreUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StoreStatus) ||
            (!string.Equals(request.StoreStatus.Trim(), StoreStatusDraft, StringComparison.Ordinal) &&
             !string.Equals(request.StoreStatus.Trim(), StoreStatusActive, StringComparison.Ordinal)))
        {
            return ValidationFailed with
            {
                Message = "Store status must be DRAFT or ACTIVE.",
                FieldErrors =
                [
                    new ApplicationFieldError("storeStatus", "Store status must be DRAFT or ACTIVE.")
                ]
            };
        }

        if (!string.IsNullOrWhiteSpace(request.TaxDisplayMode) &&
            !string.Equals(request.TaxDisplayMode.Trim(), TaxDisplayModeMatchTenant, StringComparison.Ordinal))
        {
            return ValidationFailed with
            {
                Message = "Tax display mode must be MATCH_TENANT.",
                FieldErrors =
                [
                    new ApplicationFieldError("taxDisplayMode", "Tax display mode must be MATCH_TENANT.")
                ]
            };
        }

        return null;
    }

    private static OnlineStoreDefaultsPayload ParseOnlineStoreDefaults(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new OnlineStoreDefaultsPayload(StoreStatusDraft, TaxDisplayModeMatchTenant);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<OnlineStoreDefaultsPayload>(json, IdempotencyJsonOptions);
            if (parsed is null)
            {
                return new OnlineStoreDefaultsPayload(StoreStatusDraft, TaxDisplayModeMatchTenant);
            }

            var storeStatus = string.Equals(parsed.StoreStatus, StoreStatusActive, StringComparison.Ordinal)
                ? StoreStatusActive
                : StoreStatusDraft;
            var taxDisplayMode = string.IsNullOrWhiteSpace(parsed.TaxDisplayMode)
                ? TaxDisplayModeMatchTenant
                : parsed.TaxDisplayMode;

            return new OnlineStoreDefaultsPayload(storeStatus, taxDisplayMode);
        }
        catch (JsonException)
        {
            return new OnlineStoreDefaultsPayload(StoreStatusDraft, TaxDisplayModeMatchTenant);
        }
    }

    private sealed record OnlineStoreDefaultsPayload(string StoreStatus, string TaxDisplayMode);

    private async Task<IReadOnlyList<string>> ResolveEntitledTenantPermissionCodesAsync(
        Guid tenantId,
        IReadOnlyList<string> requestedPermissionCodes,
        CancellationToken cancellationToken)
    {
        var allowed = await BuildEntitledTenantPermissionCodeSetAsync(tenantId, cancellationToken);

        return requestedPermissionCodes
            .Where(code => allowed.Contains(code))
            .ToList();
    }

    private async Task<IReadOnlyList<string>> ListEntitledTenantPermissionCodesAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var allowed = await BuildEntitledTenantPermissionCodeSetAsync(tenantId, cancellationToken);
        return allowed.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<HashSet<string>> BuildEntitledTenantPermissionCodeSetAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var effectiveFeatures = new List<string>();
        foreach (var featureCode in BootstrapFeatureCodes)
        {
            if (await _featureEntitlementEvaluator.IsEnabledAsync(tenantId, featureCode, now, cancellationToken))
            {
                effectiveFeatures.Add(featureCode);
            }
        }

        var plan = TenantAdminBootstrapPermissionCatalog.Resolve(effectiveFeatures);
        var allowed = new HashSet<string>(plan.PermissionCodes, StringComparer.OrdinalIgnoreCase);
        allowed.UnionWith(TenantAdminBootstrapPermissionCatalog.BasePermissionCodes);
        return allowed;
    }

    private static readonly string[] BootstrapFeatureCodes =
    [
        PlatformTenantFeatureCodes.OutletManagement,
        PlatformTenantFeatureCodes.TillManagement,
        PlatformTenantFeatureCodes.UserAccounts,
        PlatformTenantFeatureCodes.RoleManagement,
        PlatformTenantFeatureCodes.PermissionManagement,
        PlatformTenantFeatureCodes.ProductCatalog,
        PlatformTenantFeatureCodes.InventoryTracking,
        PlatformTenantFeatureCodes.SalesReports,
        PlatformTenantFeatureCodes.OnlineStore,
        PlatformTenantFeatureCodes.SalesOrders,
        PlatformTenantFeatureCodes.ClickCollect,
        PlatformTenantFeatureCodes.TenantSettings,
        PlatformTenantFeatureCodes.TenantProfile,
        PlatformTenantFeatureCodes.HardwareDeviceManagement,
        PlatformTenantFeatureCodes.PosCheckout,
        PlatformTenantFeatureCodes.OfflineOperationSync
    ];

    private async Task<ApplicationResult<TResponse>?> TryReplayOrConflictAsync<TResponse>(
        Guid tenantId,
        string operationType,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var record = await _bootstrapRepository.TryGetIdempotencyRecordAsync(
            tenantId,
            operationType,
            idempotencyKey,
            cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(record.RequestHash) &&
            !string.Equals(record.RequestHash, requestHash, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<TResponse>.Failure(
                Conflict with { Message = "Idempotency key was reused with a different request." });
        }

        var replay = JsonSerializer.Deserialize<TResponse>(record.ResponseJson, IdempotencyJsonOptions);
        return replay is not null
            ? ApplicationResult<TResponse>.Success(replay)
            : null;
    }

    private Task SaveIdempotencyAsync<TResponse>(
        Guid tenantId,
        string operationType,
        string idempotencyKey,
        string requestHash,
        TResponse response,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _bootstrapRepository.SaveIdempotencyResponseAsync(
            tenantId,
            operationType,
            idempotencyKey,
            JsonSerializer.Serialize(response, IdempotencyJsonOptions),
            now,
            requestHash,
            cancellationToken);

    private static string HashRequest<T>(T request) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(request, IdempotencyJsonOptions))));

    private static ApplicationError? ValidateBootstrapProductRequest(PlatformTenantBootstrapProductCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName) ||
            request.ProductName.Trim().Length < 2 ||
            request.ProductName.Trim().Length > 200)
        {
            return ValidationFailed with { Message = "Product name must be between 2 and 200 characters." };
        }

        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return ValidationFailed with { Message = "SKU is required." };
        }

        if (request.SellingPrice < 0)
        {
            return ValidationFailed with { Message = "Selling price must be greater than or equal to 0." };
        }

        return null;
    }

    private static string CsvEscape(string? value)
    {
        var safe = (value ?? string.Empty).Replace("\"", "\"\"");
        if (safe.StartsWith('=') || safe.StartsWith('+') || safe.StartsWith('-') || safe.StartsWith('@'))
        {
            safe = $"'{safe}";
        }

        return $"\"{safe}\"";
    }
}
