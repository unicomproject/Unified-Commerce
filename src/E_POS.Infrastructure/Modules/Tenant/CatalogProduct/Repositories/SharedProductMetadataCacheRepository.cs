using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Repositories;

public sealed class SharedProductMetadataCacheRepository : ISharedProductMetadataCacheRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly EPosDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<SharedProductMetadataCacheRepository> _logger;

    public SharedProductMetadataCacheRepository(
        EPosDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<SharedProductMetadataCacheRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExternalProductSuggestion?> GetValidAsync(
        string normalizedBarcode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedBarcode))
        {
            return null;
        }

        var key = normalizedBarcode.Trim();
        var now = _dateTimeProvider.UtcNow;

        try
        {
            // Query latest unexpired cache record for this barcode
            var entry = await _dbContext.SharedProductMetadataCaches
                .AsNoTracking()
                .Where(x => x.NormalizedBarcode == key && x.ExpiresAt > now)
                .OrderByDescending(x => x.CachedAt)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (entry is null)
            {
                return null;
            }

            var suggestion = JsonSerializer.Deserialize<ExternalProductSuggestion>(
                entry.NormalizedMetadataJson,
                JsonOptions);

            return suggestion;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to read from shared product metadata cache for barcode {Barcode}. Continuing without cache.",
                key);
            return null;
        }
    }

    public async Task SetAsync(
        string normalizedBarcode,
        string? identifierStandard,
        string provider,
        ExternalProductSuggestion suggestion,
        string? rawResponseJson,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedBarcode);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentNullException.ThrowIfNull(suggestion);

        var key = normalizedBarcode.Trim();
        var prov = provider.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;
        var expiresAt = now.Add(ttl);

        try
        {
            var metadataJson = JsonSerializer.Serialize(suggestion, JsonOptions);

            var existing = await _dbContext.SharedProductMetadataCaches
                .FirstOrDefaultAsync(
                    x => x.NormalizedBarcode == key && x.Provider == prov,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                existing.Update(
                    identifierStandard,
                    metadataJson,
                    rawResponseJson,
                    now,
                    expiresAt);
            }
            else
            {
                var newEntry = SharedProductMetadataCache.Create(
                    Guid.NewGuid(),
                    key,
                    identifierStandard,
                    prov,
                    metadataJson,
                    rawResponseJson,
                    now,
                    expiresAt);

                _dbContext.SharedProductMetadataCaches.Add(newEntry);
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to write to shared product metadata cache for barcode {Barcode}. Lookup operation will continue unaffected.",
                key);
        }
    }
}
