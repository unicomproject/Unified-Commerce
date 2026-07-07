using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class Currency : AuditableEntity
{
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string CurrencyName { get; protected set; } = string.Empty;
    public string? CurrencySymbol { get; protected set; }
    public int DecimalPlaces { get; protected set; }
    public bool IsActive { get; protected set; }
    public int SortOrder { get; protected set; }

    public static Currency Create(
        Guid id,
        string currencyCode,
        string currencyName,
        string? currencySymbol,
        int decimalPlaces,
        bool isActive,
        int sortOrder,
        DateTimeOffset now)
    {
        return new Currency
        {
            Id = id,
            CurrencyCode = currencyCode.Trim(),
            CurrencyName = currencyName.Trim(),
            CurrencySymbol = currencySymbol?.Trim(),
            DecimalPlaces = decimalPlaces,
            IsActive = isActive,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
