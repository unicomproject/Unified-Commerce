using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerOrders.Services;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class ClickCollectOrderStatusServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid TenantUserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpdateStatusAsync_WithoutManagePermission_ReturnsPermissionDenied()
    {
        var repository = new FakeStatusRepository();
        var service = new ClickCollectOrderStatusService(repository, new FakeClock());

        var result = await service.UpdateStatusAsync(
            new TenantRequestContext(TenantId, TenantUserId, []),
            Guid.NewGuid(),
            new ClickCollectOrderStatusUpdateRequest { Status = "ACCEPTED" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("click_collect_orders.permission_denied", result.Error.Code);
        Assert.Null(repository.TargetStatus);
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidReadyStatus_ForwardsNormalizedStatusAndContext()
    {
        var repository = new FakeStatusRepository();
        var service = new ClickCollectOrderStatusService(repository, new FakeClock());
        var orderId = Guid.NewGuid();

        var result = await service.UpdateStatusAsync(
            new TenantRequestContext(TenantId, TenantUserId, ["fulfillment.orders.manage"]),
            orderId,
            new ClickCollectOrderStatusUpdateRequest { Status = "ready" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, repository.TenantId);
        Assert.Equal(TenantUserId, repository.TenantUserId);
        Assert.Equal(orderId, repository.OrderId);
        Assert.Equal("READY_FOR_COLLECTION", repository.TargetStatus);
        Assert.Equal(Now, repository.Now);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelledStatus_IsRejected()
    {
        var repository = new FakeStatusRepository();
        var service = new ClickCollectOrderStatusService(repository, new FakeClock());

        var result = await service.UpdateStatusAsync(
            new TenantRequestContext(TenantId, TenantUserId, ["fulfillment.orders.manage"]),
            Guid.NewGuid(),
            new ClickCollectOrderStatusUpdateRequest { Status = "CANCELLED" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("click_collect_orders.invalid_status", result.Error.Code);
        Assert.Null(repository.TargetStatus);
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeStatusRepository : IClickCollectOrderStatusRepository
    {
        public Guid? TenantId { get; private set; }
        public Guid? TenantUserId { get; private set; }
        public Guid? OrderId { get; private set; }
        public string? TargetStatus { get; private set; }
        public DateTimeOffset? Now { get; private set; }

        public Task<ClickCollectOrderStatusUpdateRepositoryResult> UpdateStatusAsync(
            Guid tenantId,
            Guid tenantUserId,
            Guid orderId,
            string targetStatus,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            TenantUserId = tenantUserId;
            OrderId = orderId;
            TargetStatus = targetStatus;
            Now = now;
            return Task.FromResult(ClickCollectOrderStatusUpdateRepositoryResult.Success(
                new ClickCollectOrderStatusUpdateResponse
                {
                    Id = orderId,
                    OrderNumber = "SO-WEB-1",
                    Status = targetStatus,
                    StatusLabel = targetStatus,
                    FulfillmentStatus = targetStatus,
                    UpdatedAt = now,
                    CollectionQrAvailable = true
                }));
        }
    }
}
