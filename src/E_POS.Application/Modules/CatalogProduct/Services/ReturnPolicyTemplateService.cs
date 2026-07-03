using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Domain.Modules.PlatformAdministration.Constants;

namespace E_POS.Application.Modules.CatalogProduct.Services;

public sealed class ReturnPolicyTemplateService : IReturnPolicyTemplateService
{
    private static readonly ApplicationError AccessDenied = new("return_policy_templates.access_denied", "Return policy template access denied.");
    private static readonly ApplicationError NotFound = new("return_policy_templates.not_found", "Return policy template was not found.");
    private readonly IReturnPolicyTemplateRepository _repository;
    private readonly IReturnPolicyTemplateRequestValidator _validator;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReturnPolicyTemplateService(IReturnPolicyTemplateRepository repository, IReturnPolicyTemplateRequestValidator validator, IPlatformPermissionChecker permissionChecker, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _validator = validator;
        _permissionChecker = permissionChecker;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<ReturnPolicyTemplateResponse>> CreateAsync(Guid platformUserId, ReturnPolicyTemplateCreateRequest request, CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(platformUserId, PlatformPermissionCodes.ReturnPolicyTemplatesCreate, cancellationToken)) return ApplicationResult<ReturnPolicyTemplateResponse>.Failure(AccessDenied);
        var validationError = _validator.ValidateCreate(request);
        if (validationError is not null) return ApplicationResult<ReturnPolicyTemplateResponse>.Failure(validationError);
        var normalizedCode = ReturnPolicyTemplateConstants.NormalizeCode(request.TemplateCode);
        if (await _repository.TemplateCodeExistsAsync(normalizedCode, null, cancellationToken)) return ApplicationResult<ReturnPolicyTemplateResponse>.Failure(new ApplicationError("return_policy_templates.conflict", "Return policy template code already exists."));
        var templateId = Guid.NewGuid();
        var template = ReturnPolicyTemplate.Create(templateId, normalizedCode, request.Name, request.ReturnWindowDays, request.Status, _dateTimeProvider.UtcNow);
        await _repository.AddAsync(template, cancellationToken);
        return ApplicationResult<ReturnPolicyTemplateResponse>.Success((await _repository.GetByIdAsync(templateId, false, cancellationToken))!);
    }

    public async Task<ApplicationResult<ReturnPolicyTemplateListResponse>> ListAsync(Guid platformUserId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(platformUserId, PlatformPermissionCodes.ReturnPolicyTemplatesView, cancellationToken)) return ApplicationResult<ReturnPolicyTemplateListResponse>.Failure(AccessDenied);
        var response = await _repository.ListAsync(Math.Max(1, pageNumber), Math.Clamp(pageSize, 1, 100), search, cancellationToken);
        return ApplicationResult<ReturnPolicyTemplateListResponse>.Success(response);
    }

    public async Task<ApplicationResult<ReturnPolicyTemplateResponse>> GetByIdAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(platformUserId, PlatformPermissionCodes.ReturnPolicyTemplatesView, cancellationToken)) return ApplicationResult<ReturnPolicyTemplateResponse>.Failure(AccessDenied);
        var response = await _repository.GetByIdAsync(templateId, false, cancellationToken);
        return response is null ? ApplicationResult<ReturnPolicyTemplateResponse>.Failure(NotFound) : ApplicationResult<ReturnPolicyTemplateResponse>.Success(response);
    }

    public async Task<ApplicationResult<ReturnPolicyTemplateResponse>> UpdateAsync(Guid platformUserId, Guid templateId, ReturnPolicyTemplateUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(platformUserId, PlatformPermissionCodes.ReturnPolicyTemplatesUpdate, cancellationToken)) return ApplicationResult<ReturnPolicyTemplateResponse>.Failure(AccessDenied);
        var validationError = _validator.ValidateUpdate(request);
        if (validationError is not null) return ApplicationResult<ReturnPolicyTemplateResponse>.Failure(validationError);
        var template = await _repository.GetEditableAsync(templateId, cancellationToken);
        if (template is null) return ApplicationResult<ReturnPolicyTemplateResponse>.Failure(NotFound);
        var normalizedCode = ReturnPolicyTemplateConstants.NormalizeCode(request.TemplateCode);
        if (await _repository.TemplateCodeExistsAsync(normalizedCode, templateId, cancellationToken)) return ApplicationResult<ReturnPolicyTemplateResponse>.Failure(new ApplicationError("return_policy_templates.conflict", "Return policy template code already exists."));
        template.UpdateProfile(normalizedCode, request.Name, request.ReturnWindowDays, request.Status, _dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult<ReturnPolicyTemplateResponse>.Success((await _repository.GetByIdAsync(templateId, false, cancellationToken))!);
    }

    public async Task<ApplicationResult> DeleteAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(platformUserId, PlatformPermissionCodes.ReturnPolicyTemplatesDelete, cancellationToken)) return ApplicationResult.Failure(AccessDenied);
        var template = await _repository.GetEditableAsync(templateId, cancellationToken);
        if (template is null) return ApplicationResult.Failure(NotFound);
        template.SoftDelete(_dateTimeProvider.UtcNow);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<bool> HasAccessAsync(Guid platformUserId, string permissionCode, CancellationToken cancellationToken)
    {
        return await _permissionChecker.HasPermissionAsync(platformUserId, permissionCode, cancellationToken) ||
               await _permissionChecker.HasPermissionAsync(platformUserId, PlatformPermissionCodes.ReturnPolicyTemplatesManage, cancellationToken);
    }
}