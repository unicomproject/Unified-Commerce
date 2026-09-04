using System.Net;
using E_POS.Application.Common.Email;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Email;

/// <summary>
/// Canonical composer for Tenant Admin onboarding invitation emails (Phase A).
/// Adheres strictly to zero-password-by-email security contract.
/// </summary>
public static class TenantAdminInvitationEmailComposer
{
    public const string Subject = "Welcome to ONEVERZ \u2014 Activate Your Administrator Account";
    public const string SecurityStatement = "ONEVERZ will never email you a password.";

    public static ApplicationEmailMessage Compose(
        string toAddress,
        string tenantName,
        string tenantCode,
        string adminUsername,
        string activationUrl,
        string loginUrl,
        DateTimeOffset expiresAt,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminUsername);
        ArgumentException.ThrowIfNullOrWhiteSpace(activationUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(loginUrl);

        var safeTenantName = WebUtility.HtmlEncode(tenantName.Trim());
        var safeTenantCode = WebUtility.HtmlEncode(tenantCode.Trim());
        var safeUsername = WebUtility.HtmlEncode(adminUsername.Trim());
        var safeActivationUrl = WebUtility.HtmlEncode(activationUrl.Trim());
        var safeLoginUrl = WebUtility.HtmlEncode(loginUrl.Trim());
        var expiryTextUtc = expiresAt.ToString("u");

        var htmlBody = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <title>Welcome to ONEVERZ</title>
            </head>
            <body style="font-family: Segoe UI, -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; color: #1e293b; background-color: #f8fafc; margin: 0; padding: 24px; line-height: 1.6;">
              <div style="max-width: 600px; margin: 0 auto; background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);">
                <div style="background: #0f172a; padding: 24px 32px; text-align: left;">
                  <span style="font-size: 20px; font-weight: 700; letter-spacing: 0.05em; color: #ffffff;">ONEVERZ EPOS</span>
                </div>
                <div style="padding: 32px;">
                  <h1 style="font-size: 20px; font-weight: 600; color: #0f172a; margin-top: 0; margin-bottom: 16px;">
                    Activate Your Administrator Account
                  </h1>
                  <p style="margin-bottom: 16px; color: #334155;">
                    Your organization <strong>{safeTenantName}</strong> has been provisioned on ONEVERZ. You have been invited as the Tenant Administrator.
                  </p>
                  
                  <div style="background: #f1f5f9; border-radius: 6px; padding: 16px; margin-bottom: 24px;">
                    <table style="width: 100%; border-collapse: collapse; font-size: 14px;">
                      <tr>
                        <td style="padding: 4px 0; color: #64748b; width: 140px;">Company:</td>
                        <td style="padding: 4px 0; font-weight: 600; color: #0f172a;">{safeTenantName}</td>
                      </tr>
                      <tr>
                        <td style="padding: 4px 0; color: #64748b;">Tenant Code:</td>
                        <td style="padding: 4px 0; font-weight: 600; color: #0f172a;">{safeTenantCode}</td>
                      </tr>
                      <tr>
                        <td style="padding: 4px 0; color: #64748b;">Username / Email:</td>
                        <td style="padding: 4px 0; font-weight: 600; color: #0f172a;">{safeUsername}</td>
                      </tr>
                      <tr>
                        <td style="padding: 4px 0; color: #64748b;">Invitation Expires:</td>
                        <td style="padding: 4px 0; font-weight: 600; color: #0f172a;">{expiryTextUtc} UTC</td>
                      </tr>
                    </table>
                  </div>

                  <p style="margin-bottom: 24px; color: #334155;">
                    Please click the button below to verify your email address and activate your administrator account:
                  </p>

                  <div style="text-align: center; margin-bottom: 32px;">
                    <a href="{safeActivationUrl}" style="display: inline-block; background-color: #2563eb; color: #ffffff; font-weight: 600; font-size: 15px; padding: 12px 28px; text-decoration: none; border-radius: 6px; box-shadow: 0 2px 4px rgba(37, 99, 235, 0.2);">
                      Activate Account
                    </a>
                  </div>

                  <p style="font-size: 13px; color: #64748b; margin-bottom: 16px;">
                    If the button above does not work, copy and paste the following link into your browser:<br />
                    <span style="word-break: break-all; color: #2563eb;">{safeActivationUrl}</span>
                  </p>

                  <div style="border-top: 1px solid #e2e8f0; padding-top: 20px; margin-top: 24px; font-size: 13px; color: #64748b;">
                    <p style="margin-bottom: 8px;">
                      Tenant Admin Application Entry Point: <a href="{safeLoginUrl}" style="color: #2563eb; text-decoration: none;">{safeLoginUrl}</a>
                    </p>
                    <p style="margin-bottom: 8px; color: #dc2626; font-weight: 500;">
                      Security Notice: {SecurityStatement}
                    </p>
                    <p style="margin-bottom: 0;">
                      If you were not expecting this invitation, you can safely ignore this email.
                    </p>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;

        var plainTextBody = $"""
            ONEVERZ EPOS

            Activate Your Administrator Account

            Your organization {tenantName.Trim()} has been provisioned on ONEVERZ. You have been invited as the Tenant Administrator.

            Company: {tenantName.Trim()}
            Tenant Code: {tenantCode.Trim()}
            Username / Email: {adminUsername.Trim()}
            Invitation Expires: {expiryTextUtc} UTC

            Activate your account by opening the following secure link:
            {activationUrl.Trim()}

            Tenant Admin Application Entry Point:
            {loginUrl.Trim()}

            Security Notice: {SecurityStatement}

            If you were not expecting this invitation, you can safely ignore this email.
            """;

        return new ApplicationEmailMessage(
            toAddress.Trim(),
            Subject,
            htmlBody,
            plainTextBody,
            correlationId);
    }
}
