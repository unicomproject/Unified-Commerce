namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

/// <summary>
/// Internal-only result returned once when a password reset token is created. Never persisted or logged.
/// </summary>
public sealed record PlatformPasswordResetTokenIssueResult(
    Guid TokenId,
    string RawToken,
    DateTimeOffset ExpiresAt);
