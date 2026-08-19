using System.Net;
using E_POS.Application.Common.Email;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Email;

/// <summary>
/// Builds the OneVerz platform user invitation email (HTML + plain text).
/// Caller must supply a setup URL built from trusted configuration.
/// </summary>
public static class PlatformUserInvitationEmailComposer
{
    public const string Subject = "Set up your OneVerz Platform Admin account";

    public static ApplicationEmailMessage Compose(
        string toAddress,
        string? displayName,
        string setupUrl,
        DateTimeOffset expiresAt,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(setupUrl);

        var safeName = string.IsNullOrWhiteSpace(displayName)
            ? "there"
            : WebUtility.HtmlEncode(displayName.Trim());
        var safeUrl = WebUtility.HtmlEncode(setupUrl);
        var expiryText = expiresAt.ToString("u");

        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8" /><title>OneVerz Platform Invitation</title></head>
            <body style="font-family: Segoe UI, Arial, sans-serif; color: #1a1a1a; line-height: 1.5;">
              <p><strong>OneVerz</strong></p>
              <p>Hi {safeName},</p>
              <p>A OneVerz Platform Admin account has been created for you.</p>
              <p>Please use the button below to set up your account and choose your password:</p>
              <p>
                <a href="{safeUrl}" style="display:inline-block;padding:10px 16px;background:#0b5fff;color:#ffffff;text-decoration:none;border-radius:4px;">
                  Set up your account
                </a>
              </p>
              <p>This invitation link expires at <strong>{WebUtility.HtmlEncode(expiryText)}</strong> UTC.</p>
              <p>If you did not expect this email, you can ignore it safely.</p>
              <p style="color:#666;font-size:12px;">Do not forward this email. OneVerz will never ask you for your password by email.</p>
            </body>
            </html>
            """;

        var plain = $"""
            OneVerz

            Hi {(string.IsNullOrWhiteSpace(displayName) ? "there" : displayName.Trim())},

            A OneVerz Platform Admin account has been created for you.

            Set up your account:
            {setupUrl}

            This link expires at {expiryText} UTC.

            If you did not expect this email, you can ignore it safely.

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
