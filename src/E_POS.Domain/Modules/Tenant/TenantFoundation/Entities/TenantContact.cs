using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Entities;

public sealed class TenantContact : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public string ContactType { get; private set; } = string.Empty;
    public string ContactName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string Status { get; private set; } = "ACTIVE";
    public Guid CreatedByPlatformUserId { get; private set; }
    public Guid UpdatedByPlatformUserId { get; private set; }

    public static TenantContact Create(Guid id, Guid tenantId, string contactType, string contactName,
        string? email, string? phone, Guid actorId, DateTimeOffset now)
    {
        var type = contactType.Trim().ToUpperInvariant();
        if (type is not ("BILLING" or "SUPPORT"))
            throw new ArgumentOutOfRangeException(nameof(contactType));
        var name = contactName.Trim();
        var normalizedEmail = Normalize(email)?.ToLowerInvariant();
        var normalizedPhone = Normalize(phone);
        if (name.Length is < 2 or > 200)
            throw new ArgumentOutOfRangeException(nameof(contactName));
        if (type == "BILLING" && normalizedEmail is null)
            throw new ArgumentException("Billing contact email is required.", nameof(email));
        if (type == "SUPPORT" && normalizedEmail is null && normalizedPhone is null)
            throw new ArgumentException("Support contact email or phone is required.", nameof(email));

        return new TenantContact
        {
            Id = id,
            TenantId = tenantId,
            ContactType = type,
            ContactName = name,
            Email = normalizedEmail,
            Phone = normalizedPhone,
            CreatedByPlatformUserId = actorId,
            UpdatedByPlatformUserId = actorId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
