using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformAuditLogRepository
{
    Task<PlatformAuditLogListResponse> GetLoginSecurityAuditLogsAsync(
        PlatformAuditLogListQuery query,
        CancellationToken cancellationToken);
}
