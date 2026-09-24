using System;
using System.IO;

class Program
{
    static void Main()
    {
        var text = File.ReadAllText("src/E_POS.Application/Modules/Tenant/Reports/Services/ReportCsvGenerator.cs");
        
        text = text.Replace(
            "\"orderId\", \"orderNumber\", \"businessDate\", \"customerName\", \"customerPhone\",\r\n            \"outletId\", \"outletName\", \"tillId\", \"tillName\", \"cashierId\", \"cashierName\",\r\n            \"salesChannelName\", \"paymentMethodName\",\r\n            \"subtotalAmount\", \"discountAmount\", \"taxAmount\", \"totalAmount\", \"refundedAmount\", \"netAmount\",\r\n            \"orderStatus\", \"paymentStatus\", \"completedAt\", \"currencyCode\"",
            "\"orderId\", \"orderNumber\", \"externalReference\", \"businessDate\", \"placedAt\", \"completedAt\", \"salesChannelId\", \"salesChannelName\", \"outletId\", \"outletName\", \"tillId\", \"tillCode\", \"tillName\", \"tillSessionId\", \"cashierId\", \"cashierName\", \"customerId\", \"customerName\", \"lineCount\", \"totalQuantity\", \"currencyCode\", \"subtotalAmount\", \"discountAmount\", \"taxAmount\", \"chargeAmount\", \"roundingAmount\", \"totalAmount\", \"paidAmount\", \"refundedAmount\", \"netAmount\", \"paymentMethodNames\", \"paymentStatus\", \"fulfilmentStatus\", \"orderStatus\""
        );
        
        text = text.Replace(
            "\"salesChannelName\", \"saleCount\", \"salesExcludingTax\", \"taxAmount\", \"salesIncludingTax\", \"netAmount\", \"currencyCode\"",
            "\"departmentName\", \"categoryName\", \"subcategoryName\", \"quantitySold\", \"quantityReturned\", \"grossSalesAmount\", \"discountAmount\", \"refundAmount\", \"netSalesAmount\", \"transactionCount\", \"percentageOfTotal\", \"currencyCode\"" // actually REP-01B is channels, not categories. Wait, there is no channel section in the switch? Ah, there is "categories", but the report says "Sales by Channel". Let me fix ReportCsvGenerator.cs to map "channels" correctly.
        );
        
        File.WriteAllText("src/E_POS.Application/Modules/Tenant/Reports/Services/ReportCsvGenerator.cs", text);
    }
}
