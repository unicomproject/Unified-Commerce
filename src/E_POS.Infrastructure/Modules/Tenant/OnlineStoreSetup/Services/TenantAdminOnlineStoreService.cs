using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Application.Modules.Shared.Media.Dtos;
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
    private static readonly string[] RequiredPolicyTypes = ["TERMS", "PRIVACY", "CANCELLATION", "COLLECTION", "RETURN_REFUND"];

    private readonly EPosDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ITenantFeatureEntitlementEvaluator _entitlements;
    private readonly IMediaObjectStorage _storage;
    private readonly IIdempotencyService _idempotency;

    public TenantAdminOnlineStoreService(
        EPosDbContext db,
        IDateTimeProvider clock,
        ITenantFeatureEntitlementEvaluator entitlements,
        IMediaObjectStorage storage,
        IIdempotencyService idempotency)
    {
        _db = db;
        _clock = clock;
        _entitlements = entitlements;
        _storage = storage;
        _idempotency = idempotency;
    }

    public async Task<ApplicationResult<OnlineStoreOverviewResponse>> GetOverviewAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreOverviewResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, ensureChannel: true, cancellationToken);
        var readiness = await BuildReadinessAsync(state, cancellationToken);
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
            readiness));
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
        state.Channel.Update(request.StoreName, state.Channel.Status, state.Channel.SortOrder, _clock.UtcNow);
        state.Settings["businessDisplayName"] = request.BusinessDisplayName.Trim();
        state.Settings["storeDescription"] = Clean(request.StoreDescription);
        state.Settings["storeEmail"] = Clean(request.StoreEmail);
        state.Settings["storePhone"] = Clean(request.StorePhone);
        state.Settings["supportTagline"] = Clean(request.SupportTagline);
        await SaveSettingsAsync(state, context.UserId, cancellationToken);
        AddAudit(context, "online_store.identity_updated", "ONLINE_STORE", state.Channel.Id, new { request.StoreName, request.BusinessDisplayName });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreIdentityResponse>.Success(BuildIdentity(state));
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
        var slug = NormalizeSlug(request.StoreSlug);
        if (slug is null) return Failure<OnlineStoreUrlDomainResponse>("online_store.slug_invalid", "Store slug is invalid.", [new("storeSlug", "Store slug must contain letters, numbers or hyphens.")]);
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
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
        var domainName = NormalizeDomain(request.DomainName);
        if (domainName is null) return Failure<OnlineStoreDomainTokenResponse>("online_store.domain_invalid", "Domain name is invalid.", [new("domainName", "Enter a valid domain name.")]);
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        if (await _db.TenantDomains.AnyAsync(x => x.TenantId == context.TenantId && x.DomainName == domainName && x.Status != Deleted, cancellationToken))
            return Failure<OnlineStoreDomainTokenResponse>("online_store.domain_conflict", "Domain already exists for this tenant.", [new("domainName", "Domain already exists.")]);
        var now = _clock.UtcNow;
        var token = CreateRawToken();
        if (request.IsPrimary)
        {
            await ClearPrimaryDomainsAsync(context.TenantId, state.Channel.Id, now, cancellationToken);
        }
        var domain = TenantDomain.Create(Guid.NewGuid(), context.TenantId, state.Channel.Id, NormalizeDomainType(request.DomainType), domainName, request.IsPrimary, "PENDING", HashToken(token), null, "NOT_REQUESTED", null, null, Active, null, now);
        _db.TenantDomains.Add(domain);
        AddAudit(context, "online_store.domain_created", "TENANT_DOMAIN", domain.Id, new { domainName, domainType = domain.DomainType, domain.IsPrimary });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreDomainTokenResponse>.Success(new OnlineStoreDomainTokenResponse(domain.Id, domain.DomainName, token));
    }

    public async Task<ApplicationResult<OnlineStoreDomainDto>> VerifyDomainAsync(TenantRequestContext context, Guid domainId, VerifyOnlineStoreDomainRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainDto>(context, TenantAdminOnlineStorePermissions.DomainsManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        if (domain is null) return NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.");
        if (!string.Equals(domain.VerificationTokenHash, HashToken(request.VerificationToken), StringComparison.Ordinal))
            return Failure<OnlineStoreDomainDto>("online_store.domain_verification_failed", "Verification token did not match.", [new("verificationToken", "Token did not match.")]);
        domain.MarkVerified(null, _clock.UtcNow);
        AddAudit(context, "online_store.domain_verified", "TENANT_DOMAIN", domain.Id, new { domain.DomainName });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain));
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

    public async Task<ApplicationResult<OnlineStoreDomainDto>> GetDomainStatusAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken) =>
        await DomainRead(context, domainId, cancellationToken);

    public async Task<ApplicationResult<OnlineStoreDomainDto>> ProvisionDomainSslAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainDto>(context, TenantAdminOnlineStorePermissions.DomainsManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        if (domain is null) return NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.");
        if (domain.VerificationStatus != "VERIFIED")
            return Failure<OnlineStoreDomainDto>("online_store.domain_not_verified", "Domain must be verified before SSL provisioning.");
        domain.MarkSslProvisioning(null, _clock.UtcNow);
        AddAudit(context, "online_store.domain_ssl_requested", "TENANT_DOMAIN", domain.Id, new { domain.DomainName });
        await _db.SaveChangesAsync(cancellationToken);
        return ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain));
    }

    public async Task<ApplicationResult<OnlineStoreDomainDto>> SetPrimaryDomainAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainDto>(context, TenantAdminOnlineStorePermissions.DomainsManage, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        if (domain is null) return NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.");
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
        await ValidateMediaOwnershipAsync(context.TenantId, request.LogoMediaAssetId, cancellationToken);
        await ValidateMediaOwnershipAsync(context.TenantId, request.FaviconMediaAssetId, cancellationToken);
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
        await ValidateMediaOwnershipAsync(context.TenantId, request.ImageMediaAssetId, cancellationToken);
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
        await ValidateMediaOwnershipAsync(context.TenantId, request.ImageMediaAssetId, cancellationToken);
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
        var ids = request.Items.Select(x => x.BannerId).ToArray();
        var banners = await _db.StorefrontBanners.Where(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && ids.Contains(x.Id)).ToListAsync(cancellationToken);
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
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var support = EnsureObject(state.Settings, "support");
        support["email"] = Clean(request.Email);
        support["phone"] = Clean(request.Phone);
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
        var access = await RequireAsync<OnlineStoreClickCollectResponse>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.ClickCollect, cancellationToken);
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
        var access = await RequireAsync<IReadOnlyList<OnlineStoreCollectionOutletDto>>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.ClickCollect, cancellationToken);
        if (access is not null) return access;
        return ApplicationResult<IReadOnlyList<OnlineStoreCollectionOutletDto>>.Success(await BuildCollectionOutletsAsync(context.TenantId, cancellationToken));
    }

    public async Task<ApplicationResult<OnlineStoreCollectionOutletDto>> UpsertCollectionOutletAsync(TenantRequestContext context, Guid outletId, UpsertCollectionOutletRequest request, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreCollectionOutletDto>(context, TenantAdminOnlineStorePermissions.FulfillmentManage, PlatformTenantFeatureCodes.ClickCollect, cancellationToken);
        if (access is not null) return access;
        var method = await EnsurePickupMethodAsync(context.TenantId, cancellationToken);
        var outletExists = await _db.Outlets.AnyAsync(x => x.TenantId == context.TenantId && x.Id == outletId && x.Status != Deleted, cancellationToken);
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
        foreach (var outletId in request.OutletIds.Distinct())
        {
            _ = await UpsertCollectionOutletAsync(context, outletId, new UpsertCollectionOutletRequest(request.PreparationLeadMinutes, request.PickupWindowMinutes, request.CutoffTime, request.Status), cancellationToken);
        }
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
            .GroupJoin(_db.ProductChannelVisibilities.AsNoTracking().Where(x => x.SalesChannelId == state.Channel.Id && x.ProductVariantId == null), p => p.Id, v => v.ProductId, (p, vis) => new { p, vis = vis.FirstOrDefault() })
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
        var state = await LoadStateAsync(context.TenantId, true, cancellationToken);
        var policy = await _db.StorefrontPolicies.FirstOrDefaultAsync(x => x.TenantId == context.TenantId && x.SalesChannelId == state.Channel.Id && x.PolicyType == normalizedType && x.Version == request.Version.Trim(), cancellationToken);
        if (policy is null)
        {
            policy = StorefrontPolicy.Create(Guid.NewGuid(), context.TenantId, state.Channel.Id, normalizedType, request.Title, request.Content, request.Version, Draft, context.UserId, _clock.UtcNow);
            _db.StorefrontPolicies.Add(policy);
        }
        else
        {
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
            return ApplicationResult<OnlineStorePublishResponse>.Success(new OnlineStorePublishResponse(Published, Active, now, readiness));
        }, cancellationToken);
    }

    private async Task<ApplicationResult<OnlineStoreDomainDto>> DomainRead(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken)
    {
        var access = await RequireAsync<OnlineStoreDomainDto>(context, TenantAdminOnlineStorePermissions.View, PlatformTenantFeatureCodes.OnlineStore, cancellationToken);
        if (access is not null) return access;
        var domain = await FindDomainAsync(context.TenantId, domainId, cancellationToken);
        return domain is null ? NotFound<OnlineStoreDomainDto>("online_store.domain_not_found", "Domain was not found.") : ApplicationResult<OnlineStoreDomainDto>.Success(ToDomainDto(domain));
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
        var policies = await _db.StorefrontPolicies.AsNoTracking().Where(x => x.TenantId == state.Tenant.Id && x.SalesChannelId == state.Channel.Id && x.Status == Published).Select(x => x.PolicyType).Distinct().ToListAsync(cancellationToken);
        var totalProducts = await _db.Products.CountAsync(x => x.TenantId == state.Tenant.Id && x.Status != Deleted, cancellationToken);
        var visibleProducts = await _db.ProductChannelVisibilities.CountAsync(x => x.TenantId == state.Tenant.Id && x.SalesChannelId == state.Channel.Id && x.IsVisible && x.Status == Active, cancellationToken);
        Add(1, "overview", "Online Store Overview", true);
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
        Add(6, "support", "Contact & Support", !string.IsNullOrWhiteSpace(ReadObjectString(state.Settings, "support", "email")) && !string.IsNullOrWhiteSpace(ReadObjectString(state.Settings, "support", "phone")), "Support email or phone is missing.");
        Add(7, "click_collect", "Click & Collect Configuration", collection.Any(x => x.Status == Active && x.BusinessHoursConfigured), "No active collection outlet with business hours configured.");
        Add(8, "products_policies", "Products & Policies", totalProducts == 0 || visibleProducts > 0 && RequiredPolicyTypes.All(policies.Contains), "Visible products or published policies are incomplete.");
        Add(9, "review_publish", "Review & Publish", true);
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
        var entitlements = new List<OnlineStoreEntitlementDto>
        {
            new(PlatformTenantFeatureCodes.OnlineStore, await _entitlements.IsEnabledAsync(state.Tenant.Id, PlatformTenantFeatureCodes.OnlineStore, now, cancellationToken) ? "ENABLED" : "DISABLED"),
            new(PlatformTenantFeatureCodes.ClickCollect, await _entitlements.IsEnabledAsync(state.Tenant.Id, PlatformTenantFeatureCodes.ClickCollect, now, cancellationToken) ? "ENABLED" : "DISABLED")
        };
        return new OnlineStoreActivationResponse(ReadBool(state.Settings, "setupEnabled") ?? false, ReadString(state.Settings, "storeStatus") ?? Draft, state.Channel.Status, state.Channel.Status == Active ? "LIVE" : "NOT_LIVE", entitlements);
    }

    private OnlineStoreIdentityResponse BuildIdentity(OnlineStoreState state) =>
        new(state.Channel.Id, state.Channel.CustomName, ReadString(state.Settings, "businessDisplayName") ?? state.Tenant.DisplayName, ReadString(state.Settings, "storeDescription"), ReadString(state.Settings, "storeEmail"), ReadString(state.Settings, "storePhone"), ReadString(state.Settings, "supportTagline"), state.Tenant.BaseCurrencyCode, state.Tenant.DefaultTimezone);

    private async Task<OnlineStoreBrandingResponse> BuildBrandingAsync(OnlineStoreState state, CancellationToken cancellationToken)
    {
        var branding = ReadObject(state.Settings, "branding") ?? [];
        return new OnlineStoreBrandingResponse(ReadGuid(branding, "logoMediaAssetId"), ReadGuid(branding, "faviconMediaAssetId"), ReadString(branding, "primaryColor") ?? "#FF6A00", ReadString(branding, "secondaryColor") ?? "#000000", await GetBannersAsync(state.Tenant.Id, state.Channel.Id, cancellationToken));
    }

    private static OnlineStoreSupportResponse BuildSupport(JsonObject settings)
    {
        var support = ReadObject(settings, "support") ?? [];
        return new OnlineStoreSupportResponse(ReadString(support, "email"), ReadString(support, "phone"), ReadString(support, "whatsapp"), ReadString(support, "helpUrl"), ReadBool(support, "contactUsEnabled") ?? true, ReadString(support, "supportHours"), ReadString(support, "businessAddress"));
    }

    private async Task<IReadOnlyList<OnlineStoreDomainDto>> GetDomainsAsync(Guid channelId, Guid tenantId, CancellationToken cancellationToken) =>
        await _db.TenantDomains.AsNoTracking().Where(x => x.TenantId == tenantId && x.SalesChannelId == channelId && x.Status != Deleted).OrderByDescending(x => x.IsPrimary).ThenBy(x => x.DomainName).Select(x => ToDomainDto(x)).ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<OnlineStoreBannerDto>> GetBannersAsync(Guid tenantId, Guid channelId, CancellationToken cancellationToken) =>
        await (from banner in _db.StorefrontBanners.AsNoTracking()
               join media in _db.MediaAssets.AsNoTracking() on banner.ImageMediaAssetId equals media.Id into medias
               from media in medias.DefaultIfEmpty()
               where banner.TenantId == tenantId && banner.SalesChannelId == channelId && banner.Status != Deleted
               orderby banner.SortOrder, banner.Title
               select new OnlineStoreBannerDto(banner.Id, banner.BannerType, banner.Title, banner.Subtitle, banner.ImageMediaAssetId, media == null ? null : media.PublicUrl, banner.ActionText, banner.ActionUrl, banner.SortOrder, banner.Status)).ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<OnlineStoreCollectionOutletDto>> BuildCollectionOutletsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var method = await EnsurePickupMethodAsync(tenantId, cancellationToken);
        return await (from outlet in _db.Outlets.AsNoTracking()
                      join mapping in _db.FulfillmentMethodOutlets.AsNoTracking().Where(x => x.FulfillmentMethodId == method.Id) on outlet.Id equals mapping.OutletId into mappings
                      from mapping in mappings.DefaultIfEmpty()
                      where outlet.TenantId == tenantId && outlet.Status != Deleted
                      orderby outlet.OutletName
                      select new OnlineStoreCollectionOutletDto(
                          outlet.Id,
                          outlet.OutletName,
                          outlet.Status,
                          _db.OutletBusinessHours.Any(h => h.TenantId == tenantId && h.OutletId == outlet.Id),
                          mapping == null ? null : mapping.PreparationLeadMinutes,
                          mapping == null ? null : mapping.PickupWindowMinutes,
                          mapping == null || mapping.CutoffTime == null ? null : mapping.CutoffTime.Value.ToString("HH:mm"),
                          mapping == null ? Inactive : mapping.Status)).ToListAsync(cancellationToken);
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

    private async Task<ApplicationResult<T>?> RequireAsync<T>(TenantRequestContext context, string permission, string featureCode, CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty || !context.HasPermission(permission))
            return ApplicationResult<T>.Failure(new ApplicationError("online_store.permission_denied", "Permission denied."));
        if (!await _entitlements.IsEnabledAsync(context.TenantId, featureCode, _clock.UtcNow, cancellationToken))
            return ApplicationResult<T>.Failure(new ApplicationError("online_store.entitlement_denied", "Feature entitlement is not enabled."));
        return null;
    }

    private async Task ValidateMediaOwnershipAsync(Guid tenantId, Guid? mediaAssetId, CancellationToken cancellationToken)
    {
        if (mediaAssetId.HasValue && !await _db.MediaAssets.AnyAsync(x => x.TenantId == tenantId && x.Id == mediaAssetId.Value && (x.Status == Active || x.Status == "STAGED"), cancellationToken))
            throw new InvalidOperationException("Media asset does not belong to tenant or is not active.");
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
    private static string? BuildHostedUrl(string? slug) => string.IsNullOrWhiteSpace(slug) ? null : $"https://{slug}.oneverz.shop";
    private static string? NormalizeSlug(string? slug) => string.IsNullOrWhiteSpace(slug) ? null : slug.Trim().ToLowerInvariant() is { } s && s.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ? s : null;
    private static string? NormalizeDomain(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    private static string NormalizeDomainType(string value) => string.IsNullOrWhiteSpace(value) ? "CUSTOM" : value.Trim().ToUpperInvariant();
    private static string NormalizeRecordStatus(string value) => value.Trim().ToUpperInvariant() is Active or Inactive or Deleted ? value.Trim().ToUpperInvariant() : Active;
    private static TimeOnly? ParseTime(string? value) => TimeOnly.TryParse(value, out var time) ? time : null;
    private static bool IsHexColor(string value) => value.Trim().Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
    private static string? NormalizePolicyType(string value) => RequiredPolicyTypes.Contains(value.Trim().ToUpperInvariant()) ? value.Trim().ToUpperInvariant() : null;
    private static OnlineStoreDomainDto ToDomainDto(TenantDomain domain) => new(domain.Id, domain.DomainType, domain.DomainName, domain.IsPrimary, domain.VerificationStatus, domain.VerifiedAt, domain.SslStatus, domain.SslIssuedAt, domain.SslExpiresAt, domain.Status);
    private static OnlineStorePolicyDto ToPolicyDto(StorefrontPolicy policy) => new(policy.Id, policy.PolicyType, policy.Title, policy.Content, policy.Version, policy.Status, policy.PublishedAt);
    private static ApplicationResult<T> NotFound<T>(string code, string message) => ApplicationResult<T>.Failure(new ApplicationError(code, message));
    private static ApplicationResult<T> Failure<T>(string code, string message, IReadOnlyList<ApplicationFieldError>? fields = null) => ApplicationResult<T>.Failure(new ApplicationError(code, message, fields ?? []));

    private static IReadOnlyList<ApplicationFieldError> ValidateIdentity(UpdateOnlineStoreIdentityRequest request)
    {
        var errors = new List<ApplicationFieldError>();
        if (string.IsNullOrWhiteSpace(request.StoreName)) errors.Add(new("storeName", "Store name is required."));
        if (string.IsNullOrWhiteSpace(request.BusinessDisplayName)) errors.Add(new("businessDisplayName", "Business display name is required."));
        return errors;
    }

    private static IReadOnlyList<ApplicationFieldError> ValidateBanner(UpsertOnlineStoreBannerRequest request)
    {
        var errors = new List<ApplicationFieldError>();
        if (request.BannerType.Trim().ToUpperInvariant() is not ("HERO" or "PROMO" or "ANNOUNCEMENT")) errors.Add(new("bannerType", "Banner type must be HERO, PROMO or ANNOUNCEMENT."));
        if (string.IsNullOrWhiteSpace(request.Title)) errors.Add(new("title", "Title is required."));
        return errors;
    }

    private async Task<PreparedImage> PrepareImageAsync(MediaUploadFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaxMediaBytes) return PreparedImage.Invalid("Image file must be between 1 byte and 5 MB.");
        var fileName = Path.GetFileName(file.FileName).Trim();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var mime = file.ContentType.Trim().ToLowerInvariant() == "image/jpg" ? "image/jpeg" : file.ContentType.Trim().ToLowerInvariant();
        if (!((mime == "image/jpeg" && extension is ".jpg" or ".jpeg") || (mime == "image/png" && extension == ".png") || (mime == "image/webp" && extension == ".webp")))
            return PreparedImage.Invalid("Only JPG, JPEG, PNG and WEBP images are allowed.");
        await using var memory = new MemoryStream();
        await file.Content.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        if (!MagicMatches(bytes, mime)) return PreparedImage.Invalid("Image signature does not match MIME type.");
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
        _ => false
    };

    private static string CreateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();

    private sealed record OnlineStoreState(E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant Tenant, SalesChannel Channel, JsonObject Settings);

    private sealed record PreparedImage(string FileName, string Extension, string Mime, byte[] Bytes, int Width, int Height, string Hash, ApplicationError? Error)
    {
        public static PreparedImage Valid(string fileName, string extension, string mime, byte[] bytes, int width, int height, string hash) => new(fileName, extension, mime, bytes, width, height, hash, null);
        public static PreparedImage Invalid(string message) => new(string.Empty, string.Empty, string.Empty, [], 0, 0, string.Empty, new ApplicationError("online_store.media_invalid", message, [new ApplicationFieldError("file", message)]));
    }
}
