using E_POS.Application.Common.Models;
using E_POS.Application.Modules.TenantAdministration.Contracts;
using E_POS.Application.Modules.TenantAdministration.Dtos;

namespace E_POS.Application.Modules.TenantAdministration.Services;

public sealed class TenantAdminContextService : ITenantAdminContextService
{
    private static readonly ApplicationError UserNotFound = new(
        "tenant_admin_context.user_not_found",
        "Tenant admin context could not be loaded.");

    private readonly ITenantAdminContextRepository _repository;

    public TenantAdminContextService(ITenantAdminContextRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApplicationResult<TenantAdminContextDto>> GetContextAsync(
        Guid tenantUserId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantUserId == Guid.Empty || tenantId == Guid.Empty)
        {
            return ApplicationResult<TenantAdminContextDto>.Failure(UserNotFound);
        }

        var data = await _repository.GetContextDataAsync(tenantUserId, tenantId, cancellationToken);

        if (data is null)
        {
            return ApplicationResult<TenantAdminContextDto>.Failure(UserNotFound);
        }

        var fullName = BuildFullName(data.FirstName, data.LastName);

        var dto = new TenantAdminContextDto(
            Tenant: new TenantAdminContextTenantDto(data.TenantId, data.TenantName),
            User: new TenantAdminContextUserDto(data.UserId, fullName),
            Roles: data.Roles,
            Outlets: data.Outlets,
            EnabledFeatures: data.EnabledFeatures,
            EffectivePermissions: data.EffectivePermissions,
            RuntimeFlags: [],
            Subscription: new TenantAdminContextSubscriptionDto(data.SubscriptionStatus));

        return ApplicationResult<TenantAdminContextDto>.Success(dto);
    }

    private static string BuildFullName(string? firstName, string? lastName)
    {
        var parts = new[] { firstName, lastName }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var name = string.Join(" ", parts).Trim();
        return string.IsNullOrWhiteSpace(name) ? "Tenant Admin" : name;
    }
}
