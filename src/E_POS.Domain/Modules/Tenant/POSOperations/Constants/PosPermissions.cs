namespace E_POS.Domain.Modules.Tenant.POSOperations.Constants;

public static class PosPermissions
{
    public static class Home
    {
        public const string View = "pos.home.view";
        public const string ViewDashboard = "pos.dashboard.view";
    }

    public static class NewSale
    {
        public const string View = "pos.new_sale.view";
    }

    public static class Till
    {
        public const string Open = "pos.till.open";
        public const string Close = "pos.till.close";
        public const string ViewSession = "till.session.view";
    }

    public static class Notifications
    {
        public const string View = "notifications.view";
    }
}
