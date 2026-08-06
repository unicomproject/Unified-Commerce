using E_POS.Application.Modules.Tenant.POSOperations.Dtos;

namespace E_POS.Application.Modules.Tenant.POSOperations.Contracts;

public interface IReceiptTemplateResolutionService
{
    Task<ResolvedReceiptTemplateDto?> ResolveTemplateAsync(
        Guid tenantId,
        Guid outletId,
        Guid tillId,
        Guid deviceId,
        CancellationToken cancellationToken);
}
