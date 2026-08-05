using System.Security.Cryptography;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;

public sealed class InvitationTokenService : IInvitationTokenService
{
    private readonly ITokenHashService _hashService;
    private readonly TenantJwtOptions _options;

    public InvitationTokenService(ITokenHashService hashService, IOptions<TenantJwtOptions> options)
    {
        _hashService = hashService;
        _options = options.Value;
    }

    public string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
            throw new InvalidOperationException("Invitation hashing key is not configured.");
        return _hashService.HashToken(rawToken, _options.SigningKey);
    }
}
