using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Shared.Media;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Entities;
using E_POS.Domain.Modules.Tenant.Orders.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed class CustomerOrderReadRepository : CustomerOrderRepositoryBase, ICustomerOrderReadRepository
{
    public CustomerOrderReadRepository(EPosDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<CustomerOrderListReadModel> GetAsync(
        Guid tenantId,
        Guid customerId,
        string? normalizedStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = DbContext.SalesOrders
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.CustomerId == customerId &&
                x.OrderType == ClickAndCollectOrderType);

        query = ApplyStatusFilter(query, normalizedStatus);

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .OrderByDescending(x => x.PlacedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            return new CustomerOrderListReadModel
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = CalculateTotalPages(totalCount, pageSize)
            };
        }

        var orderIds = orders.Select(x => x.Id).ToList();
        var lines = await DbContext.SalesOrderLines
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.SalesOrderId.HasValue &&
                orderIds.Contains(x.SalesOrderId.Value) &&
                x.LineStatus != "CANCELLED")
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);

        var linesByOrder = lines
            .GroupBy(x => x.SalesOrderId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        var productIds = lines
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();
        var imageLookup = await BuildImageLookupAsync(tenantId, productIds, cancellationToken);

        var items = orders
            .Select(order => BuildSummary(order, linesByOrder, imageLookup))
            .ToList();

        return new CustomerOrderListReadModel
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = CalculateTotalPages(totalCount, pageSize)
        };
    }

    public async Task<CustomerOrderDetailReadModel?> GetDetailAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await DbContext.SalesOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.CustomerId == customerId &&
                x.Id == orderId &&
                x.OrderType == ClickAndCollectOrderType,
                cancellationToken);

        if (order is null)
            return null;

        var lines = await DbContext.SalesOrderLines
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.SalesOrderId == orderId &&
                x.LineStatus != "CANCELLED")
            .OrderBy(x => x.LineNumber)
            .ToListAsync(cancellationToken);

        var productIds = lines
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();
        var imageLookup = await BuildImageLookupAsync(tenantId, productIds, cancellationToken);
        var statusHistory = await DbContext.SalesOrderStatusHistory
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.SalesOrderId == orderId)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync(cancellationToken);

        return BuildDetail(order, lines, imageLookup, statusHistory);
    }

}
