using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IRefreshTokenService
{
    RefreshTokenResult CreateRefreshToken();
}
