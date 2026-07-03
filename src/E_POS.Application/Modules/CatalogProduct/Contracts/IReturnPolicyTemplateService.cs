using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Dtos;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IReturnPolicyTemplateService
{
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> CreateAsync(Guid platformUserId, ReturnPolicyTemplateCreateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyTemplateListResponse>> ListAsync(Guid platformUserId, int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> GetByIdAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken);
    Task<ApplicationResult<ReturnPolicyTemplateResponse>> UpdateAsync(Guid platformUserId, Guid templateId, ReturnPolicyTemplateUpdateRequest request, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(Guid platformUserId, Guid templateId, CancellationToken cancellationToken);
}