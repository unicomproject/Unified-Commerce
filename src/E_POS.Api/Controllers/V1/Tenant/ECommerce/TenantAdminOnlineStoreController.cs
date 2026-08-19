using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup.Contracts;
using E_POS.Application.Modules.Tenant.OnlineStoreSetup.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.ECommerce;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant-admin/online-store")]
public sealed class TenantAdminOnlineStoreController : ControllerBase
{
    private readonly ITenantAdminOnlineStoreService _service;
    private readonly ITenantRequestContextFactory _contexts;

    public TenantAdminOnlineStoreController(
        ITenantAdminOnlineStoreService service,
        ITenantRequestContextFactory contexts)
    {
        _service = service;
        _contexts = contexts;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken) =>
        await Execute(context => _service.GetOverviewAsync(context, cancellationToken));

    [HttpGet("readiness")]
    public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken) =>
        await Execute(context => _service.GetReadinessAsync(context, cancellationToken));

    [HttpGet("activation")]
    public async Task<IActionResult> GetActivation(CancellationToken cancellationToken) =>
        await Execute(context => _service.GetActivationAsync(context, cancellationToken));

    [HttpPut("activation")]
    public async Task<IActionResult> UpdateActivation([FromBody] UpdateOnlineStoreActivationRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateActivationAsync(context, request, cancellationToken));

    [HttpGet("identity")]
    public async Task<IActionResult> GetIdentity(CancellationToken cancellationToken) =>
        await Execute(context => _service.GetIdentityAsync(context, cancellationToken));

    [HttpPut("identity")]
    public async Task<IActionResult> UpdateIdentity([FromBody] UpdateOnlineStoreIdentityRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateIdentityAsync(context, request, cancellationToken));

    [HttpGet("url-domain")]
    public async Task<IActionResult> GetUrlDomain(CancellationToken cancellationToken) =>
        await Execute(context => _service.GetUrlDomainAsync(context, cancellationToken));

    [HttpPut("url")]
    public async Task<IActionResult> UpdateUrl([FromBody] UpdateOnlineStoreUrlRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateUrlAsync(context, request, cancellationToken));

    [HttpGet("domains")]
    public async Task<IActionResult> ListDomains(CancellationToken cancellationToken) =>
        await Execute(context => _service.ListDomainsAsync(context, cancellationToken));

    [HttpPost("domains")]
    public async Task<IActionResult> CreateDomain([FromBody] CreateOnlineStoreDomainRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.CreateDomainAsync(context, request, cancellationToken));

    [HttpPost("domains/{id:guid}/verify")]
    public async Task<IActionResult> VerifyDomain(Guid id, [FromBody] VerifyOnlineStoreDomainRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.VerifyDomainAsync(context, id, request, cancellationToken));

    [HttpPost("domains/{id:guid}/verification-token/rotate")]
    public async Task<IActionResult> RotateDomainToken(Guid id, CancellationToken cancellationToken) =>
        await Execute(context => _service.RotateDomainTokenAsync(context, id, cancellationToken));

    [HttpGet("domains/{id:guid}/status")]
    public async Task<IActionResult> GetDomainStatus(Guid id, CancellationToken cancellationToken) =>
        await Execute(context => _service.GetDomainStatusAsync(context, id, cancellationToken));

    [HttpPost("domains/{id:guid}/ssl/provision")]
    public async Task<IActionResult> ProvisionSsl(Guid id, CancellationToken cancellationToken) =>
        await Execute(context => _service.ProvisionDomainSslAsync(context, id, cancellationToken));

    [HttpPost("domains/{id:guid}/set-primary")]
    public async Task<IActionResult> SetPrimaryDomain(Guid id, CancellationToken cancellationToken) =>
        await Execute(context => _service.SetPrimaryDomainAsync(context, id, cancellationToken));

    [HttpDelete("domains/{id:guid}")]
    public async Task<IActionResult> DeleteDomain(Guid id, CancellationToken cancellationToken) =>
        await ExecuteEmpty(context => _service.DeleteDomainAsync(context, id, cancellationToken));

    [HttpGet("branding")]
    public async Task<IActionResult> GetBranding(CancellationToken cancellationToken) =>
        await Execute(context => _service.GetBrandingAsync(context, cancellationToken));

    [HttpPut("branding")]
    public async Task<IActionResult> UpdateBranding([FromBody] UpdateOnlineStoreBrandingRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateBrandingAsync(context, request, cancellationToken));

    [HttpPost("media/{purpose}")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMedia(string purpose, [FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest(CreateError(new ApplicationError("online_store.media_invalid", "Image file is required.")));
        await using var stream = file.OpenReadStream();
        return await Execute(context => _service.UploadMediaAsync(context, purpose, new MediaUploadFile(stream, file.FileName, file.ContentType, file.Length), cancellationToken));
    }

    [HttpDelete("media/{id:guid}")]
    public async Task<IActionResult> DeleteMedia(Guid id, CancellationToken cancellationToken) =>
        await ExecuteEmpty(context => _service.DeleteMediaAsync(context, id, cancellationToken));

    [HttpGet("banners")]
    public async Task<IActionResult> ListBanners(CancellationToken cancellationToken) =>
        await Execute(context => _service.ListBannersAsync(context, cancellationToken));

    [HttpPost("banners")]
    public async Task<IActionResult> CreateBanner([FromBody] UpsertOnlineStoreBannerRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.CreateBannerAsync(context, request, cancellationToken));

    [HttpGet("banners/{id:guid}")]
    public async Task<IActionResult> GetBanner(Guid id, CancellationToken cancellationToken) =>
        await Execute(context => _service.GetBannerAsync(context, id, cancellationToken));

    [HttpPut("banners/{id:guid}")]
    public async Task<IActionResult> UpdateBanner(Guid id, [FromBody] UpsertOnlineStoreBannerRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateBannerAsync(context, id, request, cancellationToken));

    [HttpPatch("banners/{id:guid}/status")]
    public async Task<IActionResult> UpdateBannerStatus(Guid id, [FromBody] UpdateOnlineStoreBannerStatusRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateBannerStatusAsync(context, id, request, cancellationToken));

    [HttpPut("banners/order")]
    public async Task<IActionResult> ReorderBanners([FromBody] ReorderOnlineStoreBannersRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.ReorderBannersAsync(context, request, cancellationToken));

    [HttpDelete("banners/{id:guid}")]
    public async Task<IActionResult> DeleteBanner(Guid id, CancellationToken cancellationToken) =>
        await ExecuteEmpty(context => _service.DeleteBannerAsync(context, id, cancellationToken));

    [HttpGet("support")]
    public async Task<IActionResult> GetSupport(CancellationToken cancellationToken) =>
        await Execute(context => _service.GetSupportAsync(context, cancellationToken));

    [HttpPut("support")]
    public async Task<IActionResult> UpdateSupport([FromBody] UpdateOnlineStoreSupportRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateSupportAsync(context, request, cancellationToken));

    [HttpGet("click-collect")]
    public async Task<IActionResult> GetClickCollect(CancellationToken cancellationToken) =>
        await Execute(context => _service.GetClickCollectAsync(context, cancellationToken));

    [HttpPut("click-collect")]
    public async Task<IActionResult> UpdateClickCollect([FromBody] UpdateOnlineStoreClickCollectRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateClickCollectAsync(context, request, cancellationToken));

    [HttpGet("click-collect/outlets")]
    public async Task<IActionResult> ListClickCollectOutlets(CancellationToken cancellationToken) =>
        await Execute(context => _service.ListCollectionOutletsAsync(context, cancellationToken));

    [HttpPost("click-collect/outlets")]
    public async Task<IActionResult> AddClickCollectOutlet([FromBody] BulkApplyCollectionOutletRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.BulkApplyCollectionOutletsAsync(context, request, cancellationToken));

    [HttpPut("click-collect/outlets/{outletId:guid}")]
    public async Task<IActionResult> UpsertClickCollectOutlet(Guid outletId, [FromBody] UpsertCollectionOutletRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpsertCollectionOutletAsync(context, outletId, request, cancellationToken));

    [HttpDelete("click-collect/outlets/{outletId:guid}")]
    public async Task<IActionResult> DeleteClickCollectOutlet(Guid outletId, CancellationToken cancellationToken) =>
        await ExecuteEmpty(context => _service.DeleteCollectionOutletAsync(context, outletId, cancellationToken));

    [HttpPost("click-collect/outlets/bulk-apply")]
    public async Task<IActionResult> BulkApplyClickCollect([FromBody] BulkApplyCollectionOutletRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.BulkApplyCollectionOutletsAsync(context, request, cancellationToken));

    [HttpGet("catalog/summary")]
    public async Task<IActionResult> GetCatalogSummary(CancellationToken cancellationToken) =>
        await Execute(context => _service.GetCatalogSummaryAsync(context, cancellationToken));

    [HttpGet("catalog/products")]
    public async Task<IActionResult> ListCatalogProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken cancellationToken = default) =>
        await Execute(context => _service.ListCatalogProductsAsync(context, pageNumber, pageSize, search, cancellationToken));

    [HttpPatch("catalog/products/{id:guid}/visibility")]
    public async Task<IActionResult> UpdateProductVisibility(Guid id, [FromBody] UpdateProductChannelVisibilityRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateProductVisibilityAsync(context, id, request, cancellationToken));

    [HttpPatch("catalog/products/{id:guid}/variants/{variantId:guid}/visibility")]
    public async Task<IActionResult> UpdateVariantVisibility(Guid id, Guid variantId, [FromBody] UpdateProductChannelVisibilityRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpdateVariantVisibilityAsync(context, id, variantId, request, cancellationToken));

    [HttpPost("catalog/products/bulk-visibility")]
    public async Task<IActionResult> BulkVisibility([FromBody] BulkProductChannelVisibilityRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.BulkUpdateProductVisibilityAsync(context, request, cancellationToken));

    [HttpGet("policies")]
    public async Task<IActionResult> ListPolicies(CancellationToken cancellationToken) =>
        await Execute(context => _service.ListPoliciesAsync(context, cancellationToken));

    [HttpGet("policies/{type}")]
    public async Task<IActionResult> GetPolicy(string type, CancellationToken cancellationToken) =>
        await Execute(context => _service.GetPolicyAsync(context, type, cancellationToken));

    [HttpPut("policies/{type}")]
    public async Task<IActionResult> UpsertPolicy(string type, [FromBody] UpsertOnlineStorePolicyRequest request, CancellationToken cancellationToken) =>
        await Execute(context => _service.UpsertPolicyAsync(context, type, request, cancellationToken));

    [HttpPost("policies/{type}/publish")]
    public async Task<IActionResult> PublishPolicy(string type, CancellationToken cancellationToken) =>
        await Execute(context => _service.PublishPolicyAsync(context, type, cancellationToken));

    [HttpGet("policies/{type}/versions")]
    public async Task<IActionResult> PolicyVersions(string type, CancellationToken cancellationToken) =>
        await Execute(context => _service.ListPolicyVersionsAsync(context, type, cancellationToken));

    [HttpPost("policies/{type}/archive")]
    public async Task<IActionResult> ArchivePolicy(string type, CancellationToken cancellationToken) =>
        await Execute(context => _service.ArchivePolicyAsync(context, type, cancellationToken));

    [HttpPost("publish")]
    public async Task<IActionResult> Publish(CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? string.Empty;
        return await Execute(context => _service.PublishAsync(context, idempotencyKey, cancellationToken));
    }

    private async Task<IActionResult> Execute<T>(Func<TenantRequestContext, Task<ApplicationResult<T>>> action)
    {
        if (!_contexts.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("online_store.invalid_tenant_context", "Invalid tenant context.")));
        var result = await action(context);
        return result.IsSuccess && result.Value is not null ? Ok(new { data = result.Value }) : ToErrorResult(result.Error);
    }

    private async Task<IActionResult> ExecuteEmpty(Func<TenantRequestContext, Task<ApplicationResult>> action)
    {
        if (!_contexts.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("online_store.invalid_tenant_context", "Invalid tenant context.")));
        var result = await action(context);
        return result.IsSuccess ? Ok(new { data = new { success = true } }) : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        var body = CreateError(error);
        if (error.Code.Contains("permission_denied", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("entitlement_denied", StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden, body);
        if (error.Code.Contains("not_found", StringComparison.OrdinalIgnoreCase))
            return NotFound(body);
        if (error.Code.Contains("conflict", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("idempotency_in_progress", StringComparison.OrdinalIgnoreCase))
            return Conflict(body);
        if (error.Code.Contains("storage_unavailable", StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, body);
        if (error.Code.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("blocked", StringComparison.OrdinalIgnoreCase))
            return UnprocessableEntity(body);
        return BadRequest(body);
    }

    private object CreateError(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        errors = error.FieldErrors,
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };
}
