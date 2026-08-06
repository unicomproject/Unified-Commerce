using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Platform.Subscription.Constants;

namespace E_POS.Domain.Modules.Platform.Subscription.Entities;

public sealed class SubscriptionPaymentEvidence : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string BlobContainer { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string SafeFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string EvidenceType { get; private set; } = "PAYMENT_PROOF";
    public string UploadedByType { get; private set; } = "PAYMENT_RECIPIENT";
    public Guid? UploadedById { get; private set; }
    public long SubmissionVersion { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? SupersededAt { get; private set; }
    public string ScanStatus { get; private set; } = ManualPaymentConstants.ScanPending;
    public string? ScanFailureCode { get; private set; }

    public static SubscriptionPaymentEvidence Create(Guid id, Guid tenantId, Guid paymentId, Guid invoiceId,
        string blobContainer, string storageKey, string originalFileName, string safeFileName,
        string contentType, long fileSize, string sha256, long submissionVersion,
        string scanStatus, DateTimeOffset now, Guid? uploadedById = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobContainer);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(safeFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        if (fileSize <= 0) throw new ArgumentOutOfRangeException(nameof(fileSize));
        if (submissionVersion <= 0) throw new ArgumentOutOfRangeException(nameof(submissionVersion));

        return new SubscriptionPaymentEvidence
        {
            Id = id,
            TenantId = tenantId,
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            BlobContainer = blobContainer.Trim(),
            StorageKey = storageKey.Trim(),
            OriginalFileName = originalFileName.Trim(),
            SafeFileName = safeFileName.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            FileSize = fileSize,
            Sha256 = sha256.Trim().ToLowerInvariant(),
            SubmissionVersion = submissionVersion,
            ScanStatus = scanStatus.Trim().ToUpperInvariant(),
            UploadedById = uploadedById,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Supersede(DateTimeOffset now)
    {
        IsActive = false;
        SupersededAt = now;
        UpdatedAt = now;
    }
}
