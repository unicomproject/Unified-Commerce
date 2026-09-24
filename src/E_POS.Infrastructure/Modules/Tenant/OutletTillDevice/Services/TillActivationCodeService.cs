using System.Security.Cryptography;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Services;

public sealed class TillActivationCodeService(EPosDbContext db, IDateTimeProvider clock,
    ITenantFeatureEntitlementEvaluator entitlements) : ITillActivationCodeService
{
    public async Task<IssuedTillActivationCode> IssueAsync(TenantRequestContext actor, Guid tillId, CancellationToken ct)
    {
        if (!actor.HasPermission(TenantAdminTillPermissions.Manage) ||
            !await entitlements.IsEnabledAsync(actor.TenantId, PlatformTenantFeatureCodes.TillManagement, clock.UtcNow, ct))
            throw new UnauthorizedAccessException("Till access denied.");
        var user = await db.TenantUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Id == actor.UserId && u.TenantId == actor.TenantId && u.AccountStatus == "ACTIVE", ct);
        var till = await db.Tills.AsNoTracking().SingleOrDefaultAsync(t => t.Id == tillId && t.TenantId == actor.TenantId && t.Status == "ACTIVE", ct);
        if (user == null || till == null || !await db.Outlets.AnyAsync(o => o.Id == till.OutletId && o.TenantId == actor.TenantId && o.Status == "ACTIVE", ct))
            throw new UnauthorizedAccessException("Till access denied.");
        var outletAllowed = user.OutletAccessScope == "ALL_OUTLETS" ||
            (user.OutletAccessScope == "SELECTED_OUTLETS" &&
             (await db.OutletUserRoles.AnyAsync(x => x.TenantId == actor.TenantId && x.TenantUserId == actor.UserId && x.OutletId == till.OutletId && x.RevokedAt == null, ct) ||
              await db.OutletUserPermissions.AnyAsync(x => x.TenantId == actor.TenantId && x.TenantUserId == actor.UserId && x.OutletId == till.OutletId && x.RevokedAt == null, ct)));
        var tillAllowed = user.TillAccessScope == "ALL_ACCESSIBLE_TILLS" ||
            (user.TillAccessScope == "SELECTED_TILLS" && await db.TenantUserTillAccess.AnyAsync(x => x.TenantId == actor.TenantId && x.TenantUserId == actor.UserId && x.TillId == tillId && x.RevokedAt == null, ct));
        if (!outletAllowed || !tillAllowed) throw new UnauthorizedAccessException("Till access denied.");
        var assignments = await db.TillDeviceAssignments.AsNoTracking().Where(a => a.TenantId == actor.TenantId && a.TillId == tillId && a.ReleasedAt == null).ToListAsync(ct);
        if (assignments.Count != 1 || assignments[0].OutletId != till.OutletId)
            throw new InvalidOperationException("Assign one active POS device to this till first.");
        var deviceId = assignments[0].PosDeviceId;
        if (!await db.PosDevices.AnyAsync(d => d.Id == deviceId && d.TenantId == actor.TenantId && d.OutletId == till.OutletId && d.Status == "ACTIVE", ct))
            throw new InvalidOperationException("Assign one active POS device to this till first.");
        var now = clock.UtcNow;
        if (await db.TillActivationCodes.AnyAsync(c => c.TenantId == actor.TenantId && c.TillId == tillId && c.Status == "ACTIVE" && c.ExpiresAt > now, ct))
            throw new InvalidOperationException("An unused activation code is still active. Use it or wait for it to expire.");
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var expires = now.AddMinutes(10);
        db.TillActivationCodes.Add(TillActivationCode.Create(Guid.NewGuid(), actor.TenantId, till.OutletId, tillId,
            DeviceFingerprintHasher.Hash(code), actor.UserId, expires, now));
        await db.SaveChangesAsync(ct);
        return new(code, expires);
    }
}
