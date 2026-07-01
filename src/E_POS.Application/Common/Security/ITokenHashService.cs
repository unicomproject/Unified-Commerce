namespace E_POS.Application.Common.Security;

public interface ITokenHashService
{
    string HashToken(string token, string signingKey);
}