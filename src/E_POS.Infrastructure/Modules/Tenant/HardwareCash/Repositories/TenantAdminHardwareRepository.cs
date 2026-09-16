using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.HardwareCash.Repositories;

public sealed class TenantAdminHardwareRepository : ITenantAdminHardwareRepository
{
    private readonly EPosDbContext _dbContext;
    private readonly HardwareQueryScope? _scope;

    public TenantAdminHardwareRepository(EPosDbContext dbContext, HardwareQueryScope? scope = null)
    {
        _dbContext = dbContext;
        _scope = scope;
    }

    public Task<bool> OutletBelongsToTenantAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
    {
        return _dbContext.Outlets.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.Id == outletId &&
                 x.Status != OutletConstants.DeletedStatus,
            cancellationToken);
    }

    public Task<HardwareTestLog?> GetLatestTelemetryAsync(Guid tenantId, Guid hardwareId, CancellationToken cancellationToken) =>
        _dbContext.HardwareTestLogs.AsNoTracking().Where(x => x.TenantId == tenantId && x.HardwareDeviceId == hardwareId && x.TestType == "TELEMETRY")
            .OrderByDescending(x => x.TestedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(cancellationToken);

    public Task<HardwareTestLog?> GetTestByRequestIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken) =>
        _dbContext.HardwareTestLogs.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.RequestId == requestId, cancellationToken);

    public Task<bool> DeviceCodeExistsAsync(
        Guid tenantId,
        string hardwareDeviceCode,
        Guid? excludeDeviceId,
        CancellationToken cancellationToken)
    {
        return _dbContext.HardwareDevices.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.HardwareDeviceCode == hardwareDeviceCode &&
                 x.Status != "DELETED" &&
                 (!excludeDeviceId.HasValue || x.Id != excludeDeviceId.Value),
            cancellationToken);
    }

    public async Task<(IReadOnlyList<HardwareDeviceListRow> Items, int TotalCount)> ListAsync(
        Guid tenantId,
        Guid? outletId,
        string? hardwareType,
        string? lifecycleStatus,
        string? assignmentStatus,
        bool? availableOnly,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query =
            from device in _dbContext.HardwareDevices.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking() on device.OutletId equals outlet.Id
            where device.TenantId == tenantId &&
                  outlet.TenantId == tenantId &&
                  device.Status != "DELETED"
            let activeAssignment = _dbContext.HardwareDeviceAssignments
                .Where(a => a.TenantId == tenantId && a.HardwareDeviceId == device.Id && a.ReleasedAt == null)
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefault()
            select new { device, outlet, activeAssignment };

        if (_scope?.TillId is Guid scopedTill)
        {
            query = query.Where(x => x.device.TenantId == _scope.TenantId &&
                x.device.OutletId == _scope.OutletId &&
                ((x.activeAssignment != null && x.activeAssignment.TillId == scopedTill &&
                  x.activeAssignment.PosDeviceId == null && x.activeAssignment.OutletId == _scope.OutletId) ||
                 (x.activeAssignment == null && x.device.CreatedByTenantUserId == _scope.UserId)));
        }

        if (outletId.HasValue)
        {
            query = query.Where(x => x.device.OutletId == outletId.Value);
        }

        if (!string.IsNullOrWhiteSpace(hardwareType))
        {
            var type = hardwareType.Trim().ToUpperInvariant();
            query = query.Where(x => x.device.HardwareDeviceType == type);
        }

        if (!string.IsNullOrWhiteSpace(lifecycleStatus))
        {
            var status = lifecycleStatus.Trim().ToUpperInvariant();
            query = query.Where(x => x.device.Status == status);
        }

        if (availableOnly == true ||
            string.Equals(assignmentStatus, "available", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assignmentStatus, "unassigned", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.activeAssignment == null);
        }
        else if (string.Equals(assignmentStatus, "assigned", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.activeAssignment != null);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.device.HardwareDeviceCode.ToUpper().Contains(term) ||
                x.device.HardwareDeviceName.ToUpper().Contains(term) ||
                (x.device.SerialNumber != null && x.device.SerialNumber.ToUpper().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var pageItems = await query
            .OrderBy(x => x.device.HardwareDeviceName)
            .ThenBy(x => x.device.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rows = pageItems
            .Select(x => new HardwareDeviceListRow(x.device, x.outlet.OutletName, x.activeAssignment))
            .ToList();

        return (rows, total);
    }

    public async Task<HardwareDeviceDetailRow?> GetDetailAsync(
        Guid tenantId,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken)
    {
        var row = await (
            from device in _dbContext.HardwareDevices.AsNoTracking()
            join outlet in _dbContext.Outlets.AsNoTracking() on device.OutletId equals outlet.Id
            where device.TenantId == tenantId &&
                  outlet.TenantId == tenantId &&
                  device.Id == hardwareDeviceId &&
                  device.Status != "DELETED"
            let activeAssignment = _dbContext.HardwareDeviceAssignments
                .Where(a => a.HardwareDeviceId == device.Id && a.ReleasedAt == null)
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefault()
            select new { device, outlet, activeAssignment }
        ).FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new HardwareDeviceDetailRow(row.device, row.outlet.OutletName, row.activeAssignment);
    }

    public async Task AddDeviceAsync(HardwareDevice device, CancellationToken cancellationToken)
    {
        _dbContext.HardwareDevices.Add(device);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<HardwareDevice?> GetEditableDeviceAsync(
        Guid tenantId,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken)
    {
        return _dbContext.HardwareDevices.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == hardwareDeviceId && x.Status != "DELETED",
            cancellationToken);
    }

    public Task<Till?> GetTillAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
    {
        return _dbContext.Tills.AsNoTracking().FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == tillId && x.Status != TillConstants.DeletedStatus,
            cancellationToken);
    }

    public Task<PosDevice?> GetPosDeviceAsync(Guid tenantId, Guid posDeviceId, CancellationToken cancellationToken)
    {
        return _dbContext.PosDevices.AsNoTracking().FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == posDeviceId,
            cancellationToken);
    }

    public Task<bool> IsTrustedPosAssignedToTillAsync(Guid tenantId, Guid outletId, Guid tillId,
        Guid posDeviceId, CancellationToken cancellationToken)
    {
        return _dbContext.TillDeviceAssignments.AsNoTracking().AnyAsync(a =>
            a.TenantId == tenantId && a.OutletId == outletId && a.TillId == tillId &&
            a.PosDeviceId == posDeviceId && a.ReleasedAt == null &&
            _dbContext.PosDevices.Any(p => p.Id == posDeviceId && p.TenantId == tenantId &&
                p.OutletId == outletId && p.IsTrusted && p.Status == "ACTIVE") &&
            _dbContext.Tills.Any(t => t.Id == tillId && t.TenantId == tenantId &&
                t.OutletId == outletId && t.Status == "ACTIVE") &&
            !_dbContext.TillDeviceAssignments.Any(other => other.PosDeviceId == posDeviceId &&
                other.ReleasedAt == null && (other.TenantId != tenantId ||
                    other.OutletId != outletId || other.TillId != tillId)), cancellationToken);
    }

    public Task<HardwareDeviceAssignment?> GetActiveAssignmentForDeviceAsync(
        Guid tenantId,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken)
    {
        return _dbContext.HardwareDeviceAssignments.FirstOrDefaultAsync(
            x => x.TenantId == tenantId &&
                 x.HardwareDeviceId == hardwareDeviceId &&
                 x.ReleasedAt == null,
            cancellationToken);
    }

    public Task<HardwareDeviceAssignment?> GetAssignmentAsync(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        return _dbContext.HardwareDeviceAssignments.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == assignmentId,
            cancellationToken);
    }

    public async Task AddAssignmentAsync(HardwareDeviceAssignment assignment, CancellationToken cancellationToken)
    {
        _dbContext.HardwareDeviceAssignments.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTestLogAsync(HardwareTestLog testLog, CancellationToken cancellationToken)
    {
        _dbContext.HardwareTestLogs.Add(testLog);
        await Task.CompletedTask;
    }

    public async Task<bool> IsHardwareLinkedToPosDeviceAsync(
        Guid tenantId,
        Guid posDeviceId,
        Guid hardwareDeviceId,
        CancellationToken cancellationToken)
    {
        var direct = await _dbContext.HardwareDeviceAssignments.AsNoTracking().AnyAsync(
            a => a.TenantId == tenantId &&
                 a.HardwareDeviceId == hardwareDeviceId &&
                 a.ReleasedAt == null &&
                 a.PosDeviceId == posDeviceId,
            cancellationToken);
        if (direct)
        {
            return true;
        }

        var tillIds = await _dbContext.TillDeviceAssignments.AsNoTracking()
            .Where(a => a.PosDeviceId == posDeviceId && a.ReleasedAt == null)
            .Select(a => a.TillId)
            .ToListAsync(cancellationToken);

        if (tillIds.Count == 0)
        {
            return false;
        }

        return await _dbContext.HardwareDeviceAssignments.AsNoTracking().AnyAsync(
            a => a.TenantId == tenantId &&
                 a.HardwareDeviceId == hardwareDeviceId &&
                 a.ReleasedAt == null &&
                 a.TillId != null &&
                 tillIds.Contains(a.TillId.Value),
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HardwareActivityItem>> GetActivityAsync(Guid tenantId,
        Guid hardwareDeviceId, CancellationToken cancellationToken)
    {
        var items = new List<HardwareActivityItem>();
        var device = await _dbContext.HardwareDevices.AsNoTracking().FirstOrDefaultAsync(
            d => d.TenantId == tenantId && d.Id == hardwareDeviceId, cancellationToken);
        if (device is null || (_scope?.TillId is not null &&
            (tenantId != _scope.TenantId || device.OutletId != _scope.OutletId))) return items;
        if (_scope?.TillId is null || device.CreatedByTenantUserId == _scope.UserId)
            items.Add(new(device.Id, "CREATED", device.CreatedAt, device.CreatedByTenantUserId, null, null));
        var assignments = _dbContext.HardwareDeviceAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.HardwareDeviceId == hardwareDeviceId);
        var changes = _dbContext.HardwareConfigurationChangeAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.HardwareDeviceId == hardwareDeviceId);
        if (_scope?.TillId is Guid tillId)
        {
            assignments = assignments.Where(a => a.OutletId == _scope.OutletId && a.TillId == tillId && a.PosDeviceId == null);
            changes = changes.Where(a => a.OutletId == _scope.OutletId && a.TillId == tillId);
        }
        items.AddRange(await assignments.OrderByDescending(a => a.AssignedAt).ThenByDescending(a => a.Id).Take(50)
            .Select(a => new HardwareActivityItem(a.Id, "ASSIGNED", a.AssignedAt, a.AssignedByTenantUserId, a.TillId, null))
            .ToListAsync(cancellationToken));
        items.AddRange(await assignments.Where(a => a.ReleasedAt != null).OrderByDescending(a => a.ReleasedAt).ThenByDescending(a => a.Id).Take(50)
            .Select(a => new HardwareActivityItem(a.Id, "RELEASED", a.ReleasedAt!.Value, a.ReleasedByTenantUserId, a.TillId, null))
            .ToListAsync(cancellationToken));
        items.AddRange(await changes.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).Take(50)
            .Select(a => new HardwareActivityItem(a.Id, "CONFIGURATION_CHANGED", a.CreatedAt, a.ChangedByTenantUserId, a.TillId, a.NewVersion))
            .ToListAsync(cancellationToken));
        return items.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id).ThenBy(a => a.Action).Take(50).ToList();
    }

    public async Task<IReadOnlyList<HardwareTestHistoryItem>> GetTestHistoryAsync(Guid tenantId,
        Guid hardwareDeviceId, CancellationToken cancellationToken)
    {
        var query = _dbContext.HardwareTestLogs.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.HardwareDeviceId == hardwareDeviceId);
        if (_scope?.TillId is Guid tillId)
            query = query.Where(x => x.TenantId == _scope.TenantId &&
                x.OutletId == _scope.OutletId && x.TillId == tillId);
        return await query.OrderByDescending(x => x.TestedAt).ThenByDescending(x => x.Id)
            .Take(50).Select(x => new HardwareTestHistoryItem(x.Id, x.TestType, x.TestStatus,
                x.ConfigurationVersion, x.TestedAt, x.CompletedAt, x.PhysicalConfirmation))
            .ToListAsync(cancellationToken);
    }
}
