using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using E_POS.Application.Modules.Tenant.Reports.Dtos;

namespace E_POS.Application.Modules.Tenant.Reports.Services
{
    public sealed record ExportJobEntry(ReportExportDto Dto, Guid TenantId, Guid UserId, byte[]? Data);

        public static class CsvGenerator
    {
        private static readonly IReadOnlyList<string> SalesTransactionsColumns = new[]
        {
            "orderId", "orderNumber", "businessDate", "customerName", "customerPhone",
            "outletId", "outletName", "tillId", "tillName", "cashierId", "cashierName",
            "salesChannelName", "paymentMethodName",
            "subtotalAmount", "discountAmount", "taxAmount", "totalAmount", "refundedAmount", "netAmount",
            "orderStatus", "paymentStatus", "completedAt", "currencyCode"
        };

        public static byte[] Generate(IReadOnlyList<IReadOnlyDictionary<string, object?>> records, string reportType = "", string section = "")
        {
            if (records == null || records.Count == 0)
            {
                var emptyHeaders = GetCanonicalColumns(reportType, section);
                if (emptyHeaders.Count == 0) return Encoding.UTF8.GetBytes("No Results");
                return Encoding.UTF8.GetBytes(string.Join(",", emptyHeaders) + "\r\n");
            }
            
            var sb = new StringBuilder();
            var headers = GetCanonicalColumns(reportType, section);
            if (headers.Count == 0)
            {
                headers = records[0].Keys.ToList();
            }
            
            sb.AppendLine(string.Join(",", headers.Select(Escape)));

            foreach (var record in records)
            {
                var row = headers.Select(h => Escape(record.TryGetValue(h, out var val) ? val : null));
                sb.AppendLine(string.Join(",", row));
            }
            
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static IReadOnlyList<string> GetCanonicalColumns(string reportType, string section)
        {
            if (reportType.Equals("sales", StringComparison.OrdinalIgnoreCase) && section.Equals("transactions", StringComparison.OrdinalIgnoreCase))
            {
                return SalesTransactionsColumns;
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


