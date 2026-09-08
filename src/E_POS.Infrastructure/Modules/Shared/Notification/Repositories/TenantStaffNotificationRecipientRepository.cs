using E_POS.Application.Modules.Shared.Notification.Contracts.Repositories;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Shared.Notification.Repositories;

public sealed class TenantStaffNotificationRecipientRepository : ITenantStaffNotificationRecipientRepository
{
    private static readonly string[] NotifiableRoleCodes =
    {
        TenantUserConstants.DefaultTenantAdminRoleCode,
        TenantUserConstants.DefaultCashierRoleCode
    };

    private readonly EPosDbContext _dbContext;

    public TenantStaffNotificationRecipientRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Guid>> GetActiveStaffTenantUserIdsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var activeRoleIds = _dbContext.TenantRoles
            .AsNoTracking()
            .Where(role => role.TenantId == tenantId && role.IsActive && NotifiableRoleCodes.Contains(role.RoleCode))
            .Select(role => role.Id);

        var assignedTenantUserIds = _dbContext.TenantUserRoles
            .AsNoTracking()
            .Where(userRole => userRole.TenantId == tenantId
                && userRole.RevokedAt == null
                && activeRoleIds.Contains(userRole.TenantRoleId))
            .Select(userRole => userRole.TenantUserId);

        return await _dbContext.TenantUsers
            .AsNoTracking()
            .Where(user => user.TenantId == tenantId
                && user.AccountStatus == TenantUserConstants.StatusActive
                && assignedTenantUserIds.Contains(user.Id))
            .Select(user => user.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
