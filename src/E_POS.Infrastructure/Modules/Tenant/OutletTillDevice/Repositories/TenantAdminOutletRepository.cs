using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Options;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Services;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

public sealed class TenantAdminOutletRepository : ITenantAdminOutletRepository
{
    private const string ActiveAccountStatus = "ACTIVE";
    private const string PendingInviteStatus = "PENDING";
    private const string OpenSessionStatus = "OPEN";

    private readonly EPosDbContext _dbContext;
    private readonly IOptionsSnapshot<TillMonitoringOptions> _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMediaReadUrlResolver _mediaReadUrlResolver;

    public TenantAdminOutletRepository(
        EPosDbContext dbContext,
        IOptionsSnapshot<TillMonitoringOptions> options,
        IDateTimeProvider dateTimeProvider,
        IMediaReadUrlResolver mediaReadUrlResolver)
    {
        _dbContext = dbContext;
        _options = options;
        _dateTimeProvider = dateTimeProvider;
        _mediaReadUrlResolver = mediaReadUrlResolver;
    }

    public async Task<TenantAdminOutletListResponse> ListAsync(
        Guid tenantId,
        TenantAdminOutletListQuery query,
        CancellationToken cancellationToken)
    {
        var heartbeatCutoff = _dateTimeProvider.UtcNow.AddSeconds(-_options.Value.HeartbeatTimeoutSeconds);
        var normalizedSearch = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var normalizedType = string.IsNullOrWhiteSpace(query.OutletType) ? null : OutletConstants.NormalizeOutletType(query.OutletType);
        var normalizedStatus = string.IsNullOrWhiteSpace(query.Status) ? null : OutletConstants.NormalizeStatus(query.Status);
        var normalizedHealth = string.IsNullOrWhiteSpace(query.OperationalHealth) ? null : query.OperationalHealth.Trim().ToUpperInvariant();

        var rows = _dbContext.Outlets
            .AsNoTracking()
            .Where(outlet => outlet.TenantId == tenantId && outlet.Status != OutletConstants.DeletedStatus)
            .Select(outlet => new TenantAdminOutletListRow
            {
                Id = outlet.Id,
                Name = outlet.OutletName,
                Code = outlet.OutletCode,
                Type = outlet.OutletType,
                Status = outlet.Status,
                ImageUrl = _dbContext.MediaAssets
                    .Where(media => media.TenantId == tenantId && media.Id == outlet.PrimaryImageMediaAssetId)
                    .Select(media => media.PublicUrl)
                    .FirstOrDefault(),
                ImageStorageKey = _dbContext.MediaAssets
                    .Where(media => media.TenantId == tenantId && media.Id == outlet.PrimaryImageMediaAssetId)
                    .Select(media => media.StorageKey)
                    .FirstOrDefault(),
                ImageContainerName = _dbContext.MediaAssets
                    .Where(media => media.TenantId == tenantId && media.Id == outlet.PrimaryImageMediaAssetId)
                    .Select(media => media.ContainerName)
                    .FirstOrDefault(),
                ManagerId = _dbContext.OutletUserRoles
                    .Where(assignment => assignment.TenantId == tenantId && assignment.OutletId == outlet.Id && assignment.IsPrimaryManager && assignment.RevokedAt == null)
                    .Select(assignment => (Guid?)assignment.TenantUserId)
                    .FirstOrDefault(),
                ManagerName = null, // ManagerName
                ManagerAvatarUrl = null, // ManagerAvatarUrl
                AddressLine = _dbContext.OutletAddresses
                    .Where(address => address.TenantId == tenantId && address.OutletId == outlet.Id && address.AddressType == OutletConstants.PhysicalAddressType)
                    .Select(address => address.AddressLine1)
                    .FirstOrDefault(),
                City = _dbContext.OutletAddresses
                    .Where(address => address.TenantId == tenantId && address.OutletId == outlet.Id && address.AddressType == OutletConstants.PhysicalAddressType)
                    .Select(address => address.City)
                    .FirstOrDefault(),
                TotalTillCount = _dbContext.Tills.Count(till => till.TenantId == tenantId && till.OutletId == outlet.Id && till.Status != TillConstants.DeletedStatus),
                ActiveTillCount = _dbContext.Tills.Count(till => till.TenantId == tenantId && till.OutletId == outlet.Id && till.Status == TillConstants.ActiveStatus),
                OnlineTillCount = _dbContext.Tills.Count(till => till.TenantId == tenantId && till.OutletId == outlet.Id && till.Status == TillConstants.ActiveStatus &&
                    _dbContext.TillDeviceAssignments.Any(assignment => assignment.TenantId == tenantId && assignment.TillId == till.Id && assignment.ReleasedAt == null &&
                        _dbContext.PosDevices.Any(device => device.TenantId == tenantId && device.Id == assignment.PosDeviceId && device.Status == OutletConstants.ActiveStatus && device.LastSeenAt != null && device.LastSeenAt >= heartbeatCutoff)))
            });

        if (normalizedSearch is not null)
        {
            var searchPattern = $"%{normalizedSearch}%";
            rows = rows.Where(row =>
                EF.Functions.ILike(row.Name, searchPattern) ||
                EF.Functions.ILike(row.Code, searchPattern) ||
                (row.AddressLine != null && EF.Functions.ILike(row.AddressLine, searchPattern)) ||
                (row.City != null && EF.Functions.ILike(row.City, searchPattern)) ||
                _dbContext.OutletUserRoles.Any(assignment => 
                    assignment.TenantId == tenantId && 
                    assignment.OutletId == row.Id && 
                    assignment.IsPrimaryManager && 
                    assignment.RevokedAt == null &&
                    _dbContext.TenantUsers.Any(user => 
                        user.Id == assignment.TenantUserId && 
                        (EF.Functions.ILike(user.DisplayName ?? user.FullName, searchPattern)))));
        }

        if (normalizedType is not null) rows = rows.Where(row => row.Type == normalizedType);
        if (normalizedStatus is not null) rows = rows.Where(row => row.Status == normalizedStatus);
        if (normalizedHealth is not null)
        {
            rows = normalizedHealth switch
            {
                OutletOperationalHealthCalculator.HealthyStatus => rows.Where(row => row.ActiveTillCount > 0 && row.OnlineTillCount == row.ActiveTillCount),
                OutletOperationalHealthCalculator.NeedsAttentionStatus => rows.Where(row => row.OnlineTillCount > 0 && row.OnlineTillCount < row.ActiveTillCount),
                OutletOperationalHealthCalculator.CriticalStatus => rows.Where(row => row.ActiveTillCount > 0 && row.OnlineTillCount == 0),
                OutletOperationalHealthCalculator.UnknownStatus => rows.Where(row => row.ActiveTillCount == 0),
                _ => rows.Where(_ => false),
            };
        }

        var totalCount = await rows.CountAsync(cancellationToken);
        rows = ApplySorting(rows, query.SortBy, query.SortDirection);
        var pageRows = await rows
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var managerIds = pageRows.Where(r => r.ManagerId.HasValue).Select(r => r.ManagerId!.Value).Distinct().ToList();
        var managers = await _dbContext.TenantUsers.AsNoTracking()
             .Where(u => managerIds.Contains(u.Id))
             .Select(u => new { u.Id, Name = u.DisplayName ?? u.FullName, u.ProfileImageUrl })
             .ToListAsync(cancellationToken);
             
        var avatarMediaIds = managers.Where(m => m.ProfileImageUrl.HasValue).Select(m => m.ProfileImageUrl!.Value).Distinct().ToList();
        var avatars = await _dbContext.MediaAssets.AsNoTracking()
             .Where(m => avatarMediaIds.Contains(m.Id))
             .Select(m => new { m.Id, m.PublicUrl, m.StorageKey, m.ContainerName })
             .ToListAsync(cancellationToken);

        var responseRows = pageRows.Select(row => 
        {
            var manager = row.ManagerId.HasValue ? managers.FirstOrDefault(m => m.Id == row.ManagerId.Value) : null;
            var avatar = manager?.ProfileImageUrl.HasValue == true ? avatars.FirstOrDefault(a => a.Id == manager.ProfileImageUrl.Value) : null;
            row.ImageUrl = _mediaReadUrlResolver.ResolveReadUrl(row.ImageContainerName, row.ImageStorageKey, row.ImageUrl);
            var resolvedAvatarUrl = _mediaReadUrlResolver.ResolveReadUrl(avatar?.ContainerName, avatar?.StorageKey, avatar?.PublicUrl);
            return MapListRow(row, manager?.Name, resolvedAvatarUrl);
        }).ToList();

        return new TenantAdminOutletListResponse(
            responseRows,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public Task<bool> OutletExistsAsync(
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

    public async Task<TenantAdminOutletLifecycleState?> GetLifecycleStateAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var outlet = await _dbContext.Outlets.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.Id == outletId && item.Status != OutletConstants.DeletedStatus)
            .Select(item => new { item.IsDefaultOutlet })
            .FirstOrDefaultAsync(cancellationToken);
        if (outlet is null) return null;

        var hasOpenTillSessions = await _dbContext.TillSessions.AsNoTracking()
            .AnyAsync(session => session.TenantId == tenantId && session.OutletId == outletId && session.Status == OpenSessionStatus, cancellationToken);
        var hasActiveTills = await _dbContext.Tills.AsNoTracking()
            .AnyAsync(till => till.TenantId == tenantId && till.OutletId == outletId && till.Status == TillConstants.ActiveStatus, cancellationToken);
        var hasOpenOrders = await _dbContext.SalesOrders.AsNoTracking()
            .AnyAsync(order => order.TenantId == tenantId && order.ReportingOutletId == outletId && OpenOrderStatuses.Contains(order.Status), cancellationToken);
        var hasAllocatedInventory = await (
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join location in _dbContext.InventoryLocations.AsNoTracking() on balance.InventoryLocationId equals location.Id
            where balance.TenantId == tenantId && location.TenantId == tenantId && location.OutletId == outletId && balance.ReservedQuantity > 0
            select balance.Id).AnyAsync(cancellationToken);

        return new TenantAdminOutletLifecycleState(outlet.IsDefaultOutlet, hasOpenTillSessions, hasActiveTills, hasOpenOrders, hasAllocatedInventory);
    }

    public async Task<bool> UpdateStatusAsync(
        Guid tenantId,
        Guid outletId,
        string status,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var outlet = await _dbContext.Outlets
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Id == outletId && item.Status != OutletConstants.DeletedStatus, cancellationToken);
        if (outlet is null) return false;

        outlet.UpdateProfile(
            outlet.OutletName,
            outlet.OutletCode,
            status,
            outlet.OutletType,
            outlet.Timezone,
            outlet.IsDefaultOutlet,
            outlet.Phone,
            outlet.Email,
            updatedByTenantUserId,
            now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TenantAdminOutletDetailResponse?> GetDetailAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var row = await (
            from outlet in _dbContext.Outlets.AsNoTracking()
            join address in _dbContext.OutletAddresses.AsNoTracking()
                on outlet.Id equals address.OutletId into addressJoin
            from address in addressJoin
                .Where(x => x.AddressType == OutletConstants.PhysicalAddressType)
                .DefaultIfEmpty()
            where outlet.TenantId == tenantId &&
                  outlet.Id == outletId &&
                  outlet.Status != OutletConstants.DeletedStatus
            select new
            {
                outlet.Id,
                outlet.OutletName,
                outlet.OutletCode,
                outlet.OutletType,
                outlet.Status,
                ContactPhone = outlet.Phone,
                ContactEmail = outlet.Email,
                outlet.CreatedAt,
                AddressLine1 = address != null ? address.AddressLine1 : null,
                AddressLine2 = address != null ? address.AddressLine2 : null,
                City = address != null ? address.City : null,
                DistrictOrProvince = address != null ? address.StateOrProvince : null,
                PostalCode = address != null ? address.PostalCode : null,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var businessHours = await _dbContext.OutletBusinessHours
            .AsNoTracking()
            .Where(x => x.OutletId == outletId)
            .OrderBy(x => x.DayOfWeek)
            .Select(x => new BusinessHourRow(x.DayOfWeek, x.OpeningTime, x.ClosingTime))
            .ToListAsync(cancellationToken);

        var managerName = await (
            from assignment in _dbContext.OutletUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on assignment.TenantUserId equals user.Id
            where assignment.TenantId == tenantId &&
                  assignment.OutletId == outletId &&
                  assignment.IsPrimaryManager &&
                  assignment.RevokedAt == null
            select user.DisplayName ?? user.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return new TenantAdminOutletDetailResponse(
            OutletId: row.Id,
            OutletName: row.OutletName,
            OutletCode: row.OutletCode,
            OutletType: row.OutletType,
            Status: row.Status,
            AddressLine1: row.AddressLine1,
            AddressLine2: row.AddressLine2,
            City: row.City,
            DistrictOrProvince: row.DistrictOrProvince,
            PostalCode: row.PostalCode,
            PhoneNumber: row.ContactPhone,
            EmailAddress: row.ContactEmail,
            ManagerName: managerName,
            OperatingHours: FormatOperatingHours(businessHours),
            OpeningDate: row.CreatedAt,
            TaxRegistrationId: null,
            Notes: null);
    }

    public async Task<TenantAdminOutletRevenueSummaryResponse> GetRevenueSummaryAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var currentStart = now.AddDays(-30);
        var previousStart = now.AddDays(-60);
        var previousEnd = currentStart;

        var currentOrders = await QueryOutletOrdersAsync(
            tenantId,
            outletId,
            currentStart,
            now,
            cancellationToken);

        var previousOrders = await QueryOutletOrdersAsync(
            tenantId,
            outletId,
            previousStart,
            previousEnd,
            cancellationToken);

        var totalRevenue = currentOrders.Sum(x => x.TotalAmount);
        var totalOrders = currentOrders.Count;
        var refunds = currentOrders.Sum(x => x.RefundedAmount);
        var discounts = currentOrders.Sum(x => x.DiscountAmount);
        var taxCollected = currentOrders.Sum(x => x.TaxAmount);
        var averageOrderValue = totalOrders == 0 ? 0m : totalRevenue / totalOrders;

        var previousRevenue = previousOrders.Sum(x => x.TotalAmount);
        var previousOrdersCount = previousOrders.Count;
        var previousRefunds = previousOrders.Sum(x => x.RefundedAmount);
        var previousAverageOrderValue = previousOrdersCount == 0
            ? 0m
            : previousRevenue / previousOrdersCount;

        var revenueOverTime = currentOrders
            .Where(x => x.CompletedAt.HasValue)
            .GroupBy(x => x.CompletedAt!.Value.UtcDateTime.Date)
            .OrderBy(x => x.Key)
            .Select(x => new TenantAdminOutletRevenuePointResponse(
                x.Key.ToString("MMM dd"),
                x.Sum(order => order.TotalAmount)))
            .ToList();

        var paymentRows = await (
            from payment in _dbContext.SalesPayments.AsNoTracking()
            join order in _dbContext.SalesOrders.AsNoTracking()
                on payment.SalesOrderId equals order.Id
            join till in _dbContext.Tills.AsNoTracking()
                on order.TillId equals till.Id
            join method in _dbContext.PaymentMethods.AsNoTracking()
                on payment.PaymentMethodId equals method.Id
            where payment.TenantId == tenantId &&
                  till.TenantId == tenantId &&
                  till.OutletId == outletId &&
                  payment.PaidAt >= currentStart &&
                  payment.PaidAt <= now
            group payment by method.MethodName into grouped
            select new
            {
                Method = grouped.Key,
                Amount = grouped.Sum(x => x.PaidAmount)
            })
            .ToListAsync(cancellationToken);

        var paymentTotal = paymentRows.Sum(x => x.Amount);
        var revenueByPaymentMethod = paymentRows
            .OrderByDescending(x => x.Amount)
            .Select(x => new TenantAdminOutletPaymentMethodShareResponse(
                x.Method,
                x.Amount,
                paymentTotal == 0m ? 0m : Math.Round(x.Amount / paymentTotal * 100m, 1)))
            .ToList();

        return new TenantAdminOutletRevenueSummaryResponse(
            TotalRevenue: totalRevenue,
            AverageOrderValue: Math.Round(averageOrderValue, 2),
            TotalOrders: totalOrders,
            Refunds: refunds,
            RevenueChangePercent: CalculateChangePercent(totalRevenue, previousRevenue),
            AverageOrderValueChangePercent: CalculateChangePercent(
                averageOrderValue,
                previousAverageOrderValue),
            OrdersChangePercent: CalculateChangePercent(totalOrders, previousOrdersCount),
            RefundsChangePercent: CalculateChangePercent(refunds, previousRefunds),
            RevenueOverTime: revenueOverTime,
            RevenueByPaymentMethod: revenueByPaymentMethod,
            RevenueSummary: new TenantAdminOutletRevenueSummaryBreakdownResponse(
                GrossRevenue: totalRevenue,
                Discounts: discounts,
                Returns: refunds,
                NetRevenue: totalRevenue - discounts - refunds,
                TaxCollected: taxCollected));
    }

    public async Task<TenantAdminOutletUsersResponse> GetUsersAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var assignedUsers = await (
            from assignment in _dbContext.OutletUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on assignment.TenantUserId equals user.Id
            join role in _dbContext.TenantRoles.AsNoTracking()
                on assignment.TenantRoleId equals role.Id
            where assignment.TenantId == tenantId &&
                  assignment.OutletId == outletId &&
                  assignment.RevokedAt == null
            orderby user.FullName
            select new TenantAdminOutletAssignedUserResponse(
                user.Id,
                user.DisplayName ?? user.FullName,
                role.RoleName,
                "Outlet Assignment",
                user.Phone,
                user.Email,
                "Outlet Access",
                user.AccountStatus,
                user.UpdatedAt))
            .ToListAsync(cancellationToken);

        var pendingInvites = await _dbContext.UserInvites
            .AsNoTracking()
            .CountAsync(
                x => x.TenantId == tenantId &&
                     x.InitialOutletId == outletId &&
                     x.InviteStatus == PendingInviteStatus,
                cancellationToken);

        var activeUsers = assignedUsers.Count(
            x => string.Equals(x.Status, ActiveAccountStatus, StringComparison.OrdinalIgnoreCase));

        var managers = await _dbContext.OutletUserRoles.AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.OutletId == outletId && x.IsPrimaryManager && x.RevokedAt == null, cancellationToken);

        return new TenantAdminOutletUsersResponse(
            new TenantAdminOutletUsersSummaryResponse(
                assignedUsers.Count,
                activeUsers,
                pendingInvites,
                managers),
            assignedUsers);
    }

    public async Task<TenantAdminOutletTillsResponse> GetTillsAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var tillRows = await _dbContext.Tills
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.OutletId == outletId &&
                        x.Status != TillConstants.DeletedStatus)
            .OrderBy(x => x.TillCode)
            .Select(x => new
            {
                x.Id,
                x.TillName,
                x.TillCode,
                x.Status,
            })
            .ToListAsync(cancellationToken);

        var tillIds = tillRows.Select(x => x.Id).ToList();
        var openSessions = tillIds.Count == 0
            ? []
            : await _dbContext.TillSessions
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.OutletId == outletId &&
                            tillIds.Contains(x.TillId) &&
                            x.Status == OpenSessionStatus)
                .Select(x => new
                {
                    x.TillId,
                    x.OpeningFloatAmount,
                    x.OpenedAt,
                    x.ClosedAt,
                    x.OpenedByTenantUserId,
                })
                .ToListAsync(cancellationToken);

        var latestClosedSessions = tillIds.Count == 0
            ? []
            : await _dbContext.TillSessions
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.OutletId == outletId &&
                            tillIds.Contains(x.TillId) &&
                            x.ClosedAt != null)
                .GroupBy(x => x.TillId)
                .Select(group => group
                    .OrderByDescending(x => x.ClosedAt)
                    .Select(x => new
                    {
                        x.TillId,
                        x.OpenedAt,
                        x.ClosedAt,
                        x.OpeningFloatAmount,
                        x.OpenedByTenantUserId,
                    })
                    .First())
                .ToListAsync(cancellationToken);

        var sessionSummaries = tillIds.Count == 0
            ? []
            : await _dbContext.TillSessionSummaries
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId &&
                            x.OutletId == outletId &&
                            tillIds.Contains(x.TillId))
                .GroupBy(x => x.TillId)
                .Select(group => group
                    .OrderByDescending(x => x.GeneratedAt)
                    .Select(x => new
                    {
                        x.TillId,
                        x.ExpectedCashAmount,
                        x.CashierTenantUserId,
                    })
                    .First())
                .ToListAsync(cancellationToken);

        var deviceAssignments = tillIds.Count == 0
            ? []
            : await (
                from assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
                join device in _dbContext.PosDevices.AsNoTracking()
                    on assignment.PosDeviceId equals device.Id
                where assignment.TenantId == tenantId &&
                      tillIds.Contains(assignment.TillId) &&
                      assignment.ReleasedAt == null
                select new
                {
                    assignment.TillId,
                    device.Status,
                })
                .ToListAsync(cancellationToken);

        var cashierUserIds = openSessions.Select(x => (Guid?)x.OpenedByTenantUserId)
            .Concat(latestClosedSessions.Select(x => (Guid?)x.OpenedByTenantUserId))
            .Concat(sessionSummaries.Select(x => (Guid?)x.CashierTenantUserId))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var cashiers = cashierUserIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.TenantUsers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && cashierUserIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName ?? x.FullName, cancellationToken);

        var items = tillRows
            .Select(till =>
            {
                var openSession = openSessions.FirstOrDefault(x => x.TillId == till.Id);
                var closedSession = latestClosedSessions.FirstOrDefault(x => x.TillId == till.Id);
                var summary = sessionSummaries.FirstOrDefault(x => x.TillId == till.Id);
                var device = deviceAssignments.FirstOrDefault(x => x.TillId == till.Id);
                var cashierId = openSession?.OpenedByTenantUserId ?? closedSession?.OpenedByTenantUserId ?? summary?.CashierTenantUserId;

                var deviceStatus = device is null
                    ? "Offline"
                    : string.Equals(device.Status, OutletConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase)
                        ? "Online"
                        : "Offline";

                var needsAttention = string.Equals(till.Status, OutletConstants.InactiveStatus, StringComparison.OrdinalIgnoreCase) ||
                                     deviceStatus == "Offline";

                return new TenantAdminOutletTillItemResponse(
                    till.Id,
                    till.TillName,
                    till.TillCode,
                    till.Status,
                    openSession is null ? summary?.ExpectedCashAmount : summary?.ExpectedCashAmount,
                    openSession?.OpeningFloatAmount ?? closedSession?.OpeningFloatAmount,
                    openSession?.OpenedAt ?? closedSession?.OpenedAt,
                    openSession?.ClosedAt ?? closedSession?.ClosedAt,
                    cashierId.HasValue && cashiers.TryGetValue(cashierId.Value, out var cashierName)
                        ? cashierName
                        : null,
                    deviceStatus);
            })
            .ToList();

        var activeTills = tillRows.Count(
            x => string.Equals(x.Status, OutletConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase));
        var openTills = openSessions.Select(x => x.TillId).Distinct().Count();
        var attentionTills = tillRows.Count(till =>
        {
            var device = deviceAssignments.FirstOrDefault(x => x.TillId == till.Id);
            var deviceStatus = device is null
                ? "Offline"
                : string.Equals(device.Status, OutletConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase)
                    ? "Online"
                    : "Offline";

            return string.Equals(till.Status, OutletConstants.InactiveStatus, StringComparison.OrdinalIgnoreCase) ||
                   deviceStatus == "Offline";
        });

        return new TenantAdminOutletTillsResponse(
            new TenantAdminOutletTillsSummaryResponse(
                tillRows.Count,
                activeTills,
                openTills,
                attentionTills),
            items);
    }

    private static readonly string[] OpenOrderStatuses = ["DRAFT", "CONFIRMED", "ACCEPTED", "PROCESSING", "READY_FOR_COLLECTION"];

    public async Task<OutletOverviewInfoResponse?> GetOverviewInfoAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        return await (
            from outlet in _dbContext.Outlets.AsNoTracking()
            join address in _dbContext.OutletAddresses.AsNoTracking()
                on outlet.Id equals address.OutletId into addressJoin
            from address in addressJoin
                .Where(x => x.AddressType == OutletConstants.PhysicalAddressType)
                .DefaultIfEmpty()
            join media in _dbContext.MediaAssets.AsNoTracking()
                on new { TenantId = outlet.TenantId, Id = outlet.PrimaryImageMediaAssetId }
                equals new { TenantId = media.TenantId, Id = (Guid?)media.Id } into mediaJoin
            from media in mediaJoin.DefaultIfEmpty()
            where outlet.TenantId == tenantId &&
                  outlet.Id == outletId &&
                  outlet.Status != OutletConstants.DeletedStatus
            select new OutletOverviewInfoResponse(
                outlet.Id,
                outlet.OutletName,
                outlet.OutletCode,
                outlet.OutletType,
                outlet.Status,
                media != null ? media.PublicUrl : null,
                address != null ? address.AddressLine1 : null,
                address != null ? address.City : null,
                outlet.PrimaryImageMediaAssetId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OutletOverviewManagerResponse?> GetOverviewManagerAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        return await (
            from assignment in _dbContext.OutletUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on assignment.TenantUserId equals user.Id
            join media in _dbContext.MediaAssets.AsNoTracking()
                on new { TenantId = user.TenantId, Id = user.ProfileImageUrl }
                equals new { TenantId = media.TenantId, Id = (Guid?)media.Id } into mediaJoin
            from media in mediaJoin.DefaultIfEmpty()
            where assignment.TenantId == tenantId &&
                  assignment.OutletId == outletId &&
                  assignment.IsPrimaryManager &&
                  assignment.RevokedAt == null &&
                  user.AccountStatus == ActiveAccountStatus
            select new OutletOverviewManagerResponse(
                user.Id,
                user.DisplayName ?? user.FullName,
                user.Email,
                user.Phone,
                media != null ? media.PublicUrl : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OutletOverviewSalesSummaryResponse> GetOverviewSalesAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var currencyCode = await GetTenantCurrencyCodeAsync(tenantId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var yesterdayStart = todayStart.AddDays(-1);

        var todayOrders = await (
            from order in _dbContext.SalesOrders.AsNoTracking()
            join till in _dbContext.Tills.AsNoTracking() on order.TillId equals till.Id
            where order.TenantId == tenantId &&
                  till.TenantId == tenantId &&
                  till.OutletId == outletId &&
                  order.CompletedAt >= todayStart &&
                  order.CompletedAt < now &&
                  order.Status == "COMPLETED"
            select new { order.TotalAmount, order.DiscountAmount, order.RefundedAmount })
            .ToListAsync(cancellationToken);

        var yesterdayOrders = await (
            from order in _dbContext.SalesOrders.AsNoTracking()
            join till in _dbContext.Tills.AsNoTracking() on order.TillId equals till.Id
            where order.TenantId == tenantId &&
                  till.TenantId == tenantId &&
                  till.OutletId == outletId &&
                  order.CompletedAt >= yesterdayStart &&
                  order.CompletedAt < todayStart &&
                  order.Status == "COMPLETED"
            select new { order.TotalAmount, order.DiscountAmount, order.RefundedAmount })
            .ToListAsync(cancellationToken);

        var todayNetSales = todayOrders.Sum(x => x.TotalAmount - x.DiscountAmount - x.RefundedAmount);
        var yesterdayNetSales = yesterdayOrders.Sum(x => x.TotalAmount - x.DiscountAmount - x.RefundedAmount);

        var changePercent = CalculateChangePercent(todayNetSales, yesterdayNetSales);

        return new OutletOverviewSalesSummaryResponse(
            TodayNetSales: Math.Max(0m, todayNetSales),
            YesterdayComparisonPercentage: changePercent,
            CurrencyCode: currencyCode);
    }

    public async Task<decimal> GetOverviewStockValueAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var stockValue = await (
            from balance in _dbContext.InventoryBalances.AsNoTracking()
            join location in _dbContext.InventoryLocations.AsNoTracking() on balance.InventoryLocationId equals location.Id
            join cost in _dbContext.InventoryCostLayers.AsNoTracking() on balance.Id equals cost.InventoryBalanceId
            where balance.TenantId == tenantId &&
                  location.TenantId == tenantId &&
                  location.OutletId == outletId &&
                  cost.TenantId == tenantId &&
                  cost.Status != "DELETED"
            select (decimal?)(cost.RemainingQuantity * cost.UnitCost))
            .SumAsync(cancellationToken);

        return stockValue ?? 0m;
    }

    public async Task<int> GetOverviewOpenOrdersCountAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        return await (
            from order in _dbContext.SalesOrders.AsNoTracking()
            join till in _dbContext.Tills.AsNoTracking() on order.TillId equals till.Id
            where order.TenantId == tenantId &&
                  till.TenantId == tenantId &&
                  till.OutletId == outletId &&
                  OpenOrderStatuses.Contains(order.Status)
            select order.Id)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutletOperationalHealthCalculator.TillHealthInput>> GetOverviewTillHealthInputsAsync(
        Guid tenantId,
        Guid outletId,
        CancellationToken cancellationToken)
    {
        var heartbeatCutoff = _dateTimeProvider.UtcNow.AddSeconds(-_options.Value.HeartbeatTimeoutSeconds);
        var rows = await (
            from till in _dbContext.Tills.AsNoTracking()
            join assignment in _dbContext.TillDeviceAssignments.AsNoTracking()
                on till.Id equals assignment.TillId into assignmentJoin
            from assignment in assignmentJoin
                .Where(a => a.ReleasedAt == null)
                .DefaultIfEmpty()
            join device in _dbContext.PosDevices.AsNoTracking()
                on assignment.PosDeviceId equals device.Id into deviceJoin
            from device in deviceJoin.DefaultIfEmpty()
            where till.TenantId == tenantId &&
                  till.OutletId == outletId &&
                  till.Status != TillConstants.DeletedStatus
            select new
            {
                TillId = till.Id,
                TillCode = till.TillCode,
                TillName = till.TillName,
                TillStatus = till.Status,
                DeviceStatus = device != null &&
                               device.Status == OutletConstants.ActiveStatus &&
                               device.LastSeenAt != null &&
                               device.LastSeenAt >= heartbeatCutoff
                    ? "Online"
                    : "Offline",
                DeviceLastSeenAt = device != null ? device.LastSeenAt : null
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new OutletOperationalHealthCalculator.TillHealthInput(
                r.TillId,
                r.TillCode,
                r.TillName,
                r.TillStatus,
                r.DeviceStatus,
                r.DeviceLastSeenAt))
            .ToList();
    }

    public async Task<string> GetTenantCurrencyCodeAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var currency = await _dbContext.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.BaseCurrencyCode)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(currency) ? "LKR" : currency;
    }

    public Task<bool> TenantUserExistsAndActiveAsync(
        Guid tenantId,
        Guid tenantUserId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantUsers
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == tenantUserId &&
                     x.AccountStatus == ActiveAccountStatus,
                cancellationToken);
    }

    public Task<bool> MediaAssetExistsAndActiveAsync(
        Guid tenantId,
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        return _dbContext.MediaAssets
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.Id == mediaAssetId &&
                     x.Status == OutletConstants.ActiveStatus,
                cancellationToken);
    }

    public async Task<bool> SetPrimaryManagerAsync(
        Guid tenantId,
        Guid outletId,
        Guid tenantUserId,
        Guid? assignedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingPrimaryManagers = await _dbContext.OutletUserRoles
            .Where(x => x.TenantId == tenantId &&
                        x.OutletId == outletId &&
                        x.IsPrimaryManager &&
                        x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingPrimaryManagers)
        {
            existing.SetPrimaryManager(false, now);
        }

        var userAssignment = await _dbContext.OutletUserRoles
            .Where(x => x.TenantId == tenantId &&
                        x.OutletId == outletId &&
                        x.TenantUserId == tenantUserId &&
                        x.RevokedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (userAssignment is not null)
        {
            userAssignment.SetPrimaryManager(true, now);
        }
        else
        {
            var defaultRole = await _dbContext.TenantRoles
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.RoleName)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultRole is null)
            {
                return false;
            }

            var newAssignment = OutletUserRole.Create(
                Guid.NewGuid(),
                tenantId,
                outletId,
                tenantUserId,
                defaultRole.Id,
                assignedByTenantUserId,
                now,
                isPrimaryManager: true);

            _dbContext.OutletUserRoles.Add(newAssignment);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemovePrimaryManagerAsync(
        Guid tenantId,
        Guid outletId,
        Guid? revokedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingPrimaryManagers = await _dbContext.OutletUserRoles
            .Where(x => x.TenantId == tenantId &&
                        x.OutletId == outletId &&
                        x.IsPrimaryManager &&
                        x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        if (existingPrimaryManagers.Count == 0)
        {
            return true;
        }

        foreach (var existing in existingPrimaryManagers)
        {
            existing.SetPrimaryManager(false, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetOutletImageAsync(
        Guid tenantId,
        Guid outletId,
        Guid mediaAssetId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var outlet = await _dbContext.Outlets
            .Where(x => x.TenantId == tenantId &&
                        x.Id == outletId &&
                        x.Status != OutletConstants.DeletedStatus)
            .FirstOrDefaultAsync(cancellationToken);

        if (outlet is null)
        {
            return false;
        }

        outlet.SetPrimaryImageMediaAssetId(mediaAssetId, updatedByTenantUserId, now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveOutletImageAsync(
        Guid tenantId,
        Guid outletId,
        Guid? updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var outlet = await _dbContext.Outlets
            .Where(x => x.TenantId == tenantId &&
                        x.Id == outletId &&
                        x.Status != OutletConstants.DeletedStatus)
            .FirstOrDefaultAsync(cancellationToken);

        if (outlet is null)
        {
            return false;
        }

        outlet.SetPrimaryImageMediaAssetId(null, updatedByTenantUserId, now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<List<OrderRevenueRow>> QueryOutletOrdersAsync(
        Guid tenantId,
        Guid outletId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        return await (
            from order in _dbContext.SalesOrders.AsNoTracking()
            join till in _dbContext.Tills.AsNoTracking()
                on order.TillId equals till.Id
            where order.TenantId == tenantId &&
                  till.TenantId == tenantId &&
                  till.OutletId == outletId &&
                  order.CompletedAt != null &&
                  order.CompletedAt >= start &&
                  order.CompletedAt < end
            select new OrderRevenueRow(
                order.TotalAmount,
                order.DiscountAmount,
                order.TaxAmount,
                order.RefundedAmount,
                order.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    private static decimal? CalculateChangePercent(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            return current == 0m ? 0m : 100m;
        }

        return Math.Round((current - previous) / previous * 100m, 1);
    }

    private static decimal? CalculateChangePercent(int current, int previous)
        => CalculateChangePercent((decimal)current, previous);

    private static string FormatOperatingHours(
        IReadOnlyList<BusinessHourRow> businessHours)
    {
        if (businessHours.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            businessHours.Select(hour =>
                hour.OpeningTime.HasValue && hour.ClosingTime.HasValue
                    ? $"{DayLabel(hour.DayOfWeek)} {hour.OpeningTime.Value:HH:mm}-{hour.ClosingTime.Value:HH:mm}"
                    : $"{DayLabel(hour.DayOfWeek)} Closed"));
    }

    private static string DayLabel(int dayOfWeek) =>
        dayOfWeek switch
        {
            0 => "Sun",
            1 => "Mon",
            2 => "Tue",
            3 => "Wed",
            4 => "Thu",
            5 => "Fri",
            6 => "Sat",
            _ => "Day",
        };

    private static IQueryable<TenantAdminOutletListRow> ApplySorting(
        IQueryable<TenantAdminOutletListRow> rows,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "code" => descending ? rows.OrderByDescending(row => row.Code).ThenBy(row => row.Id) : rows.OrderBy(row => row.Code).ThenBy(row => row.Id),
            "status" => descending ? rows.OrderByDescending(row => row.Status).ThenBy(row => row.Name).ThenBy(row => row.Id) : rows.OrderBy(row => row.Status).ThenBy(row => row.Name).ThenBy(row => row.Id),
            "type" => descending ? rows.OrderByDescending(row => row.Type).ThenBy(row => row.Name).ThenBy(row => row.Id) : rows.OrderBy(row => row.Type).ThenBy(row => row.Name).ThenBy(row => row.Id),
            "operationalhealth" or "operational_health" => descending ? rows.OrderByDescending(row => row.ActiveTillCount - row.OnlineTillCount).ThenBy(row => row.Name).ThenBy(row => row.Id) : rows.OrderBy(row => row.ActiveTillCount - row.OnlineTillCount).ThenBy(row => row.Name).ThenBy(row => row.Id),
            _ => descending ? rows.OrderByDescending(row => row.Name).ThenBy(row => row.Id) : rows.OrderBy(row => row.Name).ThenBy(row => row.Id),
        };
    }

    private static TenantAdminOutletListItemResponse MapListRow(TenantAdminOutletListRow row, string? managerName, string? managerAvatarUrl)
    {
        var healthStatus = OutletOperationalHealthCalculator.Classify(row.ActiveTillCount, row.OnlineTillCount);
        var displayLocation = string.Join(", ", new[] { row.AddressLine, row.City }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new TenantAdminOutletListItemResponse(
            row.Id,
            row.Name,
            row.Code,
            row.Type,
            row.Status,
            row.ImageUrl,
            row.ManagerId.HasValue ? new TenantAdminOutletManagerPreviewResponse(row.ManagerId.Value, managerName, managerAvatarUrl) : null,
            new TenantAdminOutletTillPreviewResponse(row.TotalTillCount, row.ActiveTillCount, row.OnlineTillCount),
            new TenantAdminOutletHealthPreviewResponse(healthStatus, Math.Max(0, row.ActiveTillCount - row.OnlineTillCount)),
            row.AddressLine is null && row.City is null ? null : new TenantAdminOutletLocationPreviewResponse(row.AddressLine, row.City, displayLocation),
            new TenantAdminOutletListSectionAccessResponse(true));
    }

    private sealed record BusinessHourRow(short DayOfWeek, TimeOnly? OpeningTime, TimeOnly? ClosingTime);

    private sealed class TenantAdminOutletListRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? ImageStorageKey { get; set; }
        public string? ImageContainerName { get; set; }
        public Guid? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerAvatarUrl { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public int TotalTillCount { get; set; }
        public int ActiveTillCount { get; set; }
        public int OnlineTillCount { get; set; }
    }

    private sealed record OrderRevenueRow(
        decimal TotalAmount,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal RefundedAmount,
        DateTimeOffset? CompletedAt);
}
