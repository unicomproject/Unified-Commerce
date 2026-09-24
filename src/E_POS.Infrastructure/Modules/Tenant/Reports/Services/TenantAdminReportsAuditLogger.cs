using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Modules.Tenant.Reports.Contracts;
using E_POS.Application.Modules.Tenant.Reports.Dtos;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Infrastructure.Persistence;

namespace E_POS.Infrastructure.Modules.Tenant.Reports.Services
{
    public sealed class TenantAdminReportsAuditLogger : ITenantAdminReportsAuditLogger
    {
        private readonly EPosDbContext _dbContext;

        public TenantAdminReportsAuditLogger(EPosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task LogExportJobCreatedAsync(Guid tenantId, Guid userId, Guid jobId, ReportExportRequest request, CancellationToken cancellationToken = default)
        {
            var auditLog = new AuditLog
            {
                TenantId = tenantId,
                ActorUserId = userId,
                ActorType = "TENANT_USER",
                EntityType = "REPORT_EXPORT_JOB",
                EntityId = jobId,
                Action = "CREATE",
                NewValues = JsonSerializer.Serialize(request),
                CreatedAt = DateTimeOffset.UtcNow
            };
            
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task LogExportDownloadedAsync(Guid tenantId, Guid userId, Guid jobId, CancellationToken cancellationToken = default)
        {
            var auditLog = new AuditLog
            {
                TenantId = tenantId,
                ActorUserId = userId,
                ActorType = "TENANT_USER",
                EntityType = "REPORT_EXPORT_JOB",
                EntityId = jobId,
                Action = "DOWNLOAD",
                CreatedAt = DateTimeOffset.UtcNow
            };
            
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}