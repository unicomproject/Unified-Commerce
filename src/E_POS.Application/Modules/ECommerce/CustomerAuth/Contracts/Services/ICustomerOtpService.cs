using System.Net;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;

public interface ICustomerOtpService
{
    (CustomerVerificationOtp VerificationOtp, string RawCode) CreateEmailVerificationOtp(
        Guid tenantId,
        Guid customerId,
        string email,
        string normalizedEmail,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now);

    string HashOtp(Guid tenantId, string normalizedEmail, string purpose, string rawCode);
    bool SecureEquals(string? left, string right);
}
