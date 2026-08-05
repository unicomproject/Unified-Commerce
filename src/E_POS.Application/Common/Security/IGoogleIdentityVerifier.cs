using E_POS.Application.Common.Models;

namespace E_POS.Application.Common.Security;

public interface IGoogleIdentityVerifier
{
    Task<ApplicationResult<GoogleIdentityResult>> VerifyAsync(
        string idToken,
        CancellationToken cancellationToken);
}

public sealed record GoogleIdentityResult(
    string Subject,
    string Email,
    bool EmailVerified,
    string? GivenName,
    string? FamilyName,
    string? DisplayName);