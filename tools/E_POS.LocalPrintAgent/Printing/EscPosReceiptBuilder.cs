using System.Globalization;
using System.Text;
using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Models;
using Microsoft.Extensions.Options;

namespace E_POS.LocalPrintAgent.Printing;

public sealed class EscPosReceiptBuilder(IOptions<PrintAgentOptions> options) : IEscPosReceiptBuilder
{
    private static readonly byte[] Initialize = [0x1B, 0x40];
    private static readonly byte[] AlignLeft = [0x1B, 0x61, 0x00];
    private static readonly byte[] AlignCenter = [0x1B, 0x61, 0x01];
    private static readonly byte[] BoldOn = [0x1B, 0x45, 0x01];
    private static readonly byte[] BoldOff = [0x1B, 0x45, 0x00];
    private static readonly byte[] BarcodeHriBelow = [0x1D, 0x48, 0x02];
    private static readonly byte[] BarcodeHeight = [0x1D, 0x68, 0x50];
    private static readonly byte[] BarcodeWidth = [0x1D, 0x77, 0x02];
    private static readonly byte[] BarcodeCode39 = [0x1D, 0x6B, 0x04];
    private static readonly byte[] FullCut = [0x1D, 0x56, 0x00];
    private readonly PrintAgentOptions _options = options.Value;

    public byte[] Build(ReceiptPrintRequest receipt)
    {
        var width = _options.PaperWidth == "58mm" ? 32 : 48;
        using var output = new MemoryStream();

        Write(output, Initialize);
        Write(output, AlignCenter);
        Write(output, BoldOn);
        Line(output, receipt.MerchantName);
        Write(output, BoldOff);
        OptionalLine(output, receipt.OutletName);
        OptionalLine(output, receipt.TaxInvoiceLabel);
        OptionalLabel(output, "Tax Reg", receipt.TaxRegistrationNumber);
        Write(output, BoldOn);
        Line(output, CopyHeading(receipt));
        Write(output, BoldOff);
        Line(output, new string('-', width));
        Write(output, AlignLeft);
        Line(output, $"Receipt: {receipt.ReceiptNumber}");
        OptionalLabel(output, "Original", receipt.OriginalReceiptReference);
        foreach (var reference in receipt.ReferenceLines ?? [])
            OptionalLabel(output, reference.Label, reference.Value);
        Line(output, $"Date: {receipt.PrintedAt:yyyy-MM-dd HH:mm:ss zzz}");
        OptionalLabel(output, "Till", receipt.TillName);
        OptionalLabel(output, "Cashier", receipt.CashierName);
        Line(output, new string('-', width));

        string? currentGroup = null;
        foreach (var item in receipt.Items!)
        {
            if (!string.IsNullOrWhiteSpace(item.ItemGroup) &&
                !string.Equals(currentGroup, item.ItemGroup, StringComparison.OrdinalIgnoreCase))
            {
                currentGroup = item.ItemGroup;
                Write(output, BoldOn);
                Line(output, currentGroup.Trim().ToUpperInvariant());
                Write(output, BoldOff);
            }
            WrappedLine(output, item.Name, width);
            Columns(
                output,
                $"{FormatQuantity(item.Quantity)} x {Money(receipt.Currency, item.UnitPrice)}",
                Money(receipt.Currency, item.LineTotal),
                width);
            if (item.DiscountAmount is > 0)
                Columns(output, "  Item discount",
                    $"-{Money(receipt.Currency, item.DiscountAmount.Value)}", width);
            if (item.TaxAmount is > 0)
                Columns(output, "  Item tax",
                    Money(receipt.Currency, item.TaxAmount.Value), width);
            OptionalLabel(output, "  Reason", item.Reason);
            foreach (var discount in receipt.DiscountLines?.Where(x =>
                         string.Equals(x.Scope, "ITEM", StringComparison.OrdinalIgnoreCase) &&
                         !string.IsNullOrWhiteSpace(item.SaleLineId) &&
                         string.Equals(x.SaleLineId, item.SaleLineId,
                             StringComparison.OrdinalIgnoreCase)) ??
                     Enumerable.Empty<ReceiptDiscountLineRequest>())
                Columns(output, $"  {DiscountLabel(discount)}",
                    $"-{Money(receipt.Currency, discount.Amount)}", width);
        }

        Line(output, new string('-', width));
        Columns(output, "Subtotal", Money(receipt.Currency, receipt.Subtotal), width);
        if (receipt.DiscountTotal > 0 &&
            !(receipt.DiscountLines?.Any(x =>
                string.Equals(x.Scope, "TRANSACTION", StringComparison.OrdinalIgnoreCase)) ?? false))
            Columns(output, "Discount", $"-{Money(receipt.Currency, receipt.DiscountTotal)}", width);
        foreach (var discount in receipt.DiscountLines?.Where(x =>
                     string.Equals(x.Scope, "TRANSACTION", StringComparison.OrdinalIgnoreCase)) ??
                 Enumerable.Empty<ReceiptDiscountLineRequest>())
            Columns(output, DiscountLabel(discount), $"-{Money(receipt.Currency, discount.Amount)}", width);
        if (receipt.TaxLines is { Count: > 0 })
        {
            Line(output, "TAX BREAKDOWN");
            foreach (var tax in receipt.TaxLines)
            {
                var label = tax.Rate is null
                    ? tax.TaxName
                    : $"{tax.TaxName} ({tax.Rate:0.##}%)";
                Columns(output, label, Money(receipt.Currency, tax.TaxAmount), width);
            }
        }
        if (receipt.TaxTotal > 0)
            Columns(output, "Tax", Money(receipt.Currency, receipt.TaxTotal), width);
        Write(output, BoldOn);
        Columns(output, "TOTAL", Money(receipt.Currency, receipt.Total), width);
        Write(output, BoldOff);
        if (receipt.Tenders is { Count: > 0 })
        {
            Line(output, "PAYMENT BREAKDOWN");
            foreach (var tender in receipt.Tenders)
            {
                Columns(output, tender.MethodName, Money(tender.Currency, tender.Amount), width);
                OptionalLabel(output, "Card", SafeCard(tender));
                OptionalLabel(output, "Auth", tender.AuthorizationReference);
                OptionalLabel(output, "Terminal", tender.TerminalReference);
                if (tender.AmountTendered is not null)
                    Columns(output, "Tendered", Money(tender.Currency, tender.AmountTendered.Value), width);
                if (tender.ChangeAmount is not null)
                    Columns(output, "Change", Money(tender.Currency, tender.ChangeAmount.Value), width);
            }
        }
        else
        {
            Line(output, $"Payment: {receipt.PaymentMethod}");
            if (receipt.AmountTendered is not null)
                Columns(output, "Tendered", Money(receipt.Currency, receipt.AmountTendered.Value), width);
            if (receipt.Change is not null)
                Columns(output, "Change", Money(receipt.Currency, receipt.Change.Value), width);
        }

        if (receipt.SettlementLines is { Count: > 0 })
        {
            Line(output, "SETTLEMENT");
            foreach (var settlement in receipt.SettlementLines)
            {
                Columns(output, settlement.Label,
                    Money(settlement.Currency, settlement.Amount), width);
                OptionalLabel(output, "Method", settlement.Method);
                OptionalLabel(output, "Reference", settlement.SafeReference);
            }
        }

        Barcode(output, receipt.BarcodeValue);

        if (receipt.FooterLines is not null)
        {
            Write(output, AlignCenter);
            foreach (var footer in receipt.FooterLines.Where(x => !string.IsNullOrWhiteSpace(x)))
                WrappedLine(output, footer, width);
        }

        FeedLines(output, _options.FeedLinesBeforeCut);
        if (_options.AutoCut) Write(output, FullCut);
        return output.ToArray();
    }

    private static string CopyHeading(ReceiptPrintRequest receipt)
    {
        var type = string.Equals(receipt.CopyType, "MERCHANT", StringComparison.OrdinalIgnoreCase)
            ? "MERCHANT COPY"
            : "CUSTOMER COPY";
        var purposeCode = (receipt.ReceiptPurpose ??
                           (receipt.IsReprint ? "saleReprint" : "saleOriginal"))
            .Trim().ToLowerInvariant();
        var purpose = purposeCode switch
        {
            "salereprint" => "SALE REPRINT",
            "return" => receipt.IsReprint ? "RETURN REPRINT" : "RETURN RECEIPT",
            "exchange" => receipt.IsReprint ? "EXCHANGE REPRINT" : "EXCHANGE RECEIPT",
            "refund" => receipt.IsReprint ? "REFUND REPRINT" : "REFUND RECEIPT",
            "test" => "PRINTER TEST - NOT A SALE",
            "report" => "REPORT",
            _ => "SALE RECEIPT"
        };
        return $"{purpose} - {type}";
    }

    private static string DiscountLabel(ReceiptDiscountLineRequest discount) =>
        string.IsNullOrWhiteSpace(discount.Name) ? "Discount" : discount.Name.Trim();

    private static string? SafeCard(PaymentTenderLineRequest tender)
    {
        var brand = tender.CardBrand?.Trim();
        var last4 = tender.MaskedCardLast4?.Trim();
        if (string.IsNullOrWhiteSpace(brand) && string.IsNullOrWhiteSpace(last4)) return null;
        return $"{brand} {last4}".Trim();
    }

    private static void Barcode(Stream output, string? value)
    {
        var normalized = NormalizeCode39(value);
        if (normalized.Length == 0) return;

        Write(output, AlignCenter);
        Write(output, BarcodeHriBelow);
        Write(output, BarcodeHeight);
        Write(output, BarcodeWidth);
        Write(output, BarcodeCode39);
        Write(output, Encoding.ASCII.GetBytes(normalized));
        output.WriteByte(0x00);
        output.WriteByte(0x0A);
        Write(output, AlignLeft);
    }

    private static string NormalizeCode39(string? value)
    {
        const string supported = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
        return string.Concat((value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(supported.Contains));
    }

    private static void OptionalLabel(Stream output, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) Line(output, $"{label}: {value.Trim()}");
    }

    private static void OptionalLine(Stream output, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) Line(output, value.Trim());
    }

    private static void Columns(Stream output, string left, string right, int width)
    {
        if (left.Length + right.Length + 1 > width)
        {
            Line(output, left);
            Line(output, right.PadLeft(width));
            return;
        }
        Line(output, left + new string(' ', width - left.Length - right.Length) + right);
    }

    private static void WrappedLine(Stream output, string value, int width)
    {
        var remaining = value.Trim();
        while (remaining.Length > width)
        {
            var breakAt = remaining.LastIndexOf(' ', width);
            if (breakAt < 1) breakAt = width;
            Line(output, remaining[..breakAt]);
            remaining = remaining[breakAt..].TrimStart();
        }
        if (remaining.Length > 0) Line(output, remaining);
    }

    private static void Line(Stream output, string value)
    {
        Write(output, Encode(value));
        output.WriteByte(0x0A);
    }

    private static byte[] Encode(string value) =>
        Encoding.Latin1.GetBytes(string.Concat(value.Select(c => c <= 0xFF ? c : '?')));

    private static void Write(Stream output, ReadOnlySpan<byte> value) => output.Write(value);
    private static void FeedLines(Stream output, int lineCount)
    {
        if (lineCount > 0)
            Write(output, [0x1B, 0x64, checked((byte)lineCount)]);
    }
    private static string Money(string currency, decimal value) =>
        $"{currency.Trim()} {value.ToString("0.00", CultureInfo.InvariantCulture)}";
    private static string FormatQuantity(decimal value) =>
        value == decimal.Truncate(value)
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
}
