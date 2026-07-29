using E_POS.LocalPrintAgent.Models;

namespace E_POS.LocalPrintAgent.Validation;

public sealed class ReceiptPrintRequestValidator
{
    public IReadOnlyDictionary<string, string[]> Validate(ReceiptPrintRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        AddIf(request.RequestId == Guid.Empty, "requestId", "A non-empty request ID is required.");
        AddIf(string.IsNullOrWhiteSpace(request.ReceiptNumber), "receiptNumber", "Receipt number is required.");
        AddIf(request.ReceiptNumber?.Length > 100, "receiptNumber", "Receipt number cannot exceed 100 characters.");
        AddIf(string.IsNullOrWhiteSpace(request.MerchantName), "merchantName", "Merchant name is required.");
        AddIf(request.MerchantName?.Length > 120, "merchantName", "Merchant name cannot exceed 120 characters.");
        AddIf(string.IsNullOrWhiteSpace(request.Currency), "currency", "Currency is required.");
        AddIf(request.Currency?.Length > 8, "currency", "Currency cannot exceed 8 characters.");
        AddIf(string.IsNullOrWhiteSpace(request.PaymentMethod), "paymentMethod", "Payment method is required.");
        AddIf(request.Items is null || request.Items.Count == 0, "items", "At least one receipt line is required.");
        AddIf(request.Subtotal < 0, "subtotal", "Subtotal cannot be negative.");
        AddIf(request.DiscountTotal < 0, "discountTotal", "Discount total cannot be negative.");
        AddIf(request.TaxTotal < 0, "taxTotal", "Tax total cannot be negative.");
        AddIf(request.Total < 0, "total", "Total cannot be negative.");
        AddIf(request.AmountTendered < 0, "amountTendered", "Amount tendered cannot be negative.");
        AddIf(request.Change < 0, "change", "Change cannot be negative.");
        AddIf(request.BarcodeValue?.Length > 80, "barcodeValue", "Barcode value cannot exceed 80 characters.");
        AddIf(request.CopyIndex < 1, "copyIndex", "Copy index must be at least one.");
        AddIf(request.CopyIndex > 5, "copyIndex", "Copy index cannot exceed five.");
        var purpose = (request.ReceiptPurpose ?? "saleOriginal").Trim();
        AddIf(!SupportedPurposes.Contains(purpose),
            "receiptPurpose", "Receipt purpose is not supported.");
        AddIf(
            NonSalePurposes.Contains(purpose) &&
            string.IsNullOrWhiteSpace(request.OriginalReceiptReference),
            "originalReceiptReference",
            "Return, exchange and refund receipts require the original receipt reference.");
        AddIf(request.PrinterConfigurationVersion is < 1,
            "printerConfigurationVersion", "Printer configuration version must be positive.");
        AddIf(request.Items is { Count: > 500 },
            "items", "A receipt cannot contain more than 500 lines.");
        AddIf(request.FooterLines is { Count: > 20 },
            "footerLines", "A receipt cannot contain more than 20 footer lines.");
        AddIf(request.ReferenceLines is { Count: > 20 },
            "referenceLines", "A receipt cannot contain more than 20 reference lines.");
        AddIf(request.SettlementLines is { Count: > 20 },
            "settlementLines", "A receipt cannot contain more than 20 settlement lines.");
        AddIf(request.CopyType is not null &&
              !string.Equals(request.CopyType, "CUSTOMER", StringComparison.OrdinalIgnoreCase) &&
              !string.Equals(request.CopyType, "MERCHANT", StringComparison.OrdinalIgnoreCase),
            "copyType", "Copy type must be CUSTOMER or MERCHANT.");
        AddIf(
            !string.IsNullOrWhiteSpace(request.BarcodeValue) &&
            request.BarcodeValue.Trim().ToUpperInvariant().Any(character =>
                !"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%".Contains(character)),
            "barcodeValue",
            "Barcode value contains characters unsupported by Code 39.");

        if (request.Items is not null)
        {
            for (var index = 0; index < request.Items.Count; index++)
            {
                var item = request.Items[index];
                AddIf(string.IsNullOrWhiteSpace(item.Name), $"items[{index}].name", "Item name is required.");
                AddIf(item.Name?.Length > 160, $"items[{index}].name", "Item name cannot exceed 160 characters.");
                AddIf(item.Quantity <= 0, $"items[{index}].quantity", "Quantity must be greater than zero.");
                AddIf(item.UnitPrice < 0, $"items[{index}].unitPrice", "Unit price cannot be negative.");
                AddIf(item.LineTotal < 0, $"items[{index}].lineTotal", "Line total cannot be negative.");
                AddIf(item.ItemGroup?.Length > 40, $"items[{index}].itemGroup",
                    "Item group cannot exceed 40 characters.");
                AddIf(item.DiscountAmount < 0, $"items[{index}].discountAmount",
                    "Item discount cannot be negative.");
                AddIf(item.TaxAmount < 0, $"items[{index}].taxAmount",
                    "Item tax cannot be negative.");
            }
        }

        ValidateSafeLines(request.ReferenceLines, "referenceLines");
        if (request.SettlementLines is not null)
        {
            for (var index = 0; index < request.SettlementLines.Count; index++)
            {
                var line = request.SettlementLines[index];
                AddIf(string.IsNullOrWhiteSpace(line.Label),
                    $"settlementLines[{index}].label", "Settlement label is required.");
                AddIf(line.Amount < 0, $"settlementLines[{index}].amount",
                    "Settlement amount cannot be negative.");
                AddIf(ContainsLongDigitRun(line.SafeReference),
                    $"settlementLines[{index}].safeReference",
                    "Sensitive card-number-like data is not accepted.");
            }
        }

        if (request.Tenders is { Count: > 0 })
        {
            AddIf(request.Tenders.Sum(x => x.Amount) != request.Total,
                "tenders", "Completed tender amounts must reconcile with the receipt total.");
            for (var index = 0; index < request.Tenders.Count; index++)
            {
                var tender = request.Tenders[index];
                AddIf(string.IsNullOrWhiteSpace(tender.MethodCode),
                    $"tenders[{index}].methodCode", "Tender method code is required.");
                AddIf(tender.Amount < 0, $"tenders[{index}].amount",
                    "Tender amount cannot be negative.");
                AddIf(tender.MaskedCardLast4 is not null &&
                      (tender.MaskedCardLast4.Length != 4 ||
                       tender.MaskedCardLast4.Any(x => !char.IsAsciiDigit(x))),
                    $"tenders[{index}].maskedCardLast4",
                    "Only a four-digit masked card suffix is accepted.");
                foreach (var safeReference in new[]
                         {
                             tender.AuthorizationReference,
                             tender.TerminalReference
                         })
                    AddIf(ContainsLongDigitRun(safeReference),
                        $"tenders[{index}]", "Sensitive card-number-like data is not accepted.");
            }
        }
        if (request.DiscountLines is { Count: > 0 })
            AddIf(request.DiscountLines.Sum(x => x.Amount) != request.DiscountTotal,
                "discountLines", "Discount details must reconcile with the discount total.");
        if (request.TaxLines is { Count: > 0 })
            AddIf(request.TaxLines.Sum(x => x.TaxAmount) != request.TaxTotal,
                "taxLines", "Tax details must reconcile with the tax total.");

        return errors.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        void AddIf(bool condition, string field, string message)
        {
            if (!condition) return;
            if (!errors.TryGetValue(field, out var fieldErrors))
            {
                fieldErrors = [];
                errors[field] = fieldErrors;
            }
            fieldErrors.Add(message);
        }

        void ValidateSafeLines(
            IReadOnlyList<ReceiptReferenceLineRequest>? lines,
            string field)
        {
            if (lines is null) return;
            for (var index = 0; index < lines.Count; index++)
            {
                AddIf(string.IsNullOrWhiteSpace(lines[index].Label),
                    $"{field}[{index}].label", "Reference label is required.");
                AddIf(string.IsNullOrWhiteSpace(lines[index].Value),
                    $"{field}[{index}].value", "Reference value is required.");
                AddIf(ContainsLongDigitRun(lines[index].Value),
                    $"{field}[{index}].value",
                    "Sensitive card-number-like data is not accepted.");
            }
        }
    }

    private static readonly HashSet<string> SupportedPurposes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "saleOriginal", "saleReprint", "return", "exchange", "refund", "test", "report"
        };

    private static readonly HashSet<string> NonSalePurposes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "return", "exchange", "refund"
        };

    private static bool ContainsLongDigitRun(string? value)
    {
        var run = 0;
        foreach (var character in value ?? string.Empty)
        {
            run = char.IsAsciiDigit(character) ? run + 1 : 0;
            if (run >= 12) return true;
        }
        return false;
    }
}
