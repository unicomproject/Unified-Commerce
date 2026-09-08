using E_POS.Domain.Modules.ECommerce.FulfilmentPickup.Entities;
using Xunit;

namespace E_POS.UnitTests.ECommerce.CustomerOrders;

public sealed class FulfillmentOrderConcurrencyTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 7, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("PICKING")]
    [InlineData("PICKED")]
    [InlineData("PACKED")]
    [InlineData("READY")]
    [InlineData("FULFILLED")]
    [InlineData("CANCELLED")]
    public void StartPicking_AlreadyStartedOrTerminalStatus_IsRejected(string status)
    {
        var fulfillment = Create(status, 5);

        var error = Assert.Throws<InvalidOperationException>(() =>
            fulfillment.StartPicking(ActorId, 5, Now));

        Assert.Equal("FULFILLMENT_NOT_STARTABLE", error.Message);
        Assert.Equal(status, fulfillment.FulfillmentStatus);
        Assert.Equal(5, fulfillment.RowVersion);
        Assert.Null(fulfillment.AssignedToTenantUserId);
    }

    [Fact]
    public void StartPicking_StaleVersion_IsRejectedWithoutMutation()
    {
        var fulfillment = Create("PENDING", 6);

        var error = Assert.Throws<InvalidOperationException>(() =>
            fulfillment.StartPicking(ActorId, 5, Now));

        Assert.Equal("FULFILLMENT_VERSION_CONFLICT", error.Message);
        Assert.Equal("PENDING", fulfillment.FulfillmentStatus);
        Assert.Equal(6, fulfillment.RowVersion);
        Assert.Null(fulfillment.AssignedToTenantUserId);
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("ALLOCATED")]
    public void StartPicking_EligibleStatus_TransitionsAssignsAndIncrementsVersion(string status)
    {
        var fulfillment = Create(status, 5);

        fulfillment.StartPicking(ActorId, 5, Now);

        Assert.Equal("PICKING", fulfillment.FulfillmentStatus);
        Assert.Equal(ActorId, fulfillment.AssignedToTenantUserId);
        Assert.Equal(ActorId, fulfillment.UpdatedByTenantUserId);
        Assert.Equal(Now, fulfillment.UpdatedAt);
        Assert.Equal(6, fulfillment.RowVersion);
    }

    private static FulfillmentOrder Create(string status, long version)
    {
        var entity = (FulfillmentOrder)Activator.CreateInstance(typeof(FulfillmentOrder), nonPublic: true)!;
        Set(entity, nameof(entity.FulfillmentStatus), status);
        Set(entity, nameof(entity.RowVersion), version);
        return entity;
    }

    private static void Set(FulfillmentOrder target, string property, object value) =>
        typeof(FulfillmentOrder).GetProperty(property)!.SetValue(target, value);
}
