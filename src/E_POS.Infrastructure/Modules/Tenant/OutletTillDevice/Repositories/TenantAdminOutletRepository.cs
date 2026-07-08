using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Constants;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

public sealed class TenantAdminOutletRepository : ITenantAdminOutletRepository
{
    private const string ActiveAccountStatus = "ACTIVE";
    private const string PendingInviteStatus = "PENDING";
    private const string OpenSessionStatus = "OPEN";
    private const string ManagerRoleKeyword = "MANAGER";

    private readonly EPosDbContext _dbContext;

    public TenantAdminOutletRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
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
                outlet.Name,
                outlet.OutletCode,
                outlet.OutletType,
                outlet.Status,
                outlet.ContactPhone,
                outlet.ContactEmail,
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
            .Select(x => new BusinessHourRow(x.DayOfWeek, x.OpenTime, x.CloseTime))
            .ToListAsync(cancellationToken);

        var managerName = await (
            from assignment in _dbContext.OutletUserRoles.AsNoTracking()
            join user in _dbContext.TenantUsers.AsNoTracking()
                on assignment.TenantUserId equals user.Id
            join role in _dbContext.TenantRoles.AsNoTracking()
                on assignment.TenantRoleId equals role.Id
            where assignment.TenantId == tenantId &&
                  assignment.OutletId == outletId &&
                  assignment.RevokedAt == null &&
                  role.RoleName.ToUpper().Contains(ManagerRoleKeyword)
            orderby assignment.AssignedAt
            select user.DisplayName ?? user.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return new TenantAdminOutletDetailResponse(
            OutletId: row.Id,
            OutletName: row.Name,
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

        var managers = assignedUsers.Count(
            x => x.RoleName.Contains(ManagerRoleKeyword, StringComparison.OrdinalIgnoreCase));

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
                x.Name,
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
                where assignment.TillId != null &&
                      tillIds.Contains(assignment.TillId!.Value) &&
                      assignment.Status == TillDeviceAssignmentConstants.ActiveStatus
                select new
                {
                    TillId = assignment.TillId!.Value,
                    device.Status,
                })
                .ToListAsync(cancellationToken);

        var cashierIds = openSessions
            .Select(x => x.OpenedByTenantUserId)
            .Concat(latestClosedSessions.Select(x => x.OpenedByTenantUserId))
            .Concat(sessionSummaries.Select(x => x.CashierTenantUserId))
            .Distinct()
            .ToList();

        var cashiers = cashierIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.TenantUsers
                .AsNoTracking()
                .Where(x => cashierIds.Contains(x.Id))
                .Select(x => new { x.Id, Name = x.DisplayName ?? x.FullName })
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var items = tillRows
            .Select(till =>
            {
                var openSession = openSessions.FirstOrDefault(x => x.TillId == till.Id);
                var closedSession = latestClosedSessions.FirstOrDefault(x => x.TillId == till.Id);
                var summary = sessionSummaries.FirstOrDefault(x => x.TillId == till.Id);
                var device = deviceAssignments.FirstOrDefault(x => x.TillId == till.Id);

                Guid? cashierId = openSession?.OpenedByTenantUserId ??
                                  summary?.CashierTenantUserId ??
                                  closedSession?.OpenedByTenantUserId;

                var deviceStatus = device is null
                    ? "Offline"
                    : string.Equals(device.Status, OutletConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase)
                        ? "Online"
                        : "Offline";

                var needsAttention = string.Equals(till.Status, OutletConstants.InactiveStatus, StringComparison.OrdinalIgnoreCase) ||
                                     deviceStatus == "Offline";

                return new
                {
                    Item = new TenantAdminOutletTillItemResponse(
                        till.Id,
                        till.Name,
                        till.TillCode,
                        till.Status,
                        openSession is null ? summary?.ExpectedCashAmount : summary?.ExpectedCashAmount,
                        openSession?.OpeningFloatAmount ?? closedSession?.OpeningFloatAmount,
                        openSession?.OpenedAt ?? closedSession?.OpenedAt,
                        openSession?.ClosedAt ?? closedSession?.ClosedAt,
                        cashierId.HasValue && cashiers.TryGetValue(cashierId.Value, out var cashierName)
                            ? cashierName
                            : null,
                        deviceStatus),
                    IsActive = string.Equals(till.Status, OutletConstants.ActiveStatus, StringComparison.OrdinalIgnoreCase),
                    IsOpen = openSession is not null,
                    NeedsAttention = needsAttention,
                };
            })
            .Select(x => x.Item)
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
                $"{DayLabel(hour.DayOfWeek)} {hour.OpenTime:HH:mm}-{hour.CloseTime:HH:mm}"));
    }

    private sealed record BusinessHourRow(int DayOfWeek, TimeOnly OpenTime, TimeOnly CloseTime);

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

    private sealed record OrderRevenueRow(
        decimal TotalAmount,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal RefundedAmount,
        DateTimeOffset? CompletedAt);
}
