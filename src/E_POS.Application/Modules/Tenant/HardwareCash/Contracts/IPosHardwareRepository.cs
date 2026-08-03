using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

public interface IPosHardwareRepository
{
    Task<IReadOnlyList<PosHardwareConfigurationDto>> GetConfigurationsAsync(
        Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken);

    Task<(string? ErrorCode, PosHardwareConfigurationDto? Configuration)> SaveConfigurationAsync(
        Guid tenantId,
        Guid userId,
        SavePosHardwareConfigurationRequest request,
        string safeSettingsJson,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<(string? ErrorCode, HardwareTestOperationDto? Operation)> CreateTestAsync(
        Guid tenantId,
        Guid userId,
        CreateHardwareTestRequest request,
        string requestPayloadHash,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<(string? ErrorCode, HardwareTestOperationDto? Operation)> CompleteTestAsync(
        Guid tenantId,
        Guid userId,
        Guid testId,
        CompleteHardwareTestRequest request,
        string? safeResultPayloadJson,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<HardwareTestOperationDto>> GetTestHistoryAsync(
        Guid tenantId, Guid posDeviceId, int take, CancellationToken cancellationToken);
}
