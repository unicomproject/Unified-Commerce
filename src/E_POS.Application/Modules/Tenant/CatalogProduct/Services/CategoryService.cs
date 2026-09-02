using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class CategoryService : ICategoryService
{
    private static readonly ApplicationError NotFound = new("category.not_found", "Category was not found.");

    private readonly ICategoryRepository _repository;
    private readonly ICategoryRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICategoryAuditLogger _auditLogger;
    private readonly CategoryAccessPolicy _accessPolicy;

    public CategoryService(
        ICategoryRepository repository,
        ICategoryRequestValidator validator,
        IDateTimeProvider dateTimeProvider,
        ITenantFeatureEntitlementEvaluator featureEntitlementEvaluator,
        ICategoryAuditLogger auditLogger,
        CategoryAccessPolicy? accessPolicy = null)
    {
        _repository = repository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
        _auditLogger = auditLogger;
        _accessPolicy = accessPolicy ?? new CategoryAccessPolicy(
            repository,
            featureEntitlementEvaluator,
            dateTimeProvider);
    }

    public async Task<ApplicationResult<CategoryResponse>> CreateAsync(
        TenantRequestContext context,
        CategoryCreateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(context, CategoryConstants.CreatePermission, cancellationToken);
        if (accessError is not null) return ApplicationResult<CategoryResponse>.Failure(accessError);

        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<CategoryResponse>.Failure(validationError);

        var parentError = await ValidateParentAsync(context.TenantId, null, request.ParentCategoryId, cancellationToken);
        if (parentError is not null) return ApplicationResult<CategoryResponse>.Failure(parentError);

        var normalizedCode = CategoryConstants.NormalizeCode(request.CategoryCode);
        if (await _repository.CategoryCodeExistsAsync(context.TenantId, normalizedCode, null, cancellationToken))
        {
            return ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.duplicate_code", "Category code already exists."));
        }

        var normalizedName = CategoryConstants.NormalizeName(request.Name);
        if (await _repository.CategoryNameExistsAsync(context.TenantId, normalizedName, null, cancellationToken))
        {
            return ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.duplicate_name", "Category name already exists."));
        }

        var slug = string.IsNullOrWhiteSpace(request.CategorySlug)
            ? normalizedCode.ToLowerInvariant()
            : CategoryConstants.NormalizeSlug(request.CategorySlug);
        if (slug.Length > CategoryConstants.MaxSlugLength)
        {
            return ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.validation_failed", $"Category slug must be {CategoryConstants.MaxSlugLength} characters or less."));
        }

        if (await _repository.CategorySlugExistsAsync(context.TenantId, slug, null, cancellationToken))
        {
            return ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.validation_failed", "Category slug already exists."));
        }

        var categoryId = Guid.NewGuid();
        var now = _dateTimeProvider.UtcNow;
        var category = Category.Create(
            categoryId,
            context.TenantId,
            request.ParentCategoryId,
            normalizedCode,
            request.Name,
            slug,
            request.Description,
            request.SortOrder,
            request.Status,
            context.UserId,
            now);

        await _repository.AddAsync(category, cancellationToken);
        _auditLogger.LogCreated(context.TenantId, context.UserId, categoryId, normalizedCode, category.Status);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetByIdAsync(context.TenantId, categoryId, false, cancellationToken);
        return ApplicationResult<CategoryResponse>.Success(response!);
    }

    public async Task<ApplicationResult<CategoryListResponse>> ListAsync(
        TenantRequestContext context,
        CategoryListQuery query,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(context, CategoryConstants.ViewPermission, cancellationToken);
        if (accessError is not null) return ApplicationResult<CategoryListResponse>.Failure(accessError);

        var validationError = _validator.ValidateList(query);
        if (validationError is not null) return ApplicationResult<CategoryListResponse>.Failure(validationError);

        var safeQuery = query with
        {
            PageNumber = Math.Max(1, query.PageNumber),
            PageSize = Math.Clamp(query.PageSize, 1, CategoryConstants.MaxPageSize)
        };

        var response = await _repository.ListAsync(context.TenantId, safeQuery, cancellationToken);
        return ApplicationResult<CategoryListResponse>.Success(response);
    }

    public async Task<ApplicationResult<CategoryTreeResponse>> GetTreeAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(context, CategoryConstants.ViewPermission, cancellationToken);
        if (accessError is not null) return ApplicationResult<CategoryTreeResponse>.Failure(accessError);

        var response = await _repository.GetTreeAsync(context.TenantId, cancellationToken);
        return ApplicationResult<CategoryTreeResponse>.Success(response);
    }

    public async Task<ApplicationResult<CategoryResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(context, CategoryConstants.ViewPermission, cancellationToken);
        if (accessError is not null) return ApplicationResult<CategoryResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, categoryId, false, cancellationToken);
        return response is null
            ? ApplicationResult<CategoryResponse>.Failure(NotFound)
            : ApplicationResult<CategoryResponse>.Success(response);
    }

    public async Task<ApplicationResult<CategoryResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid categoryId,
        CategoryUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(context, CategoryConstants.UpdatePermission, cancellationToken);
        if (accessError is not null) return ApplicationResult<CategoryResponse>.Failure(accessError);

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<CategoryResponse>.Failure(validationError);

        var category = await _repository.GetEditableAsync(context.TenantId, categoryId, cancellationToken);
        if (category is null) return ApplicationResult<CategoryResponse>.Failure(NotFound);

        var parentChanged = category.ParentCategoryId != request.ParentCategoryId;
        if (parentChanged)
        {
            var parentError = await ValidateParentAsync(context.TenantId, categoryId, request.ParentCategoryId, cancellationToken);
            if (parentError is not null) return ApplicationResult<CategoryResponse>.Failure(parentError);
        }

        var normalizedCode = CategoryConstants.NormalizeCode(request.CategoryCode);
        if (await _repository.CategoryCodeExistsAsync(context.TenantId, normalizedCode, categoryId, cancellationToken))
        {
            return ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.duplicate_code", "Category code already exists."));
        }

        var normalizedName = CategoryConstants.NormalizeName(request.Name);
        if (await _repository.CategoryNameExistsAsync(context.TenantId, normalizedName, categoryId, cancellationToken))
        {
            return ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.duplicate_name", "Category name already exists."));
        }

        var slug = string.IsNullOrWhiteSpace(request.CategorySlug)
            ? normalizedCode.ToLowerInvariant()
            : CategoryConstants.NormalizeSlug(request.CategorySlug);
        if (slug.Length > CategoryConstants.MaxSlugLength)
        {
            return ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.validation_failed", $"Category slug must be {CategoryConstants.MaxSlugLength} characters or less."));
        }

        if (await _repository.CategorySlugExistsAsync(context.TenantId, slug, categoryId, cancellationToken))
        {
            return ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.validation_failed", "Category slug already exists."));
        }

        var now = _dateTimeProvider.UtcNow;
        var previousStatus = category.Status;

        category.UpdateProfile(
            request.ParentCategoryId,
            normalizedCode,
            request.Name,
            slug,
            request.Description,
            request.SortOrder,
            request.Status,
            context.UserId,
            now);

        var statusChanged = !string.Equals(previousStatus, category.Status, StringComparison.OrdinalIgnoreCase);
        _auditLogger.LogUpdated(context.TenantId, context.UserId, categoryId, normalizedCode, category.Status, parentChanged, statusChanged);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetByIdAsync(context.TenantId, categoryId, false, cancellationToken);
        return response is null
            ? ApplicationResult<CategoryResponse>.Failure(NotFound)
            : ApplicationResult<CategoryResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var accessError = await ValidateAccessAsync(context, CategoryConstants.DeletePermission, cancellationToken);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        var category = await _repository.GetEditableAsync(context.TenantId, categoryId, cancellationToken);
        if (category is null) return ApplicationResult.Failure(NotFound);

        if (await _repository.HasChildCategoriesAsync(context.TenantId, categoryId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError("category.delete_conflict", "Category cannot be deleted while child categories exist."));
        }

        if (await _repository.HasProductLinksAsync(context.TenantId, categoryId, cancellationToken))
        {
            return ApplicationResult.Failure(new ApplicationError("category.delete_conflict", "Category cannot be deleted while products are linked."));
        }

        var now = _dateTimeProvider.UtcNow;
        var mediaAssetId = category.ImageMediaAssetId;
        var categoryCode = category.CategoryCode;

        category.SoftDelete(context.UserId, now);

        if (mediaAssetId.HasValue)
        {
            await _repository.MarkMediaAssetInactiveAsync(
                context.TenantId,
                mediaAssetId.Value,
                context.UserId,
                now,
                cancellationToken);
        }

        _auditLogger.LogArchived(context.TenantId, context.UserId, categoryId, categoryCode);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationError?> ValidateParentAsync(
        Guid tenantId,
        Guid? categoryId,
        Guid? parentCategoryId,
        CancellationToken cancellationToken)
    {
        if (!parentCategoryId.HasValue)
        {
            if (categoryId.HasValue)
            {
                var rootMoveSubtreeDepth = await _repository.GetSubtreeRelativeDepthAsync(
                    tenantId,
                    categoryId.Value,
                    cancellationToken);
                if (CategoryHierarchy.WouldExceedMaxDepth(0, rootMoveSubtreeDepth))
                {
                    return new ApplicationError("category.max_depth_exceeded", "Category hierarchy cannot exceed 5 levels.");
                }
            }

            return null;
        }

        if (categoryId.HasValue && parentCategoryId.Value == categoryId.Value)
        {
            return new ApplicationError("category.parent_self_reference", "Category cannot be its own parent.");
        }

        var parent = await _repository.GetParentInfoAsync(tenantId, parentCategoryId.Value, cancellationToken);
        if (parent is null)
        {
            return new ApplicationError("category.parent_not_found", "Parent category was not found.");
        }

        if (!string.Equals(parent.Status, CategoryConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return new ApplicationError("category.parent_inactive", "Parent category must be ACTIVE.");
        }

        if (categoryId.HasValue &&
            await _repository.WouldCreateParentCycleAsync(tenantId, categoryId.Value, parentCategoryId.Value, cancellationToken))
        {
            return new ApplicationError("category.parent_cycle", "Category parent would create a cycle.");
        }

        var subtreeRelativeDepth = categoryId.HasValue
            ? await _repository.GetSubtreeRelativeDepthAsync(tenantId, categoryId.Value, cancellationToken)
            : 1;

        if (CategoryHierarchy.WouldExceedMaxDepth(parent.Level, subtreeRelativeDepth))
        {
            return new ApplicationError("category.max_depth_exceeded", "Category hierarchy cannot exceed 5 levels.");
        }

        return null;
    }

    private Task<ApplicationError?> ValidateAccessAsync(
        TenantRequestContext context,
        string requiredPermission,
        CancellationToken cancellationToken) =>
        _accessPolicy.ValidateAsync(context, requiredPermission, cancellationToken);
}
