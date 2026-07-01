using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Infrastructure.Modules.PlatformAdministration.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.PlatformAdministration.Services;

public sealed class TokenHashService : ITokenHashService
{
    private readonly PlatformJwtOptions _options;

    public TokenHashService(IOptions<PlatformJwtOptions> options)
    {
        _options = options.Value;
    }

    public string HashToken(string token)
    {
        // Hash tokens before persistence so stored values cannot be used as bearer credentials.
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        return Base64Url.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(token)));
    }
}
