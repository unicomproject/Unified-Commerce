using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

public sealed class TenantAdminTillRepository : ITenantAdminTillRepository
{
    private const string OpenSessionStatus = "OPEN";
    private const string ActiveAssignmentStatus = "ACTIVE";

    private readonly EPosDbContext _dbContext;

    public TenantAdminTillRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> OutletBelongsToTenantAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Outlets
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == outletId &&
                     x.Status != OutletConstants.DeletedStatus,
                cancellationToken);
    }

    public Task<bool> TillCodeExistsForTenantAsync(
        Guid tenantId,
        string tillCode,
        Guid? excludeTillId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Tills
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.TillCode == tillCode &&
                     x.Status != TillConstants.DeletedStatus &&
                     (!excludeTillId.HasValue || x.Id != excludeTillId.Value),
                cancellationToken);
    }

    public async Task<TenantAdminTillListResponse> ListAsync(
        Guid tenantId,
        string? search,
        string? status,
        Guid? outletId,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken)
    {
        var rows = await BuildTillRowsQuery(tenantId).ToListAsync(cancellationToken);
        var filtered = ApplyStatusFilter(rows, status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            filtered = filtered
                .Where(x =>
                    x.TillName.ToUpperInvariant().Contains(term) ||
                    x.TillCode.ToUpperInvariant().Contains(term) ||
                    x.OutletName.ToUpperInvariant().Contains(term))
                .ToList();
        }

        if (outletId.HasValue)
        {
            filtered = filtered.Where(x => x.OutletId == outletId.Value).ToList();
        }

        filtered = ApplySort(filtered, sortBy, sortDirection);

        var totalCount = filtered.Count;
        var pageItems = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapListItem)
            .ToList();

        return new TenantAdminTillListResponse(pageItems, page, pageSize, totalCount);
    }

    public async Task<TenantAdminTillSummaryResponse> GetSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var rows = await BuildTillRowsQuery(tenantId).ToListAsync(cancellationToken);

        return new TenantAdminTillSummaryResponse(
            rows.Count,
            rows.Count(x => x.DeviceStatus == "Online"),
            rows.Count(x => x.DeviceStatus == "Offline"),
            rows.Count(x => x.TillStatus == TillConstants.InactiveStatus),
            rows.Count(x => x.NeedsAttention));
    }

    public async Task<TenantAdminTillDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        var row = await BuildTillRowsQuery(tenantId)
            .FirstOrDefaultAsync(x => x.TillId == tillId, cancellationToken);

        if (row is null)
        {
            return null;
        }

        var till = await _dbContext.Tills
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == tillId &&
                     x.Status != TillConstants.DeletedStatus,
                cancellationToken);

        if (till is null)
        {
            return null;
        }

        return MapDetail(row, till);
    }

    public async Task AddAsync(Till till, CancellationToken cancellationToken)
    {
        _dbContext.Tills.Add(till);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Till?> GetEditableAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Tills
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId &&
                     x.Id == tillId &&
                     x.Status != TillConstants.DeletedStatus,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasActiveDeviceAssignmentAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TillDeviceAssignments
            .AsNoTracking()
            .AnyAsync(
                assignment =>
                    assignment.TillId == tillId &&
                    assignment.Status == ActiveAssignmentStatus &&
                    _dbContext.Tills.Any(till =>
                        till.Id == assignment.TillId &&
                        till.TenantId == tenantId),
                cancellationToken);
    }

    public Task<bool> HasActiveSessionAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TillSessions
            .AsNoTracking()
            .AnyAsync(
                session =>
                    session.TillId == tillId &&
                    session.Status == OpenSessionStatus &&
                    _dbContext.Tills.Any(till =>
                        till.Id == session.TillId &&
                        till.TenantId == tenantId),
                cancellationToken);
    }

    public Task<bool> HasSalesAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        return _dbContext.SalesOrders
            .AsNoTracking()
            .AnyAsync(
                order =>
                    order.TillId == tillId &&
                    _dbContext.Tills.Any(till =>
                        till.Id == order.TillId &&
                        till.TenantId == tenantId),
                cancellationToken);
    }

    public Task<bool> HasCashMovementsAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TillCashMovements
            .AsNoTracking()
            .AnyAsync(
                movement =>
                    _dbContext.TillSessions.Any(session =>
                        session.Id == movement.TillSessionId &&
                        session.TillId == tillId &&
                        session.TenantId == tenantId),
                cancellationToken);
    }

    public async Task<IReadOnlyList<TenantAdminOutletOptionResponse>> GetOutletOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Outlets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != OutletConstants.DeletedStatus)
            .OrderBy(x => x.Name)
            .Select(x => new TenantAdminOutletOptionResponse(
                x.Id,
                x.Name,
                x.OutletCode,
                x.Status))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<TillRow> BuildTillRowsQuery(Guid tenantId)
    {
        return from till in _dbContext.Tills.AsNoTracking()
               join outlet in _dbContext.Outlets.AsNoTracking()
                   on till.OutletId equals outlet.Id
               where till.TenantId == tenantId &&
                     outlet.TenantId == tenantId &&
                     till.Status != TillConstants.DeletedStatus
               let activeAssignment = _dbContext.TillDeviceAssignments
                   .Where(assignment =>
                       assignment.TillId == till.Id &&
                       assignment.Status == ActiveAssignmentStatus)
                   .OrderByDescending(assignment => assignment.UpdatedAt)
                   .FirstOrDefault()
               let assignedDevice = activeAssignment == null
                   ? null
                   : _dbContext.PosDevices
                       .Where(device => device.Id == activeAssignment.PosDeviceId)
                       .FirstOrDefault()
               let lastSessionAt = _dbContext.TillSessions
                   .Where(session => session.TillId == till.Id)
                   .OrderByDescending(session => session.UpdatedAt)
                   .Select(session => (DateTimeOffset?)session.UpdatedAt)
                   .FirstOrDefault()
               select new TillRow
               {
                   TillId = till.Id,
                   TillName = till.Name,
                   TillCode = till.TillCode,
                   OutletId = outlet.Id,
                   OutletName = outlet.Name,
                   OutletCode = outlet.OutletCode,
                   TillStatus = till.Status,
                   DeviceName = till.DeviceName,
                   PrinterName = till.PrinterName,
                   ScannerName = till.ScannerName,
                   CashDrawerName = till.CashDrawerName,
                   CardReaderName = till.CardReaderName,
                   InternalNote = till.InternalNote,
                   CreatedAt = till.CreatedAt,
                   UpdatedAt = till.UpdatedAt,
                   HasActiveAssignment = activeAssignment != null,
                   AssignedDeviceActive = assignedDevice != null &&
                                          assignedDevice.Status == PosDeviceConstants.ActiveStatus,
                   LastActiveAt = lastSessionAt ?? till.UpdatedAt,
               };
    }

    private static List<TillRow> ApplyStatusFilter(List<TillRow> rows, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return rows;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "online" => rows.Where(x => x.DeviceStatus == "Online").ToList(),
            "offline" => rows.Where(x => x.DeviceStatus == "Offline").ToList(),
            "inactive" => rows.Where(x => x.TillStatus == TillConstants.InactiveStatus).ToList(),
            "needs_attention" => rows.Where(x => x.NeedsAttention).ToList(),
            _ => rows,
        };
    }

    private static List<TillRow> ApplySort(
        List<TillRow> rows,
        string sortBy,
        string sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.Trim().ToLowerInvariant() ?? "name") switch
        {
            "code" or "tillcode" => descending
                ? rows.OrderByDescending(x => x.TillCode).ToList()
                : rows.OrderBy(x => x.TillCode).ToList(),
            "outlet" or "outletname" => descending
                ? rows.OrderByDescending(x => x.OutletName).ToList()
                : rows.OrderBy(x => x.OutletName).ToList(),
            "status" => descending
                ? rows.OrderByDescending(x => x.TillStatus).ToList()
                : rows.OrderBy(x => x.TillStatus).ToList(),
            "lastactive" or "lastactiveat" => descending
                ? rows.OrderByDescending(x => x.LastActiveAt).ToList()
                : rows.OrderBy(x => x.LastActiveAt).ToList(),
            _ => descending
                ? rows.OrderByDescending(x => x.TillName).ToList()
                : rows.OrderBy(x => x.TillName).ToList(),
        };
    }

    private static TenantAdminTillListItemResponse MapListItem(TillRow row)
    {
        return new TenantAdminTillListItemResponse(
            row.TillId,
            row.TillName,
            row.TillCode,
            row.OutletId,
            row.OutletName,
            FormatStatus(row.TillStatus),
            row.DeviceStatus,
            row.LastActiveAt,
            row.NeedsAttention);
    }

    private static TenantAdminTillDetailResponse MapDetail(TillRow row, Till till)
    {
        return new TenantAdminTillDetailResponse(
            row.TillId,
            row.TillName,
            row.TillCode,
            row.OutletId,
            row.OutletName,
            row.OutletCode,
            FormatStatus(row.TillStatus),
            row.DeviceStatus,
            row.LastActiveAt,
            row.NeedsAttention,
            till.DeviceName,
            till.PrinterName,
            till.ScannerName,
            till.CashDrawerName,
            till.CardReaderName,
            till.InternalNote,
            till.CreatedAt,
            till.UpdatedAt ?? till.CreatedAt);
    }

    private static string FormatStatus(string status)
    {
        return status.Trim().ToUpperInvariant() switch
        {
            TillConstants.ActiveStatus => "Active",
            TillConstants.InactiveStatus => "Inactive",
            TillConstants.MaintenanceStatus => "Maintenance",
            _ => status,
        };
    }

    private sealed class TillRow
    {
        public Guid TillId { get; init; }
        public string TillName { get; init; } = string.Empty;
        public string TillCode { get; init; } = string.Empty;
        public Guid OutletId { get; init; }
        public string OutletName { get; init; } = string.Empty;
        public string OutletCode { get; init; } = string.Empty;
        public string TillStatus { get; init; } = string.Empty;
        public string? DeviceName { get; init; }
        public string? PrinterName { get; init; }
        public string? ScannerName { get; init; }
        public string? CashDrawerName { get; init; }
        public string? CardReaderName { get; init; }
        public string? InternalNote { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
        public bool HasActiveAssignment { get; init; }
        public bool AssignedDeviceActive { get; init; }
        public DateTimeOffset? LastActiveAt { get; init; }

        public string DeviceStatus
        {
            get
            {
                if (TillStatus == TillConstants.InactiveStatus)
                {
                    return "Offline";
                }

                if (TillStatus == TillConstants.MaintenanceStatus)
                {
                    return "Offline";
                }

                if (HasActiveAssignment && AssignedDeviceActive)
                {
                    return "Online";
                }

                return "Offline";
            }
        }

        public bool NeedsAttention =>
            TillStatus == TillConstants.MaintenanceStatus ||
            (TillStatus == TillConstants.ActiveStatus && !HasActiveAssignment) ||
            (TillStatus == TillConstants.ActiveStatus && HasActiveAssignment && !AssignedDeviceActive);
    }
}
