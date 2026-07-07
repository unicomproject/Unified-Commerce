namespace E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;

public sealed record TenantAdminContextDto(
    TenantAdminContextTenantDto Tenant,
    TenantAdminContextUserDto User,
    IReadOnlyList<TenantAdminContextRoleDto> Roles,
    IReadOnlyList<TenantAdminContextOutletDto> Outlets,
    IReadOnlyList<string> EnabledFeatures,
    IReadOnlyList<string> EffectivePermissions,
    IReadOnlyList<string> RuntimeFlags,
    TenantAdminContextSubscriptionDto Subscription);

public sealed record TenantAdminContextTenantDto(Guid Id, string Name);
public sealed record TenantAdminContextUserDto(Guid Id, string FullName);
public sealed record TenantAdminContextRoleDto(Guid Id, string Name);
public sealed record TenantAdminContextOutletDto(Guid Id, string Name);
public sealed record TenantAdminContextSubscriptionDto(string Status);

