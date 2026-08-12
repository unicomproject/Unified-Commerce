namespace E_POS.Application.Modules.Tenant.TenantAuth.Contracts;

public sealed record ProtectedInvitationDeliverySecret(string Ciphertext, string KeyVersion);

public interface IInvitationDeliverySecretProtector
{
    ProtectedInvitationDeliverySecret Protect(string rawToken);
    string Unprotect(string ciphertext, string keyVersion);
}
