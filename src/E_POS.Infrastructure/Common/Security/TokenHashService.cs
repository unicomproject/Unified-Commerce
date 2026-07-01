using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Common.Security;

namespace E_POS.Infrastructure.Common.Security;

public sealed class TokenHashService : ITokenHashService
{
    public string HashToken(string token, string signingKey)
    {
        // Hash tokens before persistence so stored values cannot be used as bearer credentials.
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        return Base64Url.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(token)));
    }
}