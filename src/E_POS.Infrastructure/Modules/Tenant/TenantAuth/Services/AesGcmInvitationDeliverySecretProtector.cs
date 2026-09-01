using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Tenant.TenantAuth.Services;

public sealed class AesGcmInvitationDeliverySecretProtector : IInvitationDeliverySecretProtector
{
    private readonly byte[] _key;
    private readonly string _keyVersion;

    public AesGcmInvitationDeliverySecretProtector(IOptions<InvitationDeliverySecretOptions> options)
    {
        var value = options.Value;
        _key = Convert.FromBase64String(value.Key);
        if (_key.Length is not (16 or 24 or 32))
            throw new InvalidOperationException("Tenant user invitation delivery secret key must be a 128, 192, or 256-bit base64 key.");
        _keyVersion = string.IsNullOrWhiteSpace(value.KeyVersion) ? "v1" : value.KeyVersion.Trim();
    }

    public ProtectedInvitationDeliverySecret Protect(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(rawToken);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, Encoding.UTF8.GetBytes(_keyVersion));
        return new ProtectedInvitationDeliverySecret(Convert.ToBase64String(nonce.Concat(tag).Concat(ciphertext).ToArray()), _keyVersion);
    }

    public string Unprotect(string ciphertext, string keyVersion)
    {
        if (!string.Equals(keyVersion, _keyVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Invitation delivery secret key version is unavailable.");
        var bytes = Convert.FromBase64String(ciphertext);
        if (bytes.Length < 29) throw new CryptographicException("Invitation delivery secret is invalid.");
        var plaintext = new byte[bytes.Length - 28];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(bytes[..12], bytes[28..], bytes[12..28], plaintext, Encoding.UTF8.GetBytes(_keyVersion));
        return Encoding.UTF8.GetString(plaintext);
    }
}
