using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface ITenantAdminContextRepository
{
    Task<TenantAdminContextData?> GetContextDataAsync(
        Guid tenantUserId,
        Guid tenantId,
        CancellationToken cancellationToken);
}

public sealed record TenantAdminContextData(
    Guid TenantId,
    string TenantName,
    string TenantTimezone,
    string CurrencyCode,
    string Locale,
    Guid UserId,
    string? FirstName,
    string? LastName,
    IReadOnlyList<TenantAdminContextRoleDto> Roles,
    IReadOnlyList<TenantAdminContextOutletDto> Outlets,
    IReadOnlyList<Guid> AccessibleOutletIds,
    IReadOnlyList<string> EnabledFeatures,
    IReadOnlyList<string> EffectivePermissions,
    string SubscriptionStatus);

