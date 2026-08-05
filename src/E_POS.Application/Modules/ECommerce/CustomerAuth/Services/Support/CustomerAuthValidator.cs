using System.Net.Mail;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;

public sealed class CustomerAuthValidator : ICustomerAuthValidator
{
    public ApplicationError? ValidateRegister(Guid tenantId, CustomerRegisterRequest request)
    {
        var emailError = ValidateEmailOnly(tenantId, request.Email);
        if (emailError is not null)
            return emailError;

        if (!request.AgreeTerms)
            return CustomerAuthErrors.TermsRequired;

        var passwordError = ValidatePassword(request.Password);
        if (passwordError is not null)
            return passwordError;

        if (request.FirstName?.Trim().Length > 100 || request.LastName?.Trim().Length > 100)
            return CustomerAuthErrors.ValidationFailed("Name fields cannot exceed 100 characters.");

        return null;
    }

    public ApplicationError? ValidateVerifyEmail(Guid tenantId, CustomerVerifyEmailRequest request)
    {
        var emailError = ValidateEmailOnly(tenantId, request.Email);
        if (emailError is not null)
            return emailError;

        var code = request.Code?.Trim() ?? string.Empty;
        if (code.Length != 6 || code.Any(x => !char.IsDigit(x)))
            return CustomerAuthErrors.ValidationFailed("A 6-digit verification code is required.");

        return null;
    }

    public ApplicationError? ValidateResetPassword(Guid tenantId, CustomerResetPasswordRequest request)
    {
        var emailError = ValidateEmailOnly(tenantId, request.Email);
        if (emailError is not null)
            return emailError;

        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Trim().Length > 512)
            return CustomerAuthErrors.ValidationFailed("A valid password reset token is required.");

        return ValidatePassword(request.NewPassword);
    }

    public ApplicationError? ValidateGoogleLogin(Guid tenantId, CustomerGoogleLoginRequest request)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(request.IdToken))
            return CustomerAuthErrors.ValidationFailed("Tenant and Google sign-in token are required.");

        if (request.IdToken.Trim().Length > 8192)
            return CustomerAuthErrors.ValidationFailed("Google sign-in token is too large.");

        if (request.DeviceName?.Trim().Length > 150)
            return CustomerAuthErrors.ValidationFailed("Device name cannot exceed 150 characters.");

        return null;
    }

    public ApplicationError? ValidateLogin(Guid tenantId, CustomerLoginRequest request)
    {
        if (tenantId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.EmailOrPhone) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.EmailOrPhone.Trim().Length > 150 ||
            request.Password.Length > 512 ||
            request.DeviceName?.Trim().Length > 150)
        {
            return CustomerAuthErrors.ValidationFailed("Tenant, email/phone, and password are required.");
        }

        return null;
    }

    public ApplicationError? ValidateEmailOnly(Guid tenantId, string? email)
    {
        if (tenantId == Guid.Empty)
            return CustomerAuthErrors.ValidationFailed("Tenant is required.");

        var normalized = NormalizeEmailAddress(email);
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Length > 150 ||
            !MailAddress.TryCreate(normalized, out var parsed) ||
            !string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return CustomerAuthErrors.ValidationFailed("A valid email address is required.");
        }

        return null;
    }

    public ApplicationError? ValidateExternalLoginAccount(CustomerLoginAccount account, DateTimeOffset now)
    {
        if (!IsTenantActive(account.TenantStatus))
            return CustomerAuthErrors.TenantAccessDenied;

        if (account.Account.IsLocked(now))
            return CustomerAuthErrors.InvalidCredentials;

        var accountStatusAllowed =
            string.Equals(account.Account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(account.Account.Status, "LOCKED", StringComparison.OrdinalIgnoreCase) &&
             account.Account.LockedUntil.HasValue && account.Account.LockedUntil <= now);

        if (!accountStatusAllowed || !IsCustomerActive(account.CustomerStatus))
            return CustomerAuthErrors.InvalidCredentials;

        return null;
    }

    public string NormalizeEmailAddress(string? email) => (email ?? string.Empty).Trim();

    public bool IsTenantActive(string status) =>
        string.Equals(status, "active", StringComparison.OrdinalIgnoreCase);

    public bool IsCustomerActive(string status) =>
        string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase);

    private static ApplicationError? ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 512)
            return CustomerAuthErrors.ValidationFailed("Password must be between 8 and 512 characters.");

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return CustomerAuthErrors.ValidationFailed("Password must contain letters and numbers.");

        return null;
    }
}
