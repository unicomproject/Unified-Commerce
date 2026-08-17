using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Models;
using E_POS.LocalPrintAgent.Validation;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.LocalPrintAgent.Tests;

public sealed class DrawerOpenRequestValidatorTests
{
    private readonly DrawerOpenRequestValidator _validator = new(
        Options.Create(new PrintAgentOptions
        {
            PrinterName = "POSPrinter POS80",
            LocalApiKey = "012345678901234567890123"
        }));

    [Fact]
    public void Validate_AcceptsSafeTypedRequest()
    {
        Assert.Empty(_validator.Validate(Valid()));
    }

    [Theory]
    [InlineData("unknown", "drawerPurpose")]
    [InlineData("cashSale", null)]
    public void Validate_RejectsInvalidPurposeOrTiming(
        string purpose, string? expectedField)
    {
        var request = Valid() with
        {
            DrawerPurpose = purpose,
            PulseOnTime = expectedField is null ? 0 : 50
        };
        var errors = _validator.Validate(request);
        Assert.True(errors.ContainsKey(expectedField ?? "pulseOnTime"));
    }

    [Fact]
    public void Validate_RejectsPrinterMismatchAndArbitraryPort()
    {
        var errors = _validator.Validate(Valid() with
        {
            PrinterName = "Other",
            DrawerPort = "raw"
        });
        Assert.Contains("printerName", errors.Keys);
        Assert.Contains("drawerPort", errors.Keys);
    }

    private static DrawerOpenRequest Valid() => new(
        "1", Guid.NewGuid(), Guid.NewGuid(), "cashSale",
        "POSPrinter POS80", "drawerPin2", 50, 100,
        RequestedAt: DateTimeOffset.UtcNow);
}
