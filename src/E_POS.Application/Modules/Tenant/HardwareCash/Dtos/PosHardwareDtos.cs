namespace E_POS.Application.Modules.Tenant.HardwareCash.Dtos;

public sealed record ReceiptPrinterSettingsDto(
    string AgentBaseUrl,
    string PrinterName,
    string PaperWidth,
    bool AutoCut,
    int RequestTimeout,
    int FeedBeforeCut,
    bool LocalApiKeyPresent,
    IReadOnlyList<string>? SupportedPurposes = null,
    bool PrintCustomerCopy = true,
    int CustomerCopyCount = 1,
    bool PrintMerchantCopy = false,
    int MerchantCopyCount = 0,
    bool ExternalTerminalSlipExpected = false,
    bool ExternalTerminalPrintsCustomerSlip = false,
    bool ExternalTerminalPrintsMerchantSlip = false);

public sealed record BarcodeScannerSettingsDto(
    string Mode,
    IReadOnlyList<string> EnabledFormats,
    bool EnterSuffixEnabled,
    string InputSuffix = "enter",
    int ScanTimeout = 120,
    int MinimumBarcodeLength = 4,
    int MaximumBarcodeLength = 128,
    bool AllowRapidScan = true,
    bool CameraEnabled = true);

public sealed record CashDrawerSettingsDto(
    Guid? LinkedReceiptPrinterId,
    string? DrawerPort,
    int? PulseOnMilliseconds,
    int? PulseOffMilliseconds,
    string Policy,
    bool OpenOnCashSale = true,
    bool OpenOnCashRefund = true,
    bool OpenOnCashSplit = true,
    bool ManualOpenEnabled = false);

public sealed record CardTerminalSettingsDto(
    string? Provider,
    string? TerminalReference,
    string? MerchantIdReference = null,
    string ConnectionMode = "providerManaged",
    string? LocalServiceBaseUrl = null,
    string PairingStatus = "notConfigured",
    int RequestTimeout = 30000,
    int StatusPollInterval = 2000,
    string Currency = "LKR",
    string CustomerSlipSource = "notConfigured",
    string MerchantSlipSource = "notConfigured");

public sealed record SavePosHardwareConfigurationRequest(
    Guid PosDeviceId,
    Guid OutletId,
    Guid? TillId,
    string HardwareType,
    string TransportType,
    string DisplayName,
    bool Enabled,
    int ExpectedVersion,
    string? ChangeReason,
    ReceiptPrinterSettingsDto? ReceiptPrinter,
    BarcodeScannerSettingsDto? BarcodeScanner,
    CashDrawerSettingsDto? CashDrawer,
    CardTerminalSettingsDto? CardTerminal);

public sealed record PosHardwareConfigurationDto(
    Guid ConfigurationId,
    Guid TenantId,
    Guid OutletId,
    Guid PosDeviceId,
    Guid? TillId,
    string HardwareType,
    string TransportType,
    string DisplayName,
    bool Enabled,
    int ConfigurationVersion,
    bool ActiveShift,
    Guid? TillSessionId,
    object Settings,
    DateTimeOffset UpdatedAt);

public sealed record CreateHardwareTestRequest(
    Guid RequestId,
    Guid PosDeviceId,
    Guid? TillId,
    Guid? HardwareConfigurationId,
    string HardwareType,
    string TestType,
    int ConfigurationVersion);

public sealed record CompleteHardwareTestRequest(
    string Status,
    string ResultCategory,
    string? SafeMessage,
    bool? PhysicalConfirmation,
    DateTimeOffset? DetectedAt = null,
    string? AutomaticResult = null,
    ScannerTestEvidenceDto? ScannerEvidence = null);

public sealed record ScannerTestEvidenceDto(
    string ScannerMode,
    int BarcodeLength,
    string? BarcodeHash,
    int EventCount,
    int? ExpectedEventCount,
    int DroppedScans,
    int DuplicateScans,
    double? AverageLatencyMs,
    double? MaximumLatencyMs);

public sealed record HardwareTestOperationDto(
    Guid TestId,
    Guid RequestId,
    Guid PosDeviceId,
    Guid? TillId,
    Guid? TillSessionId,
    Guid? HardwareConfigurationId,
    string HardwareType,
    string TestType,
    int ConfigurationVersion,
    string Status,
    string? ResultCategory,
    string? SafeMessage,
    bool? PhysicalConfirmation,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? DetectedAt = null,
    string? AutomaticResult = null,
    ScannerTestEvidenceDto? ScannerEvidence = null);
