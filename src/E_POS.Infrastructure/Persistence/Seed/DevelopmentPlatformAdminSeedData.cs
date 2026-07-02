namespace E_POS.Infrastructure.Persistence.Seed;

public static class DevelopmentPlatformAdminSeedData
{
    public const string Email = "posunique001@gmail.com";
    public const string Password = "Admin@12345";

    // Generated once with PasswordHashService.HashPassword(Password) for migration seeding.
    public const string PasswordHash =
        "PBKDF2-SHA256:100000:gg/mfMOXBS5PYc1Pu563tA==:sK9KInrLUc17SzdjICyBl1GvErK/wzQ6gLfTYO1pLNU=";

    public static string UpSql =>
        $"""
        UPDATE platform_users
        SET password_hash = '{PasswordHash}',
            status = 'ACTIVE',
            updated_at = now()
        WHERE normalized_email = '{PlatformAdminSeedConstants.DevelopmentPlatformUserEmail}';
        """;
}
