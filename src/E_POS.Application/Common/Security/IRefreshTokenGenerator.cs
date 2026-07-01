namespace E_POS.Application.Common.Security;

public interface IRefreshTokenGenerator
{
    RefreshTokenResult CreateRefreshToken(int lifetimeDays);
}