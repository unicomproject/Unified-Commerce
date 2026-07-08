using E_POS.Domain.Common.Entities;

namespace E_POS.Domain.Modules.Shared.Integration.Entities;

public class PlatformIntegrationCredential : AuditableEntity
{
    public Guid PlatformIntegrationId { get; protected set; }
    public string CredentialName { get; protected set; } = string.Empty;
    public string CredentialType { get; protected set; } = string.Empty;
    public string EncryptedValue { get; protected set; } = string.Empty;
    public string EncryptionKeyId { get; protected set; } = string.Empty;
    public string? CredentialKeyVersion { get; protected set; }
    public DateTimeOffset? ExpiresAt { get; protected set; }
    public DateTimeOffset? LastRotatedAt { get; protected set; }
    public DateTimeOffset? RevokedAt { get; protected set; }
    public string Status { get; protected set; } = string.Empty;
    public Guid? CreatedByPlatformUserId { get; protected set; }
}

