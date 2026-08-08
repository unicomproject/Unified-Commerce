using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Options;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

public sealed class TenantAdminTillRepository : ITenantAdminTillRepository
{
    private const string OpenSessionStatus = "OPEN";

    private readonly EPosDbContext _dbContext;
    private readonly IOptionsSnapshot<TillMonitoringOptions> _options;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TenantAdminTillRepository(
        EPosDbContext dbContext,
        IOptionsSnapshot<TillMonitoringOptions> options,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _options = options;
        _dateTimeProvider = dateTimeProvider;
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

    public async Task<int> GetNextTillNumberAsync(
        Guid tenantId,
        Guid outletId,
        string tillAreaName,
        CancellationToken cancellationToken)
    {
        var normalizedAreaName = TillConstants.NormalizeAreaName(tillAreaName);
        var maxNumber = await _dbContext.Tills
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.OutletId == outletId &&
                x.TillAreaName == normalizedAreaName)
            .Select(x => (int?)x.TillNumber)
            .MaxAsync(cancellationToken);

        return (maxNumber ?? 0) + 1;
    }

    public async Task<(IReadOnlyList<TillMonitoringReadModel> Items, int TotalCount)> ListAsync(
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
        var query = BuildBaseQuery(tenantId);

        if (outletId.HasValue)
        {
            query = query.Where(x => x.Till.OutletId == outletId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpper();
            query = query.Where(x =>
                x.Till.TillName.ToUpper().Contains(term) ||
                x.Till.TillCode.ToUpper().Contains(term) ||
                x.Outlet.OutletName.ToUpper().Contains(term));
        }

        var timeoutSeconds = _options.Value.HeartbeatTimeoutSeconds;
        var now = _dateTimeProvider.UtcNow;
        var heartbeatCutoff = now.AddSeconds(-timeoutSeconds);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim().ToLowerInvariant();
            if (st == "inactive")
            {
                query = query.Where(x => x.Till.Status == TillConstants.InactiveStatus);
            }
            else if (st == "needs_attention")
            {
                query = query.Where(x => 
                    x.Till.Status == TillConstants.MaintenanceStatus ||
                    (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice == null) ||
                    (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice != null && x.AssignedDevice.Status != PosDeviceConstants.ActiveStatus) ||
                    (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice != null && !x.AssignedDevice.IsTrusted) ||
                    (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice != null && x.AssignedDevice.LastSeenAt == null) ||
                    (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice != null && x.AssignedDevice.LastSeenAt < heartbeatCutoff)
                );
            }
            else if (st == "online")
            {
                query = query.Where(x => 
                    x.Till.Status == TillConstants.ActiveStatus &&
                    x.AssignedDevice != null &&
                    x.AssignedDevice.Status == PosDeviceConstants.ActiveStatus &&
                    x.AssignedDevice.IsTrusted &&
                    x.AssignedDevice.LastSeenAt != null &&
                    x.AssignedDevice.LastSeenAt >= heartbeatCutoff
                );
            }
            else if (st == "offline")
            {
                // Operational offline: Active lifecycle but not currently online.
                // Inactive lifecycle is filtered separately via status=inactive.
                query = query.Where(x =>
                    x.Till.Status == TillConstants.ActiveStatus &&
                    !(x.AssignedDevice != null &&
                      x.AssignedDevice.Status == PosDeviceConstants.ActiveStatus &&
                      x.AssignedDevice.IsTrusted &&
                      x.AssignedDevice.LastSeenAt != null &&
                      x.AssignedDevice.LastSeenAt >= heartbeatCutoff));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var sortField = sortBy?.Trim().ToLowerInvariant() ?? "name";

        query = sortField switch
        {
            "code" or "tillcode" => descending
                ? query.OrderByDescending(x => x.Till.TillCode)
                : query.OrderBy(x => x.Till.TillCode),
            "outlet" or "outletname" => descending
                ? query.OrderByDescending(x => x.Outlet.OutletName)
                : query.OrderBy(x => x.Outlet.OutletName),
            "status" => descending
                ? query.OrderByDescending(x => x.Till.Status)
                : query.OrderBy(x => x.Till.Status),
            "lastactive" or "lastactiveat" => descending
                ? query.OrderByDescending(x => x.Till.UpdatedAt)
                : query.OrderBy(x => x.Till.UpdatedAt),
            _ => descending
                ? query.OrderByDescending(x => x.Till.TillName)
                : query.OrderBy(x => x.Till.TillName),
        };

        var pageItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var results = pageItems.Select(x => new TillMonitoringReadModel(
            x.Till,
            x.Outlet,
            x.AssignedDevice,
            x.ActiveSession,
            x.CashierUser)).ToList();

        return (results, totalCount);
    }

    public async Task<TenantAdminTillSummaryResponse> GetSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var timeoutSeconds = _options.Value.HeartbeatTimeoutSeconds;
        var now = _dateTimeProvider.UtcNow;
        var heartbeatCutoff = now.AddSeconds(-timeoutSeconds);

        var query = BuildBaseQuery(tenantId);

        var total = await query.CountAsync(cancellationToken);
        
        var inactive = await query.CountAsync(x => x.Till.Status == TillConstants.InactiveStatus, cancellationToken);
        
        var needsAttention = await query.CountAsync(x => 
            x.Till.Status == TillConstants.MaintenanceStatus ||
            (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice == null) ||
            (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice != null && x.AssignedDevice.Status != PosDeviceConstants.ActiveStatus) ||
            (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice != null && !x.AssignedDevice.IsTrusted) ||
            (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice != null && x.AssignedDevice.LastSeenAt == null) ||
            (x.Till.Status == TillConstants.ActiveStatus && x.AssignedDevice != null && x.AssignedDevice.LastSeenAt < heartbeatCutoff), 
            cancellationToken);

        var online = await query.CountAsync(x => 
            x.Till.Status == TillConstants.ActiveStatus &&
            x.AssignedDevice != null &&
            x.AssignedDevice.Status == PosDeviceConstants.ActiveStatus &&
            x.AssignedDevice.IsTrusted &&
            x.AssignedDevice.LastSeenAt != null &&
            x.AssignedDevice.LastSeenAt >= heartbeatCutoff,
            cancellationToken);

        var offline = await query.CountAsync(x =>
            x.Till.Status == TillConstants.ActiveStatus &&
            !(x.AssignedDevice != null &&
              x.AssignedDevice.Status == PosDeviceConstants.ActiveStatus &&
              x.AssignedDevice.IsTrusted &&
              x.AssignedDevice.LastSeenAt != null &&
              x.AssignedDevice.LastSeenAt >= heartbeatCutoff),
            cancellationToken);

        return new TenantAdminTillSummaryResponse(
            TotalTills: total,
            OnlineTills: online,
            OfflineTills: offline,
            InactiveTills: inactive,
            NeedsAttentionTills: needsAttention);
    }

    public async Task<TillMonitoringReadModel?> GetDetailAsync(
        Guid tenantId,
        Guid tillId,
        CancellationToken cancellationToken)
    {
        var item = await BuildBaseQuery(tenantId, tillId).FirstOrDefaultAsync(cancellationToken);
        if (item == null) return null;
        
        return new TillMonitoringReadModel(
            item.Till,
            item.Outlet,
            item.AssignedDevice,
            item.ActiveSession,
            item.CashierUser);
    }

    public async Task<IReadOnlyList<TillHardwareReadinessReadModel>> GetHardwareReadinessDataAsync(
        Guid tenantId,
        Guid tillId,
        Guid? activePosDeviceId,
        CancellationToken cancellationToken)
    {
        var assignmentRows = await (
            from assignment in _dbContext.HardwareDeviceAssignments.AsNoTracking()
            join hw in _dbContext.HardwareDevices.AsNoTracking()
                on assignment.HardwareDeviceId equals hw.Id
            where assignment.TenantId == tenantId &&
                  hw.TenantId == tenantId &&
                  assignment.ReleasedAt == null &&
                  hw.Status != "DELETED" &&
                  (
                      assignment.TillId == tillId ||
                      (activePosDeviceId != null && assignment.PosDeviceId == activePosDeviceId)
                  )
            select new { Hardware = hw, Assignment = assignment }
        ).ToListAsync(cancellationToken);

        if (assignmentRows.Count == 0)
        {
            return Array.Empty<TillHardwareReadinessReadModel>();
        }

        var deduped = assignmentRows
            .GroupBy(x => x.Hardware.Id)
            .Select(group =>
            {
                var preferred = group
                    .OrderByDescending(x => x.Assignment.IsPrimary)
                    .ThenByDescending(x => x.Assignment.AssignedAt)
                    .ThenByDescending(x => x.Assignment.TillId == tillId)
                    .First();

                var source = preferred.Assignment.TillId == tillId ? "TILL" : "POS_DEVICE";
                return new { preferred.Hardware, preferred.Assignment, Source = source };
            })
            .ToList();

        var deviceIds = deduped.Select(x => x.Hardware.Id).ToList();
        var latestLogs = await _dbContext.HardwareTestLogs
            .AsNoTracking()
            .Where(log =>
                log.TenantId == tenantId &&
                log.HardwareDeviceId.HasValue &&
                deviceIds.Contains(log.HardwareDeviceId.Value))
            .OrderByDescending(log => log.TestedAt)
            .ToListAsync(cancellationToken);

        var latestByDevice = latestLogs
            .Where(log => log.HardwareDeviceId.HasValue)
            .GroupBy(log => log.HardwareDeviceId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        return deduped
            .Select(x => new TillHardwareReadinessReadModel(
                x.Hardware,
                x.Assignment,
                latestByDevice.GetValueOrDefault(x.Hardware.Id),
                x.Source))
            .ToList();
    }

    // Other unchanged methods...
    public Task AddAsync(Till till, CancellationToken cancellationToken)
    {
        _dbContext.Tills.Add(till);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Till?> GetEditableAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
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

    public Task<bool> HasActiveDeviceAssignmentAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
    {
        return _dbContext.TillDeviceAssignments
            .AsNoTracking()
            .AnyAsync(
                assignment =>
                    assignment.TillId == tillId &&
                    assignment.ReleasedAt == null &&
                    _dbContext.Tills.Any(till =>
                        till.Id == assignment.TillId &&
                        till.TenantId == tenantId),
                cancellationToken);
    }

    public Task<bool> HasActiveSessionAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
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

    public Task<bool> HasSalesAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
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

    public Task<bool> HasCashMovementsAsync(Guid tenantId, Guid tillId, CancellationToken cancellationToken)
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

    public async Task<IReadOnlyList<TenantAdminOutletOptionResponse>> GetOutletOptionsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Outlets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != OutletConstants.DeletedStatus)
            .OrderBy(x => x.OutletName)
            .Select(x => new TenantAdminOutletOptionResponse(
                x.Id,
                x.OutletName,
                x.OutletCode,
                x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantAdminTillCreateOptionsResponse> GetCreateOptionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var outlets = await GetOutletOptionsAsync(tenantId, cancellationToken);
        
        var statuses = new List<string> { TillConstants.ActiveStatus, TillConstants.InactiveStatus, TillConstants.MaintenanceStatus };
        
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        var currencyCode = tenant?.BaseCurrencyCode ?? TillConstants.DefaultCurrencyCode;

        var cashiers = await _dbContext.TenantUsers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AccountStatus == "ACTIVE")
            .OrderBy(x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.FullName : x.DisplayName)
            .Select(x => new TenantAdminTillCashierOptionResponse(x.Id, string.IsNullOrWhiteSpace(x.DisplayName) ? x.FullName : x.DisplayName!))
            .ToListAsync(cancellationToken);

        var posDevices = await _dbContext.PosDevices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == PosDeviceConstants.ActiveStatus)
            .OrderBy(x => x.DeviceName)
            .Select(x => new TenantAdminTillPosDeviceOptionResponse(x.Id, x.DeviceCode, x.DeviceName))
            .ToListAsync(cancellationToken);

        var hardwareDevices = await _dbContext.HardwareDevices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE")
            .OrderBy(x => x.HardwareDeviceName)
            .Select(x => new TenantAdminTillHardwareOptionResponse(x.Id, x.HardwareDeviceCode, x.HardwareDeviceName, x.HardwareDeviceType))
            .ToListAsync(cancellationToken);

        return new TenantAdminTillCreateOptionsResponse(
            outlets,
            cashiers,
            posDevices,
            hardwareDevices,
            statuses,
            currencyCode);
    }

    private sealed class TillMonitoringProjection
    {
        public Till Till { get; set; } = null!;
        public Outlet Outlet { get; set; } = null!;
        public PosDevice? AssignedDevice { get; set; }
        public TillSession? ActiveSession { get; set; }
        public TenantUser? CashierUser { get; set; }
    }

    private IQueryable<TillMonitoringProjection> BuildBaseQuery(Guid tenantId, Guid? tillId = null)
    {
        return from till in _dbContext.Tills.AsNoTracking()
               join outlet in _dbContext.Outlets.AsNoTracking()
                   on till.OutletId equals outlet.Id
               where till.TenantId == tenantId &&
                     outlet.TenantId == tenantId &&
                     till.Status != TillConstants.DeletedStatus &&
                     (tillId == null || till.Id == tillId)
               let assignedDevice = (from assignment in _dbContext.TillDeviceAssignments
                                     where assignment.TillId == till.Id && assignment.ReleasedAt == null
                                     orderby assignment.AssignedAt descending
                                     join device in _dbContext.PosDevices on assignment.PosDeviceId equals device.Id
                                     select device).FirstOrDefault()
               let activeSession = _dbContext.TillSessions
                   .Where(session => session.TillId == till.Id && session.Status == OpenSessionStatus)
                   .OrderByDescending(session => session.OpenedAt)
                   .FirstOrDefault()
               let cashierUser = (from session in _dbContext.TillSessions
                                  where session.TillId == till.Id && session.Status == OpenSessionStatus
                                  orderby session.OpenedAt descending
                                  join u in _dbContext.TenantUsers on session.OpenedByTenantUserId equals u.Id
                                  select u).FirstOrDefault()
               select new TillMonitoringProjection
               {
                   Till = till,
                   Outlet = outlet,
                   AssignedDevice = assignedDevice,
                   ActiveSession = activeSession,
                   CashierUser = cashierUser
               };
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        // Join ambient capacity-guard transaction when present (Phase 3).
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            await operation();
            return;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await operation();
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
