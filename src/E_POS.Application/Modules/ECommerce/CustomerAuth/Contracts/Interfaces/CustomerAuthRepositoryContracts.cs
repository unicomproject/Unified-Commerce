using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;

public sealed record CustomerExternalLoginAccount(
    CustomerLoginAccount Account,
    CustomerExternalAuthAccount ExternalAccount);

public sealed record CustomerLoginAccount(
    CustomerAuthAccount Account,
    Guid CustomerId,
    Guid TenantId,
    string DisplayName,
    string? Email,
    string? Phone,
    string CustomerStatus,
    string TenantStatus);

public sealed record CustomerEmailVerificationContext(
    CustomerVerificationOtp VerificationOtp,
    CustomerAuthAccount Account,
    string? Email,
    string DisplayName,
    string CustomerStatus,
    string TenantStatus);

public sealed record CustomerPasswordResetContext(
    CustomerPasswordResetToken ResetToken,
    CustomerAuthAccount Account,
    string? Email,
    string DisplayName,
    string CustomerStatus,
    string TenantStatus);

public enum CustomerRefreshRotationStatus
{
    Succeeded,
    Invalid,
    Reused,
    AccountUnavailable
}

public sealed record CustomerRefreshRotationResult(
    CustomerRefreshRotationStatus Status,
    CustomerLoginAccount? Account,
    Guid? SessionId);