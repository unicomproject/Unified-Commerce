using System.Text.Json;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Domain.Modules.Shared.Audit.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.CatalogProduct.Services;

public sealed class CategoryAuditLogger : ICategoryAuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<CategoryAuditLogger> _logger;
    private readonly EPosDbContext _dbContext;

    public CategoryAuditLogger(ILogger<CategoryAuditLogger> logger, EPosDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public void LogCreated(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode, string status)
    {
        Persist(tenantId, actorTenantUserId, categoryId, "category.created", Serialize(new { categoryCode, status }));
        _logger.LogInformation(
            "CATEGORY_CREATED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=CATEGORY EntityId={EntityId} CategoryCode={CategoryCode} Status={Status}",
            tenantId,
            actorTenantUserId,
            categoryId,
            categoryCode,
            status);
    }

    public void LogUpdated(
        Guid tenantId,
        Guid actorTenantUserId,
        Guid categoryId,
        string categoryCode,
        string status,
        bool parentChanged,
        bool statusChanged)
    {
        var action = parentChanged
            ? "category.parent_moved"
            : statusChanged
                ? "category.status_changed"
                : "category.updated";
        Persist(
            tenantId,
            actorTenantUserId,
            categoryId,
            action,
            Serialize(new { categoryCode, status, parentChanged, statusChanged }));
        _logger.LogInformation(
            "CATEGORY_UPDATED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=CATEGORY EntityId={EntityId} Action={Action} CategoryCode={CategoryCode} Status={Status} ParentChanged={ParentChanged} StatusChanged={StatusChanged}",
            tenantId,
            actorTenantUserId,
            categoryId,
            action,
            categoryCode,
            status,
            parentChanged,
            statusChanged);
    }

    public void LogArchived(Guid tenantId, Guid actorTenantUserId, Guid categoryId, string categoryCode)
    {
        Persist(tenantId, actorTenantUserId, categoryId, "category.archived", Serialize(new { categoryCode, status = "DELETED" }));
        _logger.LogInformation(
            "CATEGORY_ARCHIVED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=CATEGORY EntityId={EntityId} CategoryCode={CategoryCode}",
            tenantId,
            actorTenantUserId,
            categoryId,
            categoryCode);
    }

    public void LogImageUploaded(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid mediaAssetId)
    {
        Persist(
            tenantId,
            actorTenantUserId,
            categoryId,
            "category.image_uploaded",
            Serialize(new { categoryId, mediaAssetId, result = "UPLOADED" }));
        _logger.LogInformation(
            "CATEGORY_IMAGE_UPLOADED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=CATEGORY EntityId={EntityId} MediaAssetId={MediaAssetId} Result=UPLOADED",
            tenantId,
            actorTenantUserId,
            categoryId,
            mediaAssetId);
    }

    public void LogImageRemoved(Guid tenantId, Guid actorTenantUserId, Guid categoryId, Guid? previousMediaAssetId, bool noOp)
    {
        var result = noOp ? "NO_OP" : "REMOVED";
        Persist(
            tenantId,
            actorTenantUserId,
            categoryId,
            "category.image_removed",
            Serialize(new { categoryId, previousMediaAssetId, result }));
        _logger.LogInformation(
            "CATEGORY_IMAGE_REMOVED TenantId={TenantId} ActorTenantUserId={ActorTenantUserId} EntityType=CATEGORY EntityId={EntityId} PreviousMediaAssetId={PreviousMediaAssetId} Result={Result}",
            tenantId,
            actorTenantUserId,
            categoryId,
            previousMediaAssetId,
            result);
    }

    private void Persist(Guid tenantId, Guid actorTenantUserId, Guid entityId, string action, string? newValues)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            ActorUserId = actorTenantUserId,
            ActorType = "TENANT_USER",
            EntityType = "CATEGORY",
            EntityId = entityId,
            Action = action,
            NewValues = newValues,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static string Serialize(object payload) => JsonSerializer.Serialize(payload, JsonOptions);
}
