using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.Tenant.POSOperations.Services;

public static class PosNotificationSourceAccess
{
    public const string SalesPermission = "pos.sales.checkout.execute";
    public const string OnlineOrderPermission = "commerce.online_order.orders.access";
    public const string ReturnPermission = "pos.returns.search_sale.view";

    public static IReadOnlyCollection<string> Resolve(TenantRequestContext context)
    {
        var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (context.HasPermission(SalesPermission))
        {
            sources.Add("POS");
            sources.Add("Sales");
        }

        if (context.HasPermission(OnlineOrderPermission))
            sources.Add("ECommerce");

        if (context.HasPermission(ReturnPermission))
        {
            sources.Add("Returns");
            sources.Add("Refunds");
        }

        return sources;
    }
}
