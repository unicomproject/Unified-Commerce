using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup.Dtos;

namespace E_POS.Application.Modules.Tenant.OnlineStoreSetup.Contracts;

public interface ITenantAdminOnlineStoreService
{
    Task<ApplicationResult<OnlineStoreOverviewResponse>> GetOverviewAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreReadinessResponse>> GetReadinessAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreActivationResponse>> GetActivationAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreActivationResponse>> UpdateActivationAsync(TenantRequestContext context, UpdateOnlineStoreActivationRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreIdentityResponse>> GetIdentityAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreIdentityResponse>> UpdateIdentityAsync(TenantRequestContext context, UpdateOnlineStoreIdentityRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreUrlDomainResponse>> GetUrlDomainAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreUrlDomainResponse>> UpdateUrlAsync(TenantRequestContext context, UpdateOnlineStoreUrlRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<IReadOnlyList<OnlineStoreDomainDto>>> ListDomainsAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreDomainTokenResponse>> CreateDomainAsync(TenantRequestContext context, CreateOnlineStoreDomainRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreDomainDto>> VerifyDomainAsync(TenantRequestContext context, Guid domainId, VerifyOnlineStoreDomainRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreDomainTokenResponse>> RotateDomainTokenAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreDomainDto>> GetDomainStatusAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreDomainDto>> ProvisionDomainSslAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreDomainDto>> SetPrimaryDomainAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteDomainAsync(TenantRequestContext context, Guid domainId, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreBrandingResponse>> GetBrandingAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreBrandingResponse>> UpdateBrandingAsync(TenantRequestContext context, UpdateOnlineStoreBrandingRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreMediaResponse>> UploadMediaAsync(TenantRequestContext context, string purpose, MediaUploadFile file, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteMediaAsync(TenantRequestContext context, Guid mediaAssetId, CancellationToken cancellationToken);
    Task<ApplicationResult<IReadOnlyList<OnlineStoreBannerDto>>> ListBannersAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreBannerDto>> GetBannerAsync(TenantRequestContext context, Guid bannerId, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreBannerDto>> CreateBannerAsync(TenantRequestContext context, UpsertOnlineStoreBannerRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreBannerDto>> UpdateBannerAsync(TenantRequestContext context, Guid bannerId, UpsertOnlineStoreBannerRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreBannerDto>> UpdateBannerStatusAsync(TenantRequestContext context, Guid bannerId, UpdateOnlineStoreBannerStatusRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<IReadOnlyList<OnlineStoreBannerDto>>> ReorderBannersAsync(TenantRequestContext context, ReorderOnlineStoreBannersRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteBannerAsync(TenantRequestContext context, Guid bannerId, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreSupportResponse>> GetSupportAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreSupportResponse>> UpdateSupportAsync(TenantRequestContext context, UpdateOnlineStoreSupportRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreClickCollectResponse>> GetClickCollectAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreClickCollectResponse>> UpdateClickCollectAsync(TenantRequestContext context, UpdateOnlineStoreClickCollectRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<IReadOnlyList<OnlineStoreCollectionOutletDto>>> ListCollectionOutletsAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreCollectionOutletDto>> UpsertCollectionOutletAsync(TenantRequestContext context, Guid outletId, UpsertCollectionOutletRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteCollectionOutletAsync(TenantRequestContext context, Guid outletId, CancellationToken cancellationToken);
    Task<ApplicationResult<IReadOnlyList<OnlineStoreCollectionOutletDto>>> BulkApplyCollectionOutletsAsync(TenantRequestContext context, BulkApplyCollectionOutletRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreCatalogSummaryResponse>> GetCatalogSummaryAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreCatalogProductListResponse>> ListCatalogProductsAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreCatalogProductDto>> UpdateProductVisibilityAsync(TenantRequestContext context, Guid productId, UpdateProductChannelVisibilityRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStoreCatalogProductDto>> UpdateVariantVisibilityAsync(TenantRequestContext context, Guid productId, Guid variantId, UpdateProductChannelVisibilityRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<IReadOnlyList<OnlineStoreCatalogProductDto>>> BulkUpdateProductVisibilityAsync(TenantRequestContext context, BulkProductChannelVisibilityRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<IReadOnlyList<OnlineStorePolicyDto>>> ListPoliciesAsync(TenantRequestContext context, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStorePolicyDto>> GetPolicyAsync(TenantRequestContext context, string policyType, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStorePolicyDto>> UpsertPolicyAsync(TenantRequestContext context, string policyType, UpsertOnlineStorePolicyRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStorePolicyDto>> PublishPolicyAsync(TenantRequestContext context, string policyType, CancellationToken cancellationToken);
    Task<ApplicationResult<IReadOnlyList<OnlineStorePolicyDto>>> ListPolicyVersionsAsync(TenantRequestContext context, string policyType, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStorePolicyDto>> ArchivePolicyAsync(TenantRequestContext context, string policyType, CancellationToken cancellationToken);
    Task<ApplicationResult<OnlineStorePublishResponse>> PublishAsync(TenantRequestContext context, string idempotencyKey, CancellationToken cancellationToken);
}
