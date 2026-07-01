namespace E_POS.Application.Common.Security;

public sealed record JwtTokenDescriptor(
    string Issuer,
    string Audience,
    string SigningKey,
    int AccessTokenMinutes,
    IReadOnlyDictionary<string, object> Claims);