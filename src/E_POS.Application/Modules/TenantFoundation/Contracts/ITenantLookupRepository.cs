namespace E_POS.Application.Modules.TenantFoundation.Contracts;

public interface ITenantLookupRepository
{
    Task<bool> CurrencyExistsAsync(string currencyCode, CancellationToken cancellationToken);
    Task<bool> AllSalesChannelsExistAsync(Guid tenantId, Guid[] channelIds, CancellationToken cancellationToken);
}
