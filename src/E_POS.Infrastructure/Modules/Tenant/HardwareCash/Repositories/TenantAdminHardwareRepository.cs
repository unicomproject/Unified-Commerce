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

    public TenantAdminHardwareRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> OutletBelongsToTenantAsync(Guid tenantId, Guid outletId, CancellationToken cancellationToken)
    {
        return _dbContext.Outlets.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.Id == outletId &&
                 x.Status != OutletConstants.DeletedStatus,
            cancellationToken);
    }

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
                .Where(a => a.HardwareDeviceId == device.Id && a.ReleasedAt == null)
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefault()
            select new { device, outlet, activeAssignment };

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
}
