using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerLoginService : ICustomerLoginService
{
    private readonly ICustomerLoginRepository _repository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICustomerAuthValidator _validator;
    private readonly ICustomerTokenFactory _tokenFactory;

    public CustomerLoginService(
        ICustomerLoginRepository repository,
        IPasswordHashService passwordHashService,
        IDateTimeProvider dateTimeProvider,
        ICustomerAuthValidator validator,
        ICustomerTokenFactory tokenFactory)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
        _tokenFactory = tokenFactory;
    }

    public async Task<ApplicationResult<CustomerAuthTokenResult>> LoginAsync(
        Guid tenantId,
        CustomerLoginRequest request,
        IPAddress? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var validationError = _validator.ValidateLogin(tenantId, request);
        if (validationError is not null)
            return ApplicationResult<CustomerAuthTokenResult>.Failure(validationError);

        var identifier = request.EmailOrPhone.Trim();
        var isEmail = identifier.Contains('@', StringComparison.Ordinal);
        var account = await _repository.FindLoginAccountAsync(
            tenantId,
            isEmail ? CustomerEntity.NormalizeEmail(identifier) ?? string.Empty : string.Empty,
            isEmail ? string.Empty : CustomerEntity.NormalizePhone(identifier),
            cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        if (account is null || account.Account.IsLocked(now))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.InvalidCredentials);

        var accountStatusAllowed =
            string.Equals(account.Account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(account.Account.Status, "LOCKED", StringComparison.OrdinalIgnoreCase) &&
             account.Account.LockedUntil.HasValue && account.Account.LockedUntil <= now);
        if (!accountStatusAllowed ||
            !_validator.IsCustomerActive(account.CustomerStatus) ||
            string.IsNullOrWhiteSpace(account.Account.PasswordHash))
        {
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.InvalidCredentials);
        }

        if (isEmail && !account.Account.EmailVerifiedAt.HasValue)
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.EmailNotVerified);

        if (!_passwordHashService.VerifyPassword(request.Password, account.Account.PasswordHash))
        {
            account.Account.RecordFailedLogin(now, CustomerAuthConstants.MaxFailedAttempts, CustomerAuthConstants.LockDuration);
            await _repository.SaveFailedLoginAsync(account.Account, cancellationToken);
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.InvalidCredentials);
        }

        if (!_validator.IsTenantActive(account.TenantStatus))
            return ApplicationResult<CustomerAuthTokenResult>.Failure(CustomerAuthErrors.TenantAccessDenied);

        account.Account.RecordSuccessfulLogin(now);
        var login = _tokenFactory.CreateLoginPersistence(
            account,
            request.DeviceName,
            request.RememberMe,
            ipAddress,
            userAgent,
            now);

        await _repository.SaveSuccessfulLoginAsync(
            account.Account,
            login.Session,
            login.RefreshToken,
            cancellationToken);

        return ApplicationResult<CustomerAuthTokenResult>.Success(login.TokenResult);
    }
}
