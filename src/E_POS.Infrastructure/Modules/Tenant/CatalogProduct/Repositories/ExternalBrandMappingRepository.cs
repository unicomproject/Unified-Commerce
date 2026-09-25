using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class ExternalBrandMappingRepository : IExternalBrandMappingRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ExternalBrandMappingRepository> _logger;

    public ExternalBrandMappingRepository(
        EPosDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<ExternalBrandMappingRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExternalBrandMapping?> GetAsync(
        Guid tenantId,
        string provider,
        string externalBrandKey,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalBrandKey))
        {
            return null;
        }

        var normProvider = provider.Trim().ToLowerInvariant();
        var normKey = externalBrandKey.Trim().ToLowerInvariant();

        return await _dbContext.ExternalBrandMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Provider == normProvider &&
                     x.ExternalBrandKey == normKey,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ExternalBrandMapping> UpsertAsync(
        Guid tenantId,
        string provider,
        string externalBrandKey,
        string externalBrandName,
        Guid tenantBrandId,
        string mappingSource,
        Guid? userId,
        CancellationToken cancellationToken,
        bool saveChanges = true)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider cannot be empty.", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalBrandKey)) throw new ArgumentException("ExternalBrandKey cannot be empty.", nameof(externalBrandKey));
        if (string.IsNullOrWhiteSpace(externalBrandName)) throw new ArgumentException("ExternalBrandName cannot be empty.", nameof(externalBrandName));
        if (tenantBrandId == Guid.Empty) throw new ArgumentException("TenantBrandId cannot be empty.", nameof(tenantBrandId));

        var normProvider = provider.Trim().ToLowerInvariant();
        var normKey = externalBrandKey.Trim().ToLowerInvariant();
        var normName = externalBrandName.Trim();
        var now = _dateTimeProvider.UtcNow;

        var existing = await _dbContext.ExternalBrandMappings
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Provider == normProvider &&
                     x.ExternalBrandKey == normKey,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Update(normName, tenantBrandId, mappingSource, userId, now);
            if (saveChanges)
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            _logger.LogInformation(
                "Updated external brand mapping for Tenant={TenantId}, Provider={Provider}, Key={Key} -> BrandId={BrandId}",
                tenantId, normProvider, normKey, tenantBrandId);
            return existing;
        }

        var mapping = ExternalBrandMapping.Create(
            Guid.NewGuid(),
            tenantId,
            normProvider,
            normKey,
            normName,
            tenantBrandId,
            mappingSource,
            userId,
            now);

        await _dbContext.ExternalBrandMappings.AddAsync(mapping, cancellationToken).ConfigureAwait(false);

        if (saveChanges)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Created external brand mapping for Tenant={TenantId}, Provider={Provider}, Key={Key} -> BrandId={BrandId}",
                    tenantId, normProvider, normKey, tenantBrandId);
                return mapping;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrent conflict writing external brand mapping for Tenant={TenantId}, Key={Key}. Retrying update...",
                    tenantId, normKey);

                // Re-fetch and update in case of parallel insert race condition
                var concurrent = await _dbContext.ExternalBrandMappings
                    .FirstOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.Provider == normProvider &&
                             x.ExternalBrandKey == normKey,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (concurrent is not null)
                {
                    concurrent.Update(normName, tenantBrandId, mappingSource, userId, now);
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return concurrent;
                }

                throw;
            }
        }

        _logger.LogInformation(
            "Staged external brand mapping for Tenant={TenantId}, Provider={Provider}, Key={Key} -> BrandId={BrandId} in current transaction",
            tenantId, normProvider, normKey, tenantBrandId);
        return mapping;
    }
}
