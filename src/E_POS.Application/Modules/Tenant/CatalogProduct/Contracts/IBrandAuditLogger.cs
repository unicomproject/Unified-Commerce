namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface IBrandAuditLogger
{
    void LogMutation(string eventName, Guid tenantId, Guid userId, Guid brandId, long rowVersion);
}
