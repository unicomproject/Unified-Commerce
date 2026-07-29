using System.Text;
using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Models;
using E_POS.LocalPrintAgent.Printing;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.LocalPrintAgent.Tests;

public sealed class EscPosReceiptBuilderTests
{
    [Fact]
    public void Build_UsesConfigured80MmWidth_AndAuthoritativeValues()
    {
        var builder = CreateBuilder(autoCut: false);
        var bytes = builder.Build(ValidRequest());
        var text = Encoding.Latin1.GetString(bytes);

        Assert.Contains("Receipt: RCPT-42", text);
        Assert.Contains("2 x LKR 125.50", text);
        Assert.Contains("LKR 251.00", text);
        Assert.Contains(new string('-', 48), text);
        Assert.DoesNotContain(new byte[] { 0x1D, 0x56, 0x00 }, bytes);
    }

    [Fact]
    public void Build_WhenAutoCutEnabled_AppendsCutCommand()
    {
        var bytes = CreateBuilder(autoCut: true).Build(ValidRequest());
        Assert.Equal(new byte[] { 0x1B, 0x64, 0x05 }, bytes[^6..^3]);
        Assert.Equal(new byte[] { 0x1D, 0x56, 0x00 }, bytes[^3..]);
    }

    [Theory]
    [InlineData("80mm")]
    [InlineData("58mm")]
    public void Build_ContentThenFeedThenCut_HasNoPrintableContentAfterCut(string paperWidth)
    {
        var bytes = CreateBuilder(autoCut: true, paperWidth: paperWidth)
            .Build(ValidRequest());
        var footer = Encoding.Latin1.GetBytes("Thank you\n");
        var footerIndex = IndexOf(bytes, footer);
        var feedIndex = IndexOf(bytes, [0x1B, 0x64, 0x05]);
        var cutIndex = IndexOf(bytes, [0x1D, 0x56, 0x00]);

        Assert.True(footerIndex >= 0);
        Assert.True(footerIndex < feedIndex);
        Assert.True(feedIndex < cutIndex);
        Assert.Equal(bytes.Length - 3, cutIndex);
    }

    [Fact]
    public void Build_WhenAutoCutDisabled_StillFeedsButDoesNotCut()
    {
        var bytes = CreateBuilder(autoCut: false).Build(ValidRequest());

        Assert.Equal(new byte[] { 0x1B, 0x64, 0x05 }, bytes[^3..]);
        Assert.DoesNotContain(new byte[] { 0x1D, 0x56, 0x00 }, bytes);
    }

    [Fact]
    public void Build_EmitsCode39BarcodeBeforeFooterFeedAndCut()
    {
        var bytes = CreateBuilder(autoCut: true).Build(ValidRequest());
        var barcodeCommand = new byte[] { 0x1D, 0x6B, 0x04 };
        var barcodeData = Encoding.ASCII.GetBytes("RCPT-42");
        var barcodeIndex = IndexOf(bytes, barcodeCommand);
        var barcodeDataIndex = barcodeIndex + barcodeCommand.Length;
        var footerIndex = IndexOf(bytes, Encoding.Latin1.GetBytes("Thank you\n"));
        var feedIndex = IndexOf(bytes, [0x1B, 0x64, 0x05]);
        var cutIndex = IndexOf(bytes, [0x1D, 0x56, 0x00]);

        Assert.True(barcodeIndex >= 0);
        Assert.Equal(barcodeData, bytes[barcodeDataIndex..(barcodeDataIndex + barcodeData.Length)]);
        Assert.True(barcodeDataIndex < footerIndex);
        Assert.True(footerIndex < feedIndex);
        Assert.True(feedIndex < cutIndex);
    }

    [Fact]
    public void Build_V2SplitCardReceipt_PrintsAuthoritativeTenderAndSafeCardFields()
    {
        var request = ValidRequest() with
        {
            ReceiptContractVersion = "2",
            PaymentMethod = "SPLIT",
            Tenders =
            [
                new("CASH", "Cash", "CASH", 100m, 120m, 20m, "LKR", "PAID"),
                new("CARD", "Card", "CARD", 151m, null, null, "LKR", "PAID",
                    "Terminal", "VISA", "4242", "AUTH-7", "TERM-9")
            ],
            CopyType = "MERCHANT"
        };

        var text = Encoding.Latin1.GetString(CreateBuilder(autoCut: false).Build(request));

        Assert.Contains("MERCHANT COPY", text);
        Assert.Contains("PAYMENT BREAKDOWN", text);
        Assert.Contains("Cash", text);
        Assert.Contains("LKR 100.00", text);
        Assert.Contains("LKR 151.00", text);
        Assert.Contains("Card: VISA 4242", text);
        Assert.Contains("Auth: AUTH-7", text);
        Assert.DoesNotContain("Payment: SPLIT", text);
    }

    [Fact]
    public void Build_V2DiscountAndTaxDetails_DoNotDuplicateAggregateDiscount()
    {
        var request = ValidRequest() with
        {
            DiscountTotal = 10m,
            TaxTotal = 5m,
            Total = 246m,
            DiscountLines = [new("TRANSACTION", null, "Summer offer", "SUMMER", null, 10m)],
            TaxLines = [new("VAT", "VAT", 2m, 250m, 5m)]
        };

        var text = Encoding.Latin1.GetString(CreateBuilder(autoCut: false).Build(request));

        Assert.Equal(1, Count(text, "Summer offer"));
        Assert.Contains("TAX BREAKDOWN", text);
        Assert.Contains("VAT (2%)", text);
    }

    [Fact]
    public void Build_ReprintCustomerCopy_IsClearlyMarked()
    {
        var text = Encoding.Latin1.GetString(CreateBuilder(autoCut: false).Build(
            ValidRequest() with { IsReprint = true, CopyType = "CUSTOMER" }));
        Assert.Contains("REPRINT - CUSTOMER COPY", text);
    }

    [Theory]
    [InlineData("return", "RETURN REPRINT - MERCHANT COPY")]
    [InlineData("exchange", "EXCHANGE REPRINT - MERCHANT COPY")]
    [InlineData("refund", "REFUND REPRINT - MERCHANT COPY")]
    public void Build_NonSaleMerchantReprint_IsClearlyMarked(
        string purpose,
        string expected)
    {
        var bytes = CreateBuilder(autoCut: false).Build(ValidRequest() with
        {
            ReceiptPurpose = purpose,
            IsReprint = true,
            CopyType = "MERCHANT",
            OriginalReceiptReference = "SALE-100"
        });
        var text = Encoding.Latin1.GetString(bytes);

        Assert.Contains(expected, text);
        Assert.DoesNotContain("\u001bp", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ItemDiscount_IsPrintedWithMatchingSaleLine()
    {
        var request = ValidRequest() with
        {
            Items = [new ReceiptLineRequest("Tea", 2, 125.50m, 251m, "line-1")],
            DiscountTotal = 10m,
            DiscountLines = [new("ITEM", "line-1", "Member offer", "MEMBER", null, 10m)]
        };

        var text = Encoding.Latin1.GetString(CreateBuilder(autoCut: false).Build(request));
        var itemIndex = text.IndexOf("Tea", StringComparison.Ordinal);
        var discountIndex = text.IndexOf("Member offer", StringComparison.Ordinal);
        var totalsIndex = text.IndexOf("Subtotal", StringComparison.Ordinal);

        Assert.True(itemIndex >= 0);
        Assert.True(itemIndex < discountIndex);
        Assert.True(discountIndex < totalsIndex);
        Assert.Equal(1, Count(text, "Member offer"));
    }

    [Theory]
    [InlineData("return", "RETURN RECEIPT")]
    [InlineData("exchange", "EXCHANGE RECEIPT")]
    [InlineData("refund", "REFUND RECEIPT")]
    public void Build_CompletionPurpose_PrintsReferencesGroupsAndSettlement(
        string purpose,
        string heading)
    {
        var request = ValidRequest() with
        {
            ReceiptPurpose = purpose,
            OriginalReceiptReference = "SALE-100",
            ReferenceLines = [new("Return", "RET-200")],
            Items =
            [
                new("Returned product", 1, 100m, 90m, "line-1",
                    "Returned items", 10m, 5m, "Damaged"),
                new("Replacement product", 1, 120m, 120m, "line-2",
                    "Replacement items")
            ],
            Subtotal = 210m,
            DiscountTotal = 10m,
            TaxTotal = 5m,
            Total = 205m,
            AmountTendered = null,
            Change = null,
            SettlementLines =
            [
                new("Refunded", 90m, "LKR", "Cash Refund", "SAFE-REF")
            ]
        };

        var text = Encoding.Latin1.GetString(
            CreateBuilder(autoCut: false).Build(request));

        Assert.Contains(heading, text);
        Assert.Contains("Original: SALE-100", text);
        Assert.Contains("RETURNED ITEMS", text);
        Assert.Contains("REPLACEMENT ITEMS", text);
        Assert.Contains("SETTLEMENT", text);
        Assert.Contains("Reference: SAFE-REF", text);
    }

    [Fact]
    public void Build_LargeReceipt_DoesNotTruncateAndStillFeedsThenCuts()
    {
        var items = Enumerable.Range(1, 500)
            .Select(index => new ReceiptLineRequest(
                $"Long product name {index} with wrapping content",
                1, 1m, 1m))
            .ToArray();
        var bytes = CreateBuilder(autoCut: true).Build(ValidRequest() with
        {
            Items = items,
            Subtotal = 500m,
            Total = 500m,
            AmountTendered = 500m,
            Change = 0m
        });
        var text = Encoding.Latin1.GetString(bytes);

        Assert.Contains("Long product name 1", text);
        Assert.Contains("Long product name 500", text);
        Assert.Equal(new byte[] { 0x1B, 0x64, 0x05 }, bytes[^6..^3]);
        Assert.Equal(new byte[] { 0x1D, 0x56, 0x00 }, bytes[^3..]);
    }

    private static int Count(string value, string expected) =>
        value.Split(expected, StringSplitOptions.None).Length - 1;

    private static EscPosReceiptBuilder CreateBuilder(
        bool autoCut,
        string paperWidth = "80mm") =>
        new(Options.Create(new PrintAgentOptions
        {
            PrinterName = "Test Printer",
            PaperWidth = paperWidth,
            AutoCut = autoCut,
            FeedLinesBeforeCut = 5,
            LocalApiKey = "test-key-at-least-16"
        }));

    private static int IndexOf(byte[] source, byte[] pattern)
    {
        for (var index = 0; index <= source.Length - pattern.Length; index++)
        {
            if (source.AsSpan(index, pattern.Length).SequenceEqual(pattern))
                return index;
        }
        return -1;
    }

    internal static ReceiptPrintRequest ValidRequest() => new(
        Guid.NewGuid(), "RCPT-42", DateTimeOffset.Parse("2026-07-27T12:00:00+05:30"),
        "OneVerz POS", "Main Outlet", "Till 01", "Cashier", "LKR",
        [new ReceiptLineRequest("Tea", 2, 125.50m, 251m)],
        251m, 0, 0, 251m, "CASH", 300m, 49m, ["Thank you"],
        BarcodeValue: "RCPT-42");
}
