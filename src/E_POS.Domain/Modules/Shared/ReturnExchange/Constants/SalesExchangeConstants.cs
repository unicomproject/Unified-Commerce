namespace E_POS.Domain.Modules.Shared.ReturnExchange.Constants;

/// <summary>
/// Canonical Exchange status/mode/action values used by POS Exchange completion.
/// Database currently stores these as varchar without enum CHECK constraints.
/// </summary>
public static class SalesExchangeConstants
{
    public static class Status
    {
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
        public const string Cancelled = "CANCELLED";
    }

    public static class Mode
    {
        public const string CustomerPays = "CUSTOMER_PAYS";
        public const string CustomerReceives = "CUSTOMER_RECEIVES";
        public const string EvenExchange = "EVEN_EXCHANGE";
    }

    public static class ActionType
    {
        public const string Replace = "REPLACE";
    }

    public static class EventType
    {
        public const string Created = "CREATED";
        public const string SettlementProcessed = "SETTLEMENT_PROCESSED";
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
    }
}
