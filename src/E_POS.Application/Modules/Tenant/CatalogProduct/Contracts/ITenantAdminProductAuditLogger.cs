namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ITenantAdminProductAuditLogger
{
    void LogProductDeleted(
        Guid tenantId,
        Guid userId,
        Guid productId,
        string outcome,
        string status);
}
