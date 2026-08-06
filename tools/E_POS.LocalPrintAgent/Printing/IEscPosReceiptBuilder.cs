using E_POS.LocalPrintAgent.Models;

namespace E_POS.LocalPrintAgent.Printing;

public interface IEscPosReceiptBuilder
{
    byte[] Build(ReceiptPrintRequest receipt);
}
