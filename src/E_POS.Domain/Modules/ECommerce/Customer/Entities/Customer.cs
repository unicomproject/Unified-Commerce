using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.ECommerce.Customer.Entities;

public class Customer : AuditableEntity
{
    public Guid TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty; // Maps to display_name
    public string? NormalizedEmail { get; protected set; }
    public string? NormalizedPhone { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public string CustomerCode { get; protected set; } = string.Empty;
    public string? FirstName { get; protected set; }
    public string? LastName { get; protected set; }
    public string? Email { get; protected set; }
    public string? Phone { get; protected set; }
    public string SourceType { get; protected set; } = string.Empty;
    public Guid? SourceSalesChannelId { get; protected set; }
    public DateTimeOffset? AnonymizedAt { get; protected set; }

    public static Customer CreatePosCustomer(
        Guid id,
        Guid tenantId,
        string customerCode,
        string fullName,
        string phone,
        string? email,
        Guid createdBy,
        DateTimeOffset now)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedPhone = NormalizePhone(phone);

        return new Customer
        {
            Id = id,
            TenantId = tenantId,
            CustomerCode = customerCode.Trim().ToUpperInvariant(),
            Name = fullName.Trim(),
            Phone = phone.Trim(),
            NormalizedPhone = normalizedPhone,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            NormalizedEmail = normalizedEmail,
            SourceType = "POS",
            Status = "ACTIVE",
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static string NormalizePhone(string phone)
    {
        var trimmed = phone.Trim();
        var prefix = trimmed.StartsWith('+') ? "+" : string.Empty;
        return prefix + new string(trimmed.Where(char.IsDigit).ToArray());
    }

    public static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToUpperInvariant();

    /// <summary>
    /// Updates POS-editable profile fields. CustomerCode and SourceType remain immutable.
    /// </summary>
    public void UpdatePosProfile(
        string fullName,
        string phone,
        string? email,
        string status,
        Guid updatedBy,
        DateTimeOffset now)
    {
        Name = fullName.Trim();
        Phone = phone.Trim();
        NormalizedPhone = NormalizePhone(phone);
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        NormalizedEmail = NormalizeEmail(email);
        Status = status.Trim().ToUpperInvariant();
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }

    public static bool IsAllowedPosStatus(string status)
    {
        var normalized = status.Trim().ToUpperInvariant();
        return normalized is "ACTIVE" or "INACTIVE" or "BLOCKED";
    }
}
