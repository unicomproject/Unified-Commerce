namespace E_POS.Application.Modules.Tenant.TenantAuth.Dtos;

public sealed record TenantLoginUserDto(
    Guid TenantUserId,
    Guid TenantId,
    string Email,
    string Status,
    string TenantStatus);
