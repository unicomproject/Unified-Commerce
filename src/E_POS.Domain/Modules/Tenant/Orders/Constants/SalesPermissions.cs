namespace E_POS.Domain.Modules.Tenant.Orders.Constants;

public static class SalesPermissions
{
    public static class Sale
    {
        public const string Create = "sales.create";
        public const string View = "sales.view";
        public const string Checkout = "sales.checkout";
        public const string LegacyCreate = "pos.sale.create";
    }

    public static class Cart
    {
        public const string Manage = "sales.cart.manage";
        public const string AddItem = "sales.cart.add_item";
        public const string UpdateItem = "sales.cart.update_item";
        public const string RemoveItem = "sales.cart.remove_item";
        public const string Clear = "sales.cart.clear";
    }

    public static class Discount
    {
        public const string Apply = "sales.discount.apply";
    }

    public static class Park
    {
        public const string Create = "sales.park.create";
        public const string View = "sales.park.view";
        public const string Recall = "sales.park.recall";
        public const string LegacyPark = "pos.sale.park";
        public const string LegacyRecall = "pos.sale.recall";
        public const string LegacyView = "pos.sale.park.view";
    }

    public static class Orders
    {
        public const string View = "orders.view";
    }
}
