namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPasswordHashService
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
