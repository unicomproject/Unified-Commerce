using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using E_POS.Application.Common.Models;
using E_POS.Application.Common.Security;
using E_POS.Infrastructure.Modules.ECommerce.CustomerAuth.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace E_POS.Infrastructure.Integrations.Google;

public sealed class GoogleIdentityVerifier : IGoogleIdentityVerifier
{
    private const string GoogleCertsUrl = "https://www.googleapis.com/oauth2/v3/certs";
    private static readonly string[] GoogleIssuers =
    [
        "https://accounts.google.com",
        "accounts.google.com"
    ];
    private static readonly ApplicationError NotConfigured = new(
        "customer_auth.google_not_configured",
        "Google sign-in is not configured.");
    private static readonly ApplicationError InvalidToken = new(
        "customer_auth.invalid_google_token",
        "Invalid Google sign-in token.");
    private static readonly ApplicationError EmailNotVerified = new(
        "customer_auth.google_email_not_verified",
        "Google email is not verified.");
    private static readonly ApplicationError VerificationUnavailable = new(
        "customer_auth.google_verification_unavailable",
        "Google sign-in could not be verified in time. Please try again.");
    private static readonly HttpClient CertsHttpClient = CreateIpv4HttpClient();
    private static readonly SemaphoreSlim CertsCacheLock = new(1, 1);
    private static JsonWebKeySet? CachedCerts;
    private static DateTimeOffset CertsExpireAt;

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
            var timeoutSeconds = _options.VerificationTimeoutSeconds <= 0
                ? 8
                : _options.VerificationTimeoutSeconds;
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var signingKeys = await GetSigningKeysAsync(timeoutCts.Token);
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(
                idToken.Trim(),
                new TokenValidationParameters
                {
                    ValidIssuers = GoogleIssuers,
                    ValidAudience = clientId,
                    IssuerSigningKeys = signingKeys,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                },
                out _);

            var email = ClaimValue(principal, ClaimTypes.Email, "email");
            var subject = ClaimValue(principal, "sub", ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
                return ApplicationResult<GoogleIdentityResult>.Failure(InvalidToken);

            var emailVerified = IsEmailVerified(principal);
            if (!emailVerified)
                return ApplicationResult<GoogleIdentityResult>.Failure(EmailNotVerified);

            return ApplicationResult<GoogleIdentityResult>.Success(new GoogleIdentityResult(
                subject.Trim(),
                email.Trim(),
                true,
                ClaimValue(principal, ClaimTypes.GivenName, "given_name"),
                ClaimValue(principal, ClaimTypes.Surname, "family_name"),
                ClaimValue(principal, "name")));
        }
        catch (SecurityTokenException)
        {
            return ApplicationResult<GoogleIdentityResult>.Failure(InvalidToken);
        }
        catch (ArgumentException)
        {
            return ApplicationResult<GoogleIdentityResult>.Failure(InvalidToken);
        }
        catch (HttpRequestException)
        {
            return ApplicationResult<GoogleIdentityResult>.Failure(VerificationUnavailable);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApplicationResult<GoogleIdentityResult>.Failure(VerificationUnavailable);
        }
    }

    private static bool IsEmailVerified(ClaimsPrincipal principal)
    {
        var value = ClaimValue(principal, "email_verified");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ClaimValue(ClaimsPrincipal principal, params string[] types)
    {
        foreach (var type in types)
        {
            var value = principal.FindFirst(type)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static async Task<IList<SecurityKey>> GetSigningKeysAsync(
        CancellationToken cancellationToken)
    {
        if (CachedCerts is not null && CertsExpireAt > DateTimeOffset.UtcNow)
            return CachedCerts.GetSigningKeys();

        await CertsCacheLock.WaitAsync(cancellationToken);
        try
        {
            if (CachedCerts is not null && CertsExpireAt > DateTimeOffset.UtcNow)
                return CachedCerts.GetSigningKeys();

            using var response = await CertsHttpClient.GetAsync(GoogleCertsUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var certs = new JsonWebKeySet(json);
            CachedCerts = certs;
            var maxAge = response.Headers.CacheControl?.MaxAge;
            CertsExpireAt = DateTimeOffset.UtcNow.Add(maxAge is { TotalMinutes: > 5 } ? maxAge.Value : TimeSpan.FromHours(1));
            return certs.GetSigningKeys();
        }
        finally
        {
            CertsCacheLock.Release();
        }
    }

    private static HttpClient CreateIpv4HttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(5),
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };
                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(8),
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };
    }
}
