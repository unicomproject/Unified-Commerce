using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using E_POS.Domain.Modules.CatalogProduct.Constants;
using E_POS.Domain.Modules.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.CatalogProduct.Repositories;

public sealed class ReturnPolicyTemplateRepository : IReturnPolicyTemplateRepository
{
    private readonly EPosDbContext _dbContext;

    public ReturnPolicyTemplateRepository(EPosDbContext dbContext) => _dbContext = dbContext;

    public Task<bool> TemplateCodeExistsAsync(string templateCode, Guid? excludeTemplateId, CancellationToken cancellationToken)
    {
        return _dbContext.ReturnPolicyTemplates.AsNoTracking()
            .AnyAsync(x => x.TemplateCode == templateCode && (!excludeTemplateId.HasValue || x.Id != excludeTemplateId.Value), cancellationToken);
    }

    public async Task<ReturnPolicyTemplateListResponse> ListAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.ReturnPolicyTemplates.AsNoTracking().Where(x => x.Status != ReturnPolicyTemplateConstants.DeletedStatus);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                var pattern = $"%{term}%";
                query = query.Where(x => EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.TemplateCode, pattern));
            }
            else
            {
                var normalizedTerm = term.ToUpperInvariant();
                query = query.Where(x => x.Name.ToUpper().Contains(normalizedTerm) || x.TemplateCode.ToUpper().Contains(normalizedTerm));
            }
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.TemplateCode).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new ReturnPolicyTemplateSummaryResponse(x.Id, x.TemplateCode, x.Name, x.ReturnWindowDays, x.Status, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new ReturnPolicyTemplateListResponse(items, pageNumber, pageSize, totalCount);
    }

    public Task<ReturnPolicyTemplateResponse?> GetByIdAsync(Guid templateId, bool includeDeleted, CancellationToken cancellationToken)
    {
        return _dbContext.ReturnPolicyTemplates.AsNoTracking()
            .Where(x => x.Id == templateId && (includeDeleted || x.Status != ReturnPolicyTemplateConstants.DeletedStatus))
            .Select(x => new ReturnPolicyTemplateResponse(x.Id, x.TemplateCode, x.Name, x.ReturnWindowDays, x.Status, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ReturnPolicyTemplate?> GetEditableAsync(Guid templateId, CancellationToken cancellationToken)
    {
        return _dbContext.ReturnPolicyTemplates.FirstOrDefaultAsync(x => x.Id == templateId && x.Status != ReturnPolicyTemplateConstants.DeletedStatus, cancellationToken);
    }

    public async Task AddAsync(ReturnPolicyTemplate template, CancellationToken cancellationToken)
    {
        _dbContext.ReturnPolicyTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}