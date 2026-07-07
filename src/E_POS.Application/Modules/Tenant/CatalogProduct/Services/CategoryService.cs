using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Services;

public sealed class CategoryService : ICategoryService
{
    private static readonly ApplicationError PermissionDenied = new("category.permission_denied", "Permission denied for category management.");
    private static readonly ApplicationError NotFound = new("category.not_found", "Category was not found.");
    private readonly ICategoryRepository _repository;
    private readonly ICategoryRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CategoryService(ICategoryRepository repository, ICategoryRequestValidator validator, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<CategoryResponse>> CreateAsync(TenantRequestContext context, CategoryCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CategoryConstants.CreatePermission);
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

        var categoryId = Guid.NewGuid();
        var slug = string.IsNullOrWhiteSpace(request.CategorySlug)
            ? normalizedCode.ToLowerInvariant()
            : request.CategorySlug.Trim().ToLowerInvariant();

        var category = Category.Create(
            categoryId, 
            context.TenantId, 
            request.DepartmentId,
            request.ParentCategoryId, 
            normalizedCode, 
            request.Name, 
            slug,
            request.Description,
            request.SortOrder,
            request.Status, 
            context.UserId,
            _dateTimeProvider.UtcNow);

        await _repository.AddAsync(category, cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, categoryId, false, cancellationToken);
        return ApplicationResult<CategoryResponse>.Success(response!);
    }

    public async Task<ApplicationResult<CategoryListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CategoryConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<CategoryListResponse>.Failure(accessError);

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(context.TenantId, safePageNumber, safePageSize, search, cancellationToken);
        return ApplicationResult<CategoryListResponse>.Success(response);
    }

    public async Task<ApplicationResult<CategoryResponse>> GetByIdAsync(TenantRequestContext context, Guid categoryId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CategoryConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<CategoryResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, categoryId, false, cancellationToken);
        return response is null ? ApplicationResult<CategoryResponse>.Failure(NotFound) : ApplicationResult<CategoryResponse>.Success(response);
    }

    public async Task<ApplicationResult<CategoryResponse>> UpdateAsync(TenantRequestContext context, Guid categoryId, CategoryUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CategoryConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<CategoryResponse>.Failure(accessError);

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<CategoryResponse>.Failure(validationError);

        var category = await _repository.GetEditableAsync(context.TenantId, categoryId, cancellationToken);
        if (category is null) return ApplicationResult<CategoryResponse>.Failure(NotFound);

        var parentError = await ValidateParentAsync(context.TenantId, categoryId, request.ParentCategoryId, cancellationToken);
        if (parentError is not null) return ApplicationResult<CategoryResponse>.Failure(parentError);

        var normalizedCode = CategoryConstants.NormalizeCode(request.CategoryCode);
        if (await _repository.CategoryCodeExistsAsync(context.TenantId, normalizedCode, categoryId, cancellationToken))
        {
            return ApplicationResult<CategoryResponse>.Failure(new ApplicationError("category.duplicate_code", "Category code already exists."));
        }

        var slug = string.IsNullOrWhiteSpace(request.CategorySlug)
            ? normalizedCode.ToLowerInvariant()
            : request.CategorySlug.Trim().ToLowerInvariant();

        category.UpdateProfile(
            request.DepartmentId,
            request.ParentCategoryId, 
            normalizedCode, 
            request.Name, 
            slug,
            request.Description,
            request.SortOrder,
            request.Status, 
            context.UserId,
            _dateTimeProvider.UtcNow);

        await _repository.SaveChangesAsync(cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, categoryId, false, cancellationToken);
        return response is null ? ApplicationResult<CategoryResponse>.Failure(NotFound) : ApplicationResult<CategoryResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid categoryId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, CategoryConstants.DeletePermission);
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

        category.SoftDelete(context.UserId, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationError?> ValidateParentAsync(Guid tenantId, Guid? categoryId, Guid? parentCategoryId, CancellationToken cancellationToken)
    {
        if (!parentCategoryId.HasValue) return null;
        if (categoryId.HasValue && parentCategoryId.Value == categoryId.Value) return new ApplicationError("category.parent_self_reference", "Category cannot be its own parent.");
        if (!await _repository.ParentCategoryExistsAsync(tenantId, parentCategoryId.Value, cancellationToken)) return new ApplicationError("category.parent_not_found", "Parent category was not found.");
        if (categoryId.HasValue && await _repository.WouldCreateParentCycleAsync(tenantId, categoryId.Value, parentCategoryId.Value, cancellationToken)) return new ApplicationError("category.parent_cycle", "Category parent would create a cycle.");
        return null;
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("category.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(CategoryConstants.ManagePermission) ? null : PermissionDenied;
    }
}

