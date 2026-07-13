using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Services;

public sealed class TenantAdminProductAuditLogger : ITenantAdminProductAuditLogger
{
    private readonly ILogger<TenantAdminProductAuditLogger> _logger;

    public TenantAdminProductAuditLogger(ILogger<TenantAdminProductAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogProductDeleted(
        Guid tenantId,
        Guid userId,
        Guid productId,
        string outcome,
        string status)
    {
        _logger.LogInformation(
            "ProductDeleted TenantId={TenantId} UserId={UserId} ProductId={ProductId} Outcome={Outcome} Status={Status}",
            tenantId,
            userId,
            productId,
            outcome,
            status);
    }
}
