namespace E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;

/// <summary>
/// Development-only Platform Admin test-account credentials.
/// Bind from user-secrets or environment variables under <see cref="SectionName"/>.
/// Passwords must never be committed to appsettings.
/// </summary>
public sealed class DevelopmentPlatformAdminSeedOptions
{
    public const string SectionName = "DevelopmentSeed:PlatformAdmin";

    public const string DefaultBillingViewerDisplayName = "Billing Viewer Development";
    public const string DefaultNoBillingDisplayName = "No Billing Development";

    public DevelopmentPlatformAdminAccountOptions BillingViewer { get; set; } = new();

    public DevelopmentPlatformAdminAccountOptions NoBilling { get; set; } = new();
}

public sealed class DevelopmentPlatformAdminAccountOptions
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? DisplayName { get; set; }

    public bool HasCompleteCredentials =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Password);
}
