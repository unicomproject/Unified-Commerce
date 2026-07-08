using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Persistence.Seed;

/// <summary>
/// Applies TM-EPOS platform permission seed data through EF for tests and local verification.
/// Mirrors <see cref="PlatformAdminPermissionsSeedData"/> without executing PostgreSQL SQL.
/// </summary>
public static class PlatformAdminPermissionSeedApplicator
{
    public static async Task ApplyAsync(EPosDbContext dbContext, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        foreach (var definition in PlatformAdminPermissionsSeedData.Definitions)
        {
            var existingPermission = await dbContext.PlatformPermissions
                .FirstOrDefaultAsync(x => x.PermissionCode == definition.PermissionCode, cancellationToken);

            if (existingPermission is null)
            {
                dbContext.PlatformPermissions.Add(PlatformPermission.Create(
                    definition.Id,
                    definition.PermissionCode,
                    definition.Name,
                    definition.Description,
                    PlatformAuthConstants.ActiveStatus,
                    now));
            }
        }

        var superAdminRole = await dbContext.PlatformRoles
            .FirstOrDefaultAsync(x => x.RoleCode == PlatformRoleCodes.SuperAdministrator, cancellationToken);

        if (superAdminRole is null)
        {
            superAdminRole = PlatformRole.Create(
                PlatformAdminSeedConstants.SuperAdministratorRoleId,
                PlatformRoleCodes.SuperAdministrator,
                "Super Administrator",
                "Full TM-EPOS platform administration access.",
                PlatformAuthConstants.ActiveStatus,
                now);

            dbContext.PlatformRoles.Add(superAdminRole);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var permissions = await dbContext.PlatformPermissions
            .Where(x => PlatformPermissionCodes.All.Contains(x.PermissionCode))
            .ToListAsync(cancellationToken);

        var rolePermissionIndex = 1;
        foreach (var permission in permissions)
        {
            var rolePermissionId = Guid.Parse($"67000000-0000-0000-0000-{rolePermissionIndex:D12}");
            var exists = await dbContext.PlatformRolePermissions.AnyAsync(
                x => x.PlatformRoleId == superAdminRole.Id && x.PlatformPermissionId == permission.Id,
                cancellationToken);

            if (!exists)
            {
                dbContext.PlatformRolePermissions.Add(PlatformRolePermission.Create(
                    rolePermissionId,
                    superAdminRole.Id,
                    permission.Id,
                    "TM-EPOS super administrator permission seed.",
                    now));
            }

            rolePermissionIndex++;
        }

        var platformUser = await dbContext.PlatformUsers
            .FirstOrDefaultAsync(x => x.Id == PlatformAdminSeedConstants.DevelopmentPlatformUserId, cancellationToken);

        if (platformUser is not null)
        {
            var userRoleExists = await dbContext.PlatformUserRoles.AnyAsync(
                x => x.PlatformUserId == platformUser.Id && x.PlatformRoleId == superAdminRole.Id,
                cancellationToken);

            if (!userRoleExists)
            {
                dbContext.PlatformUserRoles.Add(PlatformUserRole.Create(
                    PlatformAdminSeedConstants.DevelopmentPlatformUserRoleId,
                    platformUser.Id,
                    superAdminRole.Id,
                    "Assign TM-EPOS super administrator role to development platform admin.",
                    now));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

