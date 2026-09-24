using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Reports.Dtos;

namespace E_POS.Application.Modules.Tenant.Reports.Services
{
    public sealed record ExportJobEntry(ReportExportDto Dto, Guid TenantId, Guid UserId, byte[]? Data);

    public static class CsvGenerator
    {
        private static readonly IReadOnlyList<string> SalesTransactionsColumns = new[]
        {
            "orderId", "orderNumber", "externalReference", "businessDate", "placedAt", "completedAt", "salesChannelId", "salesChannelName", "outletId", "outletName", "tillId", "tillCode", "tillName", "tillSessionId", "cashierId", "cashierName", "customerId", "customerName", "lineCount", "totalQuantity", "currencyCode", "subtotalAmount", "discountAmount", "taxAmount", "chargeAmount", "roundingAmount", "totalAmount", "paidAmount", "refundedAmount", "netAmount", "paymentMethodNames", "paymentStatus", "fulfilmentStatus", "orderStatus"
        };
        
        private static readonly IReadOnlyList<string> CategorySalesColumns = new[]
        {
            "departmentName", "categoryName", "subcategoryName", "quantitySold", "quantityReturned", "grossSalesAmount", "discountAmount", "refundAmount", "netSalesAmount", "transactionCount", "percentageOfTotal", "currencyCode"
        };
        
        private static readonly IReadOnlyList<string> SalesByChannelColumns = new[]
        {
            "salesChannelName", "saleCount", "salesExcludingTax", "taxAmount", "salesIncludingTax", "netAmount", "currencyCode"
        };
        
        private static readonly IReadOnlyList<string> TaxBreakdownColumns = new[]
        {
            "taxClassId", "taxClassName", "taxCode", "taxName", "taxRate", "taxableAmount", "taxAmount", "refundedTaxAmount", "netTaxAmount", "transactionCount", "currencyCode"
        };
        
        private static readonly IReadOnlyList<string> PaymentsByMethodColumns = new[]
        {
            "paymentMethodId", "paymentMethodCode", "paymentMethodName", "paymentType", "transactionCount", "requestedAmount", "tenderedAmount", "paidAmount", "changeAmount", "refundedAmount", "netCollectedAmount", "percentage", "currencyCode"
        };
        
        private static readonly IReadOnlyList<string> PaymentTransactionsColumns = new[]
        {
            "paymentMethodId", "paymentMethodCode", "paymentMethodName", "paymentType", "transactionCount", "requestedAmount", "tenderedAmount", "paidAmount", "changeAmount", "refundedAmount", "netCollectedAmount", "percentage", "currencyCode"
        };
        
        private static readonly IReadOnlyList<string> TillShiftClosingColumns = new[]
        {
            "tillSessionId", "sessionNumber", "businessDate", "outletId", "outletName", "tillId", "tillCode", "tillName", "cashierId", "cashierName", "openedAt", "closedAt", "openingCashAmount", "cashInAmount", "cashDropAmount", "expectedCashAmount", "countedCashAmount", "cashDifference", "grossSalesAmount", "discountAmount", "taxAmount", "netSalesAmount", "refundAmount", "voidCount", "orderCount", "sessionStatus", "approvalStatus", "currencyCode"
        };
        
        private static readonly IReadOnlyList<string> OnlineOrdersColumns = new[]
        {
            "orderId", "orderNumber", "externalReference", "businessDate", "placedAt", "completedAt", "salesChannelId", "salesChannelName", "outletId", "outletName", "tillId", "tillCode", "tillName", "tillSessionId", "cashierId", "cashierName", "customerId", "customerName", "lineCount", "totalQuantity", "currencyCode", "subtotalAmount", "discountAmount", "taxAmount", "chargeAmount", "roundingAmount", "totalAmount", "paidAmount", "refundedAmount", "netAmount", "paymentMethodNames", "paymentStatus", "fulfilmentStatus", "orderStatus"
        };
        
        private static readonly IReadOnlyList<string> OutstandingCollectionsColumns = new[]
        {
            "orderId", "orderNumber", "externalReference", "businessDate", "placedAt", "completedAt", "salesChannelId", "salesChannelName", "outletId", "outletName", "tillId", "tillCode", "tillName", "tillSessionId", "cashierId", "cashierName", "customerId", "customerName", "lineCount", "totalQuantity", "currencyCode", "subtotalAmount", "discountAmount", "taxAmount", "chargeAmount", "roundingAmount", "totalAmount", "paidAmount", "refundedAmount", "netAmount", "paymentMethodNames", "paymentStatus", "fulfilmentStatus", "orderStatus"
        };
        
        private static readonly IReadOnlyList<string> ReturnsRefundsColumns = new[]
        {
            "returnId", "returnNumber", "originalOrderId", "processingOutletId", "processingOutletName", "returnReasonCode", "returnReasonName", "requestedQuantity", "receivedQuantity", "approvedQuantity", "approvedAmount", "refundedAmount", "returnStatus", "refundStatus", "completedAt", "currencyCode"
        };
        
        private static readonly IReadOnlyList<string> ProductSalesColumns = new[]
        {
            "productId", "productName", "productVariantId", "variantName", "sku", "barcode", "brandName", "departmentName", "categoryName", "subcategoryName", "quantitySold", "quantityReturned", "netQuantity", "grossSalesAmount", "discountAmount", "taxAmount", "refundAmount", "netSalesAmount", "transactionCount", "averageSellingPrice", "currencyCode"
        };
        
        private static readonly IReadOnlyList<string> CurrentStockColumns = new[]
        {
            "inventoryBalanceId", "outletId", "outletName", "inventoryLocationId", "inventoryLocationName", "productId", "productName", "productVariantId", "variantName", "sku", "barcode", "productBatchId", "batchNumber", "expiryDate", "onHandQuantity", "reservedQuantity", "damagedQuantity", "quarantineQuantity", "availableQuantity", "reorderPointQuantity", "reorderQuantity", "unitCost", "stockValue", "stockStatus", "expiryStatus", "lastMovementAt", "currencyCode", "rowVersion"
        };
        
        private static readonly IReadOnlyList<string> StockPeriodMovementsColumns = new[]
        {
            "stockMovementId", "movementNumber", "occurredAt", "inventoryBalanceId", "productId", "productName", "productVariantId", "variantName", "sku", "outletId", "outletName", "inventoryLocationId", "inventoryLocationName", "movementType", "quantityBefore", "movedQuantity", "quantityAfter", "unitCost", "totalCost", "referenceType", "referenceNumber", "reasonCode", "reason", "notes", "performedByUserId", "performedByUserName", "currencyCode"
        };

        public static byte[] Generate(ReportExportRequest request, ReportResultDto reportResult, TenantRequestContext context, IDateTimeProvider clock)
        {
            var sb = new StringBuilder();
            
            // Metadata Rows
            sb.AppendLine("Report," + Escape(request.ReportType));
            sb.AppendLine("Section," + Escape(request.Section));
            sb.AppendLine("Period / As-Of," + Escape(reportResult.From.HasValue ? reportResult.From.Value.ToString("yyyy-MM-dd") + " to " + (reportResult.To.HasValue ? reportResult.To.Value.ToString("yyyy-MM-dd") : "") : "All Dates"));
            
            var filterStr = string.Join(";", request.Filters.GetType().GetProperties().Select(p => p.Name + "=" + p.GetValue(request.Filters)).Where(x => !x.EndsWith("=")));
            sb.AppendLine("Applied Filters," + Escape(filterStr));
            sb.AppendLine("Currency," + Escape(reportResult.CurrencyCode));
            sb.AppendLine("Business Timezone," + Escape(reportResult.Timezone));
            sb.AppendLine("Snapshot / As-Of Time," + Escape(reportResult.GeneratedAt.ToString("O")));
            sb.AppendLine("Generated Time," + Escape(clock.UtcNow.ToString("O")));
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
                if (sec == "collections") return OutstandingCollectionsColumns;
                if (sec == "returns") return ReturnsRefundsColumns;
                if (sec == "products") return ProductSalesColumns;
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

        private static string Escape(object? value)
        {
            if (value == null) return "";
            var str = value.ToString();
            if (string.IsNullOrEmpty(str)) return "";

            // formula injection & leading zero protection
            if (str.StartsWith("=") || str.StartsWith("+") || str.StartsWith("-") || str.StartsWith("@") || str.StartsWith("\t") || str.StartsWith("\r") )
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




