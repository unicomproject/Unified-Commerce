namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

/// <summary>
/// Approved tenant lifecycle values persisted on <c>tenants.status</c>.
/// Billing, subscription type, and payment status must never be stored here.
/// </summary>
public static class TenantStatusConstants
{
    public const string Draft = "draft";
    public const string PendingPayment = "pending_payment";
    public const string PendingActivation = "pending_activation";
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        PendingPayment,
        PendingActivation,
        Active,
        Suspended,
        Cancelled
    ];

    public static bool IsApproved(string? status) =>
        !string.IsNullOrWhiteSpace(status) &&
        All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string status)
    {
        var match = All.FirstOrDefault(value =>
            string.Equals(value, status.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Tenant status must be an approved lifecycle value.");
        }

        return match;
    }
}
