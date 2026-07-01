namespace E_POS.Application.Common.Security;

public sealed record RefreshTokenResult(string Token, DateTimeOffset ExpiresAt);