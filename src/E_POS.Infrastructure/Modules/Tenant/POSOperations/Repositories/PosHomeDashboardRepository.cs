using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_POS.Infrastructure.Modules.Tenant.POSOperations.Repositories;

public sealed class PosHomeDashboardRepository : IPosHomeDashboardRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly ILogger<PosHomeDashboardRepository> _logger;

    public PosHomeDashboardRepository(
        EPosDbContext dbContext,
        ILogger<PosHomeDashboardRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PosHomeContextResolutionResult> ResolveContextAsync(
        TenantRequestContext context,
        Guid? outletId,
        Guid? tillId,
        Guid? deviceId,
        string? deviceFingerprint,
        CancellationToken cancellationToken)
    {
        var cashier = await _dbContext.TenantUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == context.TenantId && x.Id == context.UserId,
                cancellationToken);

        if (cashier is null)
        {
            _logger.LogDebug(
                "POS home context unresolved: tenant user not found for tenant {TenantId}, user {UserId}.",
                context.TenantId,
                context.UserId);

            return Unresolved(
                PosHomeContextReasonCodes.UserContextMissing,
                "Cashier profile could not be resolved for the current session.",
                "Sign in again or contact your administrator.");
        }

        _logger.LogDebug(
            "POS home context: tenant user resolved {UserId}.",
            context.UserId);

        var requestedFingerprint = deviceFingerprint?.Trim() ?? string.Empty;
        if (requestedFingerprint.Length == 0)
        {
            _logger.LogDebug(
                "POS home context unresolved: device fingerprint missing for tenant {TenantId}.",
                context.TenantId);

            return Unresolved(
                PosHomeContextReasonCodes.DeviceContextMissing,
                "Current POS device could not be resolved.",
                "Activate this device or sign in from a registered POS device.");
        }

        var fingerprintHash = DeviceFingerprintHasher.Hash(requestedFingerprint);

        var resolvedDeviceId = deviceId is { } requestedDeviceId && requestedDeviceId != Guid.Empty
            ? requestedDeviceId
            : (Guid?)null;
        string deviceCode = string.Empty;
        string deviceName = string.Empty;
        string deviceStatus = string.Empty;
        Guid? clientTillHint = tillId is { } requestedTillId && requestedTillId != Guid.Empty
            ? requestedTillId
            : null;
        Guid? assignedTillId = null;

        if (resolvedDeviceId is null)
        {
            resolvedDeviceId = await ResolveTrustedDeviceIdByFingerprintAsync(
                context.TenantId,
                fingerprintHash,
                cancellationToken);
        }

        if (resolvedDeviceId is null || resolvedDeviceId == Guid.Empty)
        {
            _logger.LogDebug(
                "POS home context unresolved: device context missing for tenant {TenantId}.",
                context.TenantId);

            return Unresolved(
                PosHomeContextReasonCodes.DeviceContextMissing,
                "Current POS device could not be resolved.",
                "Activate this device or sign in from a registered POS device.");
        }

        var device = await _dbContext.PosDevices
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == context.TenantId &&
                     x.Id == resolvedDeviceId.Value &&
                     x.DeviceFingerprintHash == fingerprintHash,
                cancellationToken);

        if (device is null)
        {
            _logger.LogDebug(
                "POS home context unresolved: device {DeviceId} not found for tenant {TenantId}.",
                resolvedDeviceId,
                context.TenantId);

            return Unresolved(
                PosHomeContextReasonCodes.DeviceContextMissing,
                "Current POS device could not be resolved.",
                "Activate this device or sign in from a registered POS device.");
        }

        deviceCode = device.DeviceCode;
        deviceName = device.DeviceName;
        deviceStatus = device.Status;

        _logger.LogDebug(
            "POS home context: device resolved {DeviceId} ({DeviceCode}).",
            device.Id,
            device.DeviceCode);

        if (!string.Equals(device.Status, PosDeviceConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "POS home context unresolved: device {DeviceId} status is {Status}.",
                device.Id,
                device.Status);

            return Unresolved(
                PosHomeContextReasonCodes.DeviceContextMissing,
                "Current POS device is not active.",
                "Contact your administrator to activate this POS device.");
        }

        var assignment = await (from row in _dbContext.TillDeviceAssignments.AsNoTracking()
                join t in _dbContext.Tills.AsNoTracking() on row.TillId equals t.Id
                join outlet in _dbContext.Outlets.AsNoTracking() on t.OutletId equals outlet.Id
                where row.TenantId == context.TenantId &&
                      row.OutletId == t.OutletId &&
                      t.TenantId == context.TenantId &&
                      outlet.TenantId == context.TenantId &&
                      row.PosDeviceId == device.Id &&
                      row.ReleasedAt == null
                orderby row.AssignedAt descending
                select new
                {
                    TillId = t.Id,
                    TillOutletId = t.OutletId
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            _logger.LogDebug(
                "POS home context unresolved: no active till assignment for device {DeviceId}.",
                device.Id);

            return Unresolved(
                PosHomeContextReasonCodes.DeviceNotAssignedToTill,
                "This POS device is not assigned to a till.",
                "Assign this device to a till before starting POS operations.");
        }

        assignedTillId = assignment.TillId;
        if (assignedTillId is null || assignedTillId == Guid.Empty)
        {
            _logger.LogDebug(
                "POS home context unresolved: till could not be resolved for device {DeviceId}.",
                device.Id);

            return Unresolved(
                PosHomeContextReasonCodes.TillNotFound,
                "Assigned till could not be found.",
                "Verify till setup with your administrator.");
        }

        _logger.LogDebug(
            "POS home context: till assignment resolved till {TillId} for device {DeviceId}.",
            assignedTillId,
            device.Id);

        if (clientTillHint is not null && clientTillHint.Value != assignedTillId.Value)
        {
            _logger.LogDebug(
                "POS home context unresolved: client till {ClientTillId} does not match device-assigned till {AssignedTillId}.",
                clientTillHint,
                assignedTillId);

            return Unresolved(
                PosHomeContextReasonCodes.DeviceNotAssignedToTill,
                "This POS device is not assigned to the requested till.",
                "Refresh the POS device context or activate the correct device.");
        }

        var till = await _dbContext.Tills
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == context.TenantId && x.Id == assignedTillId,
                cancellationToken);

        if (till is null)
        {
            _logger.LogDebug(
                "POS home context unresolved: till {TillId} not found for tenant {TenantId}.",
                assignedTillId,
                context.TenantId);

            return Unresolved(
                PosHomeContextReasonCodes.TillNotFound,
                "Assigned till could not be found.",
                "Verify till setup with your administrator.");
        }

        if (!string.Equals(till.Status, TillConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "POS home context unresolved: till {TillId} status is {Status}.",
                till.Id,
                till.Status);

            return Unresolved(
                PosHomeContextReasonCodes.TillInactive,
                "Assigned till is not active.",
                "Activate the till or assign this device to an active till.");
        }

        var resolvedOutletId = till.OutletId;
        if (outletId is { } clientOutletId &&
            clientOutletId != Guid.Empty &&
            clientOutletId != resolvedOutletId)
        {
            _logger.LogDebug(
                "POS home context: client outlet {ClientOutletId} ignored; using till outlet {ResolvedOutletId}.",
                clientOutletId,
                resolvedOutletId);
        }

        _logger.LogDebug(
            "POS home context: till resolved {TillId} ({TillCode}).",
            till.Id,
            till.TillCode);

        var deviceTrusted = await IsDeviceTrustedAsync(
            context.TenantId,
            device.Id,
            fingerprintHash,
            cancellationToken);

        if (!deviceTrusted)
        {
            _logger.LogDebug(
                "POS home context unresolved: device {DeviceId} is not trusted.",
                device.Id);

            return Unresolved(
                PosHomeContextReasonCodes.DeviceNotTrusted,
                "This POS device is not trusted.",
                "Complete device activation or contact your administrator.");
        }

        var tillSession = await _dbContext.TillSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == context.TenantId &&
                     x.TillId == till.Id &&
                     x.ClosedAt == null,
                cancellationToken);

        if (tillSession is null)
        {
            _logger.LogDebug(
                "POS home context unresolved: no open till session for till {TillId}.",
                till.Id);

            return Unresolved(
                PosHomeContextReasonCodes.NoOpenTillSession,
                "No open till session found for the assigned till.",
                "Open a till session before starting POS operations.");
        }

        _logger.LogDebug(
            "POS home context: open till session resolved {SessionId} for till {TillId}.",
            tillSession.Id,
            till.Id);

        var outletName = await _dbContext.Outlets
            .AsNoTracking()
            .Where(o => o.Id == resolvedOutletId && o.TenantId == context.TenantId)
            .Select(o => o.OutletName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var outletTimezone = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == context.TenantId)
            .Select(t => t.DefaultTimezone)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(outletTimezone))
        {
            _logger.LogDebug(
                "POS home context unresolved: outlet timezone missing for tenant {TenantId}.",
                context.TenantId);

            return Unresolved(
                PosHomeContextReasonCodes.OutletTimezoneMissing,
                "Outlet timezone is not configured.",
                "Ask your administrator to configure the tenant timezone.");
        }

        var unreadNotificationCount = await _dbContext.NotificationInboxItems
            .AsNoTracking()
            .Where(x =>
                x.TenantId == context.TenantId &&
                x.TenantUserId == context.UserId &&
                x.InboxStatus == "UNREAD")
            .CountAsync(cancellationToken);

        var parkedSalesCount = await (from hold in _dbContext.PosOrderHolds.AsNoTracking()
                join order in _dbContext.SalesOrders.AsNoTracking() on hold.SalesOrderId equals order.Id
                where hold.TenantId == context.TenantId &&
                      hold.HoldStatus == "HELD" &&
                      hold.ReleasedAt == null &&
                      hold.HeldByTenantUserId == context.UserId &&
                      order.TenantId == context.TenantId &&
                      order.TillId == till.Id
                select hold.Id)
            .CountAsync(cancellationToken);

        var returnsRefundsCount = await _dbContext.SalesReturns
            .AsNoTracking()
            .Where(r =>
                r.TenantId == context.TenantId &&
                r.OutletId == resolvedOutletId &&
                r.CompletedAt == null &&
                r.CancelledAt == null)
            .CountAsync(cancellationToken);

        var customersCount = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.TenantId == context.TenantId && c.Status != "DELETED")
            .CountAsync(cancellationToken);

        var cashDrawerBalance = await CalculateCashDrawerBalanceAsync(
            context.TenantId,
            tillSession.Id,
            tillSession.OpeningFloatAmount,
            cancellationToken);

        _logger.LogDebug(
            "POS home context resolved for user {UserId}, device {DeviceId}, till {TillId}, session {SessionId}.",
            context.UserId,
            device.Id,
            till.Id,
            tillSession.Id);

        return new PosHomeContextResolutionResult(
            IsResolved: true,
            ReasonCode: null,
            Message: null,
            RequiredAction: null,
            Snapshot: new PosHomeDashboardDbSnapshot(
                CashierTenantUserId: context.UserId,
                CashierDisplayName: cashier.DisplayName ?? cashier.FullName,
                DeviceId: device.Id,
                DeviceCode: deviceCode,
                DeviceName: deviceName,
                DeviceTrusted: deviceTrusted,
                DeviceStatus: deviceStatus,
                TillId: till.Id,
                TillCode: till.TillCode,
                TillName: till.TillName,
                TillAreaName: till.TillAreaName,
                TillNumber: till.TillNumber,
                TillSessionStatus: tillSession.Status,
                TillSessionId: tillSession.Id,
                BusinessDate: tillSession.BusinessDate,
                CurrencyCode: tillSession.CurrencyCode,
                OutletId: resolvedOutletId,
                OutletName: outletName,
                OutletTimezone: outletTimezone.Trim(),
                UnreadNotificationCount: unreadNotificationCount,
                ReturnsRefundsCount: returnsRefundsCount,
                CustomersCount: customersCount,
                ParkedSalesCount: parkedSalesCount,
                CashDrawerBalance: cashDrawerBalance));
    }

    private async Task<Guid?> ResolveTrustedDeviceIdByFingerprintAsync(
        Guid tenantId,
        string fingerprintHash,
        CancellationToken cancellationToken)
    {
        return await (from d in _dbContext.PosDevices.AsNoTracking()
                where d.TenantId == tenantId &&
                      d.DeviceFingerprintHash == fingerprintHash &&
                      d.Status == PosDeviceConstants.ActiveStatus &&
                      d.IsTrusted &&
                      d.DeviceFingerprintHash != null &&
                      d.DeviceFingerprintHash != string.Empty
                select (Guid?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> IsDeviceTrustedAsync(
        Guid tenantId,
        Guid deviceId,
        string fingerprintHash,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fingerprintHash))
        {
            return false;
        }

        var hasTrustedDeviceAssignment = await (
                from device in _dbContext.PosDevices.AsNoTracking()
                join assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
                    on device.Id equals assignment.PosDeviceId
                join till in _dbContext.Tills.AsNoTracking()
                    on assignment.TillId equals till.Id
                join outlet in _dbContext.Outlets.AsNoTracking()
                    on till.OutletId equals outlet.Id
                where device.TenantId == tenantId &&
                      device.Id == deviceId &&
                      device.DeviceFingerprintHash == fingerprintHash &&
                      device.DeviceFingerprintHash != null &&
                      device.DeviceFingerprintHash != string.Empty &&
                      device.Status == PosDeviceConstants.ActiveStatus &&
                      device.IsTrusted &&
                      assignment.TenantId == tenantId &&
                      assignment.OutletId == till.OutletId &&
                      assignment.ReleasedAt == null &&
                      till.TenantId == tenantId &&
                      till.Status == TillConstants.ActiveStatus &&
                      outlet.TenantId == tenantId
                select device.Id)
            .AnyAsync(cancellationToken);

        if (!hasTrustedDeviceAssignment)
        {
            return false;
        }

        return !await _dbContext.OfflineClients
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.PosDeviceId == deviceId &&
                     x.Status != "ACTIVE",
                cancellationToken);
    }

    private async Task<double> CalculateCashDrawerBalanceAsync(
        Guid tenantId,
        Guid tillSessionId,
        decimal openingFloat,
        CancellationToken cancellationToken)
    {
        var cashIn = await _dbContext.TillCashMovements
            .AsNoTracking()
            .Where(m =>
                m.TenantId == tenantId &&
                m.TillSessionId == tillSessionId &&
                m.MovementType == "CASH_IN")
            .SumAsync(m => (decimal?)m.Amount, cancellationToken) ?? 0;

        var cashOut = await _dbContext.TillCashMovements
            .AsNoTracking()
            .Where(m =>
                m.TenantId == tenantId &&
                m.TillSessionId == tillSessionId &&
                m.MovementType == "CASH_OUT")
            .SumAsync(m => (decimal?)m.Amount, cancellationToken) ?? 0;

        var closingRemove = await _dbContext.TillCashMovements
            .AsNoTracking()
            .Where(m =>
                m.TenantId == tenantId &&
                m.TillSessionId == tillSessionId &&
                m.MovementType == "CLOSING_REMOVE")
            .SumAsync(m => (decimal?)m.Amount, cancellationToken) ?? 0;

        return (double)(openingFloat + cashIn - cashOut - closingRemove);
    }

    private static PosHomeContextResolutionResult Unresolved(
        string reasonCode,
        string message,
        string requiredAction) =>
        new(
            IsResolved: false,
            ReasonCode: reasonCode,
            Message: message,
            RequiredAction: requiredAction,
            Snapshot: null);
}
