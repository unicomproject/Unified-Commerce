namespace E_POS.Application.Common.Security;

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);