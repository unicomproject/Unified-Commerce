using E_POS.Domain.Common.Entities;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public class Tenant : AuditableEntity
{
    public string TenantCode { get; protected set; } = string.Empty;
    public string CurrencyCode { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string Status { get; protected set; } = string.Empty;
    public string BaseCurrency { get; protected set; } = string.Empty;
    public string BillingStatus { get; protected set; } = string.Empty;
    public string? BusinessType { get; protected set; }
    public Guid BusinessTypeId { get; protected set; }
    public string DefaultLocale { get; protected set; } = string.Empty;
    public string DefaultTimezone { get; protected set; } = string.Empty;
    public string OperatingMode { get; protected set; } = string.Empty;
    public string PrimaryDomain { get; protected set; } = string.Empty;

    public static Tenant Create(
        Guid id,
        string tenantCode,
        string name,
        string status,
        string billingStatus,
        DateTimeOffset createdAt,
        Guid? businessTypeId = null)
    {
        var code = tenantCode.Trim();
        return new Tenant
        {
            Id = id,
            TenantCode = code,
            CurrencyCode = "LKR",
            Name = name.Trim(),
            Status = status,
            BaseCurrency = "LKR",
            BillingStatus = billingStatus,
            BusinessTypeId = businessTypeId ?? Guid.Parse("44444444-0002-4000-8000-000000000001"),
            DefaultLocale = "en-LK",
            DefaultTimezone = "Asia/Colombo",
            OperatingMode = "unified_epos",
            PrimaryDomain = $"{code.ToLowerInvariant()}.local",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public static Tenant CreateDraft(
        Guid id,
        string tenantCode,
        string name,
        string billingStatus,
        string baseCurrency,
        string defaultTimezone,
        string defaultLocale,
        string operatingMode,
        string? businessType,
        Guid? businessTypeId,
        DateTimeOffset createdAt)
    {
        var code = tenantCode.Trim();
        return new Tenant
        {
            Id = id,
            TenantCode = code,
            CurrencyCode = baseCurrency.Trim(),
            Name = name.Trim(),
            Status = TenantStatusConstants.Draft,
            BaseCurrency = baseCurrency.Trim(),
            BillingStatus = billingStatus,
            BusinessType = NormalizeOptionalText(businessType),
            BusinessTypeId = businessTypeId ?? Guid.Parse("44444444-0002-4000-8000-000000000001"),
            DefaultLocale = defaultLocale.Trim(),
            DefaultTimezone = defaultTimezone.Trim(),
            OperatingMode = operatingMode.Trim(),
            PrimaryDomain = $"{code.ToLowerInvariant()}.local",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void UpdateDetails(
        string name,
        string baseCurrency,
        string defaultTimezone,
        string defaultLocale,
        string operatingMode,
        string? businessType,
        string billingStatus,
        DateTimeOffset now)
    {
        Name = name.Trim();
        BaseCurrency = baseCurrency.Trim();
        CurrencyCode = baseCurrency.Trim();
        DefaultTimezone = defaultTimezone.Trim();
        DefaultLocale = defaultLocale.Trim();
        OperatingMode = operatingMode.Trim();
        BusinessType = NormalizeOptionalText(businessType);
        BillingStatus = billingStatus;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        Status = TenantStatusConstants.Active;
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        Status = TenantStatusConstants.Suspended;
        UpdatedAt = now;
    }

    public void TouchUpdatedAt(DateTimeOffset now)
    {
        UpdatedAt = now;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

