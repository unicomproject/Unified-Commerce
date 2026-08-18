using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Domain.Modules.Tenant.POSOperations.Constants;

namespace E_POS.Application.Modules.Tenant.HardwareCash.Services;

public sealed class PosHardwareService : IPosHardwareService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> HardwareTypes =
        new(StringComparer.OrdinalIgnoreCase)
        { "receiptPrinter", "barcodeScanner", "cashDrawer", "cardTerminal" };
    private static readonly HashSet<string> PrinterPurposes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "customerReceipt", "merchantReceipt", "returnReceipt",
            "exchangeReceipt", "refundReceipt", "testReceipt", "reportReceipt"
        };

    private static readonly HashSet<string> FinalStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        { "Passed", "Failed", "Unknown", "Cancelled", "Expired", "Blocked" };

    private static readonly HashSet<string> ResultCategories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "agent_reachable", "printer_ready", "test_print_submitted",
            "test_print_failed", "scanner_input_detected", "scanner_not_detected",
            "scanner_disabled", "scanner_not_configured", "hid_input_not_detected",
            "scan_timeout", "incomplete_scan", "invalid_length",
            "unsupported_characters", "camera_permission_denied",
            "camera_permission_permanently_denied", "camera_initialization_failed",
            "camera_unavailable", "barcode_not_recognized",
            "duplicate_event_suppressed", "product_not_found", "product_inactive",
            "product_lookup_failed", "backend_unavailable",
            "audit_submission_failed", "device_not_trusted", "permission_denied",
            "configuration_version_conflict",
            "drawer_opened", "drawer_did_not_open", "drawer_unknown",
            "drawer_disabled", "drawer_not_configured",
            "drawer_printer_not_found", "drawer_printer_offline",
            "drawer_port_invalid", "drawer_pulse_timing_invalid",
            "spooler_rejected", "spooler_timeout",
            "card_terminal_not_configured",
            "unauthorized", "timeout", "configuration_invalid",
            "hardware_unavailable", "unknown"
        };
    private static readonly HashSet<string> ScannerTestTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "hidInput", "cameraInitialization", "cameraScan",
            "printedBarcodeRecognition", "rapidScan"
        };

    private readonly IPosHardwareRepository _repository;
    private readonly IDateTimeProvider _clock;

    public PosHardwareService(IPosHardwareRepository repository, IDateTimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<ApplicationResult<IReadOnlyList<PosHardwareConfigurationDto>>> GetConfigurationsAsync(
        TenantRequestContext context, Guid posDeviceId, CancellationToken cancellationToken)
    {
        if (!context.HasPermission(PosPermissions.Hardware.Settings))
            return Failure<IReadOnlyList<PosHardwareConfigurationDto>>("pos_hardware.permission_denied", "You do not have permission to view hardware settings.");
        if (posDeviceId == Guid.Empty)
            return Failure<IReadOnlyList<PosHardwareConfigurationDto>>("pos_hardware.invalid_device", "Activated POS device is required.");

        return ApplicationResult<IReadOnlyList<PosHardwareConfigurationDto>>.Success(
            await _repository.GetConfigurationsAsync(context.TenantId, posDeviceId, cancellationToken));
    }

    public async Task<ApplicationResult<PosHardwareConfigurationDto>> SaveConfigurationAsync(
        TenantRequestContext context,
        SavePosHardwareConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(PosPermissions.Hardware.Settings))
            return Failure<PosHardwareConfigurationDto>("pos_hardware.permission_denied", "You do not have permission to change hardware settings.");

        var validation = ValidateConfiguration(request);
        if (validation is not null)
            return Failure<PosHardwareConfigurationDto>(validation.Value.Code, validation.Value.Message);

        var safeJson = JsonSerializer.Serialize(
            SelectSafeSettings(request), JsonOptions);
        var result = await _repository.SaveConfigurationAsync(
            context.TenantId, context.UserId, request, safeJson, _clock.UtcNow, cancellationToken);
        return result.Configuration is null
            ? Failure<PosHardwareConfigurationDto>(
                result.ErrorCode ?? "pos_hardware.save_failed",
                ErrorMessage(result.ErrorCode))
            : ApplicationResult<PosHardwareConfigurationDto>.Success(result.Configuration);
    }

    public async Task<ApplicationResult<HardwareTestOperationDto>> CreateTestAsync(
        TenantRequestContext context,
        CreateHardwareTestRequest request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(PosPermissions.Hardware.Settings))
            return Failure<HardwareTestOperationDto>("pos_hardware.permission_denied", "You do not have permission to test hardware.");
        if (request.RequestId == Guid.Empty || request.PosDeviceId == Guid.Empty ||
            !HardwareTypes.Contains(request.HardwareType) || string.IsNullOrWhiteSpace(request.TestType))
            return Failure<HardwareTestOperationDto>("pos_hardware.invalid_test", "Hardware test request is invalid.");
        if ((request.HardwareType.Equals("receiptPrinter", StringComparison.OrdinalIgnoreCase) ||
             request.HardwareType.Equals("barcodeScanner", StringComparison.OrdinalIgnoreCase) ||
             request.HardwareType.Equals("cashDrawer", StringComparison.OrdinalIgnoreCase)) &&
            request.HardwareConfigurationId is null)
            return Failure<HardwareTestOperationDto>(
                "pos_hardware.configuration_not_found",
                "Save an authoritative hardware configuration before running this test.");
        if (request.HardwareType.Equals("barcodeScanner", StringComparison.OrdinalIgnoreCase) &&
            !ScannerTestTypes.Contains(request.TestType))
            return Failure<HardwareTestOperationDto>(
                "pos_hardware.invalid_test",
                "Scanner test type is unsupported.");

        var canonical = JsonSerializer.Serialize(request, JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var result = await _repository.CreateTestAsync(
            context.TenantId, context.UserId, request, hash, _clock.UtcNow, cancellationToken);
        return result.Operation is null
            ? Failure<HardwareTestOperationDto>(
                result.ErrorCode ?? "pos_hardware.test_create_failed",
                ErrorMessage(result.ErrorCode))
            : ApplicationResult<HardwareTestOperationDto>.Success(result.Operation);
    }

    public async Task<ApplicationResult<HardwareTestOperationDto>> CompleteTestAsync(
        TenantRequestContext context,
        Guid testId,
        CompleteHardwareTestRequest request,
        CancellationToken cancellationToken)
    {
        if (!context.HasPermission(PosPermissions.Hardware.Settings))
            return Failure<HardwareTestOperationDto>("pos_hardware.permission_denied", "You do not have permission to submit hardware test results.");
        if (testId == Guid.Empty || !FinalStatuses.Contains(request.Status) ||
            !ResultCategories.Contains(request.ResultCategory) ||
            (request.SafeMessage?.Length ?? 0) > 500 ||
            (request.AutomaticResult?.Length ?? 0) > 80 ||
            !ValidScannerEvidence(request.ScannerEvidence))
            return Failure<HardwareTestOperationDto>("pos_hardware.invalid_test_result", "Hardware test result is invalid.");

        var safePayload = request.ScannerEvidence is null &&
                          request.DetectedAt is null &&
                          request.AutomaticResult is null
            ? null
            : JsonSerializer.Serialize(new
            {
                request.DetectedAt,
                request.AutomaticResult,
                scannerEvidence = request.ScannerEvidence
            }, JsonOptions);
        var result = await _repository.CompleteTestAsync(
            context.TenantId, context.UserId, testId, request, safePayload,
            _clock.UtcNow, cancellationToken);
        return result.Operation is null
            ? Failure<HardwareTestOperationDto>(
                result.ErrorCode ?? "pos_hardware.test_result_failed",
                ErrorMessage(result.ErrorCode))
            : ApplicationResult<HardwareTestOperationDto>.Success(result.Operation);
    }

    public async Task<ApplicationResult<IReadOnlyList<HardwareTestOperationDto>>> GetTestHistoryAsync(
        TenantRequestContext context, Guid posDeviceId, int take, CancellationToken cancellationToken)
    {
        if (!context.HasPermission(PosPermissions.Hardware.Settings))
            return Failure<IReadOnlyList<HardwareTestOperationDto>>("pos_hardware.permission_denied", "You do not have permission to view hardware test history.");
        if (posDeviceId == Guid.Empty || take is < 1 or > 100)
            return Failure<IReadOnlyList<HardwareTestOperationDto>>("pos_hardware.invalid_history", "Hardware test history request is invalid.");
        return ApplicationResult<IReadOnlyList<HardwareTestOperationDto>>.Success(
            await _repository.GetTestHistoryAsync(context.TenantId, posDeviceId, take, cancellationToken));
    }

    private static (string Code, string Message)? ValidateConfiguration(
        SavePosHardwareConfigurationRequest request)
    {
        if (request.PosDeviceId == Guid.Empty || request.OutletId == Guid.Empty ||
            !HardwareTypes.Contains(request.HardwareType) ||
            string.IsNullOrWhiteSpace(request.DisplayName) ||
            request.DisplayName.Trim().Length > 150 ||
            request.ExpectedVersion < 0)
            return ("pos_hardware.invalid_configuration", "Hardware configuration is invalid.");

        if (request.HardwareType.Equals("receiptPrinter", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.TransportType.Equals("localPrintAgent", StringComparison.OrdinalIgnoreCase) ||
                request.ReceiptPrinter is null)
                return ("pos_hardware.unsupported_transport", "Only the Local Print Agent receipt-printer transport is supported.");
            var printer = request.ReceiptPrinter;
            if (!Uri.TryCreate(printer.AgentBaseUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https") ||
                uri.IsLoopback ||
                string.IsNullOrWhiteSpace(printer.PrinterName) ||
                printer.PaperWidth is not ("58mm" or "80mm") ||
                printer.RequestTimeout is < 1000 or > 30000 ||
                printer.FeedBeforeCut is < 0 or > 20 ||
                printer.CustomerCopyCount is < 0 or > 5 ||
                printer.MerchantCopyCount is < 0 or > 5 ||
                (printer.PrintCustomerCopy && printer.CustomerCopyCount == 0) ||
                (printer.PrintMerchantCopy && printer.MerchantCopyCount == 0) ||
                (printer.SupportedPurposes?.Any(x =>
                    !PrinterPurposes.Contains(x)) ?? false))
                return ("pos_hardware.invalid_configuration", "Local Print Agent settings are invalid. Physical devices must use the laptop LAN URL.");
        }
        else if (request.HardwareType.Equals("barcodeScanner", StringComparison.OrdinalIgnoreCase))
        {
            if (request.BarcodeScanner is null ||
                request.BarcodeScanner.Mode is not ("hid" or "usbHid" or "bluetoothHid" or "camera") ||
                request.BarcodeScanner.InputSuffix is not ("enter" or "newline") ||
                request.BarcodeScanner.ScanTimeout is < 20 or > 1000 ||
                request.BarcodeScanner.MinimumBarcodeLength is < 1 or > 128 ||
                request.BarcodeScanner.MaximumBarcodeLength is < 1 or > 512 ||
                request.BarcodeScanner.MinimumBarcodeLength >
                    request.BarcodeScanner.MaximumBarcodeLength ||
                (request.BarcodeScanner.Mode == "camera" &&
                    !request.BarcodeScanner.CameraEnabled))
                return ("pos_hardware.invalid_configuration", "Scanner configuration is invalid.");
        }
        else if (request.HardwareType.Equals("cashDrawer", StringComparison.OrdinalIgnoreCase))
        {
            var drawer = request.CashDrawer;
            if (!request.TransportType.Equals("localPrintAgent", StringComparison.OrdinalIgnoreCase) ||
                drawer is null ||
                drawer.LinkedReceiptPrinterId is null ||
                drawer.DrawerPort is not ("drawerPin2" or "drawerPin5") ||
                drawer.PulseOnMilliseconds is < 2 or > 510 ||
                drawer.PulseOffMilliseconds is < 2 or > 510 ||
                string.IsNullOrWhiteSpace(drawer.Policy) ||
                drawer.Policy.Length > 80)
                return ("pos_hardware.invalid_configuration",
                    "Cash drawer configuration, linked printer, port and safe pulse timing are required.");
        }
        else if (request.HardwareType.Equals("cardTerminal", StringComparison.OrdinalIgnoreCase))
        {
            var terminal = request.CardTerminal;
            if (terminal is null ||
                terminal.ConnectionMode is not ("providerManaged" or "localService") ||
                terminal.PairingStatus is not
                    ("notConfigured" or "unverified" or "pairingRequired" or
                     "paired" or "revoked") ||
                terminal.RequestTimeout is < 1000 or > 120000 ||
                terminal.StatusPollInterval is < 500 or > 30000 ||
                terminal.Currency.Length != 3 ||
                terminal.CustomerSlipSource is not
                    ("notConfigured" or "externalTerminal" or "pos") ||
                terminal.MerchantSlipSource is not
                    ("notConfigured" or "externalTerminal" or "pos") ||
                (terminal.LocalServiceBaseUrl is { Length: > 0 } serviceUrl &&
                 (!Uri.TryCreate(serviceUrl, UriKind.Absolute, out var serviceUri) ||
                  serviceUri.Scheme is not ("http" or "https"))) ||
                terminal.Provider?.Length > 80 ||
                terminal.TerminalReference?.Length > 120 ||
                terminal.MerchantIdReference?.Length > 120)
                return ("pos_hardware.invalid_configuration",
                    "Card-terminal configuration is invalid.");

            if (request.Enabled ||
                !string.IsNullOrWhiteSpace(terminal.Provider) ||
                terminal.PairingStatus == "paired")
                return ("pos_hardware.external_dependency",
                    "No supported real card-terminal provider is installed. Keep this configuration disabled.");
        }

        return null;
    }

    private static object SelectSafeSettings(SavePosHardwareConfigurationRequest request) =>
        request.HardwareType.ToLowerInvariant() switch
        {
            "receiptprinter" => request.ReceiptPrinter!,
            "barcodescanner" => request.BarcodeScanner!,
            "cashdrawer" => request.CashDrawer ?? new CashDrawerSettingsDto(
                null, null, null, null, "notConfigured"),
            "cardterminal" => request.CardTerminal ?? new CardTerminalSettingsDto(null, null),
            _ => new { }
        };

    private static bool ValidScannerEvidence(ScannerTestEvidenceDto? evidence)
    {
        if (evidence is null) return true;
        return evidence.ScannerMode is "hid" or "usbHid" or "bluetoothHid" or "camera" &&
               evidence.BarcodeLength is >= 0 and <= 512 &&
               (evidence.BarcodeHash is null ||
                evidence.BarcodeHash.Length is >= 16 and <= 64) &&
               evidence.EventCount is >= 0 and <= 10000 &&
               evidence.ExpectedEventCount is null or >= 0 and <= 10000 &&
               evidence.DroppedScans is >= 0 and <= 10000 &&
               evidence.DuplicateScans is >= 0 and <= 10000 &&
               evidence.AverageLatencyMs is null or >= 0 and <= 120000 &&
               evidence.MaximumLatencyMs is null or >= 0 and <= 120000;
    }

    private static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message));

    private static string ErrorMessage(string? code) => code switch
    {
        "pos_hardware.device_not_trusted" => "Activate and trust this POS device before configuring hardware.",
        "pos_hardware.assignment_mismatch" => "The outlet or till assignment does not match this POS device.",
        "pos_hardware.version_conflict" => "Hardware configuration changed elsewhere. Reload and try again.",
        "pos_hardware.active_shift_reason_required" => "A reason is required to change critical hardware during an active shift.",
        "pos_hardware.request_id_conflict" => "This request ID was already used for a different hardware test.",
        "pos_hardware.configuration_not_found" => "The hardware configuration could not be found.",
        "pos_hardware.scanner_disabled" => "Enable the scanner configuration before running a scanner test.",
        "pos_hardware.unsupported_scanner_mode" => "The selected scanner test does not match the saved scanner mode.",
        "pos_hardware.invalid_test_result" => "Scanner result evidence is incomplete.",
        "pos_hardware.test_not_found" => "The hardware test operation could not be found.",
        _ => "Hardware operation could not be completed."
    };
}
