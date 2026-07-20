using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.ReturnExchange.Entities;

public class ReturnInspectionMediaStaging : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid? OutletId { get; protected set; }
    public Guid SaleId { get; protected set; }
    public Guid SaleLineId { get; protected set; }
    public Guid? InspectionDraftId { get; protected set; }
    public Guid? InspectionDraftLineId { get; protected set; }
    public string Status { get; protected set; } = "STAGED";
    public DateTimeOffset ExpiresAt { get; protected set; }
    public DateTimeOffset? ConsumedAt { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string StorageKey { get; protected set; } = string.Empty;
    public string FileName { get; protected set; } = string.Empty;
    public string ContentType { get; protected set; } = string.Empty;
    public long SizeBytes { get; protected set; }
    public Guid UploadedByTenantUserId { get; protected set; }

    public bool IsActive =>
        string.Equals(Status, "STAGED", StringComparison.OrdinalIgnoreCase);

    public static ReturnInspectionMediaStaging Create(
        Guid id,
        Guid tenantId,
        Guid saleId,
        Guid saleLineId,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        Guid uploadedByTenantUserId,
        DateTimeOffset createdAt,
        Guid? outletId = null,
        DateTimeOffset? expiresAt = null) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            OutletId = outletId,
            SaleId = saleId,
            SaleLineId = saleLineId,
            StorageKey = storageKey.Trim(),
            FileName = SanitizeFileName(fileName),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            UploadedByTenantUserId = uploadedByTenantUserId,
            CreatedAt = createdAt,
            Status = "STAGED",
            ExpiresAt = expiresAt ?? createdAt.AddHours(ReturnInspectionDraft.DefaultLifetimeHours),
        };

    public void AttachToDraftLine(Guid draftId, Guid draftLineId, DateTimeOffset draftExpiresAt)
    {
        InspectionDraftId = draftId;
        InspectionDraftLineId = draftLineId;
        Status = "STAGED";
        if (ExpiresAt < draftExpiresAt)
        {
            ExpiresAt = draftExpiresAt;
        }
    }

    public void MarkConsumed(DateTimeOffset now)
    {
        Status = "CONSUMED";
        ConsumedAt = now;
        DeletedAt = null;
    }

    public void MarkExpired() => Status = "EXPIRED";

    public void MarkDeleted(DateTimeOffset now)
    {
        Status = "DELETED";
        DeletedAt = now;
    }

    private static string SanitizeFileName(string fileName)
    {
        var trimmed = (fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "inspection-photo";
        }

        var name = Path.GetFileName(trimmed.Replace('\\', '/'));
        return string.IsNullOrWhiteSpace(name) ? "inspection-photo" : name;
    }
}
