using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

public interface IPosDrawerService
{
    Task<ApplicationResult<CashDrawerOperationDto>> RegisterOperationAsync(
        TenantRequestContext context,
        RegisterDrawerOperationRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CashDrawerOperationDto>> FinalizeOperationAsync(
        TenantRequestContext context,
        Guid operationId,
        FinalizeDrawerOperationRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CashDrawerOperationDto>> ManualOpenDrawerAsync(
        TenantRequestContext context,
        ManualOpenDrawerRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyList<CashDrawerOperationDto>>> GetHistoryAsync(
        TenantRequestContext context,
        Guid posDeviceId,
        int take,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CashDrawerOperationDto>> GetOperationStatusAsync(
        TenantRequestContext context,
        Guid operationId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CashDrawerOperationDto>> GetOperationStatusByRequestIdAsync(
        TenantRequestContext context,
        Guid requestId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PosCashDrawerSummaryDto>> GetFinancialSummaryAsync(
        TenantRequestContext context, Guid deviceId, CancellationToken cancellationToken);

    Task<ApplicationResult<PosCashDrawerMovementPageDto>> GetFinancialMovementsAsync(
        TenantRequestContext context, Guid deviceId, int page, int pageSize, CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyList<PosCashMovementTypeDto>>> GetMovementTypesAsync(
        TenantRequestContext context, string direction, CancellationToken cancellationToken);

    Task<ApplicationResult<PosCashDrawerMovementDto>> CreateFinancialMovementAsync(
        TenantRequestContext context, CreatePosCashMovementRequest request, CancellationToken cancellationToken);
}
