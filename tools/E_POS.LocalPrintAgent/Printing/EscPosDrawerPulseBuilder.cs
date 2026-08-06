using E_POS.LocalPrintAgent.Models;

namespace E_POS.LocalPrintAgent.Printing;

public sealed class EscPosDrawerPulseBuilder : IDrawerPulseBuilder
{
    public byte[] Build(DrawerOpenRequest request)
    {
        var pin = request.DrawerPort switch
        {
            "drawerPin2" => (byte)0,
            "drawerPin5" => (byte)1,
            _ => throw new ArgumentOutOfRangeException(
                nameof(request), request.DrawerPort, "Unsupported drawer port.")
        };
        return [0x1B, 0x70, pin, ToEscPosUnit(request.PulseOnTime),
            ToEscPosUnit(request.PulseOffTime)];
    }

    private static byte ToEscPosUnit(int milliseconds) =>
        checked((byte)Math.Clamp((milliseconds + 1) / 2, 1, 255));
}
