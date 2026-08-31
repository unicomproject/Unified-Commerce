using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerOtpAuthService : ICustomerOtpAuthService
{
    private readonly ICustomerRegistrationRepository _registrationRepository;
    private readonly ICustomerEmailVerificationService _emailVerificationService;
    private readonly ICustomerLoginRepository _loginRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly ICustomerAuthValidator _validator;
    private readonly ICustomerOtpService _otpService;
    private readonly ICustomerConsentFactory _consentFactory;
    private readonly ICustomerAuthEmailService _emailService;
    private readonly ICustomerTokenFactory _tokenFactory;

    public CustomerOtpAuthService(
        ICustomerRegistrationRepository registrationRepository,
        ICustomerLoginRepository loginRepository,
        IDateTimeProvider dateTimeProvider,
        ICodeSequenceRepository codeSequenceRepository,
        ICustomerAuthValidator validator,
        ICustomerOtpService otpService,
        ICustomerConsentFactory consentFactory,
        ICustomerAuthEmailService emailService,
        ICustomerTokenFactory tokenFactory,
        ICustomerEmailVerificationService emailVerificationService)
    {
        _registrationRepository = registrationRepository;
        _loginRepository = loginRepository;
        _dateTimeProvider = dateTimeProvider;
        _codeSequenceRepository = codeSequenceRepository;
        _validator = validator;
        _otpService = otpService;
        _consentFactory = consentFactory;
        _emailService = emailService;
        _tokenFactory = tokenFactory;
        _emailVerificationService = emailVerificationService;
    }

    public async Task<ApplicationResult> RequestOtpAsync(
        Guid tenantId,
        CustomerRequestOtpRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        if (!_emailService.IsConfigured)
            return ApplicationResult.Failure(CustomerAuthErrors.EmailDeliveryUnavailable);

        if (!await _registrationRepository.TenantIsActiveAsync(tenantId, cancellationToken))
            return ApplicationResult.Failure(CustomerAuthErrors.TenantAccessDenied);

        var email = _validator.NormalizeEmailAddress(request.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var existingAccount = await _registrationRepository.FindAccountByEmailAsync(
            tenantId,
            normalizedEmail,
            trackAccount: false,
            cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        Guid customerId;
        CustomerVerificationOtp verificationOtp;
        string rawCode;
        string customerName;

        if (existingAccount is not null)
        {
            // Existing customer: Generate new OTP for login
            return await _emailVerificationService.SendVerificationForExistingAccountAsync(
                existingAccount,
                ipAddress,
                userAgent,
                cancellationToken);
        }
        else
        {
            // New customer: Create pending account and send OTP
            customerId = Guid.NewGuid();
            var customerCode = await _codeSequenceRepository.GetNextCodeAsync(
                tenantId,
                CustomerAuthConstants.CustomerCodeSequenceKey,
                CustomerAuthConstants.CustomerCodePrefix,
                CustomerAuthConstants.CustomerCodePaddingLength,
                now,
                cancellationToken);
            
            var customer = CustomerEntity.CreateECommerceCustomer(
                customerId,
                tenantId,
                customerCode,
                email,
                null,
                null,
                now);
            customerName = customer.Name;
            
            var account = CustomerAuthAccount.CreateExternal(
                Guid.NewGuid(),
                tenantId,
                customerId,
                now); // Created without password
                
            var otpResult = _otpService.CreateEmailVerificationOtp(
                tenantId,
                customerId,
                email,
                normalizedEmail,
                ipAddress,
                userAgent,
                now);
            verificationOtp = otpResult.VerificationOtp;
            rawCode = otpResult.RawCode;

            var consents = _consentFactory.CreateRegistrationConsents(
                tenantId,
                customerId,
                false,
                ipAddress,
                userAgent,
                now);

            var saved = await _registrationRepository.RegisterCustomerAsync(
                customer,
                account,
                verificationOtp,
                consents,
                cancellationToken);
                
            if (!saved)
                return ApplicationResult.Failure(CustomerAuthErrors.DuplicateEmail);
        }

        return await _emailService.SendEmailVerificationMessageAsync(
            email,
            customerName,
            rawCode,
            verificationOtp.ExpiresAt,
            customerId,
            cancellationToken);
    }

    public async Task<ApplicationResult<CustomerAuthTokenResult>> VerifyOtpAsync(
        Guid tenantId,
        CustomerVerifyOtpRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var verifyResult = await _emailVerificationService.VerifyEmailAsync(
            tenantId,
            new CustomerVerifyEmailRequest { Email = request.Email, Code = request.Code },
            cancellationToken);

        if (!verifyResult.IsSuccess)
        {
            return ApplicationResult<CustomerAuthTokenResult>.Failure(verifyResult.Error);
        }

        var identifier = request.Email.Trim();
        var normalizedEmail = CustomerEntity.NormalizeEmail(identifier) ?? string.Empty;
        var account = await _loginRepository.FindLoginAccountAsync(
            tenantId,
            normalizedEmail,
            string.Empty,
            cancellationToken);
            
        var now = _dateTimeProvider.UtcNow;
        if (account is null || account.Account.IsLocked(now))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.InvalidCredentials);

        var accountStatusAllowed =
            string.Equals(account.Account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(account.Account.Status, "LOCKED", StringComparison.OrdinalIgnoreCase) &&
             account.Account.LockedUntil.HasValue && account.Account.LockedUntil <= now);
             
        if (!accountStatusAllowed || !_validator.IsCustomerActive(account.CustomerStatus))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.InvalidCredentials);

        account.Account.RecordSuccessfulLogin(now);
        
        var login = _tokenFactory.CreateLoginPersistence(
            account,
            request.DeviceName,
            request.RememberMe,
            ipAddress,
            userAgent,
            now);

        await _loginRepository.SaveSuccessfulLoginAsync(
            account.Account,
            login.Session,
            login.RefreshToken,
            cancellationToken);

        return ApplicationResult<CustomerAuthTokenResult>.Success(login.TokenResult);
    }
}
