using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformAuditLogService
{
    Task<ApplicationResult<PlatformAuditLogListResponse>> GetAuditLogsAsync(
        PlatformAuditLogListQuery query,
        Guid platformUserId,
        CancellationToken cancellationToken);
}
