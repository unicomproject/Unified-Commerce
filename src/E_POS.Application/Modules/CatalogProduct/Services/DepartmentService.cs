using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;

namespace E_POS.Application.Modules.CatalogProduct.Services;

public sealed class DepartmentService : IDepartmentService
{
    private static readonly ApplicationError PermissionDenied = new("department.permission_denied", "Permission denied for department management.");
    private static readonly ApplicationError NotFound = new("department.not_found", "Department was not found.");
    private readonly IDepartmentRepository _repository;
    private readonly IDepartmentRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DepartmentService(IDepartmentRepository repository, IDepartmentRequestValidator validator, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<DepartmentResponse>> CreateAsync(TenantRequestContext context, DepartmentCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, DepartmentConstants.CreatePermission);
        if (accessError is not null) return ApplicationResult<DepartmentResponse>.Failure(accessError);

        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<DepartmentResponse>.Failure(validationError);

        var normalizedCode = DepartmentConstants.NormalizeCode(request.DepartmentCode);
        if (await _repository.DepartmentCodeExistsAsync(context.TenantId, normalizedCode, null, cancellationToken))
        {
            return ApplicationResult<DepartmentResponse>.Failure(new ApplicationError("department.duplicate_code", "Department code already exists."));
        }

        var departmentId = Guid.NewGuid();
        var department = Department.Create(
            departmentId, 
            context.TenantId, 
            normalizedCode, 
            request.Name, 
            request.Description,
            request.SortOrder,
            request.Status, 
            context.UserId,
            _dateTimeProvider.UtcNow);

        await _repository.AddAsync(department, cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, departmentId, false, cancellationToken);
        return ApplicationResult<DepartmentResponse>.Success(response!);
    }

    public async Task<ApplicationResult<DepartmentListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, DepartmentConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<DepartmentListResponse>.Failure(accessError);

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var response = await _repository.ListAsync(context.TenantId, safePageNumber, safePageSize, search, cancellationToken);
        return ApplicationResult<DepartmentListResponse>.Success(response);
    }

    public async Task<ApplicationResult<DepartmentResponse>> GetByIdAsync(TenantRequestContext context, Guid departmentId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, DepartmentConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<DepartmentResponse>.Failure(accessError);

        var response = await _repository.GetByIdAsync(context.TenantId, departmentId, false, cancellationToken);
        return response is null ? ApplicationResult<DepartmentResponse>.Failure(NotFound) : ApplicationResult<DepartmentResponse>.Success(response);
    }

    public async Task<ApplicationResult<DepartmentResponse>> UpdateAsync(TenantRequestContext context, Guid departmentId, DepartmentUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, DepartmentConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<DepartmentResponse>.Failure(accessError);

        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<DepartmentResponse>.Failure(validationError);

        var department = await _repository.GetEditableAsync(context.TenantId, departmentId, cancellationToken);
        if (department is null) return ApplicationResult<DepartmentResponse>.Failure(NotFound);

        var normalizedCode = DepartmentConstants.NormalizeCode(request.DepartmentCode);
        if (await _repository.DepartmentCodeExistsAsync(context.TenantId, normalizedCode, departmentId, cancellationToken))
        {
            return ApplicationResult<DepartmentResponse>.Failure(new ApplicationError("department.duplicate_code", "Department code already exists."));
        }

        department.UpdateProfile(
            normalizedCode, 
            request.Name, 
            request.Description,
            request.SortOrder,
            request.Status, 
            context.UserId,
            _dateTimeProvider.UtcNow);

        await _repository.SaveChangesAsync(cancellationToken);
        var response = await _repository.GetByIdAsync(context.TenantId, departmentId, false, cancellationToken);
        return response is null ? ApplicationResult<DepartmentResponse>.Failure(NotFound) : ApplicationResult<DepartmentResponse>.Success(response);
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid departmentId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, DepartmentConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        var department = await _repository.GetEditableAsync(context.TenantId, departmentId, cancellationToken);
        if (department is null) return ApplicationResult.Failure(NotFound);

        department.SoftDelete(context.UserId, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("department.invalid_tenant_context", "Invalid tenant context.");
        }

        return context.HasPermission(requiredPermission) || context.HasPermission(DepartmentConstants.ManagePermission) ? null : PermissionDenied;
    }
}