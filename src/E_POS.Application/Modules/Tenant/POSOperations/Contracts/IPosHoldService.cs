using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IPosHoldService
{
    Task<ApplicationResult<bool>> CancelHoldAsync(
        TenantRequestContext context,
        Guid holdId,
        string? reason,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosRecallHoldResponseDto>> RecallHoldAsync(
        TenantRequestContext context,
        Guid holdId,
        PosRecallHoldRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosHoldListItemDto>> CreateHoldAsync(
        TenantRequestContext context,
        PosCreateHoldRequestDto request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosHoldListResponseDto>> GetHoldsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken);
}
