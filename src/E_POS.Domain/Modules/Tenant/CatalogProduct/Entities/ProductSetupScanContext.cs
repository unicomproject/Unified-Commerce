using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class ProductSetupScanContext : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public string AcquisitionMode { get; protected set; } = string.Empty;
    public string? CandidateIdentifier { get; protected set; }
    public string? IdentifierStandard { get; protected set; }
    public string? SymbologyHint { get; protected set; }
    public string? NoBarcodeReason { get; protected set; }
    public string? ExternalLookupStatus { get; protected set; }
    public string? ExternalSourceReference { get; protected set; }
    public string? NormalizedPrefillJson { get; protected set; }
    public string? GeneratedSkuCandidate { get; protected set; }
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }
    public long RowVersion { get; protected set; } = 1;

    protected ProductSetupScanContext() { }

    public static ProductSetupScanContext Create(
        Guid id,
        Guid tenantId,
        Guid productId,
        string acquisitionMode,
        string? candidateIdentifier,
        string? identifierStandard,
        string? symbologyHint,
        string? noBarcodeReason,
        string? externalLookupStatus,
        string? externalSourceReference,
        string? normalizedPrefillJson,
        string? generatedSkuCandidate,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new ProductSetupScanContext
        {
            Id = id,
            TenantId = tenantId,
            ProductId = productId,
            AcquisitionMode = NormalizeRequiredCode(acquisitionMode),
            CandidateIdentifier = Normalize(candidateIdentifier),
            IdentifierStandard = NormalizeCode(identifierStandard),
            SymbologyHint = NormalizeCode(symbologyHint),
            NoBarcodeReason = NormalizeCode(noBarcodeReason),
            ExternalLookupStatus = NormalizeCode(externalLookupStatus),
            ExternalSourceReference = Normalize(externalSourceReference),
            NormalizedPrefillJson = Normalize(normalizedPrefillJson),
            GeneratedSkuCandidate = Normalize(generatedSkuCandidate),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1
        };
    }

    public void Update(
        string acquisitionMode,
        string? candidateIdentifier,
        string? identifierStandard,
        string? symbologyHint,
        string? noBarcodeReason,
        string? externalLookupStatus,
        string? externalSourceReference,
        string? normalizedPrefillJson,
        string? generatedSkuCandidate,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        AcquisitionMode = NormalizeRequiredCode(acquisitionMode);
        CandidateIdentifier = Normalize(candidateIdentifier);
        IdentifierStandard = NormalizeCode(identifierStandard);
        SymbologyHint = NormalizeCode(symbologyHint);
        NoBarcodeReason = NormalizeCode(noBarcodeReason);
        ExternalLookupStatus = NormalizeCode(externalLookupStatus);
        ExternalSourceReference = Normalize(externalSourceReference);
        NormalizedPrefillJson = Normalize(normalizedPrefillJson);
        GeneratedSkuCandidate = Normalize(generatedSkuCandidate);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
        RowVersion += 1;
    }

    public void ReplaceGeneratedSkuCandidate(
        string generatedSkuCandidate,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        if (!string.Equals(AcquisitionMode, "NO_BARCODE", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AUTO SKU base is only valid for a no-barcode Product Setup context.");
        }

        GeneratedSkuCandidate = Normalize(generatedSkuCandidate)
            ?? throw new ArgumentException("Generated SKU base is required.", nameof(generatedSkuCandidate));
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
        RowVersion += 1;
    }

    private static string NormalizeRequiredCode(string value) =>
        value.Trim().ToUpperInvariant();

    private static string? NormalizeCode(string? value) =>
        Normalize(value)?.ToUpperInvariant();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
