using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;

internal static class CustomerAuthErrors
{
    public static readonly ApplicationError InvalidCredentials =
        new("customer_auth.invalid_credentials", "Invalid email/phone or password.");

    public static readonly ApplicationError InvalidSession =
        new("customer_auth.invalid_session", "Invalid customer session.");

    public static readonly ApplicationError InvalidRefreshToken =
        new("customer_auth.invalid_refresh_token", "The refresh token is invalid or expired.");

    public static readonly ApplicationError TenantAccessDenied =
        new("customer_auth.tenant_access_denied", "Tenant access denied.");

    public static readonly ApplicationError EmailDeliveryUnavailable =
        new("customer_auth.email_delivery_unavailable", "Email delivery is not configured or could not send the message.");

    public static readonly ApplicationError DuplicateEmail =
        new("customer_auth.email_already_registered", "An account with this email address already exists.");

    public static readonly ApplicationError InvalidVerificationCode =
        new("customer_auth.invalid_verification_code", "The verification code is invalid or expired.");

    public static readonly ApplicationError InvalidResetToken =
        new("customer_auth.invalid_reset_token", "The password reset link is invalid or expired.");

    public static readonly ApplicationError EmailNotVerified =
        new("customer_auth.email_not_verified", "Please verify your email before signing in.");

    public static readonly ApplicationError GoogleEmailNotVerified =
        new("customer_auth.google_email_not_verified", "Google email is not verified.");

    public static readonly ApplicationError ExternalAccountConflict =
        new("customer_auth.external_account_conflict", "Unable to link Google account. Please try again.");

    public static readonly ApplicationError InvalidGoogleToken =
        new("customer_auth.invalid_google_token", "Invalid Google sign-in token.");

    public static ApplicationError ValidationFailed(string message) =>
        new("customer_auth.validation_failed", message);

    public static ApplicationError TermsRequired =>
        new("customer_auth.terms_required", "You must agree to the terms and privacy policy.");

    public static ApplicationError CustomerNotFound =>
        new("customer.not_found", "Customer not found.");

    public static ApplicationError InvalidFirstName =>
        new("customer.invalid_first_name", "First name is required.");
}
