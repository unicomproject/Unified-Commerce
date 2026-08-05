using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerEmailVerificationService : ICustomerEmailVerificationService
{
    private readonly ICustomerEmailVerificationRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICustomerAuthValidator _validator;
    private readonly ICustomerOtpService _otpService;
    private readonly ICustomerAuthEmailService _emailService;

    public CustomerEmailVerificationService(
        ICustomerEmailVerificationRepository repository,
        IDateTimeProvider dateTimeProvider,
        ICustomerAuthValidator validator,
        ICustomerOtpService otpService,
        ICustomerAuthEmailService emailService)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
        _otpService = otpService;
        _emailService = emailService;
    }

    public async Task<ApplicationResult> VerifyEmailAsync(
        Guid tenantId,
        CustomerVerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = _validator.ValidateVerifyEmail(tenantId, request);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);

        var email = _validator.NormalizeEmailAddress(request.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var context = await _repository.FindPendingEmailVerificationAsync(
            tenantId,
            normalizedEmail,
            cancellationToken);
        if (context is null)
            return ApplicationResult.Failure(CustomerAuthErrors.InvalidVerificationCode);

        var now = _dateTimeProvider.UtcNow;
        if (!_validator.IsTenantActive(context.TenantStatus) || !_validator.IsCustomerActive(context.CustomerStatus))
            return ApplicationResult.Failure(CustomerAuthErrors.TenantAccessDenied);

        if (!context.VerificationOtp.IsPending(now))
        {
            if (context.VerificationOtp.ExpiresAt <= now)
                context.VerificationOtp.MarkExpired(now);
            else
                context.VerificationOtp.Invalidate(now);

            await _repository.SaveEmailVerificationAsync(
                context.VerificationOtp,
                context.Account,
                cancellationToken);
            return ApplicationResult.Failure(CustomerAuthErrors.InvalidVerificationCode);
        }

        var expectedHash = _otpService.HashOtp(tenantId, normalizedEmail, "EMAIL_VERIFY", request.Code.Trim());
        if (!_otpService.SecureEquals(context.VerificationOtp.OtpHash, expectedHash))
        {
            context.VerificationOtp.RecordFailedAttempt(now);
            await _repository.SaveEmailVerificationAsync(
                context.VerificationOtp,
                context.Account,
                cancellationToken);
            return ApplicationResult.Failure(CustomerAuthErrors.InvalidVerificationCode);
        }

        context.VerificationOtp.MarkVerified(now);
        context.Account.MarkEmailVerified(now);
        await _repository.SaveEmailVerificationAsync(
            context.VerificationOtp,
            context.Account,
            cancellationToken);

        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult> ResendEmailVerificationAsync(
        Guid tenantId,
        CustomerResendEmailVerificationRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var validationError = _validator.ValidateEmailOnly(tenantId, request.Email);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);

        if (!_emailService.IsConfigured)
            return ApplicationResult.Failure(CustomerAuthErrors.EmailDeliveryUnavailable);

        if (!await _repository.TenantIsActiveAsync(tenantId, cancellationToken))
            return ApplicationResult.Failure(CustomerAuthErrors.TenantAccessDenied);

        var email = _validator.NormalizeEmailAddress(request.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var account = await _repository.FindAccountByEmailAsync(
            tenantId,
            normalizedEmail,
            trackAccount: false,
            cancellationToken);

        if (account is null || account.Account.EmailVerifiedAt.HasValue)
            return ApplicationResult.Success();

        return await SendVerificationForExistingAccountAsync(
            account,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    public async Task<ApplicationResult> SendVerificationForExistingAccountAsync(
        CustomerLoginAccount account,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (!_validator.IsTenantActive(account.TenantStatus) || !_validator.IsCustomerActive(account.CustomerStatus))
            return ApplicationResult.Failure(CustomerAuthErrors.TenantAccessDenied);

        if (string.IsNullOrWhiteSpace(account.Email))
            return ApplicationResult.Failure(CustomerAuthErrors.EmailDeliveryUnavailable);

        var now = _dateTimeProvider.UtcNow;
        var email = _validator.NormalizeEmailAddress(account.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var (verificationOtp, rawCode) = _otpService.CreateEmailVerificationOtp(
            account.TenantId,
            account.CustomerId,
            email,
            normalizedEmail,
            ipAddress,
            userAgent,
            now);

        await _repository.SaveEmailVerificationOtpAsync(
            verificationOtp,
            now,
            cancellationToken);

        return await _emailService.SendEmailVerificationMessageAsync(
            email,
            account.DisplayName,
            rawCode,
            verificationOtp.ExpiresAt,
            account.CustomerId,
            cancellationToken);
    }
}
