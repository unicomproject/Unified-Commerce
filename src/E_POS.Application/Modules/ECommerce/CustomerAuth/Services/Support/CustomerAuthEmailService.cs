using System.Net;
using E_POS.Application.Common.Email;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;

public sealed class CustomerAuthEmailService : ICustomerAuthEmailService
{
    private readonly IApplicationEmailSender _emailSender;

    public CustomerAuthEmailService(IApplicationEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public bool IsConfigured => _emailSender.IsConfigured;

    public async Task<ApplicationResult> SendEmailVerificationMessageAsync(
        string email,
        string displayName,
        string rawCode,
        DateTimeOffset expiresAt,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? email : displayName);
        var safeCode = WebUtility.HtmlEncode(rawCode);
        var message = new ApplicationEmailMessage(
            email,
            "Verify your OneVerz email",
            $"<p>Hello {safeName},</p><p>Use this 6-digit code to verify your email:</p><p><strong style='font-size:24px;letter-spacing:4px'>{safeCode}</strong></p><p>This code expires at {expiresAt:yyyy-MM-dd HH:mm} UTC.</p><p>If you did not create this account, you can ignore this email.</p>".Trim(),
            $"Your OneVerz verification code is {rawCode}. It expires at {expiresAt:yyyy-MM-dd HH:mm} UTC.",
            correlationId.ToString("D"));

        var sendResult = await _emailSender.SendAsync(message, cancellationToken);
        return sendResult.IsSuccess
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(CustomerAuthErrors.EmailDeliveryUnavailable);
    }

    public async Task<ApplicationResult> SendPasswordResetMessageAsync(
        string email,
        string displayName,
        string resetUrl,
        DateTimeOffset expiresAt,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? email : displayName);
        var safeResetUrl = WebUtility.HtmlEncode(resetUrl);
        var message = new ApplicationEmailMessage(
            email,
            "Reset your OneVerz password",
            $"<p>Hello {safeName},</p><p>Use the secure link below to reset your password.</p><p><a href='{safeResetUrl}'>Reset password</a></p><p>This link expires at {expiresAt:yyyy-MM-dd HH:mm} UTC.</p><p>If you did not request this, you can ignore this email.</p>".Trim(),
            $"Reset your OneVerz password: {resetUrl} This link expires at {expiresAt:yyyy-MM-dd HH:mm} UTC.",
            correlationId.ToString("D"));

        var sendResult = await _emailSender.SendAsync(message, cancellationToken);
        return sendResult.IsSuccess
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(CustomerAuthErrors.EmailDeliveryUnavailable);
    }
}
