namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);
