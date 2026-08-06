using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Contracts;

public interface IPosHardwareService
{
    Task<ApplicationResult<IReadOnlyList<PosHardwareConfigurationDto>>> GetConfigurationsAsync(
        TenantRequestContext context, Guid posDeviceId, CancellationToken cancellationToken);

    Task<ApplicationResult<PosHardwareConfigurationDto>> SaveConfigurationAsync(
        TenantRequestContext context,
        SavePosHardwareConfigurationRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<HardwareTestOperationDto>> CreateTestAsync(
        TenantRequestContext context,
        CreateHardwareTestRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<HardwareTestOperationDto>> CompleteTestAsync(
        TenantRequestContext context,
        Guid testId,
        CompleteHardwareTestRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyList<HardwareTestOperationDto>>> GetTestHistoryAsync(
        TenantRequestContext context, Guid posDeviceId, int take, CancellationToken cancellationToken);
}
