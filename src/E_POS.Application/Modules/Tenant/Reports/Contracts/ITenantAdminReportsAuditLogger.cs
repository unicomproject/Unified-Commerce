using System;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Modules.Tenant.Reports.Dtos;

namespace E_POS.Application.Modules.Tenant.Reports.Contracts
{
    public interface ITenantAdminReportsAuditLogger
    {
        Task LogExportJobCreatedAsync(Guid tenantId, Guid userId, Guid jobId, ReportExportRequest request, CancellationToken cancellationToken = default);
        Task LogExportDownloadedAsync(Guid tenantId, Guid userId, Guid jobId, CancellationToken cancellationToken = default);
    }
}