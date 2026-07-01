namespace E_POS.Application.Modules.AuthSecurity.Dtos;

public sealed record TenantLoginRequest(string Email, string Password);