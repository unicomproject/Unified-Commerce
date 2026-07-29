using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Models;

namespace E_POS.LocalPrintAgent.Validation;

public static class PrintContractCompatibility
{
    public static bool IsSupported(ReceiptPrintRequest request) =>
        (request.ApiVersion is null ||
         request.ApiVersion == PrintAgentOptions.ApiVersion) &&
        (request.ReceiptContractVersion is null ||
         request.ReceiptContractVersion == "1" ||
         request.ReceiptContractVersion ==
         PrintAgentOptions.ReceiptContractVersion);
}
