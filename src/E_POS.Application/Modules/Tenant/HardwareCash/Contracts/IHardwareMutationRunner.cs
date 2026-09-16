using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

public interface IHardwareMutationRunner
{
    Task<ApplicationResult<T>> ExecuteAsync<T>(TenantRequestContext context, string operation,
        Guid? entityId, string key, object payload, Func<CancellationToken, Task<ApplicationResult<T>>> execute,
        CancellationToken cancellationToken);
}
