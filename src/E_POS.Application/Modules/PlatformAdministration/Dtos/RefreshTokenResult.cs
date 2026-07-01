namespace E_POS.Application.Modules.PlatformAdministration.Dtos;

public sealed record RefreshTokenResult(string Token, DateTimeOffset ExpiresAt);
