using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class BrandService : IBrandService
{
    private static readonly ApplicationError PermissionDenied = new("brand.permission_denied", "Permission denied for brand management.");
    private static readonly ApplicationError NotFound = new("brand.not_found", "Brand was not found.");
    private readonly IBrandRepository _repository;
    private readonly IBrandRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BrandService(IBrandRepository repository, IBrandRequestValidator validator, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<BrandResponse>> CreateAsync(TenantRequestContext context, BrandCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, BrandConstants.CreatePermission);
        if (accessError is not null) return ApplicationResult<BrandResponse>.Failure(accessError);

        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<BrandResponse>.Failure(validationError);

        var normalizedCode = BrandConstants.NormalizeCode(request.BrandCode);
        if (await _repository.BrandCodeExistsAsync(context.TenantId, normalizedCode, null, cancellationToken))
        {
            return ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.duplicate_code", "Brand code already exists."));
        }

        var brandId = Guid.NewGuid();
        var slug = string.IsNullOrWhiteSpace(request.BrandSlug)
            ? normalizedCode.ToLowerInvariant()
            : request.BrandSlug.Trim().ToLowerInvariant();

        var now = _dateTimeProvider.UtcNow;
        var requestedLogoUrl = NormalizeLegacyMediaUrl(request.LogoUrl);
        var mediaAsset = LegacyMediaAssetFactory.CreateImageFromUrl(
            context.TenantId,
            brandId,
            "brands",
            "BRAND_LOGO",
            requestedLogoUrl,
            context.UserId,
            now);

        var brand = Brand.Create(
            brandId, 
            context.TenantId, 
            normalizedCode, 
            request.Name, 
            slug,
            request.Description,
            requestedLogoUrl,
            request.Status,
            context.UserId,
            now);

        if (mediaAsset is not null)
        {
            brand.UpdateLogo(mediaAsset.PublicUrl, mediaAsset.Id, context.UserId, now);
            await _repository.AddMediaAssetAsync(mediaAsset, cancellationToken);
        }

        await _repository.AddAsync(brand, cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, brandId, false, cancellationToken);
        return ApplicationResult<BrandResponse>.Success(response!);
    }

    public async Task<ApplicationResult<BrandListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, BrandConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<BrandListResponse>.Failure(accessError);

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(context.TenantId, safePageNumber, safePageSize, search, cancellationToken);
        return ApplicationResult<BrandListResponse>.Success(response);
    }

    public async Task<ApplicationResult<BrandResponse>> GetByIdAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, BrandConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<BrandResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, brandId, false, cancellationToken);
        return response is null ? ApplicationResult<BrandResponse>.Failure(NotFound) : ApplicationResult<BrandResponse>.Success(response);
    }

    public async Task<ApplicationResult<BrandResponse>> UpdateAsync(TenantRequestContext context, Guid brandId, BrandUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, BrandConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<BrandResponse>.Failure(accessError);

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<BrandResponse>.Failure(validationError);

        var brand = await _repository.GetEditableAsync(context.TenantId, brandId, cancellationToken);
        if (brand is null) return ApplicationResult<BrandResponse>.Failure(NotFound);

        var normalizedCode = BrandConstants.NormalizeCode(request.BrandCode);
        if (await _repository.BrandCodeExistsAsync(context.TenantId, normalizedCode, brandId, cancellationToken))
        {
            return ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.duplicate_code", "Brand code already exists."));
        }

        var slug = string.IsNullOrWhiteSpace(request.BrandSlug)
            ? normalizedCode.ToLowerInvariant()
            : request.BrandSlug.Trim().ToLowerInvariant();

        var now = _dateTimeProvider.UtcNow;
        var requestedLogoUrl = NormalizeLegacyMediaUrl(request.LogoUrl);
        var previousMediaAssetId = brand.LogoMediaAssetId;
        var shouldClearMedia = request.LogoUrl is not null && string.IsNullOrWhiteSpace(request.LogoUrl);
        var shouldReplaceMedia = !shouldClearMedia &&
            requestedLogoUrl is not null &&
            (!previousMediaAssetId.HasValue ||
             !string.Equals(brand.LogoUrl?.Trim(), requestedLogoUrl, StringComparison.Ordinal));

        brand.UpdateProfile(
            normalizedCode, 
            request.Name, 
            slug,
            request.Description,
            shouldClearMedia ? null : requestedLogoUrl ?? brand.LogoUrl,
            request.Status,
            context.UserId,
            now);

        if (shouldClearMedia)
        {
            brand.UpdateLogo(null, null, context.UserId, now);

            if (previousMediaAssetId.HasValue)
            {
                await _repository.MarkMediaAssetInactiveAsync(
                    context.TenantId,
                    previousMediaAssetId.Value,
                    context.UserId,
                    now,
                    cancellationToken);
            }
        }
        else if (shouldReplaceMedia)
        {
            var mediaAsset = LegacyMediaAssetFactory.CreateImageFromUrl(
                context.TenantId,
                brandId,
                "brands",
                "BRAND_LOGO",
                requestedLogoUrl,
                context.UserId,
                now);

            if (mediaAsset is not null)
            {
                brand.UpdateLogo(mediaAsset.PublicUrl, mediaAsset.Id, context.UserId, now);
                await _repository.AddMediaAssetAsync(mediaAsset, cancellationToken);

                if (previousMediaAssetId.HasValue)
                {
                    await _repository.MarkMediaAssetInactiveAsync(
                        context.TenantId,
                        previousMediaAssetId.Value,
                        context.UserId,
                        now,
                        cancellationToken);
                }
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, brandId, false, cancellationToken);
        return response is null ? ApplicationResult<BrandResponse>.Failure(NotFound) : ApplicationResult<BrandResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, BrandConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        var brand = await _repository.GetEditableAsync(context.TenantId, brandId, cancellationToken);
        if (brand is null) return ApplicationResult.Failure(NotFound);

        var now = _dateTimeProvider.UtcNow;
        var mediaAssetId = brand.LogoMediaAssetId;

        brand.SoftDelete(context.UserId, now);

        if (mediaAssetId.HasValue)
        {
            await _repository.MarkMediaAssetInactiveAsync(
                context.TenantId,
                mediaAssetId.Value,
                context.UserId,
                now,
                cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private static string? NormalizeLegacyMediaUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("brand.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(BrandConstants.ManagePermission) ? null : PermissionDenied;
    }
}

