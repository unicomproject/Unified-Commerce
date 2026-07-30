namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPlatformAdminSeedData
{
    public const string Email = "posunique001@gmail.com";
    public const string Password = "Admin@123";

    // Generated once with PasswordHashService.HashPassword(Password) for migration seeding.
    public const string PasswordHash =
        "PBKDF2-SHA256:100000:B3G83oiz74Jq8+Zv7ee0dw==:j1sFOiYVSHBURb3i2QO7j8v+SF3dtysiuAuc/Ww/7Ig=";

    public static string UpSql =>
        $"""
        UPDATE platform_users
        SET password_hash = '{PasswordHash}',
            status = 'ACTIVE',
            updated_at = now()
        WHERE normalized_email = '{PlatformAdminSeedConstants.DevelopmentPlatformUserEmail}';
        """;
}
