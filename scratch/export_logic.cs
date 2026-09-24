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
        public static byte[] Generate(IReadOnlyList<IReadOnlyDictionary<string, object?>> records)
        {
            if (records == null || records.Count == 0)
            {
                return Encoding.UTF8.GetBytes("No Results");
            }
            
            var sb = new StringBuilder();
            var headers = records[0].Keys.ToList();
            sb.AppendLine(string.Join(",", headers.Select(Escape)));

            foreach (var record in records)
            {
                var row = headers.Select(h => Escape(record.TryGetValue(h, out var val) ? val : null));
                sb.AppendLine(string.Join(",", row));
            }
            
            // utf8 preamble (BOM) is sometimes useful for excel, but standard utf8 is fine.
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string Escape(object? value)
        {
            if (value == null) return "";
            var str = value.ToString();
            if (string.IsNullOrEmpty(str)) return "";

            // formula injection protection
            if (str.StartsWith("=") || str.StartsWith("+") || str.StartsWith("-") || str.StartsWith("@") || str.StartsWith("\t") || str.StartsWith("\r"))
            {
                str = "\t" + str;
            }
            else if (str.Length > 1 && str[0] == '0' && char.IsDigit(str[1]))
            {
                // identifiers with leading zero
                str = "=\"" + str.Replace(""", """") + "\"";
                return str;
            }

            if (str.Contains(",") || str.Contains(""") || str.Contains("\n") || str.Contains("\r"))
            {
                str = """ + str.Replace(""", """") + """;
            }
            return str;
        }
    }
}
