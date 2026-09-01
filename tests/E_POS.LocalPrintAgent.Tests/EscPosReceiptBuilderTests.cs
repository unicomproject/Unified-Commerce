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
    public void Build_Canonical80Mm_IncludesPreviewParityFields()
    {
        var text = Encoding.Latin1.GetString(CreateBuilder(autoCut: false).Build(ValidRequest()));

        Assert.Contains("OneVerz", text);
        Assert.Contains("POS", text);
        Assert.Contains("Development Main Store", text);
        Assert.Contains("Receipt No", text);
        Assert.Contains("Date & Time", text);
        Assert.Contains("Aug 16, 2026", text);
        Assert.Contains("Customer", text);
        Assert.Contains("Sundhar", text);
        Assert.Contains("Terminal", text);
        Assert.Contains("Front Till 01", text);
        Assert.Contains("Payment", text);
        Assert.Contains("Cash", text);
        Assert.Contains("ITEM", text);
        Assert.Contains("VALUE", text);
        Assert.Contains("RATE", text);
        Assert.Contains("MER-001-SKU", text);
        Assert.Contains("4,500.00", text);
        Assert.Contains("3,375.00", text);
        Assert.Contains("No. of Items", text);
        Assert.Contains("Paid by Cash", text);
        Assert.Contains("Change Due", text);
        Assert.Contains("Thank you for your purchase", text);
        Assert.Contains("Goods once sold can be exchanged", text);
        Assert.Contains(new string('-', 48), text);
        Assert.DoesNotContain("SALE RECEIPT - CUSTOMER COPY", text);
        Assert.DoesNotContain("PAYMENT BREAKDOWN", text);
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
        var footer = Encoding.Latin1.GetBytes("Thank you for your purchase\n");
        var footerIndex = IndexOf(bytes, footer);
        var feedIndex = IndexOf(bytes, [0x1B, 0x64, 0x05]);
        var cutIndex = IndexOf(bytes, [0x1D, 0x56, 0x00]);

        Assert.True(footerIndex >= 0);
        Assert.True(footerIndex < feedIndex);
        Assert.True(feedIndex < cutIndex);
        Assert.Equal(bytes.Length - 3, cutIndex);
    }

    [Fact]
    public void Build_EmitsCode39BarcodeBeforeFeedAndCut()
    {
        var bytes = CreateBuilder(autoCut: true).Build(ValidRequest());
        var barcodeCommand = new byte[] { 0x1D, 0x6B, 0x04 };
        var barcodeData = Encoding.ASCII.GetBytes("RCP-000200");
        var barcodeIndex = IndexOf(bytes, barcodeCommand);
        var barcodeDataIndex = barcodeIndex + barcodeCommand.Length;
        var feedIndex = IndexOf(bytes, [0x1B, 0x64, 0x05]);
        var cutIndex = IndexOf(bytes, [0x1D, 0x56, 0x00]);

        Assert.True(barcodeIndex >= 0);
        Assert.Equal(barcodeData, bytes[barcodeDataIndex..(barcodeDataIndex + barcodeData.Length)]);
        Assert.True(barcodeDataIndex < feedIndex);
        Assert.True(feedIndex < cutIndex);
    }

    [Fact]
    public void Build_Reprint_IsClearlyMarked()
    {
        var text = Encoding.Latin1.GetString(CreateBuilder(autoCut: false).Build(
            ValidRequest() with { IsReprint = true }));
        Assert.Contains("REPRINT", text);
    }

    [Fact]
    public void Build_58Mm_StacksItemFields()
    {
        var text = Encoding.Latin1.GetString(
            CreateBuilder(autoCut: false, paperWidth: "58mm").Build(ValidRequest()));
        Assert.Contains("ITEM", text);
        Assert.Contains("QTY 1  VALUE", text);
        Assert.Contains("RATE", text);
        Assert.Contains(new string('-', 32), text);
    }

    private static EscPosReceiptBuilder CreateBuilder(
        bool autoCut,
        string paperWidth = "80mm") =>
        new(Options.Create(new PrintAgentOptions
        {
            AutoCut = autoCut,
            PaperWidth = paperWidth,
            FeedLinesBeforeCut = 5
        }));

    internal static ReceiptPrintRequest ValidRequest() => new(
        Guid.Parse("11111111-1111-4111-8111-111111111111"),
        "RCP-000200",
        new DateTimeOffset(2026, 8, 16, 10, 57, 12, TimeSpan.Zero),
        "OneVerz",
        "Development Main Store",
        "Front Till 01",
        "Kavin",
        "LKR",
        [
            new ReceiptLineRequest(
                "Team Jersey",
                1,
                4500m,
                3375m,
                Sku: "MER-001-SKU",
                ValueUnitPrice: 4500m,
                RateUnitPrice: 3375m)
        ],
        4500m,
        1125m,
        0m,
        3375m,
        "Cash",
        3400m,
        25m,
        [
            "Thank you for your purchase",
            "Goods once sold can be exchanged with the original receipt."
        ],
        ReceiptContractVersion: "3",
        BarcodeValue: "RCP-000200",
        BrandSubtitle: "POS",
        CustomerName: "Sundhar",
        IssuedAtDisplay: "Aug 16, 2026 | 10:57 AM",
        ItemCount: 1,
        PresentationLayout: "canonical_v1");

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }
}
