using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

public interface IPosDrawerRepository
{
    Task<CashDrawerOperationDto?> GetOperationByIdAsync(
        Guid tenantId, Guid operationId, CancellationToken cancellationToken);

    Task<CashDrawerOperationDto?> GetOperationByRequestIdAsync(
        Guid tenantId, Guid requestId, CancellationToken cancellationToken);

    Task<CashDrawerSettingsDto?> GetActiveDrawerSettingsAsync(
        Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken);

    Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> RegisterOperationAsync(
        Guid tenantId,
        Guid userId,
        RegisterDrawerOperationRequest request,
        Guid? approverId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<(string? ErrorCode, CashDrawerOperationDto? Operation)> FinalizeOperationAsync(
        Guid tenantId,
        Guid userId,
        Guid operationId,
        FinalizeDrawerOperationRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CashDrawerOperationDto>> GetHistoryAsync(
        Guid tenantId, Guid posDeviceId, int take, CancellationToken cancellationToken);

    Task<PosCashDrawerSummaryDto?> GetFinancialSummaryAsync(
        Guid tenantId, Guid tillSessionId, CancellationToken cancellationToken);

    Task<PosCashDrawerMovementPageDto> GetFinancialMovementsAsync(
        Guid tenantId, Guid tillSessionId, int page, int pageSize, CancellationToken cancellationToken);

    Task<(string? ErrorCode, PosCashDrawerMovementDto? Movement)> CreateFinancialMovementAsync(
        Guid tenantId,
        Guid userId,
        Guid trustedTillId,
        CreatePosCashMovementRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
