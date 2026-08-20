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
        OptionalLine(output, receipt.BrandSubtitle);
        OptionalLine(output, receipt.OutletName);
        OptionalLine(output, receipt.OutletLocation);
        OptionalLine(output, receipt.TaxInvoiceLabel);
        OptionalLabel(output, "Tax Reg", receipt.TaxRegistrationNumber);
        if (receipt.IsReprint)
        {
            Write(output, BoldOn);
            Line(output, "REPRINT");
            Write(output, BoldOff);
        }
        Line(output, new string('-', width));
        Write(output, AlignLeft);

        LabelValue(output, "Receipt No", receipt.ReceiptNumber, width);
        LabelValue(
            output,
            "Date & Time",
            string.IsNullOrWhiteSpace(receipt.IssuedAtDisplay)
                ? receipt.PrintedAt.ToString("MMM d, yyyy | h:mm tt", CultureInfo.InvariantCulture)
                : receipt.IssuedAtDisplay!,
            width);
        OptionalLabel(output, "Cashier", receipt.CashierName);
        LabelValue(
            output,
            "Customer",
            string.IsNullOrWhiteSpace(receipt.CustomerName) ? "Walk-in Customer" : receipt.CustomerName!,
            width);
        OptionalLabel(output, "Terminal", receipt.TillName);
        LabelValue(output, "Payment", receipt.PaymentMethod, width);
        Line(output, new string('-', width));

        if (width >= 48)
            WriteItems80(output, receipt, width);
        else
            WriteItems58(output, receipt, width);

        Line(output, new string('-', width));
        var itemCount = receipt.ItemCount ??
                        (int)Math.Round(receipt.Items?.Sum(x => x.Quantity) ?? 0);
        Columns(output, "No. of Items", itemCount.ToString(CultureInfo.InvariantCulture), width);
        Columns(output, "Subtotal", Money(receipt.Currency, receipt.Subtotal), width);
        if (receipt.DiscountTotal > 0)
            Columns(output, "Discount", $"-{Money(receipt.Currency, receipt.DiscountTotal)}", width);
        if (receipt.TaxTotal > 0)
            Columns(output, "Tax", Money(receipt.Currency, receipt.TaxTotal), width);
        Write(output, BoldOn);
        Columns(output, "TOTAL", Money(receipt.Currency, receipt.Total), width);
        Write(output, BoldOff);
        Columns(
            output,
            $"Paid by {receipt.PaymentMethod}",
            Money(receipt.Currency, receipt.AmountTendered ?? receipt.Total),
            width);
        Columns(output, "Change Due", Money(receipt.Currency, receipt.Change ?? 0m), width);
        Line(output, new string('-', width));

        Write(output, AlignCenter);
        var footers = receipt.FooterLines?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        if (footers.Count == 0)
        {
            WrappedLine(output, "Thank you for your purchase", width);
            WrappedLine(output, "Goods once sold can be exchanged with the original receipt.", width);
        }
        else
        {
            foreach (var footer in footers)
                WrappedLine(output, footer, width);
        }
        Write(output, AlignLeft);

        Barcode(output, receipt.BarcodeValue);

        FeedLines(output, _options.FeedLinesBeforeCut);
        if (_options.AutoCut) Write(output, FullCut);
        return output.ToArray();
    }

    private static void WriteItems80(Stream output, ReceiptPrintRequest receipt, int width)
    {
        Line(output, PadColumns80("ITEM", "QTY", "VALUE", "RATE"));
        foreach (var item in receipt.Items ?? [])
        {
            WrappedLine(output, item.Name, width);
            if (!string.IsNullOrWhiteSpace(item.Sku))
                WrappedLine(output, item.Sku!, width);
            var value = item.ValueUnitPrice ?? item.UnitPrice;
            var rate = item.RateUnitPrice ??
                       (item.Quantity == 0 ? item.UnitPrice : Math.Round(item.LineTotal / item.Quantity, MidpointRounding.AwayFromZero));
            Line(output, PadColumns80(
                "",
                FormatQuantity(item.Quantity),
                FormatAmountOnly(value),
                FormatAmountOnly(rate)));
        }
    }

    private static void WriteItems58(Stream output, ReceiptPrintRequest receipt, int width)
    {
        foreach (var item in receipt.Items ?? [])
        {
            WrappedLine(output, "ITEM", width);
            WrappedLine(output, item.Name, width);
            if (!string.IsNullOrWhiteSpace(item.Sku))
                WrappedLine(output, item.Sku!, width);
            var value = item.ValueUnitPrice ?? item.UnitPrice;
            var rate = item.RateUnitPrice ??
                       (item.Quantity == 0 ? item.UnitPrice : Math.Round(item.LineTotal / item.Quantity, MidpointRounding.AwayFromZero));
            Line(output, $"QTY {FormatQuantity(item.Quantity)}  VALUE {FormatAmountOnly(value)}");
            Line(output, $"RATE {FormatAmountOnly(rate)}");
        }
    }

    private static string PadColumns80(string item, string qty, string value, string rate)
    {
        static string Fit(string text, int size, bool right = false)
        {
            if (text.Length > size) return text[..size];
            return right ? text.PadLeft(size) : text.PadRight(size);
        }

        return $"{Fit(item, 18)}{Fit(qty, 4, true)} {Fit(value, 12, true)} {Fit(rate, 12, true)}";
    }

    private static void LabelValue(Stream output, string label, string value, int width)
    {
        var left = $"{label}:";
        if (left.Length + 1 + value.Length <= width)
            Columns(output, left, value, width);
        else
        {
            Line(output, left);
            WrappedLine(output, value, width);
        }
    }

    private static string FormatQuantity(decimal qty) =>
        qty == decimal.Truncate(qty)
            ? ((int)qty).ToString(CultureInfo.InvariantCulture)
            : qty.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Money(string currency, decimal value)
    {
        var code = string.IsNullOrWhiteSpace(currency) ? "" : $"{currency.Trim()} ";
        return $"{code}{FormatAmountOnly(value)}";
    }

    private static string FormatAmountOnly(decimal value) =>
        value.ToString("#,0.00", CultureInfo.InvariantCulture);

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
        Write(output, [0x00]);
        Write(output, [0x0A]);
        Write(output, AlignLeft);
    }

    private static string NormalizeCode39(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        const string supported = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
        var chars = value.Trim().ToUpperInvariant()
            .Where(c => supported.Contains(c))
            .ToArray();
        return new string(chars);
    }

    private static void FeedLines(Stream output, int feedLines)
    {
        var count = Math.Clamp(feedLines, 0, 20);
        if (count > 0) Write(output, [0x1B, 0x64, (byte)count]);
    }

    private static void OptionalLine(Stream output, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) Line(output, value.Trim());
    }

    private static void OptionalLabel(Stream output, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) Line(output, $"{label}: {value.Trim()}");
    }

    private static void Columns(Stream output, string left, string right, int width)
    {
        var gap = width - left.Length - right.Length;
        if (gap <= 0)
        {
            Line(output, left);
            Line(output, right.PadLeft(width));
            return;
        }
        Line(output, left + new string(' ', gap) + right);
    }

    private static void WrappedLine(Stream output, string text, int width)
    {
        var cleaned = text.Trim();
        if (cleaned.Length == 0) return;
        for (var i = 0; i < cleaned.Length; i += width)
        {
            var len = Math.Min(width, cleaned.Length - i);
            Line(output, cleaned.Substring(i, len));
        }
    }

    private static void Line(Stream output, string text)
    {
        Write(output, Encoding.Latin1.GetBytes(Sanitize(text)));
        Write(output, [0x0A]);
    }

    private static string Sanitize(string text)
    {
        var buffer = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value <= 0xFF) buffer.Append(rune.ToString());
            else buffer.Append('?');
        }
        return buffer.ToString();
    }

    private static void Write(Stream output, ReadOnlySpan<byte> bytes) => output.Write(bytes);
}
