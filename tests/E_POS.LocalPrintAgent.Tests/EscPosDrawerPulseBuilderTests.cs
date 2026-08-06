using E_POS.LocalPrintAgent.Models;
using E_POS.LocalPrintAgent.Printing;
using Xunit;

namespace E_POS.LocalPrintAgent.Tests;

public sealed class EscPosDrawerPulseBuilderTests
{
    private readonly EscPosDrawerPulseBuilder _builder = new();

    [Theory]
    [InlineData("drawerPin2", 0)]
    [InlineData("drawerPin5", 1)]
    public void Build_EmitsOnlyEscPosDrawerPulse(
        string port, byte expectedPin)
    {
        var bytes = _builder.Build(Request(port, 100, 200));

        Assert.Equal([0x1B, 0x70, expectedPin, 50, 100], bytes);
        Assert.Equal(5, bytes.Length);
    }

    [Fact]
    public void Build_RejectsUnsupportedPort()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _builder.Build(Request("raw", 50, 100)));
    }

    private static DrawerOpenRequest Request(
        string port, int onTime, int offTime) => new(
            "1", Guid.NewGuid(), Guid.NewGuid(), "hardwareTest",
            "POSPrinter POS80", port, onTime, offTime);
}
