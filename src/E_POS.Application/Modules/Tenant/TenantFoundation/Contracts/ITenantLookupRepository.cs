namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface ITenantLookupRepository
{
    Task<bool> CurrencyExistsAsync(string currencyCode, CancellationToken cancellationToken);
    Task<bool> AllSalesChannelsExistAsync(Guid tenantId, Guid[] channelIds, CancellationToken cancellationToken);
}

