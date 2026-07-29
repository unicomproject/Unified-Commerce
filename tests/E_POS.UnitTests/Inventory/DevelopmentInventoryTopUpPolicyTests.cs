using E_POS.InventoryMaintenance;
using Xunit;

namespace E_POS.UnitTests.Inventory;

public sealed class DevelopmentInventoryTopUpPolicyTests
{
    [Theory]
    [InlineData(0, 100)]
    [InlineData(25, 75)]
    [InlineData(100, 0)]
    [InlineData(125, 0)]
    public void CalculateQuantityChange_TopsUpOnlyToMinimum(
        decimal current,
        decimal expectedChange)
    {
        Assert.Equal(
            expectedChange,
            DevelopmentInventoryTopUpPolicy.CalculateQuantityChange(current, 100));
    }

    [Fact]
    public void CalculateQuantityChange_RejectsNegativeCurrentQuantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DevelopmentInventoryTopUpPolicy.CalculateQuantityChange(-1, 100));
    }
}
