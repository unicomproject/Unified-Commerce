namespace E_POS.Application.Modules.Tenant.TenantAuth.Contracts;

public sealed record TenantLoginAccount(
    Guid TenantUserId,
    Guid TenantId,
    string Email,
    string? PasswordHash,
    string UserStatus,
    string TenantStatus);
