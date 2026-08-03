using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class CollectionService : ICollectionService
{
    private static readonly ApplicationError PermissionDenied = new("collection.permission_denied", "Permission denied for collection management.");
    private static readonly ApplicationError NotFound = new("collection.not_found", "Collection was not found.");
    private readonly ICollectionRepository _repository;
    private readonly ICollectionRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CollectionService(ICollectionRepository repository, ICollectionRequestValidator validator, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<CollectionResponse>> CreateAsync(TenantRequestContext context, CollectionCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CollectionConstants.CreatePermission);
        if (accessError is not null) return ApplicationResult<CollectionResponse>.Failure(accessError);

        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<CollectionResponse>.Failure(validationError);

        var normalizedCode = CollectionConstants.NormalizeCode(request.CollectionCode);
        if (normalizedCode == CollectionConstants.PopularCollectionCode)
        {
            return ApplicationResult<CollectionResponse>.Failure(new ApplicationError("collection.reserved_code", "The collection code POS_POPULAR is reserved and cannot be created manually."));
        }

        if (await _repository.CollectionCodeExistsAsync(context.TenantId, normalizedCode, null, cancellationToken))
        {
            return ApplicationResult<CollectionResponse>.Failure(new ApplicationError("collection.duplicate_code", "Collection code already exists."));
        }

        var collectionId = Guid.NewGuid();
        var slug = string.IsNullOrWhiteSpace(request.CollectionSlug)
            ? normalizedCode.ToLowerInvariant()
            : request.CollectionSlug.Trim().ToLowerInvariant();

        var collection = Collection.Create(
            collectionId, 
            context.TenantId, 
            normalizedCode, 
            request.Name, 
            slug,
            request.Description,
            request.CollectionType,
            request.StartsAt,
            request.EndsAt,
            request.SortOrder,
            request.Status,
            context.UserId,
            _dateTimeProvider.UtcNow);

        await _repository.AddAsync(collection, cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, collectionId, false, cancellationToken);
        return ApplicationResult<CollectionResponse>.Success(response!);
    }

    public async Task<ApplicationResult<CollectionListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CollectionConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<CollectionListResponse>.Failure(accessError);

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(context.TenantId, safePageNumber, safePageSize, search, cancellationToken);
        return ApplicationResult<CollectionListResponse>.Success(response);
    }

    public async Task<ApplicationResult<CollectionResponse>> GetByIdAsync(TenantRequestContext context, Guid collectionId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CollectionConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<CollectionResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, collectionId, false, cancellationToken);
        return response is null ? ApplicationResult<CollectionResponse>.Failure(NotFound) : ApplicationResult<CollectionResponse>.Success(response);
    }

    public async Task<ApplicationResult<CollectionResponse>> UpdateAsync(TenantRequestContext context, Guid collectionId, CollectionUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CollectionConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<CollectionResponse>.Failure(accessError);

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<CollectionResponse>.Failure(validationError);

        var collection = await _repository.GetEditableAsync(context.TenantId, collectionId, cancellationToken);
        if (collection is null) return ApplicationResult<CollectionResponse>.Failure(NotFound);

        var normalizedCode = CollectionConstants.NormalizeCode(request.CollectionCode);
        if (collection.CollectionCode == CollectionConstants.PopularCollectionCode)
        {
            if (normalizedCode != CollectionConstants.PopularCollectionCode || request.CollectionType != CollectionConstants.PopularCollectionType || request.Status != CollectionConstants.ActiveStatus)
            {
                return ApplicationResult<CollectionResponse>.Failure(new ApplicationError("collection.reserved_modification_denied", "The code, type, or active status of the reserved POS_POPULAR collection cannot be modified."));
            }
        }
        else if (normalizedCode == CollectionConstants.PopularCollectionCode)
        {
            return ApplicationResult<CollectionResponse>.Failure(new ApplicationError("collection.reserved_code", "The collection code POS_POPULAR is reserved and cannot be assigned to another collection."));
        }

        if (await _repository.CollectionCodeExistsAsync(context.TenantId, normalizedCode, collectionId, cancellationToken))
        {
            return ApplicationResult<CollectionResponse>.Failure(new ApplicationError("collection.duplicate_code", "Collection code already exists."));
        }

        var slug = string.IsNullOrWhiteSpace(request.CollectionSlug)
            ? normalizedCode.ToLowerInvariant()
            : request.CollectionSlug.Trim().ToLowerInvariant();

        collection.UpdateProfile(
            normalizedCode, 
            request.Name, 
            slug,
            request.Description,
            request.CollectionType,
            request.StartsAt,
            request.EndsAt,
            request.SortOrder,
            request.Status,
            context.UserId,
            _dateTimeProvider.UtcNow);

        await _repository.SaveChangesAsync(cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, collectionId, false, cancellationToken);
        return response is null ? ApplicationResult<CollectionResponse>.Failure(NotFound) : ApplicationResult<CollectionResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid collectionId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CollectionConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        var collection = await _repository.GetEditableAsync(context.TenantId, collectionId, cancellationToken);
        if (collection is null) return ApplicationResult.Failure(NotFound);

        if (collection.CollectionCode == CollectionConstants.PopularCollectionCode)
        {
            return ApplicationResult.Failure(new ApplicationError("collection.cannot_delete_reserved_collection", "The reserved POS_POPULAR collection cannot be deleted."));
        }

        if (await _repository.HasProductLinksAsync(context.TenantId, collectionId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError("collection.delete_conflict", "Collection cannot be deleted while products are linked."));
        }

        collection.SoftDelete(context.UserId, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<IReadOnlyList<CollectionProductResponseDto>>> GetPopularProductsAsync(TenantRequestContext context, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CollectionConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<IReadOnlyList<CollectionProductResponseDto>>.Failure(accessError);

        var collection = await _repository.GetByCodeAsync(context.TenantId, CollectionConstants.PopularCollectionCode, cancellationToken);
        if (collection is null)
        {
            var collectionId = Guid.NewGuid();
            collection = Collection.Create(
                collectionId,
                context.TenantId,
                CollectionConstants.PopularCollectionCode,
                "Popular Products",
                "pos-popular",
                "Manually curated popular products for the POS screen.",
                CollectionConstants.PopularCollectionType,
                null,
                null,
                0,
                CollectionConstants.ActiveStatus,
                context.UserId,
                _dateTimeProvider.UtcNow);
            await _repository.AddAsync(collection, cancellationToken);
        }

        var products = await _repository.GetCollectionProductsAsync(context.TenantId, collection.Id, cancellationToken);
        return ApplicationResult<IReadOnlyList<CollectionProductResponseDto>>.Success(products);
    }

    public async Task<ApplicationResult<IReadOnlyList<CollectionProductResponseDto>>> ReplacePopularProductsAsync(TenantRequestContext context, List<Guid> productIds, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CollectionConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<IReadOnlyList<CollectionProductResponseDto>>.Failure(accessError);

        if (productIds == null)
        {
            return ApplicationResult<IReadOnlyList<CollectionProductResponseDto>>.Failure(new ApplicationError("collection.invalid_request", "Product list cannot be null."));
        }

        if (productIds.Distinct().Count() != productIds.Count)
        {
            return ApplicationResult<IReadOnlyList<CollectionProductResponseDto>>.Failure(new ApplicationError("collection.duplicate_product_ids", "Duplicate product IDs are not allowed in the assignment list."));
        }

        var collection = await _repository.GetByCodeAsync(context.TenantId, CollectionConstants.PopularCollectionCode, cancellationToken);
        if (collection is null)
        {
            var collectionId = Guid.NewGuid();
            collection = Collection.Create(
                collectionId,
                context.TenantId,
                CollectionConstants.PopularCollectionCode,
                "Popular Products",
                "pos-popular",
                "Manually curated popular products for the POS screen.",
                CollectionConstants.PopularCollectionType,
                null,
                null,
                0,
                CollectionConstants.ActiveStatus,
                context.UserId,
                _dateTimeProvider.UtcNow);
            await _repository.AddAsync(collection, cancellationToken);
        }

        if (productIds.Count > 0)
        {
            var valid = await _repository.AllProductsExistAndNotDeletedAsync(context.TenantId, productIds, cancellationToken);
            if (!valid)
            {
                return ApplicationResult<IReadOnlyList<CollectionProductResponseDto>>.Failure(new ApplicationError("collection.invalid_product_ids", "One or more product IDs are invalid, belong to a different tenant, or are deleted."));
            }
        }

        await _repository.ReplaceCollectionProductsAsync(context.TenantId, collection.Id, productIds, context.UserId, _dateTimeProvider.UtcNow, cancellationToken);

        var products = await _repository.GetCollectionProductsAsync(context.TenantId, collection.Id, cancellationToken);
        return ApplicationResult<IReadOnlyList<CollectionProductResponseDto>>.Success(products);
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("collection.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(CollectionConstants.ManagePermission) ? null : PermissionDenied;
    }
}


