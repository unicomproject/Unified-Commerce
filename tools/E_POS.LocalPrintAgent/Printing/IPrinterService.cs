using E_POS.LocalPrintAgent.Models;

namespace E_POS.LocalPrintAgent.Printing;

public interface IPrinterService
{
    Task<PrinterHealth> GetHealthAsync(CancellationToken cancellationToken);
    Task<PrintOperationResult> PrintRawAsync(
        string documentName,
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken);
}
