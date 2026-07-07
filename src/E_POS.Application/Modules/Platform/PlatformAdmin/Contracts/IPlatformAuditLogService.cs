using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformAuditLogService
{
    Task<ApplicationResult<PlatformAuditLogListResponse>> GetAuditLogsAsync(
        PlatformAuditLogListQuery query,
        Guid platformUserId,
        CancellationToken cancellationToken);
}

