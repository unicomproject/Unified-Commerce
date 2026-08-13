namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;

public sealed class InvitationDeliverySecretOptions
{
    public const string SectionName = "TenantUserInvitationDeliverySecret";
    public string Key { get; init; } = string.Empty;
    public string KeyVersion { get; init; } = "v1";
}
