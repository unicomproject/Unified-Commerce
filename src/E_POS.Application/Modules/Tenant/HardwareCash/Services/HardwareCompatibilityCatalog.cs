namespace E_POS.Application.Modules.Tenant.HardwareCash.Services;

/// <summary>
/// Software adapter inventory. Entries describe protocol support, not physical
/// model certification. Extend this catalog when a tested adapter is introduced.
/// </summary>
public static class HardwareCompatibilityCatalog
{
    public sealed record Profile(string Id, string DeviceType, string ConnectionType,
        string Protocol, string AdapterKey, string[] CapabilityProfile,
        string SupportLevel, bool IsCertified, string CapabilitySource, string Notes);

    public static IReadOnlyList<Profile> Profiles { get; } = Array.AsReadOnly(new[]
    {
        new Profile("escpos-usb", "RECEIPT_PRINTER", "USB", "ESC/POS", "usb_receipt_printer",
            ["receiptPrint"], "UNVERIFIED", false, "DECLARED", "Android USB Host; each physical model requires testing."),
        new Profile("escpos-bluetooth", "RECEIPT_PRINTER", "BLUETOOTH", "ESC/POS", "bluetooth_receipt_printer",
            ["receiptPrint"], "UNVERIFIED", false, "DECLARED", "Android Bluetooth Classic SPP; pairing required."),
        new Profile("escpos-network", "RECEIPT_PRINTER", "NETWORK", "ESC/POS", "network_receipt_printer",
            ["receiptPrint"], "UNVERIFIED", false, "DECLARED", "Native TCP connection; configure host and port."),
        new Profile("hid-usb", "BARCODE_SCANNER", "USB", "HID", "pos_hid_scanner",
            ["barcodeInput"], "UNVERIFIED", false, "DECLARED", "Keyboard-wedge mode with a configured suffix; verify actual input."),
        new Profile("hid-bluetooth", "BARCODE_SCANNER", "BLUETOOTH", "HID", "pos_hid_scanner",
            ["barcodeInput"], "UNVERIFIED", false, "DECLARED", "Pair in the operating system; verify actual input."),
        new Profile("camera-built-in", "BUILT_IN_CAMERA_SCANNER", "BUILT_IN", "CAMERA", "mobile_scanner",
            ["barcodeInput"], "UNVERIFIED", false, "DECLARED", "Native camera access and permission required; verify actual input."),
        new Profile("drawer-printer", "CASH_DRAWER", "USB", "ESC/POS", "cash_drawer_transport",
            ["drawerPulse"], "UNVERIFIED", false, "DECLARED", "Printer-attached only; requires a compatible parent and physical open test."),
        new Profile("terminal-provider", "CARD_READER", "PROVIDER", "PROVIDER_SDK", "unavailable",
            [], "UNSUPPORTED", false, "DECLARED", "No certified provider integration is configured."),
        new Profile("future-scale", "SCALE", "SERIAL", "UNSPECIFIED", "unavailable",
            [], "UNSUPPORTED", false, "DECLARED", "Future device family; no runtime adapter."),
        new Profile("future-display", "CUSTOMER_DISPLAY", "USB", "UNSPECIFIED", "unavailable",
            [], "UNSUPPORTED", false, "DECLARED", "Future device family; no runtime adapter."),
    });

    public static IEnumerable<Profile> Search(string? search = null, string? deviceType = null,
        string? connectionType = null, string? supportLevel = null) => Profiles.Where(p =>
        (string.IsNullOrWhiteSpace(deviceType) || p.DeviceType.Equals(deviceType, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(connectionType) || p.ConnectionType.Equals(connectionType, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(supportLevel) || p.SupportLevel.Equals(supportLevel, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(search) ||
            $"{p.DeviceType} {p.Protocol} {p.AdapterKey} {p.Notes}".Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)));
}
