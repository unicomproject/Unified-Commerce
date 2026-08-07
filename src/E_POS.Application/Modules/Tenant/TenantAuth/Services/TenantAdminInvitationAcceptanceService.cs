using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Validators;
using E_POS.Application.Modules.Tenant.TenantAuth.Contracts;
using E_POS.Application.Modules.Tenant.TenantAuth.Dtos;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.TenantAuth.Constants;
using E_POS.Domain.Modules.Tenant.TenantFoundation.Constants;
using Microsoft.Extensions.Logging;

namespace E_POS.Application.Modules.Tenant.TenantAuth.Services;

public sealed class TenantAdminInvitationAcceptanceService : ITenantAdminInvitationAcceptanceService
{
    public const string ErrorInviteInvalid = "INVITE_INVALID";
    public const string ErrorInviteExpired = "INVITE_EXPIRED";
    public const string ErrorInviteCancelled = "INVITE_CANCELLED";
    public const string ErrorInviteUsed = "INVITE_USED";
    public const string ErrorTenantNotOperational = "TENANT_NOT_OPERATIONAL";
    public const string ErrorPasswordInvalid = "PASSWORD_INVALID";
    public const string ErrorPasswordMismatch = "PASSWORD_MISMATCH";

    private static readonly ApplicationError InviteInvalid =
        new(ErrorInviteInvalid, "This invitation link is invalid or no longer available.");
    private static readonly ApplicationError InviteExpired =
        new(ErrorInviteExpired, "This invitation link has expired.");
    private static readonly ApplicationError InviteCancelled =
        new(ErrorInviteCancelled, "This invitation link has been cancelled.");
    private static readonly ApplicationError InviteUsed =
        new(ErrorInviteUsed, "This invitation link has already been used.");
    private static readonly ApplicationError TenantNotOperational =
        new(ErrorTenantNotOperational, "This tenant is not available for account setup.");
    private static readonly ApplicationError PasswordMismatch =
        new(ErrorPasswordMismatch, "Password and confirmation do not match.");

    private readonly ITenantAdminInvitationAcceptanceRepository _repository;
    private readonly IInvitationTokenService _invitationTokenService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPlatformPasswordPolicyValidator _passwordPolicyValidator;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<TenantAdminInvitationAcceptanceService> _logger;

    public TenantAdminInvitationAcceptanceService(
        ITenantAdminInvitationAcceptanceRepository repository,
        IInvitationTokenService invitationTokenService,
        IPasswordHashService passwordHashService,
        IPlatformPasswordPolicyValidator passwordPolicyValidator,
        IDateTimeProvider clock,
        ILogger<TenantAdminInvitationAcceptanceService> logger)
    {
        _repository = repository;
        _invitationTokenService = invitationTokenService;
        _passwordHashService = passwordHashService;
        _passwordPolicyValidator = passwordPolicyValidator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ValidateTenantAdminSetupTokenResponse> ValidateSetupTokenAsync(
        string rawToken,
        CancellationToken cancellationToken)
    {
        var token = rawToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return InvalidResponse(token, expired: false, "This invitation link is invalid.");
        }

        string hash;
        try
        {
            hash = _invitationTokenService.HashToken(token);
        }
        catch (InvalidOperationException)
        {
            return InvalidResponse(token, expired: false, "This invitation link is invalid.");
        }

        var snapshot = await _repository.GetByTokenHashForReadAsync(hash, cancellationToken);
        if (snapshot is null)
        {
            return InvalidResponse(token, expired: false, "This invitation link is invalid.");
        }

        var now = _clock.UtcNow;
        var reject = ClassifyRejection(snapshot, now);
        if (reject is not null)
        {
            return InvalidResponse(
                token,
                expired: reject.Code == ErrorInviteExpired,
                reject.Message);
        }

        return new ValidateTenantAdminSetupTokenResponse(
            SetupToken: token,
            Valid: true,
            Expired: false,
            Email: snapshot.InvitedEmail,
            Message: null);
    }

    public async Task<ApplicationResult<SetupTenantAdminPasswordResponse>> SetupPasswordAsync(
        SetupTenantAdminPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = request.SetupToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(InviteInvalid);
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(PasswordMismatch);
        }

        var policyError = _passwordPolicyValidator.Validate(request.Password);
        if (policyError is not null)
        {
            return ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(
                new ApplicationError(ErrorPasswordInvalid, policyError.Message));
        }

        string hash;
        try
        {
            hash = _invitationTokenService.HashToken(token);
        }
        catch (InvalidOperationException)
        {
            return ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(InviteInvalid);
        }

        // Hash before entering the claim transaction so CPU work is outside the row lock.
        var passwordHash = _passwordHashService.HashPassword(request.Password);
        var now = _clock.UtcNow;

        return await _repository.ExecuteClaimAsync(hash, async (claim, ct) =>
        {
            if (claim is null)
            {
                return ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(InviteInvalid);
            }

            var snapshot = ToSnapshot(claim);
            var reject = ClassifyRejection(snapshot, now);
            if (reject is not null)
            {
                return ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(reject);
            }

            try
            {
                claim.User.ActivateFromInvitation(passwordHash, "pbkdf2_embedded", now);
                claim.Invite.MarkAccepted(claim.User.Id, now);
                foreach (var sibling in claim.SiblingOpenInvites)
                {
                    if (sibling.Id != claim.Invite.Id)
                    {
                        sibling.Cancel(now);
                    }
                }

                claim.Operation?.MarkInvitationAccepted(now);
            }
            catch (InvalidOperationException)
            {
                return ApplicationResult<SetupTenantAdminPasswordResponse>.Failure(InviteUsed);
            }

            _logger.LogInformation(
                "Tenant Admin invitation accepted. InviteId={InviteId} TenantId={TenantId} UserId={UserId}",
                claim.Invite.Id,
                claim.Tenant.Id,
                claim.User.Id);

            return ApplicationResult<SetupTenantAdminPasswordResponse>.Success(
                new SetupTenantAdminPasswordResponse(true, "Account setup completed. You can sign in."));
        }, cancellationToken);
    }

    private static ApplicationError? ClassifyRejection(
        TenantAdminInvitationAcceptanceSnapshot snapshot,
        DateTimeOffset now)
    {
        if (snapshot.AcceptedAt.HasValue ||
            string.Equals(snapshot.InviteStatus, UserInviteConstants.StatusAccepted, StringComparison.OrdinalIgnoreCase))
        {
            return InviteUsed;
        }

        if (snapshot.CancelledAt.HasValue ||
            snapshot.InviteStatus is UserInviteConstants.StatusCancelled or UserInviteConstants.StatusRevoked)
        {
            return InviteCancelled;
        }

        if (snapshot.ExpiresAt <= now ||
            string.Equals(snapshot.InviteStatus, UserInviteConstants.StatusExpired, StringComparison.OrdinalIgnoreCase))
        {
            return InviteExpired;
        }

        if (snapshot.InviteStatus is not (UserInviteConstants.StatusPending or UserInviteConstants.StatusSent))
        {
            return InviteInvalid;
        }

        if (!string.Equals(snapshot.TenantStatus, TenantStatusConstants.Active, StringComparison.OrdinalIgnoreCase))
        {
            return TenantNotOperational;
        }

        if (snapshot.TenantUserId is null ||
            !string.Equals(snapshot.TenantUserStatus, TenantUserConstants.StatusInvited, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(snapshot.TenantUserStatus, TenantUserConstants.StatusActive, StringComparison.OrdinalIgnoreCase)
                ? InviteUsed
                : InviteInvalid;
        }

        return null;
    }

    private static TenantAdminInvitationAcceptanceSnapshot ToSnapshot(TenantAdminInvitationAcceptanceClaim claim) =>
        new()
        {
            InviteId = claim.Invite.Id,
            TenantId = claim.Tenant.Id,
            InviteStatus = claim.Invite.InviteStatus,
            ExpiresAt = claim.Invite.ExpiresAt,
            AcceptedAt = claim.Invite.AcceptedAt,
            CancelledAt = claim.Invite.CancelledAt,
            InvitedEmail = claim.Invite.InvitedEmail,
            NormalizedInvitedEmail = claim.Invite.NormalizedInvitedEmail,
            TenantStatus = claim.Tenant.Status,
            TenantDisplayName = claim.Tenant.DisplayName,
            TenantUserId = claim.User.Id,
            TenantUserStatus = claim.User.AccountStatus
        };

    private static ValidateTenantAdminSetupTokenResponse InvalidResponse(
        string token,
        bool expired,
        string message) =>
        new(
            SetupToken: token,
            Valid: false,
            Expired: expired,
            Email: null,
            Message: message);
}
