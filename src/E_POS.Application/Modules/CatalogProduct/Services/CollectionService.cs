using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;

namespace E_POS.Application.Modules.CatalogProduct.Services;

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
        if (await _repository.CollectionCodeExistsAsync(context.TenantId, normalizedCode, null, cancellationToken))
        {
            return ApplicationResult<CollectionResponse>.Failure(new ApplicationError("collection.duplicate_code", "Collection code already exists."));
        }

        var collectionId = Guid.NewGuid();
        var collection = Collection.Create(collectionId, context.TenantId, normalizedCode, request.Name, request.Status, _dateTimeProvider.UtcNow);
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
        if (await _repository.CollectionCodeExistsAsync(context.TenantId, normalizedCode, collectionId, cancellationToken))
        {
            return ApplicationResult<CollectionResponse>.Failure(new ApplicationError("collection.duplicate_code", "Collection code already exists."));
        }

        collection.UpdateProfile(normalizedCode, request.Name, request.Status, _dateTimeProvider.UtcNow);
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

        if (await _repository.HasProductLinksAsync(context.TenantId, collectionId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError("collection.delete_conflict", "Collection cannot be deleted while products are linked."));
        }

        collection.SoftDelete(_dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
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