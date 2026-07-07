using System.Text.Json.Serialization;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformAdminLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    // Refresh token is delivered through an HttpOnly cookie instead of JSON.
    [property: JsonIgnore] string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    PlatformAdminUserDto User,
    IReadOnlyList<string> Permissions);

