using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IJwtTokenService
{
    JwtTokenResult CreateAccessToken(
        PlatformUser user,
        Guid sessionId,
        string jwtId,
        IReadOnlyList<string> permissions);
}
