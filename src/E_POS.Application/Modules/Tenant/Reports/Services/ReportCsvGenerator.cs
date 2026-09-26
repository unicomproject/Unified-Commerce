using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;

namespace E_POS.Application.Modules.Tenant.Reports.Services
{
    public sealed record ExportJobEntry(ReportExportDto Dto, Guid TenantId, Guid UserId, byte[]? Data, ReportQueryRequest? Filters = null, string? ScopeStamp = null);

    public static class CsvGenerator
    {
        // Column lists use the exact row keys produced by TenantAdminReportsRepository for each section.
        // Internal user identifiers are deliberately not exported; staff are identified by name.
        private static readonly IReadOnlyList<string> SalesTransactionsColumns = new[]
        {
            "rowType", "orderId", "orderNumber", "returnNumber", "originalOrderNumber", "externalReference", "businessDate", "placedAt", "postedAt", "completedAt",
            "salesChannelName", "channelCode", "outletName", "tillCode", "tillName", "cashierName", "customerName", "lineCount", "totalQuantity",
            "currencyCode", "salesExcludingTax", "subtotalAmount", "discountAmount", "taxAmount", "chargeAmount", "roundingAmount", "totalAmount",
            "paidAmount", "refundedAmount", "netAmount", "paymentMethodNames", "paymentStatus", "fulfilmentStatus", "orderStatus"
        };

        private static readonly IReadOnlyList<string> CategorySalesColumns = new[]
        {
            "departmentName", "categoryName", "subcategoryName", "quantitySold", "quantityReturned", "grossSalesAmount", "discountAmount", "refundAmount", "netSalesAmount", "transactionCount", "percentageOfTotal", "currencyCode"
        };

        private static readonly IReadOnlyList<string> SalesByChannelColumns = new[]
        {
            "salesChannelName", "channelCode", "channelType", "saleCount", "salesExcludingTax", "returnAdjustment", "netSalesExcludingTax",
            "saleTax", "returnTax", "taxAmount", "salesIncludingTax", "netAmount", "currencyCode"
        };

        private static readonly IReadOnlyList<string> TaxBreakdownColumns = new[]
        {
            "taxCode", "taxTreatment", "taxName", "taxRate", "taxableAmount", "returnBase", "netBase", "taxAmount", "refundedTaxAmount", "netTaxAmount", "currencyCode"
        };

        private static readonly IReadOnlyList<string> PaymentsByMethodColumns = new[]
        {
            "paymentMethodCode", "paymentMethodName", "paymentType", "transactionCount", "refundCount", "successfulReceipts", "successfulRefunds", "netReceipts",
            "requestedAmount", "tenderedAmount", "changeAmount", "percentage", "currencyCode"
        };

        private static readonly IReadOnlyList<string> PaymentTransactionsColumns = new[]
        {
            "eventType", "eventReference", "paymentId", "refundId", "orderNumber", "eventAt", "outletName", "tillId", "paymentMethodName", "provider",
            "maskedReference", "requestedAmount", "tenderedAmount", "changeAmount", "paidAmount", "refundedAmount", "signedAmount", "paymentStatus", "outcome", "currencyCode"
        };

        private static readonly IReadOnlyList<string> TillShiftClosingColumns = new[]
        {
            "sessionNumber", "businessDate", "outletName", "tillCode", "tillName", "cashierName", "closedByName", "openedAt", "closedAt",
            "openingFloat", "cashReceipts", "cashRefunds", "otherCashIn", "otherCashOut", "expectedCashAmount", "countedCashAmount", "cashDifference",
            "varianceReason", "sessionStatus", "reviewStatus", "reviewedByName", "closeSnapshotAt", "requiresCorrectionReview",
            "revisedExpectedCashAmount", "revisedCashDifference", "correctionDelta",
            "grossSalesAmount", "discountAmount", "taxAmount", "netSalesAmount", "refundAmount", "voidCount", "orderCount", "currencyCode"
        };

        private static readonly IReadOnlyList<string> OnlineOrdersColumns = new[]
        {
            "orderNumber", "externalReference", "placedAt", "collectionOutletName", "customerReference", "itemCount", "totalAmount", "paidAmount",
            "outstandingAmount", "paymentStatus", "fulfilmentStatus", "orderStatus", "scheduledCollectionAt", "collectedAt", "collectedByName", "cancelledAt", "currencyCode"
        };

        private static readonly IReadOnlyList<string> ReturnsRefundsColumns = new[]
        {
            "returnNumber", "originalOrderNumber", "returnPostedAt", "processingOutletName", "productName", "variantName", "sku", "unit", "quantity",
            "returnReasonCode", "returnReasonName", "returnValueExcludingTax", "returnTax", "returnValueIncludingTax", "stockDisposition", "restockable",
            "returnStatus", "refundNumber", "refundDate", "refundMethod", "refundAmount", "refundStatus", "refundOutcome", "processedByName", "currencyCode"
        };

        private static readonly IReadOnlyList<string> ProductSalesColumns = new[]
        {
            "productName", "variantName", "sku", "barcode", "unit", "quantitySold", "quantityReturned", "netQuantity",
            "salesValueExTax", "returnValueExTax", "netValueExTax", "currencyCode"
        };

        private static readonly IReadOnlyList<string> CurrentStockColumns = new[]
        {
            "outletName", "inventoryLocationName", "productName", "variantName", "sku", "batchNumber", "expiryDate", "onHandQuantity", "reservedQuantity",
            "damagedQuantity", "quarantineQuantity", "availableQuantity", "reorderPointQuantity", "unitCost", "stockValue", "stockStatus", "expiryStatus", "lastMovementAt", "currencyCode"
        };

        private static readonly IReadOnlyList<string> StockPeriodMovementsColumns = new[]
        {
            "movementNumber", "movementAt", "productName", "variantName", "sku", "unit", "outletName", "inventoryLocationName", "batchNumber", "movementType",
            "reconciliationBucket", "quantityBefore", "quantityChange", "quantityAfter", "unitCost", "totalCost", "referenceType", "referenceNumber",
            "sourceOutletId", "destinationOutletId", "reasonCode", "reason", "performedByUserName"
        };

        private static readonly IReadOnlyList<string> DailySalesColumns = new[]
        {
            "businessDate", "grossSalesAmount", "discountAmount", "refundAmount", "taxAmount", "netSalesAmount", "totalCollectedAmount", "transactionCount", "averageOrderValue", "currencyCode"
        };

        public static byte[] Generate(ReportExportRequest request, ReportResultDto reportResult, TenantRequestContext context, IDateTimeProvider clock)
        {
            var sb = new StringBuilder();

            // Metadata rows: the file is self-describing and ties back to the same snapshot/filters as the screen.
            sb.AppendLine("Report," + Escape(reportResult.ReportName ?? request.ReportType));
            sb.AppendLine("Report ID," + Escape(reportResult.ReportId ?? request.Section));
            sb.AppendLine("Section," + Escape(request.Section));
            sb.AppendLine("Period / As-Of," + Escape(reportResult.From.HasValue || reportResult.To.HasValue
                ? $"{reportResult.From:yyyy-MM-dd} to {reportResult.To:yyyy-MM-dd}"
                : "All Dates / Current"));
            var filterStr = string.Join(";", request.Filters.GetType().GetProperties()
                .Where(p => p.Name is not ("Page" or "PageSize" or "SnapshotId"))
                .Select(p => p.Name + "=" + p.GetValue(request.Filters)).Where(x => !x.EndsWith("=")));
            sb.AppendLine("Applied Filters," + Escape(filterStr));
            sb.AppendLine("Currency," + Escape(reportResult.CurrencyCode));
            sb.AppendLine("Business Timezone," + Escape(reportResult.Timezone));
            sb.AppendLine("Snapshot ID," + Escape(reportResult.SnapshotId));
            sb.AppendLine("Snapshot / As-Of Time," + Escape((reportResult.AsOf ?? reportResult.GeneratedAt).ToString("O")));
            sb.AppendLine("Completeness," + Escape(reportResult.Completeness));
            sb.AppendLine("Known Pending Sync Count," + Escape(reportResult.KnownPendingSyncCount));
            sb.AppendLine("Row Count," + Escape(reportResult.Records?.Count ?? 0));
            sb.AppendLine("Generated Time," + Escape(clock.UtcNow.ToString("O")));
            if (reportResult.Summary is not null)
            {
                foreach (var (key, value) in reportResult.Summary.Where(x => x.Value is null || IsScalar(x.Value)))
                    sb.AppendLine("Summary " + Escape(key) + "," + Escape(value));
            }
            sb.AppendLine();

            var records = reportResult.Records;
            var headers = GetCanonicalColumns(request.ReportType, request.Section);

            if (headers.Count == 0 && records != null && records.Count > 0)
            {
                headers = records[0].Keys.ToList();
            }

            if (headers.Count == 0)
            {
                throw new ApplicationException("Unsupported report section for CSV export: " + request.ReportType + "/" + request.Section);
            }

            sb.AppendLine(string.Join(",", headers.Select(Escape)));

            if (records != null && records.Count > 0)
            {
                foreach (var record in records)
                {
                    var row = headers.Select(h => Escape(record.TryGetValue(h, out var val) ? val : null));
                    sb.AppendLine(string.Join(",", row));
                }
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static byte[] Generate(IReadOnlyList<IReadOnlyDictionary<string, object?>> records, string reportType = "", string section = "")
        {
            var sb = new StringBuilder();
            var headers = GetCanonicalColumns(reportType, section);
            if (headers.Count == 0 && records != null && records.Count > 0)
            {
                headers = records[0].Keys.ToList();
            }
            if (headers.Count > 0)
            {
                sb.AppendLine(string.Join(",", headers.Select(Escape)));
            }
            if (records != null)
            {
                foreach (var record in records)
                {
                    var row = headers.Count > 0 ? headers.Select(h => Escape(record.TryGetValue(h, out var val) ? val : null)) : record.Values.Select(Escape);
                    sb.AppendLine(string.Join(",", row));
                }
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static IReadOnlyList<string> GetCanonicalColumns(string reportType, string section)
        {
            var type = reportType.Trim().ToLowerInvariant();
            var sec = section.Trim().ToLowerInvariant();

            if (type == "sales")
            {
                if (sec == "transactions") return SalesTransactionsColumns;
                if (sec == "channels") return SalesByChannelColumns;
                if (sec == "categories") return CategorySalesColumns;
                if (sec == "tax") return TaxBreakdownColumns;
                if (sec == "payments") return PaymentsByMethodColumns;
                if (sec == "payment-transactions") return PaymentTransactionsColumns;
                if (sec == "online") return OnlineOrdersColumns;
                if (sec == "collections") return OnlineOrdersColumns;
                if (sec == "returns") return ReturnsRefundsColumns;
                if (sec == "products") return ProductSalesColumns;
                if (sec == "daily") return DailySalesColumns;
            }
            if (type == "stock")
            {
                if (sec == "current") return CurrentStockColumns;
                if (sec == "movements") return StockPeriodMovementsColumns;
            }
            if (type == "outlets")
            {
                if (sec == "tills") return TillShiftClosingColumns;
            }

            return Array.Empty<string>();
        }

        private static bool IsScalar(object value) => value is string || value is not IEnumerable;

        private static string Escape(object? value)
        {
            if (value == null) return "";
            // Numbers and dates are written invariantly and never prefixed, so negative amounts stay numeric.
            var isNumeric = value is decimal or double or float or int or long or short;
            var str = value switch
            {
                IFormattable formattable when isNumeric => formattable.ToString(null, CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
                DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
            if (string.IsNullOrEmpty(str)) return "";

            // Spreadsheet formula injection: text starting with a formula trigger is neutralised with a tab.
            if (!isNumeric && (str.StartsWith("=") || str.StartsWith("+") || str.StartsWith("-") || str.StartsWith("@") || str.StartsWith("\t") || str.StartsWith("\r")))
            {
                str = "\t" + str;
            }

            if (str.Contains(",") || str.Contains("\"") || str.Contains("\n") || str.Contains("\r"))
            {
                str = "\"" + str.Replace("\"", "\"\"") + "\"";
            }
            return str;
        }
    }
}
