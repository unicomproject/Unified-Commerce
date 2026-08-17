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
    private readonly IBrandAuditLogger? _auditLogger;

    public BrandService(IBrandRepository repository, IBrandRequestValidator validator, IDateTimeProvider dateTimeProvider, IBrandAuditLogger? auditLogger = null)
    {
        _repository = repository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
        _auditLogger = auditLogger;
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
            return ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.code_conflict", "Brand code already exists.", [new ApplicationFieldError("brandCode", "Brand code already exists.")]));
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
            request.Status,
            context.UserId,
            now,
            request.SortOrder);

        if (mediaAsset is not null)
        {
            brand.UpdateLogo(mediaAsset.Id, context.UserId, now);
            await _repository.AddMediaAssetAsync(mediaAsset, cancellationToken);
        }

        try
        {
            await _repository.AddAsync(brand, cancellationToken);
        }
        catch (BrandPersistenceException ex)
        {
            return ApplicationResult<BrandResponse>.Failure(PersistenceConflict(ex.ErrorCode));
        }
        _auditLogger?.LogMutation("BrandCreated", context.TenantId, context.UserId, brandId, brand.RowVersion);
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

    public async Task<ApplicationResult<BrandResponse>> GetByIdAfterMutationAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, BrandConstants.UpdatePermission);
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

        if (request.ExpectedRowVersion != brand.RowVersion)
        {
            return ApplicationResult<BrandResponse>.Failure(PersistenceConflict("brand.concurrency_conflict"));
        }

        var normalizedCode = BrandConstants.NormalizeCode(request.BrandCode);
        if (await _repository.BrandCodeExistsAsync(context.TenantId, normalizedCode, brandId, cancellationToken))
        {
            return ApplicationResult<BrandResponse>.Failure(new ApplicationError("brand.code_conflict", "Brand code already exists.", [new ApplicationFieldError("brandCode", "Brand code already exists.")]));
        }

        var slug = string.IsNullOrWhiteSpace(request.BrandSlug)
            ? normalizedCode.ToLowerInvariant()
            : request.BrandSlug.Trim().ToLowerInvariant();

        var now = _dateTimeProvider.UtcNow;
        var previousStatus = brand.Status;
        var requestedLogoUrl = NormalizeLegacyMediaUrl(request.LogoUrl);
        var previousMediaAssetId = brand.LogoMediaAssetId;
        var shouldClearMedia = request.LogoUrl is not null && string.IsNullOrWhiteSpace(request.LogoUrl);
        var shouldReplaceMedia = !shouldClearMedia && requestedLogoUrl is not null;

        brand.UpdateProfile(
            normalizedCode, 
            request.Name, 
            slug,
            request.Description,
            request.Status,
            context.UserId,
            now,
            request.SortOrder);

        if (shouldClearMedia)
        {
            brand.UpdateLogo(null, context.UserId, now);

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
                brand.UpdateLogo(mediaAsset.Id, context.UserId, now);
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

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (BrandPersistenceException ex)
        {
            return ApplicationResult<BrandResponse>.Failure(PersistenceConflict(ex.ErrorCode));
        }
        _auditLogger?.LogMutation("BrandUpdated", context.TenantId, context.UserId, brandId, brand.RowVersion);
        if (!string.Equals(previousStatus, brand.Status, StringComparison.Ordinal))
        {
            _auditLogger?.LogMutation("BrandStatusChanged", context.TenantId, context.UserId, brandId, brand.RowVersion);
        }
        var response = await _repository.GetByIdAsync(context.TenantId, brandId, false, cancellationToken);
        return response is null ? ApplicationResult<BrandResponse>.Failure(NotFound) : ApplicationResult<BrandResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid brandId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, BrandConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        var brand = await _repository.GetEditableAsync(context.TenantId, brandId, cancellationToken);
        if (brand is null) return ApplicationResult.Failure(NotFound);

        if (await _repository.HasProductLinksAsync(context.TenantId, brandId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError("brand.delete_conflict", "Brand cannot be deleted while active products reference it."));
        }

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

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (BrandPersistenceException ex)
        {
            return ApplicationResult.Failure(PersistenceConflict(ex.ErrorCode));
        }
        _auditLogger?.LogMutation("BrandDeleted", context.TenantId, context.UserId, brandId, brand.RowVersion);
        return ApplicationResult.Success();
    }

    private static string? NormalizeLegacyMediaUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
    private static ApplicationError PersistenceConflict(string code)
    {
        return code switch
        {
            "brand.code_conflict" => new ApplicationError(code, "Brand code already exists.", [new ApplicationFieldError("brandCode", "Brand code already exists.")]),
            "brand.slug_conflict" => new ApplicationError(code, "The server-managed Brand slug conflicts with another Brand.", [new ApplicationFieldError("brandCode", "Brand code produces a conflicting Brand slug.")]),
            _ => new ApplicationError("brand.concurrency_conflict", "The Brand was changed by another request. Reload and try again.")
        };
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

