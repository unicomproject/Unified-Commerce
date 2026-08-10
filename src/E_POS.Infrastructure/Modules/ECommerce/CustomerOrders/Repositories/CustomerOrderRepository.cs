using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.Shared.Media.Contracts;
using E_POS.Infrastructure.Persistence;

namespace E_POS.Infrastructure.Modules.ECommerce.CustomerOrders.Repositories;

public sealed class CustomerOrderRepository : ICustomerOrderRepository
{
    private readonly ICustomerOrderReadRepository _readRepository;
    private readonly ICustomerOrderCancelRepository _cancelRepository;

    public CustomerOrderRepository(EPosDbContext dbContext, IMediaReadUrlResolver? mediaReadUrlResolver = null)
        : this(
            new CustomerOrderReadRepository(dbContext, mediaReadUrlResolver),
            new CustomerOrderCancelRepository(dbContext, mediaReadUrlResolver))
    {
    }

    public CustomerOrderRepository(
        ICustomerOrderReadRepository readRepository,
        ICustomerOrderCancelRepository cancelRepository)
    {
        _readRepository = readRepository;
        _cancelRepository = cancelRepository;
    }

    public Task<CustomerOrderListReadModel> GetAsync(
        Guid tenantId,
        Guid customerId,
        string? normalizedStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        _readRepository.GetAsync(
            tenantId,
            customerId,
            normalizedStatus,
            page,
            pageSize,
            cancellationToken);

    public Task<CustomerOrderDetailReadModel?> GetDetailAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        CancellationToken cancellationToken) =>
        _readRepository.GetDetailAsync(
            tenantId,
            customerId,
            orderId,
            cancellationToken);

    public Task<CustomerOrderCancelRepositoryResult> CancelAsync(
        Guid tenantId,
        Guid customerId,
        Guid orderId,
        string? reason,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _cancelRepository.CancelAsync(
            tenantId,
            customerId,
            orderId,
            reason,
            now,
            cancellationToken);
}
