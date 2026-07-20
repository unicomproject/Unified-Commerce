using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Services;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class CustomerOrderServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_ReadyHyphenStatus_ForwardsNormalizedStatus()
    {
        var repository = new FakeCustomerOrderRepository();
        var service = new CustomerOrderService(repository, new FakeDateTimeProvider(Now));

        var result = await service.GetAsync(
            TenantId,
            CustomerId,
            "ready-for-collection",
            1,
            10,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("READY_FOR_COLLECTION", repository.NormalizedStatus);
    }

    [Fact]
    public async Task GetDetailAsync_MissingCustomerContext_DoesNotCallRepository()
    {
        var repository = new FakeCustomerOrderRepository();
        var service = new CustomerOrderService(repository, new FakeDateTimeProvider(Now));

        var result = await service.GetDetailAsync(
            Guid.Empty,
            CustomerId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_orders.invalid_customer_context", result.Error.Code);
        Assert.Null(repository.DetailOrderId);
    }

    [Fact]
    public async Task GetDetailAsync_NotFound_ReturnsSafeNotFoundError()
    {
        var repository = new FakeCustomerOrderRepository();
        var service = new CustomerOrderService(repository, new FakeDateTimeProvider(Now));

        var result = await service.GetDetailAsync(
            TenantId,
            CustomerId,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_orders.not_found", result.Error.Code);
    }

    [Fact]
    public async Task CancelAsync_ValidRequest_ForwardsTrimmedReasonAndClock()
    {
        var orderId = Guid.NewGuid();
        var repository = new FakeCustomerOrderRepository
        {
            CancelResult = CustomerOrderCancelRepositoryResult.Success(new CustomerOrderCancelResponse
            {
                Id = orderId,
                OrderNumber = "SO-WEB-1",
                Status = "CANCELLED",
                StatusLabel = "Cancelled",
                CancelledAt = Now,
                Message = "Order cancelled successfully."
            })
        };
        var service = new CustomerOrderService(repository, new FakeDateTimeProvider(Now));

        var result = await service.CancelAsync(
            TenantId,
            CustomerId,
            orderId,
            new CustomerOrderCancelRequest { Reason = "  Changed my mind  " },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(orderId, repository.CancelOrderId);
        Assert.Equal("Changed my mind", repository.CancelReason);
        Assert.Equal(Now, repository.CancelNow);
    }

    [Fact]
    public async Task CancelAsync_TooLongReason_ReturnsValidationErrorWithoutCallingRepository()
    {
        var repository = new FakeCustomerOrderRepository();
        var service = new CustomerOrderService(repository, new FakeDateTimeProvider(Now));

        var result = await service.CancelAsync(
            TenantId,
            CustomerId,
            Guid.NewGuid(),
            new CustomerOrderCancelRequest { Reason = new string('x', 501) },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("customer_orders.invalid_cancel_reason", result.Error.Code);
        Assert.Null(repository.CancelOrderId);
    }

    private sealed class FakeCustomerOrderRepository : ICustomerOrderRepository
    {
        public string? NormalizedStatus { get; private set; }
        public Guid? DetailOrderId { get; private set; }
        public Guid? CancelOrderId { get; private set; }
        public string? CancelReason { get; private set; }
        public DateTimeOffset? CancelNow { get; private set; }
        public CustomerOrderCancelRepositoryResult CancelResult { get; init; } =
            CustomerOrderCancelRepositoryResult.Failure("customer_orders.not_found");

        public Task<CustomerOrderListReadModel> GetAsync(
            Guid tenantId,
            Guid customerId,
            string? normalizedStatus,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            NormalizedStatus = normalizedStatus;
            return Task.FromResult(new CustomerOrderListReadModel());
        }

        public Task<CustomerOrderDetailReadModel?> GetDetailAsync(
            Guid tenantId,
            Guid customerId,
            Guid orderId,
            CancellationToken cancellationToken)
        {
            DetailOrderId = orderId;
            return Task.FromResult<CustomerOrderDetailReadModel?>(null);
        }

        public Task<CustomerOrderCancelRepositoryResult> CancelAsync(
            Guid tenantId,
            Guid customerId,
            Guid orderId,
            string? reason,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CancelOrderId = orderId;
            CancelReason = reason;
            CancelNow = now;
            return Task.FromResult(CancelResult);
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
