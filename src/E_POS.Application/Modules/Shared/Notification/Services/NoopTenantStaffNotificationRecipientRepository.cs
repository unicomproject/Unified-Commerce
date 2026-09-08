using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;

namespace E_POS.Application.Modules.Shared.Notification.Services;

public sealed class NoopTenantStaffNotificationRecipientRepository : ITenantStaffNotificationRecipientRepository
{
    public static NoopTenantStaffNotificationRecipientRepository Instance { get; } = new();

    private NoopTenantStaffNotificationRecipientRepository()
    {
    }

    public Task<IReadOnlyList<Guid>> GetActiveStaffTenantUserIdsAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
}
