using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IReturnPolicyTemplateService
{
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> CreateAsync(Guid platformUserId, ReturnPolicyTemplateCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyTemplateListResponse>> ListAsync(Guid platformUserId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> GetByIdAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> UpdateAsync(Guid platformUserId, Guid templateId, ReturnPolicyTemplateUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> PublishAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken) =>
        Task.FromResult(ApplicationResult<ReturnPolicyTemplateResponse>.Failure(new ApplicationError("return_policy_templates.not_implemented", "Not implemented.")));
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> ArchiveAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken) =>
        Task.FromResult(ApplicationResult<ReturnPolicyTemplateResponse>.Failure(new ApplicationError("return_policy_templates.not_implemented", "Not implemented.")));
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> SetDefaultAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken) =>
        Task.FromResult(ApplicationResult<ReturnPolicyTemplateResponse>.Failure(new ApplicationError("return_policy_templates.not_implemented", "Not implemented.")));
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> DuplicateAsync(Guid platformUserId, Guid templateId, string? newCode, string? newName, CancellationToken cancellationToken) =>
        Task.FromResult(ApplicationResult<ReturnPolicyTemplateResponse>.Failure(new ApplicationError("return_policy_templates.not_implemented", "Not implemented.")));
    Task<ApplicationResult> DeleteAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken);
}
