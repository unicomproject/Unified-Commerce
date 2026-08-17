using System.Net;
using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Models;
using E_POS.LocalPrintAgent.Security;
using E_POS.LocalPrintAgent.Validation;
using Microsoft.Extensions.Options;
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
    public void Invalid_cidr_fails_closed_at_construction()
    {
        Assert.Throws<FormatException>(() => new NetworkRangeAllowList(["not-a-cidr"]));
        Assert.Throws<FormatException>(() => new NetworkRangeAllowList(["192.168.1.0/99"]));
    }

    [Fact]
    public void Api_key_comparison_accepts_exact_key_only()
    {
        const string key = "a-long-random-local-print-key";
        Assert.True(LocalApiKeyAuthenticator.FixedTimeEquals(key, key));
        Assert.False(LocalApiKeyAuthenticator.FixedTimeEquals(key, key + "x"));
        Assert.False(LocalApiKeyAuthenticator.FixedTimeEquals(key, "different-key-of-same-length!!"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("CHANGE_ME_CHANGE_ME_CHANGE_ME")]
    [InlineData("passwordpasswordpassword")]
    [InlineData("000000000000000000000000")]
    public void Api_key_policy_rejects_empty_placeholder_and_low_entropy(string? key)
    {
        Assert.False(LocalApiKeyPolicy.IsAcceptable(key, out var reason));
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Fact]
    public void Api_key_policy_accepts_store_specific_secret()
    {
        Assert.True(LocalApiKeyPolicy.IsAcceptable(
            "store-A7f9Qx2LmN8pR4vW6yZ1bC3d", out var reason));
        Assert.Equal(string.Empty, reason);
    }

    [Fact]
    public void Loopback_only_allow_list_prefers_loopback_listen_url()
    {
        Assert.True(LocalApiKeyPolicy.IsLoopbackOnlyAllowList(["127.0.0.1/32", "::1/128"]));
        Assert.True(LocalApiKeyPolicy.TryGetPreferredListenUrl(
            9101, false, ["127.0.0.1/32"], out var url));
        Assert.Equal("http://127.0.0.1:9101", url);

        Assert.True(LocalApiKeyPolicy.TryGetPreferredListenUrl(
            9101, false, ["192.168.18.0/24"], out var lanUrl));
        Assert.Equal("http://0.0.0.0:9101", lanUrl);
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
        Assert.Equal("3", PrintAgentOptions.ReceiptContractVersion);
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

    [Fact]
    public void Drawer_validator_rejects_stale_requested_at()
    {
        var validator = new DrawerOpenRequestValidator(Options.Create(new PrintAgentOptions
        {
            PrinterName = "POSPrinter POS80",
            LocalApiKey = "store-A7f9Qx2LmN8pR4vW6yZ1bC3d",
            DrawerRequestMaxAgeSeconds = 120
        }));

        var stale = new DrawerOpenRequest(
            "1", Guid.NewGuid(), Guid.NewGuid(), "cashSale",
            "POSPrinter POS80", "drawerPin2", 50, 100,
            RequestedAt: DateTimeOffset.UtcNow.AddMinutes(-10));
        var errors = validator.Validate(stale);
        Assert.True(errors.ContainsKey("requestedAt"));
    }

    [Fact]
    public void Drawer_validator_rejects_missing_requested_at()
    {
        var validator = new DrawerOpenRequestValidator(Options.Create(new PrintAgentOptions
        {
            PrinterName = "POSPrinter POS80",
            LocalApiKey = "store-A7f9Qx2LmN8pR4vW6yZ1bC3d"
        }));

        var missing = new DrawerOpenRequest(
            "1", Guid.NewGuid(), Guid.NewGuid(), "cashSale",
            "POSPrinter POS80", "drawerPin2", 50, 100);
        Assert.True(validator.Validate(missing).ContainsKey("requestedAt"));
    }

    [Fact]
    public void Drawer_validator_accepts_fresh_requested_at()
    {
        var validator = new DrawerOpenRequestValidator(Options.Create(new PrintAgentOptions
        {
            PrinterName = "POSPrinter POS80",
            LocalApiKey = "store-A7f9Qx2LmN8pR4vW6yZ1bC3d",
            DrawerRequestMaxAgeSeconds = 120
        }));

        var fresh = new DrawerOpenRequest(
            "1", Guid.NewGuid(), Guid.NewGuid(), "cashSale",
            "POSPrinter POS80", "drawerPin2", 50, 100,
            RequestedAt: DateTimeOffset.UtcNow);
        Assert.Empty(validator.Validate(fresh));
    }

    private static ReceiptPrintRequest Request() => new(
        Guid.NewGuid(), "R-1", DateTimeOffset.UtcNow, "Merchant", null, null,
        null, "LKR", [new ReceiptLineRequest("Item", 1, 100, 100)],
        100, 0, 0, 100, "CASH", 100, 0, []);
}
