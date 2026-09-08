namespace E_POS.Application.Modules.Tenant.OnlineStoreSetup;

public static class OnlineStoreReleaseOnePolicy
{
    public const string Release = "R1";

    public const bool CustomerRegistrationRequired = true;
    public const string CustomerAccountMode = "REGISTRATION_REQUIRED";
    public const string CustomerAccountLabel = "Registration required";

    public const bool GuestCheckoutAvailable = false;
    public const string GuestCheckoutMode = "NOT_AVAILABLE";
    public const string GuestCheckoutLabel = "Not available";

    public const bool EmailVerificationRequired = true;
    public const string EmailVerificationMode = "REQUIRED";
    public const string EmailVerificationLabel = "Required";

    public const string FulfilmentMode = "CLICK_COLLECT";
    public const string FulfilmentLabel = "Click & Collect";
    public const string ActivationReleaseScope = "CLICK_COLLECT_ONLY";

    public const string PaymentMode = "PAY_AT_PICKUP";
    public const string PaymentLabel = "Pay at Pickup";
}
