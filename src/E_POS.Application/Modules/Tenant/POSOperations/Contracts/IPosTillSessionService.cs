using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosTillSessionService
{
    Task<ApplicationResult<CurrentTillSessionResponseDto>> GetCurrentSessionAsync(
        TenantRequestContext context,
        Guid? deviceId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CurrentTillSessionResponseDto>> OpenTillAsync(
        TenantRequestContext context,
        OpenTillRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CloseTillResponseDto>> CloseTillAsync(
        TenantRequestContext context,
        CloseTillRequest request,
        CancellationToken cancellationToken);
}
