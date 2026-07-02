namespace E_POS.Api.Models;

public sealed record LegacyPlatformLoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset SessionExpiresAt,
    LegacyPlatformLoginUserResponse User);

public sealed record LegacyPlatformLoginUserResponse(
    Guid Id,
    string Email,
    string FullName,
    string Status,
    IReadOnlyList<string> PlatformPermissions);
