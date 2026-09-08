namespace E_POS.Domain.Modules.Tenant.Orders.Constants;

public static class SalesPermissions
{
    public static class Sale
    {
        public const string Create = "sales.create";
        public const string View = "sales.view";
        public const string Checkout = "pos.sales.checkout.execute";
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
        public const string Approve = "sales.discount.approve";
    }

    public static class DiscountPolicy
    {
        public const string View = "discount.policy.view";
        public const string Create = "discount.policy.create";
        public const string Update = "discount.policy.update";
        public const string Activate = "discount.policy.activate";
        public const string Delete = "discount.policy.delete";
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

    /// <summary>
    /// Canonical held-sales action codes (Chunk 2 definitions).
    /// Chunk 6 enforces these (with legacy Park.* accepted for create/view/recall).
    /// Cancel requires <see cref="Cancel"/> and is not authorized by create alone.
    /// </summary>
    public static class HeldSales
    {
        public const string Create = "pos.sales.held_sales.create";
        public const string View = "pos.sales.held_sales.view";
        public const string Recall = "pos.sales.held_sales.recall";
        public const string Cancel = "pos.sales.held_sales.cancel";
    }

    public static class Orders
    {
        public const string View = "orders.view";
    }
}
