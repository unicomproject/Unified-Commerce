using E_POS.LocalPrintAgent.Models;

namespace E_POS.LocalPrintAgent.Printing;

public interface IDrawerPulseBuilder
{
    byte[] Build(DrawerOpenRequest request);
}
