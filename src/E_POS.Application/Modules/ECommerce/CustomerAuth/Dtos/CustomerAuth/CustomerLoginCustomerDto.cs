namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

public sealed record CustomerLoginCustomerDto(
    Guid Id,
    Guid TenantId,
    string DisplayName,
    string? Email,
    string? Phone);
