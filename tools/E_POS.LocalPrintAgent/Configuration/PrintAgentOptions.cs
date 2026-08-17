using System.ComponentModel.DataAnnotations;

namespace E_POS.LocalPrintAgent.Configuration;

public sealed class PrintAgentOptions
{
    public const string SectionName = "PrintAgent";
    public const string ApiVersion = "1";
    public const string ReceiptContractVersion = "3";

    [Required, Url]
    public string ListenUrl { get; init; } = "http://0.0.0.0:9101";

    [Required, MinLength(1)]
    public string PrinterName { get; init; } = string.Empty;

    [RegularExpression("^(58|80)mm$")]
    public string PaperWidth { get; init; } = "80mm";

    public bool AutoCut { get; init; } = true;

    [Range(0, 20)]
    public int FeedLinesBeforeCut { get; init; } = 5;

    [Range(1, 30)]
    public int SpoolerTimeoutSeconds { get; init; } = 5;

    [Required, MinLength(24)]
    public string LocalApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Maximum age of a drawer-open request (client <c>requestedAt</c>).
    /// Prevents delayed/stale pulses after reconnect.
    /// </summary>
    [Range(5, 600)]
    public int DrawerRequestMaxAgeSeconds { get; init; } = 120;

    [Required, MinLength(1)]
    public string IdempotencyDirectory { get; init; } = "data/print-requests";

    [Range(1, 365)]
    public int OperationRetentionDays { get; init; } = 30;

    [MinLength(1)]
    public string[] AllowedNetworkRanges { get; init; } = ["127.0.0.1/32", "::1/128"];

    [Range(4096, 1048576)]
    public long RequestBodyLimit { get; init; } = 262144;

    [Required, MinLength(1)]
    public string LoggingDirectory { get; init; } = "data/logs";

    [Range(1, 90)]
    public int LogRetentionDays { get; init; } = 14;

    [Range(1048576, 104857600)]
    public long MaxLogFileBytes { get; init; } = 10485760;

    [Range(10485760, 10737418240)]
    public long MinimumFreeDiskBytes { get; init; } = 104857600;

    [Range(1, 100)]
    public int FailedAuthenticationLimit { get; init; } = 10;

    [Range(1, 60)]
    public int FailedAuthenticationWindowMinutes { get; init; } = 5;

    [Range(1, 10)]
    public int DrawerMinimumPulseMilliseconds { get; init; } = 2;

    [Range(2, 510)]
    public int DrawerMaximumPulseMilliseconds { get; init; } = 510;
}
