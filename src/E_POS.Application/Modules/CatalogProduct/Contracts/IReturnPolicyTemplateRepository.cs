using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Entities;

namespace E_POS.Application.Modules.CatalogProduct.Contracts;

public interface IReturnPolicyTemplateRepository
{
    Task<bool> TemplateCodeExistsAsync(string templateCode, Guid? excludeTemplateId, CancellationToken cancellationToken);
    Task<ReturnPolicyTemplateListResponse> ListAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken);
    Task<ReturnPolicyTemplateResponse?> GetByIdAsync(Guid templateId, bool includeDeleted, CancellationToken cancellationToken);
    Task<ReturnPolicyTemplate?> GetEditableAsync(Guid templateId, CancellationToken cancellationToken);
    Task AddAsync(ReturnPolicyTemplate template, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}