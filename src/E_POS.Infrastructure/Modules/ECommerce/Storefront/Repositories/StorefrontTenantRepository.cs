using System.Text.Json;
using E_POS.Application.Modules.ECommerce.Storefront.Contracts;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TenantEntity = E_POS.Domain.Modules.Tenant.TenantFoundation.Entities.Tenant;

namespace E_POS.Infrastructure.Modules.ECommerce.Storefront.Repositories;

public sealed class StorefrontTenantRepository : IStorefrontTenantRepository
{
    private readonly EPosDbContext _dbContext;

    public StorefrontTenantRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(Guid? TenantId, string? BaseCurrencyCode, string? StoreName, string? LogoUrl)> GetTenantIdBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim();
        var tenant = await _dbContext.Set<TenantEntity>()
            .AsNoTracking()
            .Select(t => new { t.Id, t.TenantSlug, t.Status, t.BaseCurrencyCode, t.DisplayName })
            .FirstOrDefaultAsync(t => t.TenantSlug == normalizedSlug && t.Status.ToLower() == TenantStatusConstants.Active, cancellationToken);

        if (tenant == null)
            return (null, null, null, null);

        string? storeName = tenant.DisplayName;
        string? logoUrl = null;

        var setting = await (from s in _dbContext.TenantSettings.AsNoTracking()
                             join d in _dbContext.SettingDefinitions.AsNoTracking() on s.SettingDefinitionId equals d.Id
                             where s.TenantId == tenant.Id && d.SettingKey == "online_store_defaults"
                             select s.SettingValue).FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(setting))
        {
            try
            {
                using var doc = JsonDocument.Parse(setting);
                if (doc.RootElement.TryGetProperty("businessDisplayName", out var bdn) && bdn.ValueKind == JsonValueKind.String)
                {
                    var name = bdn.GetString();
                    if (!string.IsNullOrWhiteSpace(name)) storeName = name;
                }

                if (doc.RootElement.TryGetProperty("branding", out var branding) && branding.TryGetProperty("logoMediaAssetId", out var logoIdElement))
                {
                    if (logoIdElement.ValueKind == JsonValueKind.String && Guid.TryParse(logoIdElement.GetString(), out var logoId))
                    {
                        logoUrl = await _dbContext.MediaAssets.AsNoTracking()
                            .Where(m => m.Id == logoId)
                            .Select(m => m.PublicUrl)
                            .FirstOrDefaultAsync(cancellationToken);
                    }
                }
            }
            catch
            {
                // Ignore parse errors
            }
        }

        return (tenant.Id, tenant.BaseCurrencyCode, storeName, logoUrl);
    }
}