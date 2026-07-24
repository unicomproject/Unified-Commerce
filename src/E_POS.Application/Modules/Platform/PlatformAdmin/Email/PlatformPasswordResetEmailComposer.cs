using System.Net;
using E_POS.Application.Common.Email;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Email;

/// <summary>
/// Builds the OneVerz platform password-reset email (HTML + plain text).
/// Caller must supply a reset URL already built from trusted configuration.
/// </summary>
public static class PlatformPasswordResetEmailComposer
{
    public const string Subject = "Reset your OneVerz password";

    public static ApplicationEmailMessage Compose(
        string toAddress,
        string? displayName,
        string resetUrl,
        DateTimeOffset expiresAt,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetUrl);

        var safeName = string.IsNullOrWhiteSpace(displayName)
            ? "there"
            : WebUtility.HtmlEncode(displayName.Trim());
        var safeUrl = WebUtility.HtmlEncode(resetUrl);
        var expiryText = expiresAt.ToString("u");
        var lifetimeHours = PlatformPasswordResetConstants.DefaultLifetimeHours;

        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8" /><title>OneVerz password reset</title></head>
            <body style="font-family: Segoe UI, Arial, sans-serif; color: #1a1a1a; line-height: 1.5;">
              <p><strong>OneVerz</strong></p>
              <p>Hi {safeName},</p>
              <p>A OneVerz Platform Admin requested a password reset for your account.</p>
              <p>
                <a href="{safeUrl}" style="display:inline-block;padding:10px 16px;background:#0b5fff;color:#ffffff;text-decoration:none;border-radius:4px;">
                  Reset password
                </a>
              </p>
              <p>This link expires at <strong>{WebUtility.HtmlEncode(expiryText)}</strong> UTC
              (within {lifetimeHours} hour(s) of the request).</p>
              <p>If you did not expect this email, you can ignore it. Your password will stay the same.</p>
              <p style="color:#666;font-size:12px;">Do not forward this email. OneVerz will never ask you for your password by email.</p>
            </body>
            </html>
            """;

        var plain = $"""
            OneVerz

            Hi {(string.IsNullOrWhiteSpace(displayName) ? "there" : displayName.Trim())},

            A OneVerz Platform Admin requested a password reset for your account.

            Reset your password:
            {resetUrl}

            This link expires at {expiryText} UTC (within {lifetimeHours} hour(s) of the request).

            If you did not expect this email, you can ignore it. Your password will stay the same.

            Do not forward this email. OneVerz will never ask you for your password by email.
            """;

        return new ApplicationEmailMessage(
            toAddress.Trim(),
            Subject,
            html,
            plain,
            correlationId);
    }
}
