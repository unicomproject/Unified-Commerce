using E_POS.Application.Common.Models;

namespace E_POS.Application.Common.Idempotency;

public interface IIdempotencyService
{
    Task<ApplicationResult<T>> ExecuteAsync<T>(
        Guid tenantId,
        Guid actorUserId,
        string operation,
        string idempotencyKey,
        string requestHash,
        Func<CancellationToken, Task<ApplicationResult<T>>> operationFunc,
        CancellationToken cancellationToken);
}
