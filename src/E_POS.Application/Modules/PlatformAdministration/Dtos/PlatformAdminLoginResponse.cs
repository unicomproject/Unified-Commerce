using System.Text.Json.Serialization;

namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record PlatformAdminLoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    [property: JsonIgnore] string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    PlatformAdminUserDto User,
    IReadOnlyList<string> Permissions);
