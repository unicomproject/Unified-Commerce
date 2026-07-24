using E_POS.Application.Modules.ECommerce.Customer.Contracts;
using E_POS.Application.Modules.ECommerce.Customer.Dtos;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Infrastructure.Modules.ECommerce.Customer.Repositories;

public sealed class PosCustomerRepository : IPosCustomerRepository
{
    private const string UniqueViolationSqlState = PostgresErrorCodes.UniqueViolation;
    private readonly EPosDbContext _dbContext;

    public PosCustomerRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> NormalizedPhoneExistsAsync(
        Guid tenantId,
        string normalizedPhone,
        CancellationToken cancellationToken) =>
        _dbContext.Customers.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.NormalizedPhone == normalizedPhone &&
                 x.Status != "DELETED",
            cancellationToken);

    public Task<bool> NormalizedPhoneExistsAsync(
        Guid tenantId,
        string normalizedPhone,
        Guid excludingCustomerId,
        CancellationToken cancellationToken) =>
        _dbContext.Customers.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.Id != excludingCustomerId &&
                 x.NormalizedPhone == normalizedPhone &&
                 x.Status != "DELETED",
            cancellationToken);

    public Task<bool> NormalizedEmailExistsAsync(
        Guid tenantId,
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        _dbContext.Customers.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.NormalizedEmail == normalizedEmail &&
                 x.Status != "DELETED",
            cancellationToken);

    public Task<bool> NormalizedEmailExistsAsync(
        Guid tenantId,
        string normalizedEmail,
        Guid excludingCustomerId,
        CancellationToken cancellationToken) =>
        _dbContext.Customers.AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId &&
                 x.Id != excludingCustomerId &&
                 x.NormalizedEmail == normalizedEmail &&
                 x.Status != "DELETED",
            cancellationToken);

    public async Task<bool> AddAsync(
        CustomerEntity customer,
        CancellationToken cancellationToken)
    {
        _dbContext.Customers.Add(customer);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            _dbContext.Entry(customer).State = EntityState.Detached;
            return false;
        }
    }

    public Task<CustomerEntity?> GetTrackedByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        _dbContext.Customers.FirstOrDefaultAsync(
            x => x.TenantId == tenantId &&
                 x.Id == customerId &&
                 x.Status != "DELETED",
            cancellationToken);

    public Task<string?> GetCustomerStatusAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken) =>
        _dbContext.Customers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == customerId)
            .Select(x => (string?)x.Status)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> UpdateAsync(
        CustomerEntity customer,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            _dbContext.Entry(customer).State = EntityState.Detached;
            return false;
        }
    }

    public Task<string?> GetTenantDefaultTimezoneAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        _dbContext.Tenants.AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => x.DefaultTimezone)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PosCustomerListResponseDto> ListAsync(
        Guid tenantId,
        string? search,
        string? status,
        string? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != "DELETED");

        var statusFilter = status?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "ALL")
        {
            query = query.Where(x => x.Status == statusFilter);
        }
        else if (string.IsNullOrWhiteSpace(statusFilter) || statusFilter == "ALL")
        {
            // Default POS list remains active-first unless an explicit filter is provided.
            if (string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == "ACTIVE");
            }
        }

        var sourceFilter = source?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(sourceFilter) && sourceFilter != "ALL")
        {
            query = query.Where(x => x.SourceType == sourceFilter);
        }

        var searchTerm = search?.Trim();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, pattern) ||
                (x.Phone != null && EF.Functions.ILike(x.Phone, pattern)) ||
                (x.NormalizedPhone != null && EF.Functions.ILike(x.NormalizedPhone, pattern)) ||
                (x.Email != null && EF.Functions.ILike(x.Email, pattern)) ||
                (x.NormalizedEmail != null && EF.Functions.ILike(x.NormalizedEmail, pattern)) ||
                EF.Functions.ILike(x.CustomerCode, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageRows = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.Status,
                x.CustomerCode,
                x.SourceType,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var customerIds = pageRows.Select(x => x.Id).ToArray();
        var completedOrderRows = customerIds.Length == 0
            ? []
            : await _dbContext.SalesOrders.AsNoTracking()
                .Where(o => o.TenantId == tenantId &&
                            o.CustomerId.HasValue &&
                            customerIds.Contains(o.CustomerId.Value) &&
                            o.CancelledAt == null &&
                            o.Status == "COMPLETED")
                .Select(o => new
                {
                    CustomerId = o.CustomerId!.Value,
                    o.TotalAmount,
                    o.CurrencyCode,
                    OrderDate = o.CompletedAt ?? o.PlacedAt ?? o.CreatedAt
                })
                .ToListAsync(cancellationToken);

        var orderStats = completedOrderRows
            .GroupBy(o => o.CustomerId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var currencies = g
                        .Select(x => x.CurrencyCode)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    var mixed = currencies.Length > 1;
                    return new
                    {
                        OrderCount = g.Count(),
                        TotalSpent = mixed ? 0m : g.Sum(x => x.TotalAmount),
                        LastPurchaseAt = (DateTimeOffset?)g.Max(x => x.OrderDate),
                        CurrencyCode = mixed ? null : currencies.FirstOrDefault(),
                        IsMixedCurrencySpend = mixed
                    };
                });

        var items = pageRows.Select(x =>
        {
            orderStats.TryGetValue(x.Id, out var stats);
            return new PosCustomerListItemResponseDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.Status,
                x.CustomerCode,
                x.SourceType,
                x.CreatedAt,
                stats?.OrderCount ?? 0,
                stats?.TotalSpent ?? 0m,
                stats?.CurrencyCode,
                stats?.LastPurchaseAt,
                stats?.IsMixedCurrencySpend ?? false);
        }).ToList();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PosCustomerListResponseDto(
            items,
            page,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<PosCustomerSummaryResponseDto> GetSummaryAsync(
        Guid tenantId,
        DateTimeOffset monthStartUtc,
        DateTimeOffset monthEndUtc,
        string timeZoneId,
        CancellationToken cancellationToken)
    {
        var customers = _dbContext.Customers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != "DELETED");

        var totalCustomers = await customers.CountAsync(cancellationToken);
        var activeCustomers = await customers.CountAsync(x => x.Status == "ACTIVE", cancellationToken);
        var newCustomersThisMonth = await customers.CountAsync(
            x => x.CreatedAt >= monthStartUtc && x.CreatedAt < monthEndUtc,
            cancellationToken);

        var customersWithOrders = await _dbContext.SalesOrders.AsNoTracking()
            .Where(o => o.TenantId == tenantId &&
                        o.CustomerId.HasValue &&
                        o.CancelledAt == null &&
                        o.Status == "COMPLETED")
            .Select(o => o.CustomerId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);

        return new PosCustomerSummaryResponseDto(
            totalCustomers,
            activeCustomers,
            customersWithOrders,
            newCustomersThisMonth,
            timeZoneId);
    }

    public async Task<PosCustomerListItemResponseDto?> GetByIdAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == customerId && x.Status != "DELETED")
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.Status,
                x.CustomerCode,
                x.SourceType,
                x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var completedOrders = await _dbContext.SalesOrders.AsNoTracking()
            .Where(o => o.TenantId == tenantId &&
                        o.CustomerId == customerId &&
                        o.CancelledAt == null &&
                        o.Status == "COMPLETED")
            .Select(o => new
            {
                o.TotalAmount,
                o.CurrencyCode,
                OrderDate = o.CompletedAt ?? o.PlacedAt ?? o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var orderCount = completedOrders.Count;
        var currencies = completedOrders
            .Select(o => o.CurrencyCode)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var mixedCurrency = currencies.Length > 1;
        var totalSpent = mixedCurrency || orderCount == 0
            ? 0m
            : completedOrders.Sum(o => o.TotalAmount);
        var lastPurchase = orderCount == 0
            ? null
            : (DateTimeOffset?)completedOrders.Max(o => o.OrderDate);
        var currency = mixedCurrency || orderCount == 0
            ? null
            : currencies.FirstOrDefault();

        return new PosCustomerListItemResponseDto(
            customer.Id,
            customer.Name,
            customer.Phone,
            customer.Email,
            customer.Status,
            customer.CustomerCode,
            customer.SourceType,
            customer.CreatedAt,
            orderCount,
            totalSpent,
            currency,
            lastPurchase,
            mixedCurrency);
    }

    public async Task<PosCustomerOrdersResponseDto> GetOrdersAsync(
        Guid tenantId,
        Guid customerId,
        int page,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? status,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.SalesOrders.AsNoTracking()
            .Where(o => o.TenantId == tenantId &&
                        o.CustomerId == customerId &&
                        o.CancelledAt == null);

        var statusFilter = status?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "ALL")
        {
            query = query.Where(o => o.Status == statusFilter);
        }
        else
        {
            query = query.Where(o => o.Status == "COMPLETED" || o.Status == "DRAFT");
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => (o.CompletedAt ?? o.PlacedAt ?? o.CreatedAt) >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => (o.CompletedAt ?? o.PlacedAt ?? o.CreatedAt) <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var orderRows = await query
            .OrderByDescending(o => o.CompletedAt ?? o.PlacedAt ?? o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                OrderDate = o.CompletedAt ?? o.PlacedAt ?? o.CreatedAt,
                o.TotalAmount,
                o.CurrencyCode,
                o.Status,
                o.TillId
            })
            .ToListAsync(cancellationToken);

        var tillIds = orderRows
            .Where(x => x.TillId.HasValue)
            .Select(x => x.TillId!.Value)
            .Distinct()
            .ToArray();
        var outletNamesByTill = tillIds.Length == 0
            ? new Dictionary<Guid, string?>()
            : await (
                from till in _dbContext.Tills.AsNoTracking()
                join outlet in _dbContext.Outlets.AsNoTracking()
                    on new { till.TenantId, till.OutletId } equals new { outlet.TenantId, OutletId = outlet.Id }
                where till.TenantId == tenantId && tillIds.Contains(till.Id)
                select new { till.Id, OutletName = outlet.OutletName })
                .ToDictionaryAsync(x => x.Id, x => (string?)x.OutletName, cancellationToken);

        var items = orderRows
            .Select(o => new PosCustomerOrderItemDto(
                o.Id,
                o.OrderNumber,
                o.OrderDate,
                o.TotalAmount,
                o.CurrencyCode,
                o.Status,
                o.TillId.HasValue && outletNamesByTill.TryGetValue(o.TillId.Value, out var name)
                    ? name
                    : null))
            .ToList();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PosCustomerOrdersResponseDto(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<bool> TryAssignCustomerToEditableSaleAsync(
        Guid tenantId,
        Guid saleId,
        Guid customerId,
        string? customerNameSnapshot,
        Guid? tillSessionId,
        Guid updatedByTenantUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sale = await _dbContext.SalesOrders
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == saleId,
                cancellationToken);
        if (sale is null)
        {
            return false;
        }

        if (tillSessionId.HasValue &&
            sale.TillSessionId.HasValue &&
            sale.TillSessionId.Value != tillSessionId.Value)
        {
            return false;
        }

        if (!sale.TryAssignCustomer(
                customerId,
                customerNameSnapshot,
                updatedByTenantUserId,
                now))
        {
            return false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
