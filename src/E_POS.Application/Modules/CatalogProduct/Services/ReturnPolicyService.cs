using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;

namespace E_POS.Application.Modules.CatalogProduct.Services;

public sealed class ReturnPolicyService : IReturnPolicyService
{
    private static readonly ApplicationError PermissionDenied = new("return_policies.permission_denied", "Permission denied for return policy management.");
    private static readonly ApplicationError NotFound = new("return_policies.not_found", "Return policy was not found.");
    private readonly IReturnPolicyRepository _repository;
    private readonly IReturnPolicyRequestValidator _validator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReturnPolicyService(IReturnPolicyRepository repository, IReturnPolicyRequestValidator validator, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _validator = validator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<ReturnPolicyResponse>> CreateAsync(TenantRequestContext context, ReturnPolicyCreateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ReturnPolicyConstants.CreatePermission);
        if (accessError is not null) return ApplicationResult<ReturnPolicyResponse>.Failure(accessError);
        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<ReturnPolicyResponse>.Failure(validationError);
        var normalizedCode = ReturnPolicyConstants.NormalizeCode(request.PolicyCode);
        if (await _repository.PolicyCodeExistsAsync(context.TenantId, normalizedCode, null, cancellationToken)) return ApplicationResult<ReturnPolicyResponse>.Failure(new ApplicationError("return_policies.duplicate_code", "Return policy code already exists."));
        var policyId = Guid.NewGuid();
        var policy = ReturnPolicy.Create(
            policyId, 
            context.TenantId, 
            normalizedCode, 
            request.Name, 
            request.Description, 
            request.ReturnWindowDays,
            request.ExchangeWindowDays,
            request.RequiresReceipt ?? true,
            request.AllowDefectiveReturn ?? true,
            request.RequiresManagerApproval ?? false,
            request.IsDefaultPolicy ?? false,
            request.Status, 
            context.UserId,
            _dateTimeProvider.UtcNow);
        await _repository.AddAsync(policy, cancellationToken);
        return ApplicationResult<ReturnPolicyResponse>.Success((await _repository.GetByIdAsync(context.TenantId, policyId, false, cancellationToken))!);
    }

    public async Task<ApplicationResult<ReturnPolicyListResponse>> ListAsync(TenantRequestContext context, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ReturnPolicyConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<ReturnPolicyListResponse>.Failure(accessError);
        var response = await _repository.ListAsync(context.TenantId, Math.Max(1, pageNumber), Math.Clamp(pageSize, 1, 100), search, cancellationToken);
        return ApplicationResult<ReturnPolicyListResponse>.Success(response);
    }

    public async Task<ApplicationResult<ReturnPolicyResponse>> GetByIdAsync(TenantRequestContext context, Guid policyId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ReturnPolicyConstants.ViewPermission);
        if (accessError is not null) return ApplicationResult<ReturnPolicyResponse>.Failure(accessError);
        var response = await _repository.GetByIdAsync(context.TenantId, policyId, false, cancellationToken);
        return response is null ? ApplicationResult<ReturnPolicyResponse>.Failure(NotFound) : ApplicationResult<ReturnPolicyResponse>.Success(response);
    }

    public async Task<ApplicationResult<ReturnPolicyResponse>> UpdateAsync(TenantRequestContext context, Guid policyId, ReturnPolicyUpdateRequest request, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ReturnPolicyConstants.UpdatePermission);
        if (accessError is not null) return ApplicationResult<ReturnPolicyResponse>.Failure(accessError);
        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<ReturnPolicyResponse>.Failure(validationError);
        var policy = await _repository.GetEditableAsync(context.TenantId, policyId, cancellationToken);
        if (policy is null) return ApplicationResult<ReturnPolicyResponse>.Failure(NotFound);
        var normalizedCode = ReturnPolicyConstants.NormalizeCode(request.PolicyCode);
        if (await _repository.PolicyCodeExistsAsync(context.TenantId, normalizedCode, policyId, cancellationToken)) return ApplicationResult<ReturnPolicyResponse>.Failure(new ApplicationError("return_policies.duplicate_code", "Return policy code already exists."));
        policy.UpdateProfile(
            normalizedCode, 
            request.Name, 
            request.Description, 
            request.ReturnWindowDays,
            request.ExchangeWindowDays,
            request.RequiresReceipt ?? true,
            request.AllowDefectiveReturn ?? true,
            request.RequiresManagerApproval ?? false,
            request.IsDefaultPolicy ?? false,
            request.Status, 
            context.UserId,
            _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult<ReturnPolicyResponse>.Success((await _repository.GetByIdAsync(context.TenantId, policyId, false, cancellationToken))!);
    }

    public async Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid policyId, CancellationToken cancellationToken)
    {
        var accessError = ValidateAccess(context, ReturnPolicyConstants.DeletePermission);
        if (accessError is not null) return ApplicationResult.Failure(accessError);
        var policy = await _repository.GetEditableAsync(context.TenantId, policyId, cancellationToken);
        if (policy is null) return ApplicationResult.Failure(NotFound);
        policy.SoftDelete(context.UserId, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private static ApplicationError? ValidateAccess(TenantRequestContext context, string requiredPermission)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty) return new ApplicationError("return_policies.invalid_tenant_context", "Invalid tenant context.");
        return context.HasPermission(requiredPermission) || context.HasPermission(ReturnPolicyConstants.ManagePermission) ? null : PermissionDenied;
    }
}