using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerGoogleAuthService : ICustomerGoogleAuthService
{
    private readonly ICustomerExternalAuthRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICodeSequenceRepository _codeSequenceRepository;
    private readonly IGoogleIdentityVerifier _googleIdentityVerifier;
    private readonly ICustomerAuthValidator _validator;
    private readonly ICustomerTokenFactory _tokenFactory;
    private readonly ICustomerConsentFactory _consentFactory;

    public CustomerGoogleAuthService(
        ICustomerExternalAuthRepository repository,
        IDateTimeProvider dateTimeProvider,
        ICodeSequenceRepository codeSequenceRepository,
        ICustomerAuthValidator validator,
        ICustomerTokenFactory tokenFactory,
        ICustomerConsentFactory consentFactory,
        IGoogleIdentityVerifier? googleIdentityVerifier = null)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _codeSequenceRepository = codeSequenceRepository;
        _validator = validator;
        _tokenFactory = tokenFactory;
        _consentFactory = consentFactory;
        _googleIdentityVerifier = googleIdentityVerifier ?? new DisabledGoogleIdentityVerifier();
    }

    public async Task<ApplicationResult<CustomerAuthTokenResult>> GoogleLoginAsync(
        Guid tenantId,
        CustomerGoogleLoginRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var validationError = _validator.ValidateGoogleLogin(tenantId, request);
        if (validationError is not null)
            return ApplicationResult<CustomerAuthTokenResult>.Failure(validationError);

        if (!await _repository.TenantIsActiveAsync(tenantId, cancellationToken))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.TenantAccessDenied);

        var identityResult = await _googleIdentityVerifier.VerifyAsync(
            request.IdToken,
            cancellationToken);
        if (identityResult.IsFailure || identityResult.Value is null)
            return ApplicationResult<CustomerAuthTokenResult>.Failure(identityResult.Error);

        var identity = identityResult.Value;
        if (!identity.EmailVerified)
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.GoogleEmailNotVerified);

        var email = _validator.NormalizeEmailAddress(identity.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(identity.Subject))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.InvalidGoogleToken);

        var now = _dateTimeProvider.UtcNow;
        var externalLogin = await _repository.FindExternalLoginAccountAsync(
            tenantId,
            CustomerExternalAuthAccount.GoogleProviderCode,
            identity.Subject,
            trackAccount: true,
            trackExternalAccount: true,
            cancellationToken);

        if (externalLogin is not null)
        {
            return await SignInExternalAccountAsync(
                externalLogin,
                identity,
                request.DeviceName,
                request.RememberMe,
                ipAddress,
                userAgent,
                now,
                cancellationToken);
        }

        var existingAccount = await _repository.FindAccountByEmailAsync(
            tenantId,
            normalizedEmail,
            trackAccount: true,
            cancellationToken);

        if (existingAccount is not null)
        {
            var accountError = _validator.ValidateExternalLoginAccount(existingAccount, now);
            if (accountError is not null)
                return ApplicationResult<CustomerAuthTokenResult>.Failure(accountError);

            var externalAccount = CustomerExternalAuthAccount.Create(
                Guid.NewGuid(),
                tenantId,
                existingAccount.Account.Id,
                CustomerExternalAuthAccount.GoogleProviderCode,
                identity.Subject,
                email,
                identity.EmailVerified,
                now);
            externalAccount.RecordSuccessfulLogin(email, identity.EmailVerified, now);
            existingAccount.Account.MarkEmailVerified(now);
            existingAccount.Account.RecordSuccessfulLogin(now);
            var login = _tokenFactory.CreateLoginPersistence(
                existingAccount,
                request.DeviceName,
                request.RememberMe,
                ipAddress,
                userAgent,
                now);

            var linked = await _repository.LinkExternalAccountAndSaveLoginAsync(
                existingAccount.Account,
                externalAccount,
                login.Session,
                login.RefreshToken,
                cancellationToken);
            return linked
                ? ApplicationResult<CustomerAuthTokenResult>.Success(login.TokenResult)
                : ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.ExternalAccountConflict);
        }

        if (!request.AgreeTerms)
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.TermsRequired);

        var customerId = Guid.NewGuid();
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
            identity.GivenName,
            identity.FamilyName,
            now);
        var account = CustomerAuthAccount.CreateExternal(
            Guid.NewGuid(),
            tenantId,
            customerId,
            now);
        account.RecordSuccessfulLogin(now);
        var newLoginAccount = new CustomerLoginAccount(
            account,
            customer.Id,
            customer.TenantId,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Status,
            "active");
        var newExternalAccount = CustomerExternalAuthAccount.Create(
            Guid.NewGuid(),
            tenantId,
            account.Id,
            CustomerExternalAuthAccount.GoogleProviderCode,
            identity.Subject,
            email,
            identity.EmailVerified,
            now);
        newExternalAccount.RecordSuccessfulLogin(email, identity.EmailVerified, now);
        var consents = _consentFactory.CreateRegistrationConsents(
            tenantId,
            customerId,
            request.SendOffers,
            ipAddress,
            userAgent,
            now);
        var newLogin = _tokenFactory.CreateLoginPersistence(
            newLoginAccount,
            request.DeviceName,
            request.RememberMe,
            ipAddress,
            userAgent,
            now);

        var saved = await _repository.RegisterExternalCustomerAsync(
            customer,
            account,
            newExternalAccount,
            consents,
            newLogin.Session,
            newLogin.RefreshToken,
            cancellationToken);

        return saved
            ? ApplicationResult<CustomerAuthTokenResult>.Success(newLogin.TokenResult)
            : ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.DuplicateEmail);
    }

    private async Task<ApplicationResult<CustomerAuthTokenResult>> SignInExternalAccountAsync(
        CustomerExternalLoginAccount externalLogin,
        GoogleIdentityResult identity,
        string? deviceName,
        bool rememberMe,
        IPAddress? ipAddress,
        string? userAgent,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(externalLogin.ExternalAccount.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.InvalidCredentials);

        var accountError = _validator.ValidateExternalLoginAccount(externalLogin.Account, now);
        if (accountError is not null)
            return ApplicationResult<CustomerAuthTokenResult>.Failure(accountError);

        externalLogin.ExternalAccount.RecordSuccessfulLogin(
            identity.Email,
            identity.EmailVerified,
            now);
        externalLogin.Account.Account.RecordSuccessfulLogin(now);
        var login = _tokenFactory.CreateLoginPersistence(
            externalLogin.Account,
            deviceName,
            rememberMe,
            ipAddress,
            userAgent,
            now);

        await _repository.SaveSuccessfulExternalLoginAsync(
            externalLogin.Account.Account,
            externalLogin.ExternalAccount,
            login.Session,
            login.RefreshToken,
            cancellationToken);

        return ApplicationResult<CustomerAuthTokenResult>.Success(login.TokenResult);
    }

    private sealed class DisabledGoogleIdentityVerifier : IGoogleIdentityVerifier
    {
        private static readonly ApplicationError NotConfigured = new(
            "customer_auth.google_not_configured",
            "Google sign-in is not configured.");

        public Task<ApplicationResult<GoogleIdentityResult>> VerifyAsync(
            string idToken,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<GoogleIdentityResult>.Failure(NotConfigured));
    }
}
