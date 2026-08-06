using System.Net;
using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Models;
using E_POS.LocalPrintAgent.Security;
using E_POS.LocalPrintAgent.Validation;
using Xunit;

namespace E_POS.LocalPrintAgent.Tests;

public sealed class ProductionHardeningTests
{
    [Fact]
    public void Lan_allow_list_accepts_configured_subnet_and_loopback_only()
    {
        var allowList = new NetworkRangeAllowList(["192.168.18.0/24"]);

        Assert.True(allowList.IsAllowed(IPAddress.Parse("192.168.18.8")));
        Assert.True(allowList.IsAllowed(IPAddress.Loopback));
        Assert.False(allowList.IsAllowed(IPAddress.Parse("192.168.19.8")));
        Assert.False(allowList.IsAllowed(IPAddress.Parse("8.8.8.8")));
    }

    [Fact]
    public void Api_key_comparison_accepts_exact_key_only()
    {
        const string key = "a-long-random-local-print-key";
        Assert.True(LocalApiKeyAuthenticator.FixedTimeEquals(key, key));
        Assert.False(LocalApiKeyAuthenticator.FixedTimeEquals(key, key + "x"));
        Assert.False(LocalApiKeyAuthenticator.FixedTimeEquals(key, "different-key-of-same-length!!"));
    }

    [Fact]
    public void Receipt_contract_keeps_version_fields_optional_for_upgrade_compatibility()
    {
        var request = Request();
        Assert.Empty(new ReceiptPrintRequestValidator().Validate(request));
        Assert.Null(request.ApiVersion);
        Assert.Null(request.ReceiptContractVersion);
    }

    [Fact]
    public void Current_contract_versions_are_explicit()
    {
        Assert.Equal("1", PrintAgentOptions.ApiVersion);
        Assert.Equal("2", PrintAgentOptions.ReceiptContractVersion);
    }

    [Fact]
    public void Current_and_legacy_optional_contracts_are_supported()
    {
        Assert.True(PrintContractCompatibility.IsSupported(Request()));
        Assert.True(PrintContractCompatibility.IsSupported(
            Request() with
            {
                ApiVersion = PrintAgentOptions.ApiVersion,
                ReceiptContractVersion =
                    PrintAgentOptions.ReceiptContractVersion
            }));
    }

    [Fact]
    public void Unsupported_contract_requires_an_agent_or_app_update()
    {
        Assert.False(PrintContractCompatibility.IsSupported(
            Request() with { ReceiptContractVersion = "999" }));
        Assert.False(PrintContractCompatibility.IsSupported(
            Request() with { ApiVersion = "999" }));
    }

    private static ReceiptPrintRequest Request() => new(
        Guid.NewGuid(), "R-1", DateTimeOffset.UtcNow, "Merchant", null, null,
        null, "LKR", [new ReceiptLineRequest("Item", 1, 100, 100)],
        100, 0, 0, 100, "CASH", 100, 0, []);
}
