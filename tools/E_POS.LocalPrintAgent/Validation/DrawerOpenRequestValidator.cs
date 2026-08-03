using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Models;
using Microsoft.Extensions.Options;

namespace E_POS.LocalPrintAgent.Validation;

public sealed class DrawerOpenRequestValidator(IOptions<PrintAgentOptions> options)
{
    private static readonly HashSet<string> AllowedPurposes =
        ["cashSale", "cashRefund", "splitPaymentCash", "manualNoSale", "hardwareTest"];
    private static readonly HashSet<string> AllowedPorts =
        ["drawerPin2", "drawerPin5"];
    private readonly PrintAgentOptions _options = options.Value;

    public IReadOnlyDictionary<string, string[]> Validate(DrawerOpenRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        Add(request.ApiVersion != PrintAgentOptions.ApiVersion, "apiVersion",
            $"API version {PrintAgentOptions.ApiVersion} is required.");
        Add(request.RequestId == Guid.Empty, "requestId", "Request ID is required.");
        Add(request.DrawerOperationId == Guid.Empty, "drawerOperationId",
            "Drawer operation ID is required.");
        Add(!AllowedPurposes.Contains(request.DrawerPurpose ?? string.Empty),
            "drawerPurpose", "Drawer purpose is not supported.");
        Add(string.IsNullOrWhiteSpace(request.PrinterName), "printerName",
            "Printer name is required.");
        Add(!string.Equals(request.PrinterName?.Trim(), _options.PrinterName,
                StringComparison.OrdinalIgnoreCase),
            "printerName", "Requested printer does not match the configured printer.");
        Add(!AllowedPorts.Contains(request.DrawerPort ?? string.Empty), "drawerPort",
            "Drawer port must be drawerPin2 or drawerPin5.");
        Add(!IsSafe(request.PulseOnTime), "pulseOnTime",
            $"Pulse on time must be between {_options.DrawerMinimumPulseMilliseconds} and {_options.DrawerMaximumPulseMilliseconds} milliseconds.");
        Add(!IsSafe(request.PulseOffTime), "pulseOffTime",
            $"Pulse off time must be between {_options.DrawerMinimumPulseMilliseconds} and {_options.DrawerMaximumPulseMilliseconds} milliseconds.");
        return errors;

        bool IsSafe(int value) =>
            value >= _options.DrawerMinimumPulseMilliseconds &&
            value <= _options.DrawerMaximumPulseMilliseconds;
        void Add(bool condition, string field, string message)
        {
            if (condition) errors[field] = [message];
        }
    }
}
