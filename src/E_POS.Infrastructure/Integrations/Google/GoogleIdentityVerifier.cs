using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Options;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace E_POS.Infrastructure.Integrations.Google;

public sealed class GoogleIdentityVerifier : IGoogleIdentityVerifier
{
    private static readonly ApplicationError NotConfigured = new(
        "customer_auth.google_not_configured",
        "Google sign-in is not configured.");
    private static readonly ApplicationError InvalidToken = new(
        "customer_auth.invalid_google_token",
        "Invalid Google sign-in token.");
    private static readonly ApplicationError EmailNotVerified = new(
        "customer_auth.google_email_not_verified",
        "Google email is not verified.");

    private readonly GoogleAuthOptions _options;

    public GoogleIdentityVerifier(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ApplicationResult<GoogleIdentityResult>> VerifyAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return ApplicationResult<GoogleIdentityResult>.Failure(InvalidToken);

        var clientId = _options.ClientId?.Trim();
        if (string.IsNullOrWhiteSpace(clientId))
            return ApplicationResult<GoogleIdentityResult>.Failure(NotConfigured);

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken.Trim(),
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [clientId]
                });

            if (string.IsNullOrWhiteSpace(payload.Subject) ||
                string.IsNullOrWhiteSpace(payload.Email))
            {
                return ApplicationResult<GoogleIdentityResult>.Failure(InvalidToken);
            }

            if (payload.EmailVerified != true)
                return ApplicationResult<GoogleIdentityResult>.Failure(EmailNotVerified);

            return ApplicationResult<GoogleIdentityResult>.Success(new GoogleIdentityResult(
                payload.Subject.Trim(),
                payload.Email.Trim(),
                payload.EmailVerified,
                payload.GivenName,
                payload.FamilyName,
                payload.Name));
        }
        catch (InvalidJwtException)
        {
            return ApplicationResult<GoogleIdentityResult>.Failure(InvalidToken);
        }
        catch (ArgumentException)
        {
            return ApplicationResult<GoogleIdentityResult>.Failure(InvalidToken);
        }
    }
}