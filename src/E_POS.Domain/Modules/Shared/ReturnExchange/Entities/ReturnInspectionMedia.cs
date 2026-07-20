using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class ReturnInspectionMedia : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ReturnInspectionId { get; protected set; }
    public string StorageKey { get; protected set; } = string.Empty;
    public string FileName { get; protected set; } = string.Empty;
    public string ContentType { get; protected set; } = string.Empty;
    public long SizeBytes { get; protected set; }
    public Guid UploadedByTenantUserId { get; protected set; }

    public static ReturnInspectionMedia Create(Guid id, Guid tenantId, Guid returnInspectionId,
        string storageKey, string fileName, string contentType, long sizeBytes, Guid uploadedBy, DateTimeOffset now) =>
        new() { Id = id, TenantId = tenantId, ReturnInspectionId = returnInspectionId, StorageKey = storageKey,
            FileName = fileName, ContentType = contentType, SizeBytes = sizeBytes,
            UploadedByTenantUserId = uploadedBy, CreatedAt = now };
}
