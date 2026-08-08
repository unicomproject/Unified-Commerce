using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;

public class Product : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string ProductCode { get; protected set; } = string.Empty;
    public string ProductName { get; protected set; } = string.Empty;
    public string ProductSlug { get; protected set; } = string.Empty;
    public string ProductType { get; protected set; } = string.Empty;
    public string ProductStructure { get; protected set; } = string.Empty;
    public Guid? BusinessTypeId { get; protected set; }
    public Guid? BrandId { get; protected set; }
    public Guid? ReturnPolicyId { get; protected set; }
    public string? ShortDescription { get; protected set; }
    public string? LongDescription { get; protected set; }
    public bool IsSellable { get; protected set; } = true;
    public bool IsTaxable { get; protected set; } = true;
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public int CurrentSetupStep { get; protected set; } = 1;
    public DateTimeOffset? DraftSavedAt { get; protected set; }
    public DateTimeOffset? PublishedAt { get; protected set; }
    public Guid? PublishedByTenantUserId { get; protected set; }
    public DateTimeOffset? ArchivedAt { get; protected set; }
    public Guid? ArchivedByTenantUserId { get; protected set; }
    public long RowVersion { get; protected set; } = 1;

    public static Product Create(
        Guid id,
        Guid tenantId,
        string productCode,
        string productName,
        string productSlug,
        string productType,
        string productStructure,
        Guid? businessTypeId,
        Guid? brandId,
        Guid? returnPolicyId,
        string? shortDescription,
        string? longDescription,
        bool isSellable,
        bool isTaxable,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new Product
        {
            Id = id,
            TenantId = tenantId,
            ProductCode = ProductConstants.NormalizeCode(productCode),
            ProductName = productName.Trim(),
            ProductSlug = productSlug.Trim().ToLowerInvariant(),
            ProductType = productType.Trim().ToUpperInvariant(),
            ProductStructure = productStructure.Trim().ToUpperInvariant(),
            BusinessTypeId = businessTypeId,
            BrandId = brandId,
            ReturnPolicyId = returnPolicyId,
            ShortDescription = shortDescription?.Trim(),
            LongDescription = longDescription?.Trim(),
            IsSellable = isSellable,
            IsTaxable = isTaxable,
            Status = ProductConstants.NormalizeStatus(status),
            CreatedByTenantUserId = createdByTenantUserId,
            UpdatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now,
            CurrentSetupStep = status == ProductConstants.DraftStatus ? 1 : 8,
            DraftSavedAt = status == ProductConstants.DraftStatus ? now : null,
            PublishedAt = status != ProductConstants.DraftStatus ? now : null,
            PublishedByTenantUserId = status != ProductConstants.DraftStatus ? createdByTenantUserId : null,
            RowVersion = 1
        };
    }

    public void UpdateProfile(
        string productCode,
        string productName,
        string productSlug,
        string productType,
        string productStructure,
        Guid? businessTypeId,
        Guid? brandId,
        Guid? returnPolicyId,
        string? shortDescription,
        string? longDescription,
        bool isSellable,
        bool isTaxable,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        ProductCode = ProductConstants.NormalizeCode(productCode);
        ProductName = productName.Trim();
        ProductSlug = productSlug.Trim().ToLowerInvariant();
        ProductType = productType.Trim().ToUpperInvariant();
        ProductStructure = productStructure.Trim().ToUpperInvariant();
        BusinessTypeId = businessTypeId;
        BrandId = brandId;
        ReturnPolicyId = returnPolicyId;
        ShortDescription = shortDescription?.Trim();
        LongDescription = longDescription?.Trim();
        IsSellable = isSellable;
        IsTaxable = isTaxable;
        Status = ProductConstants.NormalizeStatus(status);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
        if (status == ProductConstants.DraftStatus)
        {
            DraftSavedAt = now;
        }
    }

    public void UpdateStatus(string status, Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = ProductConstants.NormalizeStatus(status);
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = ProductConstants.ArchivedStatus;
        ArchivedAt = now;
        ArchivedByTenantUserId = updatedByTenantUserId;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void Archive(Guid? updatedByTenantUserId, DateTimeOffset now)
    {
        Status = ProductConstants.ArchivedStatus;
        ArchivedAt = now;
        ArchivedByTenantUserId = updatedByTenantUserId;
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SetDraftSaved(int setupStep, DateTimeOffset now)
    {
        Status = ProductConstants.DraftStatus;
        CurrentSetupStep = setupStep;
        DraftSavedAt = now;
        UpdatedAt = now;
    }

    public void SetPublished(Guid? userId, DateTimeOffset now)
    {
        PublishedAt = now;
        PublishedByTenantUserId = userId;
        CurrentSetupStep = 8;
        UpdatedAt = now;
    }

    public void IncrementRowVersion()
    {
        RowVersion++;
    }
}

