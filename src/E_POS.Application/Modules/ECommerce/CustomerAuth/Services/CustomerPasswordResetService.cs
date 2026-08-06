using System.Net;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Services.Support;
using E_POS.Domain.Modules.ECommerce.Customer.Entities;
using CustomerEntity = E_POS.Domain.Modules.ECommerce.Customer.Entities.Customer;

namespace E_POS.Application.Modules.ECommerce.CustomerAuth.Services;

public sealed class CustomerPasswordResetService : ICustomerPasswordResetService
{
    private readonly ICustomerPasswordResetRepository _repository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITokenHashService _tokenHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICustomerPasswordResetLinkBuilder _passwordResetLinkBuilder;
    private readonly ICustomerAuthValidator _validator;
    private readonly ICustomerAuthEmailService _emailService;
    private readonly ICustomerTokenFactory _tokenFactory;
    private readonly CustomerJwtSettings _jwtSettings;

    public CustomerPasswordResetService(
        ICustomerPasswordResetRepository repository,
        IPasswordHashService passwordHashService,
        ITokenHashService tokenHashService,
        IDateTimeProvider dateTimeProvider,
        ICustomerPasswordResetLinkBuilder passwordResetLinkBuilder,
        ICustomerAuthValidator validator,
        ICustomerAuthEmailService emailService,
        ICustomerTokenFactory tokenFactory,
        CustomerJwtSettings jwtSettings)
    {
        _repository = repository;
        _passwordHashService = passwordHashService;
        _tokenHashService = tokenHashService;
        _dateTimeProvider = dateTimeProvider;
        _passwordResetLinkBuilder = passwordResetLinkBuilder;
        _validator = validator;
        _emailService = emailService;
        _tokenFactory = tokenFactory;
        _jwtSettings = jwtSettings;
    }

    public async Task<ApplicationResult> ForgotPasswordAsync(
        Guid tenantId,
        CustomerForgotPasswordRequest request,
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

        if (account is null ||
            !_validator.IsCustomerActive(account.CustomerStatus) ||
            string.Equals(account.Account.Status, "DELETED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(account.Account.Status, "DISABLED", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult.Success();
        }

        var now = _dateTimeProvider.UtcNow;
        var rawToken = _tokenFactory.CreateSecureToken();
        var resetToken = CustomerPasswordResetToken.Create(
            Guid.NewGuid(),
            tenantId,
            account.Account.Id,
            _tokenHashService.HashToken(rawToken, _jwtSettings.SigningKey),
            now.AddMinutes(CustomerAuthConstants.PasswordResetTokenMinutes),
            now,
            ipAddress,
            userAgent);

        await _repository.SavePasswordResetTokenAsync(
            resetToken,
            now,
            cancellationToken);

        var resetUrl = _passwordResetLinkBuilder.BuildResetUrl(email, rawToken);
        return await _emailService.SendPasswordResetMessageAsync(
            email,
            account.DisplayName,
            resetUrl,
            resetToken.ExpiresAt,
            account.CustomerId,
            cancellationToken);
    }

    public async Task<ApplicationResult> ResetPasswordAsync(
        Guid tenantId,
        CustomerResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = _validator.ValidateResetPassword(tenantId, request);
        if (validationError is not null)
            return ApplicationResult.Failure(validationError);

        var email = _validator.NormalizeEmailAddress(request.Email);
        var normalizedEmail = CustomerEntity.NormalizeEmail(email)!;
        var tokenHash = _tokenHashService.HashToken(request.Token.Trim(), _jwtSettings.SigningKey);
        var context = await _repository.FindActivePasswordResetAsync(
            tenantId,
            normalizedEmail,
            tokenHash,
            cancellationToken);
        if (context is null)
            return ApplicationResult.Failure(CustomerAuthErrors.InvalidResetToken);

        var now = _dateTimeProvider.UtcNow;
        if (!_validator.IsTenantActive(context.TenantStatus) || !_validator.IsCustomerActive(context.CustomerStatus))
            return ApplicationResult.Failure(CustomerAuthErrors.TenantAccessDenied);

        if (!context.ResetToken.IsActive(now))
            return ApplicationResult.Failure(CustomerAuthErrors.InvalidResetToken);

        context.ResetToken.Use(now);
        context.Account.SetPasswordHash(_passwordHashService.HashPassword(request.NewPassword), now);
        context.Account.MarkEmailVerified(now);
        await _repository.SavePasswordResetAsync(
            context.ResetToken,
            context.Account,
            now,
            cancellationToken);

        return ApplicationResult.Success();
    }
}
