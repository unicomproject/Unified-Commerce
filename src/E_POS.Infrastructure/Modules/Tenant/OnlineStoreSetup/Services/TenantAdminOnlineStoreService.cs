using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup.Contracts;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup.Dtos;
using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using E_POS.Domain.Modules.ECommerce.Storefront.Constants;
using E_POS.Domain.Modules.ECommerce.Storefront.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace E_POS.Infrastructure.Modules.Tenant.OnlineStoreSetup.Services;

public sealed class TenantAdminOnlineStoreService : ITenantAdminOnlineStoreService
{
    private const string Active = "ACTIVE";
    private const string Inactive = "INACTIVE";
    private const string Deleted = "DELETED";
    private const string Published = "PUBLISHED";
    private const string Draft = "DRAFT";
    private const string OnlineChannelCode = "ONLINE";
    private const string PickupMethodType = "PICKUP";
    private const int TotalSteps = 9;
    private const long MaxMediaBytes = 5 * 1024 * 1024;
    private const long MaxPixels = 16_000_000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly EPosDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IMediaObjectStorage _storage;
    private readonly IIdempotencyService _idempotency;
    private readonly IApplicationEmailSender _emailSender;
    private readonly IDomainVerificationProvider _domainVerificationProvider;
    private readonly ICertificateProvisioningProvider _certificateProvisioningProvider;
    private readonly IMediaReadUrlResolver? _mediaReadUrlResolver;
    private readonly OnlineStoreSetupOptions _options;

    public TenantAdminOnlineStoreService(
        EPosDbContext db,
        IDateTimeProvider clock,
        ITenantFeatureEntitlementEvaluator entitlements,
        IMediaObjectStorage storage,
        IIdempotencyService idempotency,
        IApplicationEmailSender emailSender,
        IDomainVerificationProvider domainVerificationProvider,
        ICertificateProvisioningProvider certificateProvisioningProvider,
        IOptions<OnlineStoreSetupOptions> options,
        IMediaReadUrlResolver? mediaReadUrlResolver = null)
    {
        _db = db;
        _clock = clock;
        _entitlements = entitlements;
        _storage = storage;
        _idempotency = idempotency;
        _emailSender = emailSender;
        _domainVerificationProvider = domainVerificationProvider;
        _certificateProvisioningProvider = certificateProvisioningProvider;
        _options = options.Value;
        _mediaReadUrlResolver = mediaReadUrlResolver;
    }

    public async Task<ApplicationResult<OnlineStoreOverviewResponse>> GetOverviewAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreOverviewResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, ensureChannel: true, cancellationToken);
        var readiness = await BuildReadinessAsync(state, cancellationToken);
        var domains = await GetDomainsAsync(state.Channel.Id, state.Tenant.Id, cancellationToken);
        var primaryDomain = domains.FirstOrDefault(domain => domain.IsPrimary && string.Equals(domain.DomainType, "CUSTOM", StringComparison.OrdinalIgnoreCase));
        var branding = ReadObject(state.Settings, "branding");
        var support = ReadObject(state.Settings, "support");
        var collection = await BuildCollectionOutletsAsync(state.Tenant.Id, cancellationToken);
        var totalProducts = await _db.Products.CountAsync(product => product.TenantId == state.Tenant.Id && product.Status != Deleted, cancellationToken);
        var visibleProducts = await _db.ProductChannelVisibilities.CountAsync(visibility => visibility.TenantId == state.Tenant.Id && visibility.SalesChannelId == state.Channel.Id && visibility.IsVisible && visibility.Status == Active, cancellationToken);
        var publishedPolicyTypes = await _db.StorefrontPolicies.AsNoTracking().Where(policy => policy.TenantId == state.Tenant.Id && policy.SalesChannelId == state.Channel.Id && policy.Status == Published).Select(policy => policy.PolicyType).ToListAsync(cancellationToken);
        var publishedPolicies = OnlineStoreContractRules.CountPublishedRequiredPolicies(publishedPolicyTypes);
        var supportComplete = IsSupportReady(support);
        var eligibleOutletCount = collection.Count(outlet => outlet.Eligible);
        return ApplicationResult<OnlineStoreOverviewResponse>.Success(new OnlineStoreOverviewResponse(
            state.Channel.Id,
            ReadString(state.Settings, "storeStatus") ?? Draft,
            state.Channel.Status,
            ReadBool(state.Settings, "setupEnabled") ?? false,
            state.Channel.Status == Active ? "LIVE" : "NOT_LIVE",
            ReadString(state.Settings, "storeSlug"),
            BuildHostedUrl(ReadString(state.Settings, "storeSlug")),
            readiness.Steps.Count(x => x.Status == "PASS"),
            TotalSteps,
            readiness.Steps.Count(x => x.Status == "PASS") * 100 / TotalSteps,
            readiness.Steps,
            readiness,
            new OnlineStoreDomainSummary(primaryDomain is not null, primaryDomain?.DomainName, primaryDomain?.VerificationStatus, primaryDomain?.SslStatus, primaryDomain?.IsPrimary ?? false),
            new OnlineStoreSectionSummary(branding is not null ? "CONFIGURED" : "INCOMPLETE"),
            new OnlineStoreSectionSummary(supportComplete ? "COMPLETE" : "INCOMPLETE"),
            new OnlineStoreClickCollectSummary(collection.Any(outlet => outlet.Status == Active), eligibleOutletCount, eligibleOutletCount > 0 ? "READY" : "INCOMPLETE"),
            new OnlineStoreCatalogOverview(totalProducts, visibleProducts),
            new OnlineStorePolicySummary(OnlineStoreContractRules.RequiredPolicyTypes.Count, publishedPolicies, publishedPolicies == OnlineStoreContractRules.RequiredPolicyTypes.Count ? "COMPLETE" : "INCOMPLETE"),
            OnlineStoreReleaseOnePolicy.CustomerAccountMode,
            OnlineStoreReleaseOnePolicy.EmailVerificationRequired,
            OnlineStoreReleaseOnePolicy.PaymentMode,
            _emailSender.IsConfigured ? "READY" : "NOT_READY",
            readiness.Steps.Where(step => step.Status != "PASS").Select(step => new OnlineStoreNextActionDto(step.Code.ToUpperInvariant(), step.StepNumber, true)).ToList()));
    }

    public async Task<ApplicationResult<OnlineStoreReadinessResponse>> GetReadinessAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreReadinessResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        return ApplicationResult<OnlineStoreReadinessResponse>.Success(await BuildReadinessAsync(await LoadStateAsync(context.TenantId, true, cancellationToken), cancellationToken));
    }

    public async Task<ApplicationResult<OnlineStoreActivationResponse>> GetActivationAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreActivationResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        return ApplicationResult<OnlineStoreActivationResponse>.Success(await BuildActivationAsync(await LoadStateAsync(context.TenantId, true, cancellationToken), cancellationToken));
    }

    public async Task<ApplicationResult<OnlineStoreActivationResponse>> UpdateActivationAsync(TenantRequestContext context, UpdateOnlineStoreActivationRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreActivationResponse>(context, TenantAdminOnlineStorePermissions.Manage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        state.Settings["setupEnabled"] = request.SetupEnabled;
        state.Settings["storeStatus"] ??= Draft;
        state.Channel.Update(state.Channel.CustomName, request.SetupEnabled ? Inactive : Inactive, state.Channel.SortOrder, _clock.UtcNow);
        await SaveSettingsAsync(state, context.UserId, cancellationToken);
        AddAudit(context, "online_store.setup_enabled", "ONLINE_STORE", state.Channel.Id, new { request.SetupEnabled });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreActivationResponse>.Success(await BuildActivationAsync(state, cancellationToken));
    }

    public async Task<ApplicationResult<OnlineStoreIdentityResponse>> GetIdentityAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreIdentityResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        return ApplicationResult<OnlineStoreIdentityResponse>.Success(BuildIdentity(await LoadStateAsync(context.TenantId, true, cancellationToken)));
    }

    public async Task<ApplicationResult<OnlineStoreIdentityResponse>> UpdateIdentityAsync(TenantRequestContext context, UpdateOnlineStoreIdentityRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreIdentityResponse>(context, TenantAdminOnlineStorePermissions.Manage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var errors = ValidateIdentity(request);
        if (errors.Count > 0) return Failure<OnlineStoreIdentityResponse>("online_store.identity_invalid", "Store identity is invalid.", errors);
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        state.Channel.Update(request.StoreName.Trim(), state.Channel.Status, state.Channel.SortOrder, _clock.UtcNow);
        state.Settings["businessDisplayName"] = request.BusinessDisplayName.Trim();
        state.Settings["storeDescription"] = Clean(request.StoreDescription);
        state.Settings["storeEmail"] = string.IsNullOrWhiteSpace(request.StoreEmail) ? null : request.StoreEmail.Trim();
        state.Settings["storePhone"] = OnlineStoreContractRules.NormalizePhone(request.StorePhone);
        state.Settings["supportTagline"] = Clean(request.SupportTagline);
        await SaveSettingsAsync(state, context.UserId, cancellationToken);
        AddAudit(context, "online_store.identity_updated", "ONLINE_STORE", state.Channel.Id, new { request.StoreName, request.BusinessDisplayName });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreIdentityResponse>.Success(BuildIdentity(state));
    }

    public async Task<ApplicationResult<OnlineStoreCheckoutRulesResponse>> GetCheckoutRulesAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreCheckoutRulesResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;

        var clickCollectEnabled = await _entitlements.IsEnabledAsync(
            context.TenantId,
            PlatformTenantFeatureCodes.ClickCollect,
            _clock.UtcNow,
            cancellationToken);
        var collectionOutlets = await BuildCollectionOutletsAsync(context.TenantId, cancellationToken);

        return ApplicationResult<OnlineStoreCheckoutRulesResponse>.Success(new OnlineStoreCheckoutRulesResponse(
            OnlineStoreReleaseOnePolicy.Release,
            new OnlineStoreCustomerAccountRuleDto(
                OnlineStoreReleaseOnePolicy.CustomerRegistrationRequired,
                OnlineStoreReleaseOnePolicy.CustomerAccountMode,
                OnlineStoreReleaseOnePolicy.CustomerAccountLabel),
            new OnlineStoreGuestCheckoutRuleDto(
                OnlineStoreReleaseOnePolicy.GuestCheckoutAvailable,
                OnlineStoreReleaseOnePolicy.GuestCheckoutMode,
                OnlineStoreReleaseOnePolicy.GuestCheckoutLabel),
            new OnlineStoreEmailVerificationRuleDto(
                OnlineStoreReleaseOnePolicy.EmailVerificationRequired,
                OnlineStoreReleaseOnePolicy.EmailVerificationMode,
                OnlineStoreReleaseOnePolicy.EmailVerificationLabel),
            new OnlineStoreFulfilmentRuleDto(
                OnlineStoreReleaseOnePolicy.FulfilmentMode,
                OnlineStoreReleaseOnePolicy.FulfilmentLabel,
                clickCollectEnabled,
                clickCollectEnabled && collectionOutlets.Any(outlet => outlet.Status == Active && outlet.Eligible)),
            new OnlineStorePaymentRuleDto(
                OnlineStoreReleaseOnePolicy.PaymentMode,
                OnlineStoreReleaseOnePolicy.PaymentLabel)));
    }

    public async Task<ApplicationResult<OnlineStoreUrlDomainResponse>> GetUrlDomainAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreUrlDomainResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        return ApplicationResult<OnlineStoreUrlDomainResponse>.Success(new OnlineStoreUrlDomainResponse(
            ReadString(state.Settings, "storeSlug"),
            BuildHostedUrl(ReadString(state.Settings, "storeSlug")),
            await GetDomainsAsync(state.Channel.Id, state.Tenant.Id, cancellationToken)));
    }

    public async Task<ApplicationResult<OnlineStoreUrlDomainResponse>> UpdateUrlAsync(TenantRequestContext context, UpdateOnlineStoreUrlRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreUrlDomainResponse>(context, TenantAdminOnlineStorePermissions.Manage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var slug = OnlineStoreContractRules.NormalizeSlug(request.StoreSlug);
        if (slug is null) return Failure<OnlineStoreUrlDomainResponse>("online_store.slug_invalid", "Store slug is invalid.", [new("storeSlug", "Use 3-63 lowercase letters, numbers or single hyphens; reserved names are not allowed.")]);
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        if (string.Equals(ReadString(state.Settings, "storeStatus"), Published, StringComparison.OrdinalIgnoreCase))
            return Failure<OnlineStoreUrlDomainResponse>("online_store.slug_immutable", "Store slug cannot be changed after publishing.");
        var definitionId = await _db.SettingDefinitions.Where(x => x.SettingKey == TenantSettingKeys.OnlineStoreDefaults).Select(x => x.Id).SingleAsync(cancellationToken);
        var slugInUse = await _db.TenantSettings.AsNoTracking()
            .Where(x => x.SettingDefinitionId == definitionId && x.TenantId != context.TenantId)
            .AnyAsync(x => EF.Functions.ILike(x.SettingValue, $"%\"storeSlug\":\"{slug}\"%"), cancellationToken);
        if (slugInUse)
            return Failure<OnlineStoreUrlDomainResponse>("online_store.slug_conflict", "Store slug is already in use.", [new("storeSlug", "Choose another store slug.")]);
        state.Settings["storeSlug"] = slug;
        await SaveSettingsAsync(state, context.UserId, cancellationToken);
        AddAudit(context, "online_store.url_updated", "ONLINE_STORE", state.Channel.Id, new { storeSlug = slug });
        await _db.SaveChangesAsync(cancellationToken);
        return await GetUrlDomainAsync(context, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<OnlineStoreDomainDto>>> ListDomainsAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<IReadOnlyList<OnlineStoreDomainDto>>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        return ApplicationResult<IReadOnlyList<OnlineStoreDomainDto>>.Success(await GetDomainsAsync(state.Channel.Id, context.TenantId, cancellationToken));
    }

    public async Task<ApplicationResult<OnlineStoreDomainTokenResponse>> CreateDomainAsync(TenantRequestContext context, CreateOnlineStoreDomainRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainTokenResponse>(context, TenantAdminOnlineStorePermissions.DomainsManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var domainName = OnlineStoreContractRules.NormalizeDomain(request.DomainName);
        if (domainName is null) return Failure<OnlineStoreDomainTokenResponse>("online_store.domain_invalid", "Domain name is invalid.", [new("domainName", "Enter a valid domain name.")]);
        if (NormalizeDomainType(request.DomainType) != "CUSTOM")
            return Failure<OnlineStoreDomainTokenResponse>("online_store.domain_type_invalid", "Only custom domains can be created here.");
        if (request.IsPrimary)
            return Failure<OnlineStoreDomainTokenResponse>("online_store.domain_not_ready", "A new domain must be verified and have active SSL before it can become primary.");
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        if (await _db.TenantDomains.AnyAsync(x => x.DomainName == domainName && x.Status != Deleted, cancellationToken))
            return Failure<OnlineStoreDomainTokenResponse>("online_store.domain_conflict", "Domain is already registered.", [new("domainName", "Domain is already registered.")]);
        var now = _clock.UtcNow;
        var token = CreateRawToken();
        var domain = TenantDomain.Create(Guid.NewGuid(), context.TenantId, state.Channel.Id, "CUSTOM", domainName, false, "PENDING", HashToken(token), null, "NOT_REQUESTED", null, null, Active, null, now);
        _db.TenantDomains.Add(domain);
        AddAudit(context, "online_store.domain_created", "TENANT_DOMAIN", domain.Id, new { domainName, domainType = domain.DomainType, domain.IsPrimary });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreDomainTokenResponse>.Success(new OnlineStoreDomainTokenResponse(domain.Id, domain.DomainName, token));
    }

    public async Task<ApplicationResult<OnlineStoreDomainDto>> VerifyDomainAsync(TenantRequestContext context, Guid domainId, VerifyOnlineStoreDomainRequest _, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainDto>(context, TenantAdminOnlineStorePermissions.DomainsManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        if (domain is null) return NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.");
        if (string.IsNullOrWhiteSpace(domain.VerificationTokenHash))
            return Failure<OnlineStoreDomainDto>("online_store.domain_verification_invalid", "Domain verification token is not configured.");
        var verification = await _domainVerificationProvider.VerifyTxtRecordAsync(
            domain.DomainName,
            domain.VerificationTokenHash,
            cancellationToken);
        var now = _clock.UtcNow;
        if (verification.Status == DomainVerificationProviderStatus.Verified)
        {
            domain.MarkVerified(null, now);
            AddAudit(context, "online_store.domain_verified", "TENANT_DOMAIN", domain.Id, new { domain.DomainName, providerStatus = verification.Status.ToString() });
        }
        else
        {
            domain.MarkVerificationFailed(
                verification.Status == DomainVerificationProviderStatus.Timeout ? "TIMEOUT" : "FAILED",
                null,
                now);
            AddAudit(context, "online_store.domain_verification_failed", "TENANT_DOMAIN", domain.Id, new { domain.DomainName, providerStatus = verification.Status.ToString(), verification.FailureCode });
        }
        await _db.SaveChangesAsync(cancellationToken);
        return verification.Status switch
        {
            DomainVerificationProviderStatus.Verified => ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain)),
            DomainVerificationProviderStatus.Unavailable => Failure<OnlineStoreDomainDto>("online_store.domain_verification_provider_unavailable", "Domain verification provider is unavailable."),
            DomainVerificationProviderStatus.Timeout => Failure<OnlineStoreDomainDto>("online_store.domain_verification_timeout", "Domain verification timed out."),
            _ => Failure<OnlineStoreDomainDto>("online_store.domain_verification_failed", "The expected DNS TXT verification record was not found.")
        };
    }

    public async Task<ApplicationResult<OnlineStoreDomainTokenResponse>> RotateDomainTokenAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainTokenResponse>(context, TenantAdminOnlineStorePermissions.DomainsManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        if (domain is null) return NotFound<OnlineStoreDomainTokenResponse>("online_store.domain_not_found", "Domain was not found.");
        var token = CreateRawToken();
        domain.MarkVerificationStarted(HashToken(token), null, _clock.UtcNow);
        AddAudit(context, "online_store.domain_verification_started", "TENANT_DOMAIN", domain.Id, new { domain.DomainName });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreDomainTokenResponse>.Success(new OnlineStoreDomainTokenResponse(domain.Id, domain.DomainName, token));
    }

    public async Task<ApplicationResult<OnlineStoreDomainDto>> GetDomainStatusAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainDto>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        if (domain is null) return NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.");
        await ReconcileDomainProviderStateAsync(context, domain, cancellationToken);
        return ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain));
    }

    public async Task<ApplicationResult<OnlineStoreDomainDto>> ProvisionDomainSslAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainDto>(context, TenantAdminOnlineStorePermissions.DomainsManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        if (domain is null) return NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.");
        if (domain.VerificationStatus != "VERIFIED")
            return Failure<OnlineStoreDomainDto>("online_store.domain_not_verified", "Domain must be verified before SSL provisioning.");
        if (domain.Status != Active)
            return Failure<OnlineStoreDomainDto>("online_store.domain_inactive", "Domain must be active before SSL provisioning.");
        if (domain.SslStatus == "ACTIVE")
            return ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain));
        if (domain.SslStatus == "PENDING")
        {
            if (await ReconcileCertificateStateAsync(context, domain, cancellationToken))
            {
                AddAudit(context, "online_store.domain_ssl_reconciled", "TENANT_DOMAIN", domain.Id, new { domain.DomainName, domain.SslStatus });
                await _db.SaveChangesAsync(cancellationToken);
            }
            return ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain));
        }
        var provision = await _certificateProvisioningProvider.RequestAsync(
            context.TenantId,
            domain.Id,
            domain.DomainName,
            cancellationToken);
        ApplyCertificateProviderResult(domain, provision, _clock.UtcNow);
        AddAudit(context, "online_store.domain_ssl_requested", "TENANT_DOMAIN", domain.Id, new { domain.DomainName, providerStatus = provision.Status.ToString(), provision.FailureCode });
        await _db.SaveChangesAsync(cancellationToken);
        return provision.Status switch
        {
            CertificateProvisioningProviderStatus.Unavailable => Failure<OnlineStoreDomainDto>("online_store.certificate_provider_unavailable", "Certificate provisioning provider is unavailable."),
            CertificateProvisioningProviderStatus.Timeout => Failure<OnlineStoreDomainDto>("online_store.certificate_provisioning_timeout", "Certificate provisioning timed out."),
            CertificateProvisioningProviderStatus.Failed => Failure<OnlineStoreDomainDto>("online_store.certificate_provisioning_failed", "Certificate provisioning failed."),
            _ => ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain))
        };
    }

    public async Task<ApplicationResult<OnlineStoreDomainDto>> SetPrimaryDomainAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainDto>(context, TenantAdminOnlineStorePermissions.DomainsManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        if (domain is null) return NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.");
        if (domain.SalesChannelId != state.Channel.Id)
            return NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.");
        await ReconcileDomainProviderStateAsync(context, domain, cancellationToken);
        if (domain.VerificationStatus != "VERIFIED" || domain.SslStatus != "ACTIVE")
            return Failure<OnlineStoreDomainDto>("online_store.domain_not_ready", "Domain must be verified with active SSL before it can become primary.");
        await ClearPrimaryDomainsAsync(context.TenantId, state.Channel.Id, _clock.UtcNow, cancellationToken);
        domain.SetPrimary(true, null, _clock.UtcNow);
        AddAudit(context, "online_store.domain_primary_changed", "TENANT_DOMAIN", domain.Id, new { domain.DomainName });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain));
    }

    public async Task<ApplicationResult> DeleteDomainAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<object>(context, TenantAdminOnlineStorePermissions.DomainsManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return ApplicationResult.Failure(access.Error);
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        if (domain is null) return ApplicationResult.Failure(new ApplicationError("online_store.domain_not_found", "Domain was not found."));
        domain.SoftDelete(null, _clock.UtcNow);
        AddAudit(context, "online_store.domain_removed", "TENANT_DOMAIN", domain.Id, new { domain.DomainName });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<OnlineStoreBrandingResponse>> GetBrandingAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreBrandingResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        return ApplicationResult<OnlineStoreBrandingResponse>.Success(await BuildBrandingAsync(await LoadStateAsync(context.TenantId, true, cancellationToken), cancellationToken));
    }

    public async Task<ApplicationResult<OnlineStoreBrandingResponse>> UpdateBrandingAsync(TenantRequestContext context, UpdateOnlineStoreBrandingRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreBrandingResponse>(context, TenantAdminOnlineStorePermissions.BrandingManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        if (!IsHexColor(request.PrimaryColor) || !IsHexColor(request.SecondaryColor))
            return Failure<OnlineStoreBrandingResponse>("online_store.branding_invalid", "Branding colours are invalid.");
        if (!await MediaBelongsToTenantAsync(context.TenantId, request.LogoMediaAssetId, "ONLINE_STORE_LOGO", cancellationToken) ||
            !await MediaBelongsToTenantAsync(context.TenantId, request.FaviconMediaAssetId, "ONLINE_STORE_FAVICON", cancellationToken))
            return Failure<OnlineStoreBrandingResponse>("online_store.media_not_found", "A branding media asset was not found for this tenant.");
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var branding = EnsureObject(state.Settings, "branding");
        branding["logoMediaAssetId"] = request.LogoMediaAssetId?.ToString();
        branding["faviconMediaAssetId"] = request.FaviconMediaAssetId?.ToString();
        branding["primaryColor"] = request.PrimaryColor.Trim().ToUpperInvariant();
        branding["secondaryColor"] = request.SecondaryColor.Trim().ToUpperInvariant();
        await SaveSettingsAsync(state, context.UserId, cancellationToken);
        AddAudit(context, "online_store.branding_updated", "ONLINE_STORE", state.Channel.Id, new { request.LogoMediaAssetId, request.FaviconMediaAssetId, request.PrimaryColor, request.SecondaryColor });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreBrandingResponse>.Success(await BuildBrandingAsync(state, cancellationToken));
    }

    public async Task<ApplicationResult<OnlineStoreMediaResponse>> UploadMediaAsync(TenantRequestContext context, string purpose, MediaUploadFile file, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreMediaResponse>(context, TenantAdminOnlineStorePermissions.BrandingManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var normalizedPurpose = purpose.Trim().ToUpperInvariant();
        if (normalizedPurpose is not "ONLINE_STORE_LOGO" and not "ONLINE_STORE_FAVICON" and not "STOREFRONT_BANNER")
            return Failure<OnlineStoreMediaResponse>("online_store.media_invalid", "Unsupported media purpose.", [new("purpose", "Purpose must be ONLINE_STORE_LOGO, ONLINE_STORE_FAVICON or STOREFRONT_BANNER.")]);
        if (!_storage.IsConfigured) return Failure<OnlineStoreMediaResponse>("online_store.media_storage_unavailable", "Image storage is not configured.");
        var prepared = await PrepareImageAsync(file, cancellationToken);
        if (prepared.Error is not null) return ApplicationResult<OnlineStoreMediaResponse>.Failure(prepared.Error);
        await using var content = new MemoryStream(prepared.Bytes);
        var id = Guid.NewGuid();
        var key = $"tenants/{context.TenantId:D}/online-store/{normalizedPurpose.ToLowerInvariant()}/{id:N}{prepared.Extension}";
        var upload = await _storage.UploadAsync(new MediaObjectUploadRequest(key, content, prepared.Mime, new Dictionary<string, string> { ["tenant-id"] = context.TenantId.ToString("D"), ["purpose"] = normalizedPurpose }), cancellationToken);
        var now = _clock.UtcNow;
        var asset = MediaAsset.Create(id, context.TenantId, upload.ContainerName, upload.StorageKey, upload.PublicUrl, prepared.FileName, prepared.Mime, prepared.Extension, prepared.Bytes.Length, prepared.Width, prepared.Height, prepared.Hash, "IMAGE", normalizedPurpose, Active, context.UserId, now);
        _db.MediaAssets.Add(asset);
        AddAudit(context, "online_store.media_uploaded", "MEDIA_ASSET", id, new { purpose = normalizedPurpose });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreMediaResponse>.Success(new OnlineStoreMediaResponse(id, normalizedPurpose, upload.PublicUrl, prepared.FileName, prepared.Mime, prepared.Bytes.Length, prepared.Width, prepared.Height));
    }

    public async Task<ApplicationResult> DeleteMediaAsync(TenantRequestContext context, Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<object>(context, TenantAdminOnlineStorePermissions.BrandingManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return ApplicationResult.Failure(access.Error);
        var asset = await _db.MediaAssets.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.Id == mediaAssetId, cancellationToken);
        if (asset is null) return ApplicationResult.Failure(new ApplicationError("online_store.media_not_found", "Media asset was not found."));
        asset.MarkDeletePending(context.UserId, _clock.UtcNow);
        AddAudit(context, "online_store.media_removed", "MEDIA_ASSET", mediaAssetId, new { asset.AssetPurpose });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<IReadOnlyList<OnlineStoreBannerDto>>> ListBannersAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<IReadOnlyList<OnlineStoreBannerDto>>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        return ApplicationResult<IReadOnlyList<OnlineStoreBannerDto>>.Success(await GetBannersAsync(context.TenantId, state.Channel.Id, cancellationToken));
    }

    public async Task<ApplicationResult<OnlineStoreBannerDto>> GetBannerAsync(TenantRequestContext context, Guid bannerId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreBannerDto>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var banner = await GetBannersAsync(context.TenantId, state.Channel.Id, cancellationToken);
        var dto = banner.FirstOrDefault(x => x.Id == bannerId);
        return dto is null ? NotFound<OnlineStoreBannerDto>("online_store.banner_not_found", "Banner was not found.") : ApplicationResult<OnlineStoreBannerDto>.Success(dto);
    }

    public async Task<ApplicationResult<OnlineStoreBannerDto>> CreateBannerAsync(TenantRequestContext context, UpsertOnlineStoreBannerRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreBannerDto>(context, TenantAdminOnlineStorePermissions.BrandingManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var validation = ValidateBanner(request);
        if (validation.Count > 0) return Failure<OnlineStoreBannerDto>("online_store.banner_invalid", "Banner is invalid.", validation);
        if (!await MediaBelongsToTenantAsync(context.TenantId, request.ImageMediaAssetId, "STOREFRONT_BANNER", cancellationToken))
            return Failure<OnlineStoreBannerDto>("online_store.media_not_found", "Banner media asset was not found for this tenant.");
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var banner = StorefrontBanner.Create(context.TenantId, state.Channel.Id, request.BannerType.Trim().ToUpperInvariant(), request.Title.Trim(), Clean(request.Subtitle), request.ImageMediaAssetId, Clean(request.ActionText), Clean(request.ActionUrl), request.SortOrder, NormalizeRecordStatus(request.Status));
        _db.StorefrontBanners.Add(banner);
        AddAudit(context, "online_store.banner_created", "STOREFRONT_BANNER", banner.Id, new { banner.BannerType, banner.Title });
        await _db.SaveChangesAsync(cancellationToken);
        return await GetBannerAsync(context, banner.Id, cancellationToken);
    }

    public async Task<ApplicationResult<OnlineStoreBannerDto>> UpdateBannerAsync(TenantRequestContext context, Guid bannerId, UpsertOnlineStoreBannerRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreBannerDto>(context, TenantAdminOnlineStorePermissions.BrandingManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var validation = ValidateBanner(request);
        if (validation.Count > 0) return Failure<OnlineStoreBannerDto>("online_store.banner_invalid", "Banner is invalid.", validation);
        if (!await MediaBelongsToTenantAsync(context.TenantId, request.ImageMediaAssetId, "STOREFRONT_BANNER", cancellationToken))
            return Failure<OnlineStoreBannerDto>("online_store.media_not_found", "Banner media asset was not found for this tenant.");
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var banner = await _db.StorefrontBanners.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.Id == bannerId, cancellationToken);
        if (banner is null) return NotFound<OnlineStoreBannerDto>("online_store.banner_not_found", "Banner was not found.");
        banner.Update(request.BannerType, request.Title, request.Subtitle, request.ImageMediaAssetId, request.ActionText, request.ActionUrl, request.SortOrder, NormalizeRecordStatus(request.Status), _clock.UtcNow);
        AddAudit(context, "online_store.banner_updated", "STOREFRONT_BANNER", banner.Id, new { banner.BannerType, banner.Title });
        await _db.SaveChangesAsync(cancellationToken);
        return await GetBannerAsync(context, banner.Id, cancellationToken);
    }

    public async Task<ApplicationResult<OnlineStoreBannerDto>> UpdateBannerStatusAsync(TenantRequestContext context, Guid bannerId, UpdateOnlineStoreBannerStatusRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreBannerDto>(context, TenantAdminOnlineStorePermissions.BrandingManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var banner = await _db.StorefrontBanners.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.Id == bannerId, cancellationToken);
        if (banner is null) return NotFound<OnlineStoreBannerDto>("online_store.banner_not_found", "Banner was not found.");
        banner.SetStatus(NormalizeRecordStatus(request.Status), _clock.UtcNow);
        AddAudit(context, "online_store.banner_status_changed", "STOREFRONT_BANNER", banner.Id, new { banner.Status });
        await _db.SaveChangesAsync(cancellationToken);
        return await GetBannerAsync(context, banner.Id, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<OnlineStoreBannerDto>>> ReorderBannersAsync(TenantRequestContext context, ReorderOnlineStoreBannersRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<IReadOnlyList<OnlineStoreBannerDto>>(context, TenantAdminOnlineStorePermissions.BrandingManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        if (request.Items.Count == 0 || request.Items.Select(x => x.BannerId).Distinct().Count() != request.Items.Count || request.Items.Select(x => x.SortOrder).Distinct().Count() != request.Items.Count || request.Items.Any(x => x.SortOrder < 0))
            return Failure<IReadOnlyList<OnlineStoreBannerDto>>("online_store.banner_order_invalid", "Banner order must contain unique banner IDs and sort positions.");
        var ids = request.Items.Select(x => x.BannerId).ToArray();
        var banners = await _db.StorefrontBanners.Where(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.Status != Deleted && ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (banners.Count != ids.Length)
            return Failure<IReadOnlyList<OnlineStoreBannerDto>>("online_store.banner_order_invalid", "One or more banners were not found for this tenant.");
        foreach (var item in request.Items)
        {
            banners.FirstOrDefault(x => x.Id == item.BannerId)?.Reorder(item.SortOrder, _clock.UtcNow);
        }
        AddAudit(context, "online_store.banner_reordered", "ONLINE_STORE", state.Channel.Id, new { count = request.Items.Count });
        await _db.SaveChangesAsync(cancellationToken);
        return await ListBannersAsync(context, cancellationToken);
    }

    public async Task<ApplicationResult> DeleteBannerAsync(TenantRequestContext context, Guid bannerId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<object>(context, TenantAdminOnlineStorePermissions.BrandingManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return ApplicationResult.Failure(access.Error!);
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var banner = await _db.StorefrontBanners.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.Id == bannerId, cancellationToken);
        if (banner is null) return ApplicationResult.Failure(new ApplicationError("online_store.banner_not_found", "Banner was not found."));
        banner.SetStatus(Deleted, _clock.UtcNow);
        AddAudit(context, "online_store.banner_removed", "STOREFRONT_BANNER", banner.Id, new { banner.Title });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<OnlineStoreSupportResponse>> GetSupportAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreSupportResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        return ApplicationResult<OnlineStoreSupportResponse>.Success(BuildSupport((await LoadStateAsync(context.TenantId, true, cancellationToken)).Settings));
    }

    public async Task<ApplicationResult<OnlineStoreSupportResponse>> UpdateSupportAsync(TenantRequestContext context, UpdateOnlineStoreSupportRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreSupportResponse>(context, TenantAdminOnlineStorePermissions.SupportManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var supportErrors = ValidateSupport(request);
        if (supportErrors.Count > 0) return Failure<OnlineStoreSupportResponse>("online_store.support_invalid", "Support details are invalid.", supportErrors);
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var support = EnsureObject(state.Settings, "support");
        support["email"] = Clean(request.Email);
        support["phone"] = OnlineStoreContractRules.NormalizePhone(request.Phone);
        support["whatsapp"] = Clean(request.Whatsapp);
        support["helpUrl"] = Clean(request.HelpUrl);
        support["contactUsEnabled"] = request.ContactUsEnabled;
        support["supportHours"] = Clean(request.SupportHours);
        support["businessAddress"] = Clean(request.BusinessAddress);
        await SaveSettingsAsync(state, context.UserId, cancellationToken);
        AddAudit(context, "online_store.support_updated", "ONLINE_STORE", state.Channel.Id, request);
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreSupportResponse>.Success(BuildSupport(state.Settings));
    }

    public async Task<ApplicationResult<OnlineStoreClickCollectResponse>> GetClickCollectAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreClickCollectResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.ClickCollect, cancellationToken, TenantAdminOnlineStorePermissions.FulfillmentManage);
        if (access is not null) return access;
        var outlets = await BuildCollectionOutletsAsync(context.TenantId, cancellationToken);
        return ApplicationResult<OnlineStoreClickCollectResponse>.Success(new OnlineStoreClickCollectResponse(outlets.Any(x => x.Status == Active), outlets.Count, outlets));
    }

    public async Task<ApplicationResult<OnlineStoreClickCollectResponse>> UpdateClickCollectAsync(TenantRequestContext context, UpdateOnlineStoreClickCollectRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreClickCollectResponse>(context, TenantAdminOnlineStorePermissions.FulfillmentManage, PlatformTenantFeatureCodes.ClickCollect, cancellationToken);
        if (access is not null) return access;
        var method = await EnsurePickupMethodAsync(context.TenantId, cancellationToken);
        var mappings = await _db.FulfillmentMethodOutlets.Where(x => x.TenantId == context.TenantId && x.FulfillmentMethodId == method.Id).ToListAsync(cancellationToken);
        foreach (var mapping in mappings)
        {
            mapping.SetStatus(request.Enabled ? Active : Inactive, _clock.UtcNow);
        }
        AddAudit(context, "online_store.click_collect_updated", "FULFILLMENT_METHOD", method.Id, new { request.Enabled });
        await _db.SaveChangesAsync(cancellationToken);
        return await GetClickCollectAsync(context, cancellationToken);
    }

    public async Task<ApplicationResult<IReadOnlyList<OnlineStoreCollectionOutletDto>>> ListCollectionOutletsAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<IReadOnlyList<OnlineStoreCollectionOutletDto>>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.ClickCollect, cancellationToken, TenantAdminOnlineStorePermissions.FulfillmentManage);
        if (access is not null) return access;
        return ApplicationResult<IReadOnlyList<OnlineStoreCollectionOutletDto>>.Success(await BuildCollectionOutletsAsync(context.TenantId, cancellationToken));
    }

    public async Task<ApplicationResult<OnlineStoreCollectionOutletDto>> UpsertCollectionOutletAsync(TenantRequestContext context, Guid outletId, UpsertCollectionOutletRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreCollectionOutletDto>(context, TenantAdminOnlineStorePermissions.FulfillmentManage, PlatformTenantFeatureCodes.ClickCollect, cancellationToken);
        if (access is not null) return access;
        var validation = ValidateCollectionRules(request.PreparationLeadMinutes, request.PickupWindowMinutes, request.CutoffTime);
        if (validation.Count > 0) return Failure<OnlineStoreCollectionOutletDto>("online_store.collection_rules_invalid", "Collection rules are invalid.", validation);
        var method = await EnsurePickupMethodAsync(context.TenantId, cancellationToken);
        var outletExists = await _db.Outlets.AnyAsync(x => x.TenantId == context.TenantId && x.Id == outletId && x.Status == Active, cancellationToken);
        if (!outletExists) return NotFound<OnlineStoreCollectionOutletDto>("online_store.outlet_not_found", "Outlet was not found.");
        var mapping = await _db.FulfillmentMethodOutlets.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.FulfillmentMethodId == method.Id && x.OutletId == outletId, cancellationToken);
        if (mapping is null)
        {
            mapping = FulfillmentMethodOutlet.Create(Guid.NewGuid(), context.TenantId, method.Id, outletId, request.PreparationLeadMinutes, request.PickupWindowMinutes, ParseTime(request.CutoffTime), NormalizeRecordStatus(request.Status), _clock.UtcNow);
            _db.FulfillmentMethodOutlets.Add(mapping);
            AddAudit(context, "online_store.collection_outlet_added", "FULFILLMENT_METHOD_OUTLET", mapping.Id, new { outletId });
        }
        else
        {
            mapping.ConfigureCollection(request.PreparationLeadMinutes, request.PickupWindowMinutes, ParseTime(request.CutoffTime), NormalizeRecordStatus(request.Status), _clock.UtcNow);
            AddAudit(context, "online_store.collection_outlet_updated", "FULFILLMENT_METHOD_OUTLET", mapping.Id, new { outletId });
        }
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreCollectionOutletDto>.Success((await BuildCollectionOutletsAsync(context.TenantId, cancellationToken)).Single(x => x.OutletId == outletId));
    }

    public async Task<ApplicationResult> DeleteCollectionOutletAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<object>(context, TenantAdminOnlineStorePermissions.FulfillmentManage, PlatformTenantFeatureCodes.ClickCollect, cancellationToken);
        if (access is not null) return ApplicationResult.Failure(access.Error!);
        var method = await EnsurePickupMethodAsync(context.TenantId, cancellationToken);
        var mapping = await _db.FulfillmentMethodOutlets.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.FulfillmentMethodId == method.Id && x.OutletId == outletId, cancellationToken);
        if (mapping is null) return ApplicationResult.Success();
        mapping.SetStatus(Deleted, _clock.UtcNow);
        AddAudit(context, "online_store.collection_outlet_removed", "FULFILLMENT_METHOD_OUTLET", mapping.Id, new { outletId });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<IReadOnlyList<OnlineStoreCollectionOutletDto>>> BulkApplyCollectionOutletsAsync(TenantRequestContext context, BulkApplyCollectionOutletRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<IReadOnlyList<OnlineStoreCollectionOutletDto>>(context, TenantAdminOnlineStorePermissions.FulfillmentManage, PlatformTenantFeatureCodes.ClickCollect, cancellationToken);
        if (access is not null) return access;
        var outletIds = request.OutletIds.Distinct().ToArray();
        if (outletIds.Length == 0)
            return Failure<IReadOnlyList<OnlineStoreCollectionOutletDto>>("online_store.collection_outlets_required", "Select at least one outlet.");
        var validation = ValidateCollectionRules(request.PreparationLeadMinutes, request.PickupWindowMinutes, request.CutoffTime);
        if (validation.Count > 0) return Failure<IReadOnlyList<OnlineStoreCollectionOutletDto>>("online_store.collection_rules_invalid", "Collection rules are invalid.", validation);
        var validOutletCount = await _db.Outlets.CountAsync(x => x.TenantId == context.TenantId && outletIds.Contains(x.Id) && x.Status == Active, cancellationToken);
        if (validOutletCount != outletIds.Length)
            return Failure<IReadOnlyList<OnlineStoreCollectionOutletDto>>("online_store.collection_outlet_invalid", "One or more outlets are unavailable for collection.");
        var method = await EnsurePickupMethodAsync(context.TenantId, cancellationToken);
        var mappings = await _db.FulfillmentMethodOutlets.Where(x => x.TenantId == context.TenantId && x.FulfillmentMethodId == method.Id && outletIds.Contains(x.OutletId)).ToListAsync(cancellationToken);
        foreach (var outletId in outletIds)
        {
            var mapping = mappings.FirstOrDefault(x => x.OutletId == outletId);
            if (mapping is null)
            {
                mapping = FulfillmentMethodOutlet.Create(Guid.NewGuid(), context.TenantId, method.Id, outletId, request.PreparationLeadMinutes, request.PickupWindowMinutes, ParseTime(request.CutoffTime), NormalizeRecordStatus(request.Status), _clock.UtcNow);
                _db.FulfillmentMethodOutlets.Add(mapping);
            }
            else
            {
                mapping.ConfigureCollection(request.PreparationLeadMinutes, request.PickupWindowMinutes, ParseTime(request.CutoffTime), NormalizeRecordStatus(request.Status), _clock.UtcNow);
            }
        }
        AddAudit(context, "online_store.collection_outlets_bulk_updated", "FULFILLMENT_METHOD", method.Id, new { outletIds, request.Status });
        await _db.SaveChangesAsync(cancellationToken);
        return await ListCollectionOutletsAsync(context, cancellationToken);
    }

    public async Task<ApplicationResult<OnlineStoreCatalogSummaryResponse>> GetCatalogSummaryAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreCatalogSummaryResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var total = await _db.Products.CountAsync(x => x.TenantId == context.TenantId && x.Status != Deleted, cancellationToken);
        var visible = await _db.ProductChannelVisibilities.CountAsync(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.IsVisible && x.Status == Active, cancellationToken);
        var orderable = await _db.ProductChannelVisibilities.CountAsync(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.IsOrderable && x.Status == Active, cancellationToken);
        return ApplicationResult<OnlineStoreCatalogSummaryResponse>.Success(new OnlineStoreCatalogSummaryResponse(total, visible, Math.Max(0, total - visible), orderable, 0, 0));
    }

    public async Task<ApplicationResult<OnlineStoreCatalogProductListResponse>> ListCatalogProductsAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreCatalogProductListResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Products.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.Status != Deleted);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => EF.Functions.ILike(x.ProductName, $"%{search.Trim()}%"));
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.ProductName).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .GroupJoin(_db.ProductChannelVisibilities.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.ProductVariantId == null), p => p.Id, v => v.ProductId, (p, vis) => new { p, vis = vis.FirstOrDefault() })
            .Select(x => new OnlineStoreCatalogProductDto(x.p.Id, null, x.p.ProductName, null, x.vis == null ? false : x.vis.IsVisible, x.vis == null ? false : x.vis.IsOrderable, x.vis == null ? null : x.vis.AvailableFrom, x.vis == null ? null : x.vis.AvailableUntil, x.vis == null ? Inactive : x.vis.Status))
            .ToListAsync(cancellationToken);
        return ApplicationResult<OnlineStoreCatalogProductListResponse>.Success(new OnlineStoreCatalogProductListResponse(pageNumber, pageSize, total, rows));
    }

    public async Task<ApplicationResult<OnlineStoreCatalogProductDto>> UpdateProductVisibilityAsync(TenantRequestContext context, Guid productId, UpdateProductChannelVisibilityRequest request, CancellationToken cancellationToken) =>
        await UpsertVisibilityAsync(context, productId, null, request, cancellationToken);

    public async Task<ApplicationResult<OnlineStoreCatalogProductDto>> UpdateVariantVisibilityAsync(TenantRequestContext context, Guid productId, Guid variantId, UpdateProductChannelVisibilityRequest request, CancellationToken cancellationToken) =>
        await UpsertVisibilityAsync(context, productId, variantId, request, cancellationToken);

    public async Task<ApplicationResult<IReadOnlyList<OnlineStoreCatalogProductDto>>> BulkUpdateProductVisibilityAsync(TenantRequestContext context, BulkProductChannelVisibilityRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<IReadOnlyList<OnlineStoreCatalogProductDto>>(context, TenantAdminOnlineStorePermissions.CatalogManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var result = new List<OnlineStoreCatalogProductDto>();
        foreach (var id in request.ProductIds.Distinct())
        {
            var item = await UpsertVisibilityAsync(context, id, null, new UpdateProductChannelVisibilityRequest(request.IsVisible, request.IsOrderable, request.AvailableFrom, request.AvailableUntil, request.Status), cancellationToken);
            if (item.IsSuccess && item.Value is not null) result.Add(item.Value);
        }
        return ApplicationResult<IReadOnlyList<OnlineStoreCatalogProductDto>>.Success(result);
    }

    public async Task<ApplicationResult<IReadOnlyList<OnlineStorePolicyDto>>> ListPoliciesAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<IReadOnlyList<OnlineStorePolicyDto>>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var policies = await _db.StorefrontPolicies.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.Status != "ARCHIVED").OrderBy(x => x.PolicyType).Select(x => ToPolicyDto(x)).ToListAsync(cancellationToken);
        return ApplicationResult<IReadOnlyList<OnlineStorePolicyDto>>.Success(policies);
    }

    public async Task<ApplicationResult<OnlineStorePolicyDto>> GetPolicyAsync(TenantRequestContext context, string policyType, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStorePolicyDto>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var policy = await FindCurrentPolicyAsync(context, policyType, cancellationToken);
        return policy is null ? NotFound<OnlineStorePolicyDto>("online_store.policy_not_found", "Policy was not found.") : ApplicationResult<OnlineStorePolicyDto>.Success(ToPolicyDto(policy));
    }

    public async Task<ApplicationResult<OnlineStorePolicyDto>> UpsertPolicyAsync(TenantRequestContext context, string policyType, UpsertOnlineStorePolicyRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStorePolicyDto>(context, TenantAdminOnlineStorePermissions.PoliciesManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var normalizedType = NormalizePolicyType(policyType);
        if (normalizedType is null) return Failure<OnlineStorePolicyDto>("online_store.policy_type_invalid", "Policy type is invalid.");
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.Version))
            return Failure<OnlineStorePolicyDto>("online_store.policy_invalid", "Policy title, content and version are required.");
        if (OnlineStoreContractRules.ContainsUnsafeMarkup(request.Content))
            return Failure<OnlineStorePolicyDto>("online_store.policy_content_unsafe", "Policy content contains unsafe markup.", [new("content", "Scripts, iframes, event handlers and executable URLs are not allowed.")]);
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var policy = await _db.StorefrontPolicies.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.PolicyType == normalizedType && x.Version == request.Version.Trim(), cancellationToken);
        if (policy is null)
        {
            policy = StorefrontPolicy.Create(Guid.NewGuid(), context.TenantId, state.Channel.Id, normalizedType, request.Title, request.Content, request.Version, Draft, context.UserId, _clock.UtcNow);
            _db.StorefrontPolicies.Add(policy);
        }
        else
        {
            if (policy.Status != Draft)
                return Failure<OnlineStorePolicyDto>("online_store.policy_version_immutable", "Published or archived policy versions cannot be edited. Create a new version.");
            policy.UpdateDraft(request.Title, request.Content, request.Version, context.UserId, _clock.UtcNow);
        }
        AddAudit(context, "online_store.policy_saved", "STOREFRONT_POLICY", policy.Id, new { normalizedType, request.Version });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStorePolicyDto>.Success(ToPolicyDto(policy));
    }

    public async Task<ApplicationResult<OnlineStorePolicyDto>> PublishPolicyAsync(TenantRequestContext context, string policyType, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStorePolicyDto>(context, TenantAdminOnlineStorePermissions.PoliciesManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var policy = await FindCurrentPolicyAsync(context, policyType, cancellationToken);
        if (policy is null) return NotFound<OnlineStorePolicyDto>("online_store.policy_not_found", "Policy was not found.");
        var existing = await _db.StorefrontPolicies.Where(x => x.TenantId == context.TenantId && x.SalesChannelId == policy.SalesChannelId && x.PolicyType == policy.PolicyType && x.Status == Published && x.Id != policy.Id).ToListAsync(cancellationToken);
        foreach (var row in existing) row.Archive(context.UserId, _clock.UtcNow);
        policy.Publish(context.UserId, _clock.UtcNow);
        AddAudit(context, "online_store.policy_published", "STOREFRONT_POLICY", policy.Id, new { policy.PolicyType, policy.Version });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStorePolicyDto>.Success(ToPolicyDto(policy));
    }

    public async Task<ApplicationResult<IReadOnlyList<OnlineStorePolicyDto>>> ListPolicyVersionsAsync(TenantRequestContext context, string policyType, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<IReadOnlyList<OnlineStorePolicyDto>>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var normalizedType = NormalizePolicyType(policyType);
        if (normalizedType is null) return Failure<IReadOnlyList<OnlineStorePolicyDto>>("online_store.policy_type_invalid", "Policy type is invalid.");
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var rows = await _db.StorefrontPolicies.AsNoTracking().Where(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.PolicyType == normalizedType).OrderByDescending(x => x.CreatedAt).Select(x => ToPolicyDto(x)).ToListAsync(cancellationToken);
        return ApplicationResult<IReadOnlyList<OnlineStorePolicyDto>>.Success(rows);
    }

    public async Task<ApplicationResult<OnlineStorePolicyDto>> ArchivePolicyAsync(TenantRequestContext context, string policyType, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStorePolicyDto>(context, TenantAdminOnlineStorePermissions.PoliciesManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var policy = await FindCurrentPolicyAsync(context, policyType, cancellationToken);
        if (policy is null) return NotFound<OnlineStorePolicyDto>("online_store.policy_not_found", "Policy was not found.");
        policy.Archive(context.UserId, _clock.UtcNow);
        AddAudit(context, "online_store.policy_archived", "STOREFRONT_POLICY", policy.Id, new { policy.PolicyType, policy.Version });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStorePolicyDto>.Success(ToPolicyDto(policy));
    }

    public async Task<ApplicationResult<OnlineStorePublishResponse>> PublishAsync(TenantRequestContext context, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Failure<OnlineStorePublishResponse>("online_store.idempotency_key_required", "Idempotency-Key header is required.");
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{context.TenantId:N}:online-store-publish"))).ToLowerInvariant();
        return await _idempotency.ExecuteAsync(context.TenantId, context.UserId, "tenant-admin/online-store/publish", idempotencyKey, requestHash, async ct =>
        {
            var access = await RequireAsync<OnlineStorePublishResponse>(context, TenantAdminOnlineStorePermissions.Publish, PlatformTenantFeatureCodes.OnlineStore, ct);
            if (access is not null) return access;
            var state = await LoadStateAsync(context.TenantId, true, ct);
            var readiness = await BuildReadinessAsync(state, ct);
            if (!readiness.CanPublish)
            {
                AddAudit(context, "online_store.publish_failed", "ONLINE_STORE", state.Channel.Id, new { readiness.BlockingReasons });
                await _db.SaveChangesAsync(ct);
                return Failure<OnlineStorePublishResponse>("online_store.publish_blocked", "Online Store is not ready to publish.");
            }
            var now = _clock.UtcNow;
            state.Channel.Update(state.Channel.CustomName, Active, state.Channel.SortOrder, now);
            state.Settings["storeStatus"] = Published;
            state.Settings["publishedAt"] = now;
            await SaveSettingsAsync(state, context.UserId, ct);
            AddAudit(context, "online_store.published", "ONLINE_STORE", state.Channel.Id, new { publishedAt = now });
            await _db.SaveChangesAsync(ct);
            var primaryDomain = await _db.TenantDomains.AsNoTracking()
                .Where(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.IsPrimary && x.Status == Active)
                .Select(x => x.DomainName)
                .FirstOrDefaultAsync(ct);
            return ApplicationResult<OnlineStorePublishResponse>.Success(new OnlineStorePublishResponse(
                Published,
                Active,
                now,
                readiness,
                BuildHostedUrl(ReadString(state.Settings, "storeSlug")),
                primaryDomain));
        }, cancellationToken);
    }

    private async Task<ApplicationResult<OnlineStoreDomainDto>> DomainRead(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainDto>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        return domain is null ? NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.") : ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain));
    }

    private async Task ReconcileDomainProviderStateAsync(
        TenantRequestContext context,
        TenantDomain domain,
        CancellationToken cancellationToken)
    {
        var changed = false;
        if (domain.VerificationStatus != "VERIFIED" && !string.IsNullOrWhiteSpace(domain.VerificationTokenHash))
        {
            var verification = await _domainVerificationProvider.VerifyTxtRecordAsync(
                domain.DomainName,
                domain.VerificationTokenHash,
                cancellationToken);
            if (verification.Status == DomainVerificationProviderStatus.Verified)
            {
                domain.MarkVerified(null, _clock.UtcNow);
                changed = true;
            }
            else if (verification.Status is DomainVerificationProviderStatus.NotFound or DomainVerificationProviderStatus.Timeout or DomainVerificationProviderStatus.Failed)
            {
                domain.MarkVerificationFailed(
                    verification.Status == DomainVerificationProviderStatus.Timeout ? "TIMEOUT" : "FAILED",
                    null,
                    _clock.UtcNow);
                changed = true;
            }
        }

        if (domain.VerificationStatus == "VERIFIED" && domain.SslStatus is "PENDING" or "ACTIVE")
        {
            changed |= await ReconcileCertificateStateAsync(context, domain, cancellationToken);
        }

        if (changed)
        {
            AddAudit(context, "online_store.domain_status_reconciled", "TENANT_DOMAIN", domain.Id, new
            {
                domain.DomainName,
                domain.VerificationStatus,
                domain.SslStatus
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> ReconcileCertificateStateAsync(
        TenantRequestContext context,
        TenantDomain domain,
        CancellationToken cancellationToken)
    {
        var status = await _certificateProvisioningProvider.GetStatusAsync(
            context.TenantId,
            domain.Id,
            domain.DomainName,
            cancellationToken);
        if (status.Status == CertificateProvisioningProviderStatus.Unavailable)
        {
            return false;
        }

        ApplyCertificateProviderResult(domain, status, _clock.UtcNow);
        return true;
    }

    private static void ApplyCertificateProviderResult(
        TenantDomain domain,
        CertificateProvisioningProviderResult result,
        DateTimeOffset now)
    {
        switch (result.Status)
        {
            case CertificateProvisioningProviderStatus.Active:
                domain.MarkSslActive(result.IssuedAt ?? now, result.ExpiresAt, null, now);
                break;
            case CertificateProvisioningProviderStatus.Provisioning:
                domain.MarkSslProvisioning(null, now);
                break;
            case CertificateProvisioningProviderStatus.Timeout:
                domain.MarkSslFailed("TIMEOUT", null, now);
                break;
            case CertificateProvisioningProviderStatus.Failed:
            case CertificateProvisioningProviderStatus.Unavailable:
                domain.MarkSslFailed("FAILED", null, now);
                break;
            default:
                domain.MarkSslFailed("NOT_REQUESTED", null, now);
                break;
        }
    }

    private async Task<OnlineStoreCatalogProductDto> ToCatalogDtoAsync(Product product, Guid? variantId, Guid channelId, CancellationToken cancellationToken)
    {
        var vis = await _db.ProductChannelVisibilities.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == product.TenantId && x.ProductId == product.Id && x.ProductVariantId == variantId && x.SalesChannelId == channelId, cancellationToken);
        var variantName = variantId.HasValue ? await _db.ProductVariants.AsNoTracking().Where(x => x.Id == variantId.Value).Select(x => x.VariantName).FirstOrDefaultAsync(cancellationToken) : null;
        return new OnlineStoreCatalogProductDto(product.Id, variantId, product.ProductName, variantName, vis?.IsVisible ?? false, vis?.IsOrderable ?? false, vis?.AvailableFrom, vis?.AvailableUntil, vis?.Status ?? Inactive);
    }

    private async Task<ApplicationResult<OnlineStoreCatalogProductDto>> UpsertVisibilityAsync(TenantRequestContext context, Guid productId, Guid? variantId, UpdateProductChannelVisibilityRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreCatalogProductDto>(context, TenantAdminOnlineStorePermissions.CatalogManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var product = await _db.Products.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.Id == productId && x.Status != Deleted, cancellationToken);
        if (product is null) return NotFound<OnlineStoreCatalogProductDto>("online_store.product_not_found", "Product was not found.");
        if (variantId.HasValue && !await _db.ProductVariants.AnyAsync(x => x.TenantId == context.TenantId && x.ProductId == productId && x.Id == variantId.Value && x.Status != Deleted, cancellationToken))
            return NotFound<OnlineStoreCatalogProductDto>("online_store.variant_not_found", "Product variant was not found.");
        var row = await _db.ProductChannelVisibilities.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.ProductId == productId && x.ProductVariantId == variantId && x.SalesChannelId == state.Channel.Id, cancellationToken);
        if (row is null)
        {
            row = ProductChannelVisibility.Create(Guid.NewGuid(), context.TenantId, productId, variantId, state.Channel.Id, request.IsVisible, request.IsOrderable, request.AvailableFrom, request.AvailableUntil, NormalizeRecordStatus(request.Status), context.UserId, _clock.UtcNow);
            _db.ProductChannelVisibilities.Add(row);
        }
        else
        {
            row.Update(request.IsVisible, request.IsOrderable, request.AvailableFrom, request.AvailableUntil, NormalizeRecordStatus(request.Status), context.UserId, _clock.UtcNow);
        }
        AddAudit(context, "online_store.product_visibility_changed", "PRODUCT", productId, new { variantId, request.IsVisible, request.IsOrderable });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreCatalogProductDto>.Success(await ToCatalogDtoAsync(product, variantId, state.Channel.Id, cancellationToken));
    }

    private async Task<OnlineStoreReadinessResponse> BuildReadinessAsync(OnlineStoreState state, CancellationToken cancellationToken)
    {
        var blockers = new List<string>();
        var steps = new List<OnlineStoreSetupStepDto>();
        void Add(int step, string code, string label, bool pass, params string[] reasons)
        {
            steps.Add(new OnlineStoreSetupStepDto(step, code, label, pass ? "PASS" : "BLOCKED", reasons));
            if (!pass) blockers.AddRange(reasons);
        }
        var domains = await GetDomainsAsync(state.Channel.Id, state.Tenant.Id, cancellationToken);
        var banners = await GetBannersAsync(state.Tenant.Id, state.Channel.Id, cancellationToken);
        var collection = await BuildCollectionOutletsAsync(state.Tenant.Id, cancellationToken);
        var policies = await _db.StorefrontPolicies.AsNoTracking().Where(x => x.TenantId == state.Tenant.Id && x.SalesChannelId == state.Channel.Id && x.Status == Published).Select(x => x.PolicyType).ToListAsync(cancellationToken);
        var totalProducts = await _db.Products.CountAsync(x => x.TenantId == state.Tenant.Id && x.Status != Deleted, cancellationToken);
        var visibleProducts = await _db.ProductChannelVisibilities.CountAsync(x => x.TenantId == state.Tenant.Id && x.SalesChannelId == state.Channel.Id && x.IsVisible && x.Status == Active, cancellationToken);
        Add(1, "overview", "Online Store Overview", string.Equals(state.Tenant.Status, TenantStatusConstants.Active, StringComparison.OrdinalIgnoreCase), "Tenant is not active.");
        Add(2, "activation", "Activation & Access", ReadBool(state.Settings, "setupEnabled") == true, "Online Store setup is not enabled.");
        Add(3, "identity", "Store Identity", !string.IsNullOrWhiteSpace(state.Channel.CustomName) && !string.IsNullOrWhiteSpace(ReadString(state.Settings, "businessDisplayName")), "Store identity is incomplete.");
        var storeSlugConfigured = !string.IsNullOrWhiteSpace(ReadString(state.Settings, "storeSlug"));
        var primaryCustomDomain = domains.FirstOrDefault(x =>
            x.IsPrimary &&
            string.Equals(x.DomainType, "CUSTOM", StringComparison.OrdinalIgnoreCase));
        var domainReady = storeSlugConfigured &&
            (primaryCustomDomain is null ||
             primaryCustomDomain.VerificationStatus == "VERIFIED" && primaryCustomDomain.SslStatus == "ACTIVE");
        Add(4, "domain", "Storefront URL & Domain", domainReady, primaryCustomDomain is null
            ? "Hosted store URL slug is missing."
            : "Primary custom domain verification or SSL is incomplete.");
        Add(5, "branding", "Branding & Appearance", ReadObject(state.Settings, "branding") is not null && banners.Any(x => x.Status == Active), "Branding or active banner is missing.");
        Add(6, "support", "Contact & Support", IsSupportReady(ReadObject(state.Settings, "support")), "Valid support email, phone, business address and support hours are required.");
        Add(7, "click_collect", "Click & Collect Configuration", collection.Any(x => x.Status == Active && x.Eligible), "No eligible active collection outlet with usable business hours is configured.");
        Add(8, "products_policies", "Products & Policies", totalProducts > 0 && visibleProducts > 0 && OnlineStoreContractRules.AreRequiredPoliciesPublished(policies), "At least one visible product and every required published policy are required.");
        Add(9, "review_publish", "Review & Publish", _emailSender.IsConfigured, "Email service is not configured.");
        return new OnlineStoreReadinessResponse(blockers.Count == 0, blockers.Distinct().ToList(), steps);
    }

    private async Task<OnlineStoreState> LoadStateAsync(Guid tenantId, bool ensureChannel, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.FirstAsync(x => x.Id == tenantId, cancellationToken);
        var channel = await (from sc in _db.SalesChannels
                             join psc in _db.PlatformSalesChannels on sc.PlatformSalesChannelId equals psc.Id
                             where sc.TenantId == tenantId && psc.ChannelCode == OnlineChannelCode
                             select sc).FirstOrDefaultAsync(cancellationToken);
        if (channel is null && ensureChannel)
        {
            var platformChannelId = await _db.PlatformSalesChannels.Where(x => x.ChannelCode == OnlineChannelCode).Select(x => x.Id).FirstAsync(cancellationToken);
            channel = SalesChannel.Create(Guid.NewGuid(), tenantId, platformChannelId, $"{tenant.DisplayName} Online Store", Inactive, 20, _clock.UtcNow);
            _db.SalesChannels.Add(channel);
            await _db.SaveChangesAsync(cancellationToken);
        }
        var settings = await LoadSettingsAsync(tenantId, cancellationToken);
        return new OnlineStoreState(tenant, channel!, settings);
    }

    private async Task<JsonObject> LoadSettingsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var raw = await (from setting in _db.TenantSettings.AsNoTracking()
                         join definition in _db.SettingDefinitions.AsNoTracking() on setting.SettingDefinitionId equals definition.Id
                         where setting.TenantId == tenantId && definition.SettingKey == TenantSettingKeys.OnlineStoreDefaults
                         select setting.SettingValue).FirstOrDefaultAsync(cancellationToken);
        var defaults = DefaultSettings();
        if (string.IsNullOrWhiteSpace(raw)) return defaults;
        var parsed = JsonNode.Parse(raw)?.AsObject() ?? [];
        foreach (var pair in parsed) defaults[pair.Key] = pair.Value?.DeepClone();
        return defaults;
    }

    private async Task SaveSettingsAsync(OnlineStoreState state, Guid actorUserId, CancellationToken cancellationToken)
    {
        var definition = await _db.SettingDefinitions.FirstAsync(x => x.SettingKey == TenantSettingKeys.OnlineStoreDefaults, cancellationToken);
        var setting = await _db.TenantSettings.FirstOrDefaultAsync(x => x.TenantId == state.Tenant.Id && x.SettingDefinitionId == definition.Id, cancellationToken);
        var value = state.Settings.ToJsonString(JsonOptions);
        if (setting is null)
            _db.TenantSettings.Add(TenantSetting.Create(Guid.NewGuid(), state.Tenant.Id, definition.Id, value, null, _clock.UtcNow));
        else
            setting.UpdateValue(value, _clock.UtcNow);
    }

    private async Task<OnlineStoreActivationResponse> BuildActivationAsync(OnlineStoreState state, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var onlineStoreEnabled = await _entitlements.IsEnabledAsync(state.Tenant.Id, PlatformTenantFeatureCodes.OnlineStore, now, cancellationToken);
        var clickCollectEnabled = await _entitlements.IsEnabledAsync(state.Tenant.Id, PlatformTenantFeatureCodes.ClickCollect, now, cancellationToken);
        var entitlements = new List<OnlineStoreEntitlementDto>
        {
            new(PlatformTenantFeatureCodes.OnlineStore, onlineStoreEnabled ? "ENABLED" : "DISABLED"),
            new(PlatformTenantFeatureCodes.ClickCollect, clickCollectEnabled ? "ENABLED" : "DISABLED")
        };
        var collectionOutlets = await BuildCollectionOutletsAsync(state.Tenant.Id, cancellationToken);
        var hasEligibleCollectionOutlet = collectionOutlets.Any(outlet => outlet.Eligible);
        var isLive = state.Channel.Status == Active && string.Equals(ReadString(state.Settings, "storeStatus"), Published, StringComparison.OrdinalIgnoreCase);
        var tenantActive = string.Equals(state.Tenant.Status, Active, StringComparison.OrdinalIgnoreCase);
        var readiness = new List<OnlineStoreActivationReadinessItemDto>
        {
            new(
                "channel_entitlement",
                "Channel Entitlement",
                onlineStoreEnabled ? "READY" : "NOT_READY",
                onlineStoreEnabled
                    ? "Your tenant is entitled to Online Store."
                    : "Online Store is not enabled for this tenant."),
            new(
                "authentication_ready",
                "Authentication Ready",
                onlineStoreEnabled && tenantActive ? "READY" : "NOT_READY",
                onlineStoreEnabled && tenantActive
                    ? "Registered customer authentication is available."
                    : "Customer authentication requires an active tenant and Online Store entitlement."),
            new(
                "email_service_ready",
                "Email Service Ready",
                _emailSender.IsConfigured ? "READY" : "NOT_READY",
                _emailSender.IsConfigured
                    ? "Email service is configured and active."
                    : "Email service must be configured before publishing."),
            new(
                "collection_outlet_requirement",
                "Collection Outlet Requirement",
                clickCollectEnabled && hasEligibleCollectionOutlet ? "READY" : "REQUIRED",
                clickCollectEnabled && hasEligibleCollectionOutlet
                    ? "At least one eligible collection outlet is ready."
                    : "Configure at least one eligible collection outlet in Step 7.")
        };

        return new OnlineStoreActivationResponse(
            ReadBool(state.Settings, "setupEnabled") ?? false,
            ReadString(state.Settings, "storeStatus") ?? Draft,
            state.Channel.Status,
            isLive ? "LIVE" : "NOT_LIVE",
            entitlements,
            OnlineStoreReleaseOnePolicy.ActivationReleaseScope,
            OnlineStoreReleaseOnePolicy.CustomerAccountMode,
            OnlineStoreReleaseOnePolicy.EmailVerificationRequired,
            OnlineStoreReleaseOnePolicy.PaymentMode,
            _emailSender.IsConfigured ? "READY" : "NOT_READY",
            !isLive,
            readiness);
    }

    private OnlineStoreIdentityResponse BuildIdentity(OnlineStoreState state) =>
        new(state.Channel.Id, state.Channel.CustomName, ReadString(state.Settings, "businessDisplayName") ?? state.Tenant.DisplayName, ReadString(state.Settings, "storeDescription"), ReadString(state.Settings, "storeEmail"), ReadString(state.Settings, "storePhone"), ReadString(state.Settings, "supportTagline"), state.Tenant.BaseCurrencyCode, state.Tenant.DefaultTimezone);

    private async Task<OnlineStoreBrandingResponse> BuildBrandingAsync(OnlineStoreState state, CancellationToken cancellationToken)
    {
        var branding = ReadObject(state.Settings, "branding") ?? [];
        var logoMediaAssetId = ReadGuid(branding, "logoMediaAssetId");
        var faviconMediaAssetId = ReadGuid(branding, "faviconMediaAssetId");
        var mediaIds = new[] { logoMediaAssetId, faviconMediaAssetId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var media = await _db.MediaAssets.AsNoTracking()
            .Where(asset => asset.TenantId == state.Tenant.Id && mediaIds.Contains(asset.Id) &&
                            (asset.Status == Active || asset.Status == "STAGED"))
            .ToDictionaryAsync(asset => asset.Id, cancellationToken);

        return new OnlineStoreBrandingResponse(
            logoMediaAssetId,
            faviconMediaAssetId,
            ReadString(branding, "primaryColor") ?? "#FF6A00",
            ReadString(branding, "secondaryColor") ?? "#000000",
            await GetBannersAsync(state.Tenant.Id, state.Channel.Id, cancellationToken),
            ResolveMediaUrl(logoMediaAssetId, media),
            ResolveMediaUrl(faviconMediaAssetId, media));
    }

    private static OnlineStoreSupportResponse BuildSupport(JsonObject settings)
    {
        var support = ReadObject(settings, "support") ?? [];
        return new OnlineStoreSupportResponse(ReadString(support, "email"), ReadString(support, "phone"), ReadString(support, "whatsapp"), ReadString(support, "helpUrl"), ReadBool(support, "contactUsEnabled") ?? true, ReadString(support, "supportHours"), ReadString(support, "businessAddress"));
    }

    private async Task<IReadOnlyList<OnlineStoreDomainDto>> GetDomainsAsync(Guid channelId, Guid tenantId, CancellationToken cancellationToken) =>
        await _db.TenantDomains.AsNoTracking().Where(x => x.TenantId == tenantId && x.SalesChannelId == channelId && x.Status != Deleted).OrderByDescending(x => x.IsPrimary).ThenBy(x => x.DomainName).Select(x => ToDomainDto(x)).ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<OnlineStoreBannerDto>> GetBannersAsync(Guid tenantId, Guid channelId, CancellationToken cancellationToken)
    {
        var rows = await (from banner in _db.StorefrontBanners.AsNoTracking()
                          join media in _db.MediaAssets.AsNoTracking() on banner.ImageMediaAssetId equals media.Id into medias
                          from media in medias.DefaultIfEmpty()
                          where banner.TenantId == tenantId && banner.SalesChannelId == channelId && banner.Status != Deleted
                          orderby banner.SortOrder, banner.Title
                          select new
                          {
                              Banner = banner,
                              MediaContainerName = media == null ? null : media.ContainerName,
                              MediaStorageKey = media == null ? null : media.StorageKey,
                              MediaPublicUrl = media == null ? null : media.PublicUrl,
                              MediaStatus = media == null ? null : media.Status
                          }).ToListAsync(cancellationToken);

        return rows.Select(row => new OnlineStoreBannerDto(
            row.Banner.Id,
            row.Banner.BannerType,
            row.Banner.Title,
            row.Banner.Subtitle,
            row.Banner.ImageMediaAssetId,
            row.MediaStatus is Active or "STAGED"
                ? _mediaReadUrlResolver?.ResolveReadUrl(row.MediaContainerName, row.MediaStorageKey, row.MediaPublicUrl) ?? row.MediaPublicUrl
                : null,
            row.Banner.ActionText,
            row.Banner.ActionUrl,
            row.Banner.SortOrder,
            row.Banner.Status)).ToList();
    }

    private async Task<IReadOnlyList<OnlineStoreCollectionOutletDto>> BuildCollectionOutletsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var methodId = await _db.FulfillmentMethods.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.MethodType == PickupMethodType && x.Status != Deleted)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var rows = await (from outlet in _db.Outlets.AsNoTracking()
                          join mapping in _db.FulfillmentMethodOutlets.AsNoTracking().Where(x => methodId.HasValue && x.FulfillmentMethodId == methodId.Value) on outlet.Id equals mapping.OutletId into mappings
                          from mapping in mappings.DefaultIfEmpty()
                          where outlet.TenantId == tenantId && outlet.Status != Deleted
                          orderby outlet.OutletName
                          let hasHours = _db.OutletBusinessHours.Any(h => h.TenantId == tenantId && h.OutletId == outlet.Id && !h.IsClosed && h.OpeningTime != null && h.ClosingTime != null && h.ClosingTime > h.OpeningTime)
                          select new
                          {
                              outlet.Id,
                              outlet.OutletName,
                              OutletStatus = outlet.Status,
                              HasHours = hasHours,
                              LeadMinutes = mapping == null ? null : mapping.PreparationLeadMinutes,
                              WindowMinutes = mapping == null ? null : mapping.PickupWindowMinutes,
                              CutoffTime = mapping == null ? null : mapping.CutoffTime,
                              MappingStatus = mapping == null ? Inactive : mapping.Status
                          }).ToListAsync(cancellationToken);
        return rows.Select(row =>
        {
            var eligible = row.OutletStatus == Active && row.HasHours;
            IReadOnlyList<string> reasons = row.OutletStatus != Active
                ? ["OUTLET_NOT_ACTIVE"]
                : !row.HasHours ? ["BUSINESS_HOURS_NOT_CONFIGURED"] : [];
            return new OnlineStoreCollectionOutletDto(row.Id, row.OutletName, row.OutletStatus, row.HasHours, row.LeadMinutes, row.WindowMinutes, row.CutoffTime?.ToString("HH:mm"), row.MappingStatus, eligible, reasons);
        }).ToList();
    }

    private async Task<FulfillmentMethod> EnsurePickupMethodAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var method = await _db.FulfillmentMethods.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MethodType == PickupMethodType && x.Status != Deleted, cancellationToken);
        if (method is not null) return method;
        method = FulfillmentMethod.Create(Guid.NewGuid(), tenantId, "PICKUP", "Pickup", "Click & Collect pickup", Active, PickupMethodType, _clock.UtcNow);
        _db.FulfillmentMethods.Add(method);
        await _db.SaveChangesAsync(cancellationToken);
        return method;
    }

    private async Task<StorefrontPolicy?> FindCurrentPolicyAsync(TenantRequestContext context, string policyType, CancellationToken cancellationToken)
    {
        var normalizedType = NormalizePolicyType(policyType);
        if (normalizedType is null) return null;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        return await _db.StorefrontPolicies.Where(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.PolicyType == normalizedType && x.Status != "ARCHIVED").OrderByDescending(x => x.Status == Published).ThenByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<TenantDomain?> FindDomainAsync(Guid tenantId, Guid domainId, CancellationToken cancellationToken) =>
        await _db.TenantDomains.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == domainId && x.Status != Deleted, cancellationToken);

    private async Task ClearPrimaryDomainsAsync(Guid tenantId, Guid channelId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var primaries = await _db.TenantDomains.Where(x => x.TenantId == tenantId && x.SalesChannelId == channelId && x.Status == Active && x.IsPrimary).ToListAsync(cancellationToken);
        foreach (var primary in primaries) primary.SetPrimary(false, null, now);
    }

    private async Task<ApplicationResult<T>?> RequireAsync<T>(TenantRequestContext context, string permission, string featureCode, CancellationToken cancellationToken, string? fallbackPermission = null)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
            return ApplicationResult<T>.Failure(new ApplicationError("online_store.permission_denied", "Permission denied."));
        var tenantActive = await _db.Tenants.AsNoTracking().AnyAsync(x => x.Id == context.TenantId && x.Status == TenantStatusConstants.Active, cancellationToken);
        if (!tenantActive)
            return ApplicationResult<T>.Failure(new ApplicationError("online_store.tenant_inactive", "Tenant is not active."));
        if (!await _entitlements.IsEnabledAsync(context.TenantId, PlatformTenantFeatureCodes.OnlineStore, _clock.UtcNow, cancellationToken))
            return ApplicationResult<T>.Failure(new ApplicationError("online_store.entitlement_denied", "Online Store entitlement is not enabled."));
        if (featureCode != PlatformTenantFeatureCodes.OnlineStore && !await _entitlements.IsEnabledAsync(context.TenantId, featureCode, _clock.UtcNow, cancellationToken))
            return ApplicationResult<T>.Failure(new ApplicationError("online_store.entitlement_denied", "Feature entitlement is not enabled."));
        var hasPermission = context.HasPermission(permission) || (fallbackPermission is not null && context.HasPermission(fallbackPermission));
        if (!hasPermission)
            return ApplicationResult<T>.Failure(new ApplicationError("online_store.permission_denied", "Permission denied."));
        return null;
    }

    private async Task<bool> MediaBelongsToTenantAsync(Guid tenantId, Guid? mediaAssetId, string expectedPurpose, CancellationToken cancellationToken) =>
        !mediaAssetId.HasValue || await _db.MediaAssets.AnyAsync(
            x => x.TenantId == tenantId && x.Id == mediaAssetId.Value && x.AssetPurpose == expectedPurpose &&
                 (x.Status == Active || x.Status == "STAGED"),
            cancellationToken);

    private string? ResolveMediaUrl(Guid? mediaAssetId, IReadOnlyDictionary<Guid, MediaAsset> media)
    {
        if (!mediaAssetId.HasValue || !media.TryGetValue(mediaAssetId.Value, out var asset)) return null;
        return _mediaReadUrlResolver?.ResolveReadUrl(asset.ContainerName, asset.StorageKey, asset.PublicUrl) ?? asset.PublicUrl;
    }

    private void AddAudit(TenantRequestContext context, string action, string entityType, Guid? entityId, object payload) =>
        _db.AuditLogs.Add(new AuditLog { TenantId = context.TenantId, ActorUserId = context.UserId, ActorType = "TENANT_USER", EntityType = entityType, EntityId = entityId, Action = action, NewValues = JsonSerializer.Serialize(payload, JsonOptions), CreatedAt = _clock.UtcNow });

    private static JsonObject DefaultSettings() => JsonNode.Parse("""{"schemaVersion":1,"storeStatus":"DRAFT","taxDisplayMode":"MATCH_TENANT","setupEnabled":false,"storeSlug":null,"businessDisplayName":null,"storeDescription":null,"storeEmail":null,"storePhone":null,"supportTagline":null,"branding":{"logoMediaAssetId":null,"faviconMediaAssetId":null,"primaryColor":"#FF6A00","secondaryColor":"#000000"},"support":{"email":null,"phone":null,"whatsapp":null,"helpUrl":null,"contactUsEnabled":true,"supportHours":null,"businessAddress":null},"publishedAt":null}""")!.AsObject();

    private static JsonObject EnsureObject(JsonObject root, string key)
    {
        if (root[key] is JsonObject existing) return existing;
        var created = new JsonObject();
        root[key] = created;
        return created;
    }

    private static JsonObject? ReadObject(JsonObject root, string key) => root[key] as JsonObject;
    private static string? ReadString(JsonObject root, string key) => root[key]?.GetValue<string>();
    private static string? ReadObjectString(JsonObject root, string objectKey, string key) => ReadObject(root, objectKey) is { } obj ? ReadString(obj, key) : null;
    private static bool? ReadBool(JsonObject root, string key) => root[key]?.GetValue<bool>();
    private static Guid? ReadGuid(JsonObject root, string key) => Guid.TryParse(ReadString(root, key), out var value) ? value : null;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsSupportReady(JsonObject? support) =>
        support is not null && OnlineStoreContractRules.IsSupportReady(
            ReadString(support, "email"),
            ReadString(support, "phone"),
            ReadString(support, "businessAddress"),
            ReadString(support, "supportHours"));
    private string? BuildHostedUrl(string? slug) => string.IsNullOrWhiteSpace(slug) ? null : $"https://{slug}.{_options.HostedDomain.Trim().Trim('.').ToLowerInvariant()}";
    private static string NormalizeDomainType(string value) => string.IsNullOrWhiteSpace(value) ? "CUSTOM" : value.Trim().ToUpperInvariant();
    private static string NormalizeRecordStatus(string value) => value.Trim().ToUpperInvariant() is Active or Inactive or Deleted ? value.Trim().ToUpperInvariant() : Active;
    private static TimeOnly? ParseTime(string? value) => TimeOnly.TryParse(value, out var time) ? time : null;
    private static bool IsHexColor(string value) => value.Trim().Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
    private static string? NormalizePolicyType(string value) =>
        value.Trim().ToUpperInvariant() is "TERMS" or "PRIVACY" or "CANCELLATION" or "COLLECTION" or "RETURN_REFUND"
            ? value.Trim().ToUpperInvariant()
            : null;
    private static OnlineStoreDomainDto ToDomainDto(TenantDomain domain) => new(domain.Id, domain.DomainType, domain.DomainName, domain.IsPrimary, domain.VerificationStatus, domain.VerifiedAt, domain.SslStatus, domain.SslIssuedAt, domain.SslExpiresAt, domain.Status);
    private static OnlineStorePolicyDto ToPolicyDto(StorefrontPolicy policy) => new(policy.Id, policy.PolicyType, policy.Title, policy.Content, policy.Version, policy.Status, policy.PublishedAt);
    private static ApplicationResult<T> NotFound<T>(string code, string message) => ApplicationResult<T>.Failure(new ApplicationError(code, message));
    private static ApplicationResult<T> Failure<T>(string code, string message, IReadOnlyList<ApplicationFieldError>? fields = null) => ApplicationResult<T>.Failure(new ApplicationError(code, message, fields ?? []));

    private static IReadOnlyList<ApplicationFieldError> ValidateIdentity(UpdateOnlineStoreIdentityRequest request)
    {
        var errors = new List<ApplicationFieldError>();
        if (string.IsNullOrWhiteSpace(request.StoreName)) errors.Add(new("storeName", "Store name is required."));
        if (request.StoreName?.Trim().Length > OnlineStoreContractRules.StoreNameMaxLength) errors.Add(new("storeName", "Store name is too long."));
        if (string.IsNullOrWhiteSpace(request.BusinessDisplayName)) errors.Add(new("businessDisplayName", "Business display name is required."));
        if (request.BusinessDisplayName?.Trim().Length > OnlineStoreContractRules.BusinessDisplayNameMaxLength) errors.Add(new("businessDisplayName", "Business display name is too long."));
        if (request.StoreDescription?.Trim().Length > OnlineStoreContractRules.StoreDescriptionMaxLength) errors.Add(new("storeDescription", "Store description is too long."));
        if (!string.IsNullOrWhiteSpace(request.StoreEmail) && !OnlineStoreContractRules.IsValidEmail(request.StoreEmail)) errors.Add(new("storeEmail", "Enter a valid store email address."));
        if (!string.IsNullOrWhiteSpace(request.StorePhone) && OnlineStoreContractRules.NormalizePhone(request.StorePhone) is null) errors.Add(new("storePhone", "Enter a valid store phone number."));
        if (request.SupportTagline?.Trim().Length > OnlineStoreContractRules.SupportTaglineMaxLength) errors.Add(new("supportTagline", "Support tagline is too long."));
        return errors;
    }

    private static IReadOnlyList<ApplicationFieldError> ValidateSupport(UpdateOnlineStoreSupportRequest request)
    {
        var errors = new List<ApplicationFieldError>();
        if (!OnlineStoreContractRules.IsValidEmail(request.Email)) errors.Add(new("email", "Enter a valid support email address."));
        if (!OnlineStoreContractRules.IsValidOptionalHttpsUrl(request.HelpUrl)) errors.Add(new("helpUrl", "Help URL must use HTTPS."));
        if (OnlineStoreContractRules.NormalizePhone(request.Phone) is null) errors.Add(new("phone", "Enter a valid support phone number."));
        if (request.Whatsapp?.Trim().Length > OnlineStoreContractRules.StorePhoneMaxLength) errors.Add(new("whatsapp", "WhatsApp number is too long."));
        if (!OnlineStoreContractRules.IsValidSupportHours(request.SupportHours)) errors.Add(new("supportHours", "Enter valid support hours, for example Mon - Fri: 9:00 AM - 6:00 PM."));
        if (string.IsNullOrWhiteSpace(request.BusinessAddress) || request.BusinessAddress.Trim().Length > OnlineStoreContractRules.BusinessAddressMaxLength) errors.Add(new("businessAddress", "Business address is required and must not exceed the maximum length."));
        return errors;
    }

    private static IReadOnlyList<ApplicationFieldError> ValidateBanner(UpsertOnlineStoreBannerRequest request)
    {
        var errors = new List<ApplicationFieldError>();
        if (request.BannerType.Trim().ToUpperInvariant() is not ("HERO" or "PROMO" or "ANNOUNCEMENT")) errors.Add(new("bannerType", "Banner type must be HERO, PROMO or ANNOUNCEMENT."));
        if (string.IsNullOrWhiteSpace(request.Title)) errors.Add(new("title", "Title is required."));
        if (!OnlineStoreContractRules.IsValidOptionalHttpsUrl(request.ActionUrl)) errors.Add(new("actionUrl", "Banner action URL must use HTTPS."));
        return errors;
    }

    private static IReadOnlyList<ApplicationFieldError> ValidateCollectionRules(int? leadMinutes, int? windowMinutes, string? cutoffTime)
    {
        var errors = new List<ApplicationFieldError>();
        if (leadMinutes is < 0 or > 10080) errors.Add(new("preparationLeadMinutes", "Preparation lead time must be between 0 and 10080 minutes."));
        if (windowMinutes is <= 0 or > 1440) errors.Add(new("pickupWindowMinutes", "Pickup window must be between 1 and 1440 minutes."));
        if (!string.IsNullOrWhiteSpace(cutoffTime) && ParseTime(cutoffTime) is null) errors.Add(new("cutoffTime", "Cutoff time must be a valid time."));
        return errors;
    }

    private async Task<PreparedImage> PrepareImageAsync(MediaUploadFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaxMediaBytes) return PreparedImage.Invalid("Image file must be between 1 byte and 5 MB.");
        var fileName = Path.GetFileName(file.FileName).Trim();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var mime = NormalizeImageMimeType(file.ContentType);
        if (!OnlineStoreContractRules.IsSupportedBrandingMediaFormat(mime, extension))
            return PreparedImage.Invalid("Only JPG, JPEG, PNG, WEBP, SVG and ICO images are allowed.");
        await using var memory = new MemoryStream();
        await file.Content.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        if (!MagicMatches(bytes, mime)) return PreparedImage.Invalid("Image signature does not match MIME type.");
        if (mime == "image/svg+xml") return PrepareSvg(fileName, extension, mime, bytes);
        if (mime == "image/x-icon") return PrepareIcon(fileName, extension, mime, bytes);
        try
        {
            using var image = Image.Load(bytes);
            if ((long)image.Width * image.Height > MaxPixels) return PreparedImage.Invalid("Image dimensions exceed the 16 MP limit.");
            return PreparedImage.Valid(fileName, extension, mime, bytes, image.Width, image.Height, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        }
        catch
        {
            return PreparedImage.Invalid("Image data is corrupted or cannot be decoded.");
        }
    }

    private static bool MagicMatches(byte[] bytes, string mime) => mime switch
    {
        "image/jpeg" => bytes.Length > 2 && bytes[0] == 0xFF && bytes[1] == 0xD8,
        "image/png" => bytes.Length > 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/webp" => bytes.Length > 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        "image/svg+xml" => bytes.Length > 4 && Encoding.UTF8.GetString(bytes.AsSpan(0, Math.Min(bytes.Length, 512))).Contains("<svg", StringComparison.OrdinalIgnoreCase),
        "image/x-icon" => bytes.Length > 8 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0,
        _ => false
    };

    private static string NormalizeImageMimeType(string contentType) => contentType.Trim().ToLowerInvariant() switch
    {
        "image/jpg" => "image/jpeg",
        "image/vnd.microsoft.icon" => "image/x-icon",
        "image/ico" => "image/x-icon",
        var mime => mime
    };

    private static PreparedImage PrepareIcon(string fileName, string extension, string mime, byte[] bytes)
    {
        if (bytes.Length < 22 || bytes[4] == 0 && bytes[5] == 0)
            return PreparedImage.Invalid("ICO image does not contain an icon entry.");
        var width = bytes[6] == 0 ? 256 : bytes[6];
        var height = bytes[7] == 0 ? 256 : bytes[7];
        return PreparedImage.Valid(fileName, extension, mime, bytes, width, height, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    private static PreparedImage PrepareSvg(string fileName, string extension, string mime, byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 5 * 1024 * 1024
            });
            var document = XDocument.Load(reader, LoadOptions.None);
            var root = document.Root;
            if (root is null || !string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase) || ContainsUnsafeSvgContent(document))
                return PreparedImage.Invalid("SVG image contains unsupported or unsafe content.");

            var width = ParseSvgDimension(root.Attribute("width")?.Value);
            var height = ParseSvgDimension(root.Attribute("height")?.Value);
            if ((!width.HasValue || !height.HasValue) && root.Attribute("viewBox")?.Value is { } viewBox)
            {
                var values = viewBox.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 4 && double.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var viewBoxWidth) &&
                    double.TryParse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var viewBoxHeight))
                {
                    width ??= (int)Math.Ceiling(viewBoxWidth);
                    height ??= (int)Math.Ceiling(viewBoxHeight);
                }
            }
            if (width.HasValue && height.HasValue && (long)width.Value * height.Value > MaxPixels)
                return PreparedImage.Invalid("Image dimensions exceed the 16 MP limit.");
            return PreparedImage.Valid(fileName, extension, mime, bytes, width, height, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        }
        catch
        {
            return PreparedImage.Invalid("SVG image is corrupted or cannot be decoded.");
        }
    }

    private static bool ContainsUnsafeSvgContent(XDocument document)
    {
        string[] blockedElements = ["script", "foreignObject", "iframe", "object", "embed"];
        foreach (var element in document.Descendants())
        {
            if (blockedElements.Contains(element.Name.LocalName, StringComparer.OrdinalIgnoreCase)) return true;
            foreach (var attribute in element.Attributes())
            {
                var name = attribute.Name.LocalName;
                var value = attribute.Value.Trim();
                if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(name, "href", StringComparison.OrdinalIgnoreCase) && !value.StartsWith('#')) ||
                    value.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("data:text/html", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("url(", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;
    }

    private static int? ParseSvgDimension(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var numeric = new string(value.Trim().TakeWhile(character => char.IsDigit(character) || character is '.' or '-').ToArray());
        return double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? (int)Math.Ceiling(parsed)
            : null;
    }

    private static string CreateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();

    private sealed record OnlineStoreState(E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant Tenant, SalesChannel Channel, JsonObject Settings);

    private sealed record PreparedImage(string FileName, string Extension, string Mime, byte[] Bytes, int? Width, int? Height, string Hash, ApplicationError? Error)
    {
        public static PreparedImage Valid(string fileName, string extension, string mime, byte[] bytes, int? width, int? height, string hash) => new(fileName, extension, mime, bytes, width, height, hash, null);
        public static PreparedImage Invalid(string message) => new(string.Empty, string.Empty, string.Empty, [], 0, 0, string.Empty, new ApplicationError("online_store.media_invalid", message, [new ApplicationFieldError("file", message)]));
    }
}
