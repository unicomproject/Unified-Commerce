using System.Net;
using E_POS.Application.Common.Email;

namespace E_POS.Application.Modules.Tenant.TenantAuth;

public static class TenantAdminInvitationEmailComposer
{
    public static ApplicationEmailMessage Compose(string email, string company, string setupUrl,
        DateTimeOffset expiresAt, string correlationId)
    {
        static string E(string value) => WebUtility.HtmlEncode(value);
        var expiry = expiresAt.ToUniversalTime().ToString("dd MMM yyyy, HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
        var subject = $"ONEVERZ — {company.Replace('\r', ' ').Replace('\n', ' ')}: Activate your Tenant Admin account";
        var html = $$"""
        <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;color:#20242b;-webkit-text-size-adjust:100%">
        <div style="display:none;max-height:0;overflow:hidden;mso-hide:all">You have been invited to manage {{E(company)}} on ONEVERZ. Set your password to get started.</div>
        <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0"><tr><td align="center" style="padding:32px 12px">
        <table role="presentation" width="600" border="0" cellspacing="0" cellpadding="0" style="width:100%;max-width:600px;background:#ffffff;border:1px solid #e5e7eb;border-radius:12px">
        <tr><td align="left" style="background:#171717;padding:26px 28px;border-bottom:4px solid #f56700;color:#ffffff;font-size:28px;line-height:34px;font-weight:bold;letter-spacing:-1px">ONE<span style="color:#ff7900">VERZ</span></td></tr>
        <tr><td align="left" style="padding:30px 28px;font-size:15px;line-height:24px">
        <p style="margin:0 0 10px;font-size:11px;line-height:16px;font-weight:bold;letter-spacing:1.5px;color:#9a4100">TENANT ADMIN INVITATION</p>
        <h1 style="margin:0 0 20px;font-size:26px;line-height:34px;font-weight:bold;color:#171717">Welcome to ONEVERZ</h1>
        <p style="margin:0 0 12px">Hello,</p><p style="margin:0 0 24px">You have been invited to join <strong>{{E(company)}}</strong> as a <strong>Tenant Admin</strong>. Activate your account to get started.</p>
        <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="background:#f8f9fb;border:1px solid #e8ebef;border-radius:8px"><tr><td style="padding:18px 20px">
        <p style="margin:0 0 3px;color:#68707d;font-size:12px;line-height:18px">Organization</p><p style="margin:0 0 14px;font-weight:bold;overflow-wrap:anywhere">{{E(company)}}</p>
        <p style="margin:0 0 3px;color:#68707d;font-size:12px;line-height:18px">Login email:</p><p style="margin:0 0 14px;overflow-wrap:anywhere;word-break:break-word">{{E(email)}}</p>
        <p style="margin:0 0 3px;color:#68707d;font-size:12px;line-height:18px">Role</p><p style="margin:0;font-weight:bold">Tenant Admin</p>
        </td></tr></table>
        <h2 style="margin:26px 0 14px;font-size:18px;line-height:26px">Get started in four steps</h2>
        <table role="presentation" width="100%" border="0" cellspacing="0" cellpadding="0" style="font-size:14px;line-height:22px">
        <tr><td valign="top" width="28" style="padding:0 0 12px;color:#9a4100;font-weight:bold">1.</td><td style="padding:0 0 12px">Install the ONEVERZ app provided by your administrator, if you have not already.</td></tr>
        <tr><td valign="top" width="28" style="padding:0 0 12px;color:#9a4100;font-weight:bold">2.</td><td style="padding:0 0 12px">Select <strong>Activate account</strong> below to open your secure invitation.</td></tr>
        <tr><td valign="top" width="28" style="padding:0 0 12px;color:#9a4100;font-weight:bold">3.</td><td style="padding:0 0 12px">Enter your <strong>New Password</strong> and <strong>Confirm Password</strong> to complete activation.</td></tr>
        <tr><td valign="top" width="28" style="padding:0;color:#9a4100;font-weight:bold">4.</td><td style="padding:0">Sign in with the email above and your new password.</td></tr></table>
        <table role="presentation" border="0" cellspacing="0" cellpadding="0" style="margin:26px 0 18px"><tr><td align="center" bgcolor="#c44c00" style="border-radius:7px;mso-padding-alt:15px 26px"><a href="{{E(setupUrl)}}" style="display:inline-block;padding:15px 26px;border:1px solid #c44c00;border-radius:7px;color:#ffffff;font-size:15px;line-height:22px;font-weight:bold;text-decoration:none">Activate account</a></td></tr></table>
        <p style="margin:0 0 24px;color:#68707d;font-size:13px;line-height:21px">Installing the app now? Return to this email after installation and select <strong>Activate account</strong>.</p>
        <p style="margin:0 0 24px;padding-top:20px;border-top:1px solid #e8ebef;font-size:13px;line-height:22px"><strong>Invitation expires</strong><br>{{E(expiry)}}</p>
        <p style="margin:0 0 24px;color:#68707d;font-size:13px;line-height:21px">This invitation is personal to you. Do not share it. If it expires, contact your organization administrator for a new invitation. If you were not expecting this email, you can safely ignore it.</p>
        <p style="margin:0">Best regards,<br><strong>The ONEVERZ Team</strong></p></td></tr>
        <tr><td align="left" style="padding:18px 28px;background:#f8f9fb;border-top:1px solid #e8ebef;color:#68707d;font-size:12px;line-height:19px">This is an automated account invitation. For assistance, contact your organization administrator.</td></tr>
        </table></td></tr></table></body></html>
        """;
        var text = $"ONEVERZ — {company}\nTenant Admin invitation\nLogin email: {email}\n\n" +
            "Install the ONEVERZ app provided by your administrator. Open the activation link, set New Password and Confirm Password, then log in with your email and new password. After installing the app, reopen this invitation.\n\n" +
            $"Activate your account: {setupUrl}\nExpires: {expiry}\nDo not share this link. Contact your administrator if it expires. Ignore this invitation if unexpected.\nONEVERZ";
        return new ApplicationEmailMessage(email, subject, html, text, correlationId);
    }
}
