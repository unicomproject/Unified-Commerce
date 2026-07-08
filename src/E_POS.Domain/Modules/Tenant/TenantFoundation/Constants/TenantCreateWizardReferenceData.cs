namespace E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;

public static class TenantCreateWizardReferenceData
{
    public static readonly IReadOnlyList<(string Value, string Label)> Timezones =
    [
        ("Asia/Colombo", "Asia/Colombo (Sri Lanka)"),
        ("UTC", "UTC"),
        ("Europe/London", "Europe/London"),
        ("America/New_York", "America/New_York")
    ];

    public static readonly IReadOnlyList<(string Value, string Label)> Locales =
    [
        ("en-LK", "English (Sri Lanka)"),
        ("en-GB", "English (United Kingdom)"),
        ("en-US", "English (United States)")
    ];

    public static readonly IReadOnlyList<(string Code, string Name)> CountryCodes =
    [
        ("LK", "Sri Lanka"),
        ("IN", "India"),
        ("GB", "United Kingdom"),
        ("US", "United States")
    ];

    public static readonly IReadOnlyList<string> TenantAdminBootstrapPermissionCodes =
    [
        "tenant.dashboard.view",
        "tenant.settings.manage",
        "tenant.users.manage",
        "tenant.roles.manage",
        "tenant.outlets.manage",
        "catalog.products.view",
        "catalog.products.create",
        "catalog.products.update",
        "inventory.stock.view",
        "reports.sales.view"
    ];
}

