using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Services;

public sealed class BrandAuditLogger(ILogger<BrandAuditLogger> logger) : IBrandAuditLogger
{
    public void LogMutation(string eventName, Guid tenantId, Guid userId, Guid brandId, long rowVersion)
    {
        logger.LogInformation(
            "{BrandAuditEvent} TenantId={TenantId} UserId={UserId} BrandId={BrandId} RowVersion={RowVersion}",
            eventName, tenantId, userId, brandId, rowVersion);
    }
}
