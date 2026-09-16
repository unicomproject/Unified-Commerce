using System.Text.Json;
namespace E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

public interface IHardwareDashboardQuery
{
    Task<JsonElement> GetAsync(Guid tenantId, Guid? outletId, string search, string type,
        string status, string sort, int page, int pageSize, CancellationToken cancellationToken);
}
