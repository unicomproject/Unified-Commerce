namespace E_POS.Domain.Modules.Platform.Subscription.Constants;

/// <summary>
/// Commercial subscription type for tenant create orchestration (PAID / TRIAL / DEMO).
/// Separate from <see cref="TenantSubscriptionStatusConstants"/> lifecycle values.
/// </summary>
public static class TenantSubscriptionTypeConstants
{
    public const string Paid = "PAID";
    public const string Trial = "TRIAL";
    public const string Demo = "DEMO";

    public static readonly IReadOnlyList<string> All =
    [
        Paid,
        Trial,
        Demo
    ];

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        All.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string value)
    {
        var match = All.FirstOrDefault(item =>
            string.Equals(item, value.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Subscription type must be PAID, TRIAL, or DEMO.");
        }

        return match;
    }
}
