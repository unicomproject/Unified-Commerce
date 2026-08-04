namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;

public sealed class ManualPaymentEvidenceScannerOptions
{
    public const string SectionName = "ManualPaymentEvidenceScanner";
    public string? Host { get; init; }
    public int Port { get; init; } = 3310;
    public int TimeoutSeconds { get; init; } = 15;
}
