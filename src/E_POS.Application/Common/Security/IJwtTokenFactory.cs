namespace E_POS.Application.Common.Security;

public interface IJwtTokenFactory
{
    JwtTokenResult CreateAccessToken(JwtTokenDescriptor descriptor);
}