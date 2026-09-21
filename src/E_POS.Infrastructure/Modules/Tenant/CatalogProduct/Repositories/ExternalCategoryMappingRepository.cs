using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class ExternalCategoryMappingRepository : IExternalCategoryMappingRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ExternalCategoryMappingRepository> _logger;

    public ExternalCategoryMappingRepository(
        EPosDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<ExternalCategoryMappingRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExternalCategoryMapping?> GetAsync(
        Guid tenantId,
        string provider,
        string externalCategoryKey,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalCategoryKey))
        {
            return null;
        }

        var normProvider = provider.Trim().ToLowerInvariant();
        var normKey = externalCategoryKey.Trim().ToLowerInvariant();

        return await _dbContext.ExternalCategoryMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Provider == normProvider &&
                     x.ExternalCategoryKey == normKey,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ExternalCategoryMapping> UpsertAsync(
        Guid tenantId,
        string provider,
        string externalCategoryKey,
        string externalCategoryName,
        Guid tenantCategoryId,
        string mappingSource,
        Guid? userId,
        CancellationToken cancellationToken,
        bool saveChanges = true)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider cannot be empty.", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalCategoryKey)) throw new ArgumentException("ExternalCategoryKey cannot be empty.", nameof(externalCategoryKey));
        if (string.IsNullOrWhiteSpace(externalCategoryName)) throw new ArgumentException("ExternalCategoryName cannot be empty.", nameof(externalCategoryName));
        if (tenantCategoryId == Guid.Empty) throw new ArgumentException("TenantCategoryId cannot be empty.", nameof(tenantCategoryId));

        var normProvider = provider.Trim().ToLowerInvariant();
        var normKey = externalCategoryKey.Trim().ToLowerInvariant();
        var normName = externalCategoryName.Trim();
        var now = _dateTimeProvider.UtcNow;

        var existing = await _dbContext.ExternalCategoryMappings
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Provider == normProvider &&
                     x.ExternalCategoryKey == normKey,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Update(normName, tenantCategoryId, mappingSource, userId, now);
            if (saveChanges)
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            _logger.LogInformation(
                "Updated external category mapping for Tenant={TenantId}, Provider={Provider}, Key={Key} -> CategoryId={CategoryId}",
                tenantId, normProvider, normKey, tenantCategoryId);
            return existing;
        }

        var mapping = ExternalCategoryMapping.Create(
            Guid.NewGuid(),
            tenantId,
            normProvider,
            normKey,
            normName,
            tenantCategoryId,
            mappingSource,
            userId,
            now);

        await _dbContext.ExternalCategoryMappings.AddAsync(mapping, cancellationToken).ConfigureAwait(false);

        if (saveChanges)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Created external category mapping for Tenant={TenantId}, Provider={Provider}, Key={Key} -> CategoryId={CategoryId}",
                    tenantId, normProvider, normKey, tenantCategoryId);
                return mapping;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrent conflict writing external category mapping for Tenant={TenantId}, Key={Key}. Retrying update...",
                    tenantId, normKey);

                // Re-fetch and update in case of parallel insert race condition
                var concurrent = await _dbContext.ExternalCategoryMappings
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.Provider == normProvider &&
                             x.ExternalCategoryKey == normKey,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (concurrent is not null)
                {
                    concurrent.Update(normName, tenantCategoryId, mappingSource, userId, now);
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return concurrent;
                }

                throw;
            }
        }

        _logger.LogInformation(
            "Staged external category mapping for Tenant={TenantId}, Provider={Provider}, Key={Key} -> CategoryId={CategoryId} in current transaction",
            tenantId, normProvider, normKey, tenantCategoryId);
        return mapping;
    }
}
