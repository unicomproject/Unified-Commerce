using E_POS.Application.Modules.Tenant.POSOperations.Services;
using Xunit;

namespace E_POS.UnitTests.POSOperations;

public sealed class PosReceiptServiceTests
{
    [Theory]
    [InlineData("success", "PRINTED")]
    [InlineData("printed", "PRINTED")]
    [InlineData("failed", "FAILED")]
    [InlineData(null, "PRINTED")]
    public void TryNormalizePrintStatus_WithKnownValues_ReturnsExpected(string? status, string expected)
    {
        var success = PosReceiptService.TryNormalizePrintStatus(status, out var normalized);

        Assert.True(success);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("unknown")]
    public void TryNormalizePrintStatus_WithUnknownValues_ReturnsFalse(string? status)
    {
        var success = PosReceiptService.TryNormalizePrintStatus(status, out var normalized);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
    }
}
