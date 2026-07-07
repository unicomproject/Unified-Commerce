using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.PricingTax.Entities;

public class PriceList : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string PriceListCode { get; protected set; } = string.Empty;
    public string PriceListName { get; protected set; } = string.Empty;
    public string PriceListType { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public bool PriceIncludesTax { get; protected set; }
    public bool IsDefaultPriceList { get; protected set; }
    public int Priority { get; protected set; }
    public DateTimeOffset? ValidFrom { get; protected set; }
    public DateTimeOffset? ValidUntil { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByTenantUserId { get; protected set; }
    public Guid? UpdatedByTenantUserId { get; protected set; }

    public static PriceList Create(
        Guid id,
        Guid tenantId,
        string priceListCode,
        string priceListName,
        string priceListType,
        string currencyCode,
        bool priceIncludesTax,
        bool isDefaultPriceList,
        int priority,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        string status,
        Guid? createdByTenantUserId,
        DateTimeOffset now)
    {
        return new PriceList
        {
            Id = id,
            TenantId = tenantId,
            PriceListCode = priceListCode.Trim().ToUpperInvariant(),
            PriceListName = priceListName.Trim(),
            PriceListType = priceListType.Trim().ToUpperInvariant(),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            PriceIncludesTax = priceIncludesTax,
            IsDefaultPriceList = isDefaultPriceList,
            Priority = priority,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            Status = status.Trim().ToUpperInvariant(),
            CreatedByTenantUserId = createdByTenantUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateProfile(
        string priceListCode,
        string priceListName,
        string priceListType,
        string currencyCode,
        bool priceIncludesTax,
        bool isDefaultPriceList,
        int priority,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now)
    {
        PriceListCode = priceListCode.Trim().ToUpperInvariant();
        PriceListName = priceListName.Trim();
        PriceListType = priceListType.Trim().ToUpperInvariant();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        PriceIncludesTax = priceIncludesTax;
        IsDefaultPriceList = isDefaultPriceList;
        Priority = priority;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        Status = status.Trim().ToUpperInvariant();
        UpdatedByTenantUserId = updatedByTenantUserId;
        UpdatedAt = now;
    }

    public void SoftDelete(Guid? deletedByUserId, DateTimeOffset now)
    {
        Status = "DELETED";
        UpdatedByTenantUserId = deletedByUserId;
        UpdatedAt = now;
    }

    public void ClearDefaultFlag()
    {
        IsDefaultPriceList = false;
    }
}

