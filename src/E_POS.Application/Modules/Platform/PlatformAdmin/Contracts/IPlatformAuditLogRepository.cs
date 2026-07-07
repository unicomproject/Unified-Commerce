using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformAuditLogRepository
{
    Task<PlatformAuditLogListResponse> GetLoginSecurityAuditLogsAsync(
        PlatformAuditLogListQuery query,
        CancellationToken cancellationToken);
}

