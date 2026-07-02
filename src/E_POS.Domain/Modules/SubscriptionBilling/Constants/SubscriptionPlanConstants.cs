namespace E_POS.Domain.Modules.SubscriptionBilling.Constants;

public static class SubscriptionPlanConstants
{
    public static class Status
    {
        public const string Draft = "draft";
        public const string Active = "active";
        public const string Retired = "retired";
    }

    public static class BillingInterval
    {
        public const string Monthly = "MONTHLY";
        public const string Yearly = "YEARLY";
        public const string OneTime = "ONE_TIME";
    }

    public static class PlanFeatureStatus
    {
        public const string Included = "included";
    }

    public const string DefaultBaseCurrency = "LKR";
}
