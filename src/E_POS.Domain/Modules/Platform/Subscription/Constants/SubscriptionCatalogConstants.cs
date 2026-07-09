namespace E_POS.Domain.Modules.Platform.Subscription.Constants;

public static class SubscriptionCatalogConstants
{
    public static class RecordStatus
    {
        public const string Active = "ACTIVE";
        public const string Inactive = "INACTIVE";
        public const string Deleted = "DELETED";
    }

    public static class LimitValueType
    {
        public const string Integer = "INTEGER";
        public const string Decimal = "DECIMAL";
    }

    public static class AddonType
    {
        public const string Capacity = "CAPACITY";
        public const string Feature = "FEATURE";
    }

    public const string DefaultBillingCycle = SubscriptionPlanConstants.BillingInterval.Monthly;
    public const string DefaultBaseCurrency = SubscriptionPlanConstants.DefaultBaseCurrency;
    public const int DefaultMinQuantity = 1;
}
