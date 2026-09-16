namespace E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;

public static class ReturnPolicyTemplateConstants
{
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";
    public const string DeletedStatus = "DELETED";

    public const string LifecycleStatusDraft = "DRAFT";
    public const string LifecycleStatusPublished = "PUBLISHED";
    public const string LifecycleStatusArchived = "ARCHIVED";

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    public static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    public static string NormalizeLifecycleStatus(string? lifecycleStatus)
    {
        if (string.IsNullOrWhiteSpace(lifecycleStatus))
        {
            return LifecycleStatusDraft;
        }

        var normalized = lifecycleStatus.Trim().ToUpperInvariant();
        return normalized switch
        {
            LifecycleStatusDraft => LifecycleStatusDraft,
            LifecycleStatusPublished => LifecycleStatusPublished,
            LifecycleStatusArchived => LifecycleStatusArchived,
            _ => LifecycleStatusDraft
        };
    }

    public static bool IsValidWriteStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is ActiveStatus or InactiveStatus;
    }

    public static bool IsValidLifecycleStatus(string status)
    {
        var normalized = status.Trim().ToUpperInvariant();
        return normalized is LifecycleStatusDraft or LifecycleStatusPublished or LifecycleStatusArchived;
    }
}
