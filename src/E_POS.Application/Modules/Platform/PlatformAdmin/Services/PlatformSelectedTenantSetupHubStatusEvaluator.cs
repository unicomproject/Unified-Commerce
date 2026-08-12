using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public static class PlatformSelectedTenantSetupHubStatusEvaluator
{
    public const string StatusNotEntitled = "NOT_ENTITLED";
    public const string StatusNotRequired = "NOT_REQUIRED";
    public const string StatusNotStarted = "NOT_STARTED";
    public const string StatusConfigured = "CONFIGURED";
    public const string StatusBlocked = "BLOCKED";

    public sealed record Input(
        bool OutletEntitled,
        bool TillEntitled,
        bool ProductEntitled,
        int ActiveOutletCount,
        int ActiveTillCount,
        int CustomRoleCount,
        int TenantUserCount,
        int ActiveOrDraftProductCount,
        bool TenantSuspended,
        bool CanManageOutlets,
        bool CanManageTills,
        bool CanManageRoles,
        bool CanManageUsers,
        bool CanManageProducts);

    public static IReadOnlyList<PlatformTenantBootstrapModuleStatusDto> Evaluate(Input input)
    {
        return
        [
            EvaluateOutlets(input),
            EvaluateTills(input),
            EvaluateRoles(input),
            EvaluateUsers(input),
            EvaluateProducts(input)
        ];
    }

    private static PlatformTenantBootstrapModuleStatusDto EvaluateOutlets(Input input)
    {
        if (!input.OutletEntitled)
        {
            return Module("outlets", StatusNotEntitled, input.ActiveOutletCount, entitled: false, canConfigure: false, null);
        }

        var status = input.ActiveOutletCount >= 1 ? StatusConfigured : StatusNotStarted;
        return Module("outlets", status, input.ActiveOutletCount, entitled: true, input.CanManageOutlets, null);
    }

    private static PlatformTenantBootstrapModuleStatusDto EvaluateTills(Input input)
    {
        if (!input.TillEntitled)
        {
            return Module("tills", StatusNotEntitled, input.ActiveTillCount, entitled: false, canConfigure: false, null);
        }

        if (input.ActiveOutletCount == 0)
        {
            return Module(
                "tills",
                StatusBlocked,
                input.ActiveTillCount,
                entitled: true,
                canConfigure: false,
                "Create an outlet before adding tills.");
        }

        var status = input.ActiveTillCount >= 1 ? StatusConfigured : StatusNotStarted;
        return Module("tills", status, input.ActiveTillCount, entitled: true, input.CanManageTills, null);
    }

    private static PlatformTenantBootstrapModuleStatusDto EvaluateRoles(Input input)
    {
        if (input.CustomRoleCount >= 1)
        {
            return Module("roles", StatusConfigured, input.CustomRoleCount, entitled: true, input.CanManageRoles, null);
        }

        return Module("roles", StatusNotRequired, 0, entitled: true, input.CanManageRoles, null);
    }

    private static PlatformTenantBootstrapModuleStatusDto EvaluateUsers(Input input)
    {
        var status = input.TenantUserCount > 1 ? StatusConfigured : StatusNotStarted;
        return Module("users", status, input.TenantUserCount, entitled: true, input.CanManageUsers, null);
    }

    private static PlatformTenantBootstrapModuleStatusDto EvaluateProducts(Input input)
    {
        if (!input.ProductEntitled)
        {
            return Module("products", StatusNotEntitled, input.ActiveOrDraftProductCount, entitled: false, canConfigure: false, null);
        }

        var status = input.ActiveOrDraftProductCount >= 1 ? StatusConfigured : StatusNotStarted;
        return Module("products", status, input.ActiveOrDraftProductCount, entitled: true, input.CanManageProducts, null);
    }

    private static PlatformTenantBootstrapModuleStatusDto Module(
        string moduleKey,
        string status,
        int count,
        bool entitled,
        bool canConfigure,
        string? dependencyNotice) =>
        new(moduleKey, status, count, entitled, canConfigure && !status.Equals(StatusBlocked, StringComparison.Ordinal), dependencyNotice);
}
