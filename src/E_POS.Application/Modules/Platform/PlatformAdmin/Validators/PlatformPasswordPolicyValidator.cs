using System.Text.RegularExpressions;
using E_POS.Application.Common.Models;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Validators;

public interface IPlatformPasswordPolicyValidator
{
    ApplicationError? Validate(string? password);
}

public sealed class PlatformPasswordPolicyValidator : IPlatformPasswordPolicyValidator
{
    private static readonly Regex HasLower = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex HasUpper = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex HasDigit = new("[0-9]", RegexOptions.Compiled);

    public ApplicationError? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new ApplicationError(
                "platform_password_reset.password_policy",
                "Password is required.");
        }

        if (password.Length < PlatformPasswordResetConstants.MinPasswordLength)
        {
            return new ApplicationError(
                "platform_password_reset.password_policy",
                $"Password must be at least {PlatformPasswordResetConstants.MinPasswordLength} characters.");
        }

        if (password.Length > PlatformPasswordResetConstants.MaxPasswordLength)
        {
            return new ApplicationError(
                "platform_password_reset.password_policy",
                $"Password must be at most {PlatformPasswordResetConstants.MaxPasswordLength} characters.");
        }

        if (!HasLower.IsMatch(password) || !HasUpper.IsMatch(password) || !HasDigit.IsMatch(password))
        {
            return new ApplicationError(
                "platform_password_reset.password_policy",
                "Password must include uppercase, lowercase, and numeric characters.");
        }

        return null;
    }
}
