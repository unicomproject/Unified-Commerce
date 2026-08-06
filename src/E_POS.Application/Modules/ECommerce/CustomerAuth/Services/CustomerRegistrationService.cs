using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerRegistrationService : ICustomerRegistrationService
{
    private readonly ICustomerRegistrationRepository _repository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly ICustomerAuthValidator _validator;
    private readonly ICustomerEmailVerificationService _emailVerificationService;
    private readonly ICustomerOtpService _otpService;
    private readonly ICustomerConsentFactory _consentFactory;
    private readonly ICustomerAuthEmailService _emailService;

    public CustomerRegistrationService(
        ICustomerRegistrationRepository repository,
        IPasswordHashService passwordHashService,
        IDateTimeProvider dateTimeProvider,
        ICodeSequenceRepository codeSequenceRepository,
        ICustomerAuthValidator validator,
        ICustomerEmailVerificationService emailVerificationService,
        ICustomerOtpService otpService,
        ICustomerConsentFactory consentFactory,
        ICustomerAuthEmailService emailService)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
        _dateTimeProvider = dateTimeProvider;
        _codeSequenceRepository = codeSequenceRepository;
        _validator = validator;
        _emailVerificationService = emailVerificationService;
        _otpService = otpService;
        _consentFactory = consentFactory;
        _emailService = emailService;
    }

    public async Task<ApplicationResult> RegisterAsync(
        Guid tenantId,
        CustomerRegisterRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var validationError = _validator.ValidateRegister(tenantId, request);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);

        if (!_emailService.IsConfigured)
            return ApplicationResult.Failure(Support.CustomerAuthErrors.EmailDeliveryUnavailable);

        if (!await _repository.TenantIsActiveAsync(tenantId, cancellationToken))
            return ApplicationResult.Failure(Support.CustomerAuthErrors.TenantAccessDenied);

        var email = _validator.NormalizeEmailAddress(request.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var existingAccount = await _repository.FindAccountByEmailAsync(
            tenantId,
            normalizedEmail,
            trackAccount: false,
            cancellationToken);

        if (existingAccount is not null)
        {
            if (existingAccount.Account.EmailVerifiedAt.HasValue)
                return ApplicationResult.Failure(Support.CustomerAuthErrors.DuplicateEmail);

            return await _emailVerificationService.SendVerificationForExistingAccountAsync(
                existingAccount,
                ipAddress,
                userAgent,
                cancellationToken);
        }

        var now = _dateTimeProvider.UtcNow;
        var customerId = Guid.NewGuid();
        var customerCode = await _codeSequenceRepository.GetNextCodeAsync(
            tenantId,
            Support.CustomerAuthConstants.CustomerCodeSequenceKey,
            Support.CustomerAuthConstants.CustomerCodePrefix,
            Support.CustomerAuthConstants.CustomerCodePaddingLength,
            now,
            cancellationToken);
        var customer = CustomerEntity.CreateECommerceCustomer(
            customerId,
            tenantId,
            customerCode,
            email,
            request.FirstName,
            request.LastName,
            now);
        var account = CustomerAuthAccount.Create(
            Guid.NewGuid(),
            tenantId,
            customerId,
            _passwordHashService.HashPassword(request.Password),
            now);
        var (verificationOtp, rawCode) = _otpService.CreateEmailVerificationOtp(
            tenantId,
            customerId,
            email,
            normalizedEmail,
            ipAddress,
            userAgent,
            now);
        var consents = _consentFactory.CreateRegistrationConsents(
            tenantId,
            customerId,
            request.SendOffers,
            ipAddress,
            userAgent,
            now);

        var saved = await _repository.RegisterCustomerAsync(
            customer,
            account,
            verificationOtp,
            consents,
            cancellationToken);
        if (!saved)
            return ApplicationResult.Failure(Support.CustomerAuthErrors.DuplicateEmail);

        return await _emailService.SendEmailVerificationMessageAsync(
            email,
            customer.Name,
            rawCode,
            verificationOtp.ExpiresAt,
            customerId,
            cancellationToken);
    }
}
