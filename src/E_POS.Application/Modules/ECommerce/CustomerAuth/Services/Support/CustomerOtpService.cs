using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;

public sealed class CustomerOtpService : ICustomerOtpService
{
    private readonly ITokenHashService _tokenHashService;
    private readonly CustomerJwtSettings _jwtSettings;

    public CustomerOtpService(ITokenHashService tokenHashService, CustomerJwtSettings jwtSettings)
    {
        _tokenHashService = tokenHashService;
        _jwtSettings = jwtSettings;
    }

    public (CustomerVerificationOtp VerificationOtp, string RawCode) CreateEmailVerificationOtp(
        Guid tenantId,
        Guid customerId,
        string email,
        string normalizedEmail,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now)
    {
        var rawCode = CreateNumericCode();
        return (
            CustomerVerificationOtp.Create(
                Guid.NewGuid(),
                tenantId,
                customerId,
                "EMAIL_VERIFY",
                "EMAIL",
                email,
                normalizedEmail,
                HashOtp(tenantId, normalizedEmail, "EMAIL_VERIFY", rawCode),
                CustomerAuthConstants.VerificationOtpMaxAttempts,
                now,
                now.AddMinutes(CustomerAuthConstants.VerificationOtpMinutes),
                ipAddress,
                userAgent),
            rawCode);
    }

    public string HashOtp(Guid tenantId, string normalizedEmail, string purpose, string rawCode) =>
        _tokenHashService.HashToken(
            $"{tenantId:N}:{normalizedEmail}:{purpose}:{rawCode}",
            _jwtSettings.SigningKey);

    public bool SecureEquals(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string CreateNumericCode() =>
        RandomNumberGenerator.GetInt32(0, 10_000).ToString("D4", CultureInfo.InvariantCulture);
}
