using E_POS.Application.Common.Models;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerAuthEmailService
{
    bool IsConfigured { get; }

    Task<ApplicationResult> SendEmailVerificationMessageAsync(
        string email,
        string displayName,
        string rawCode,
        DateTimeOffset expiresAt,
        Guid correlationId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> SendPasswordResetMessageAsync(
        string email,
        string displayName,
        string resetUrl,
        DateTimeOffset expiresAt,
        Guid correlationId,
        CancellationToken cancellationToken);
}
