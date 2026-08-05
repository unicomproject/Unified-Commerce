namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;

internal static class CustomerAuthConstants
{
    public const int MaxFailedAttempts = 5;
    public const int VerificationOtpMaxAttempts = 5;
    public const int VerificationOtpMinutes = 15;
    public const int PasswordResetTokenMinutes = 60;
    public const string CustomerCodeSequenceKey = "CUSTOMER_CODE";
    public const string CustomerCodePrefix = "CUS";
    public const int CustomerCodePaddingLength = 6;
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
}
