namespace E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;

public interface ICategoryAuditLogger
{
    void LogCreated(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode, string status);
    void LogUpdated(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode, string status, bool parentChanged, bool statusChanged);
    void LogArchived(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode);
    void LogImageUploaded(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid mediaAssetId);
    void LogImageRemoved(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid? previousMediaAssetId, bool noOp);
}
