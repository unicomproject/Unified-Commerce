namespace E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

public sealed record PlatformPasswordResetSettings(
    string PublicAppBaseUrl,
    string ResetPath);

public sealed record InitiatePlatformPasswordResetResponse(
    Guid UserId,
    string Email,
    DateTimeOffset ExpiresAt,
    string DeliveryMode,
    string? ResetUrl,
    string Message);

public sealed record ValidatePlatformPasswordResetTokenRequest(string Token);

public sealed record ValidatePlatformPasswordResetTokenResponse(
    bool IsValid,
    string Status,
    DateTimeOffset? ExpiresAt);

public sealed record CompletePlatformPasswordResetRequest(
    string Token,
    string NewPassword,
    string ConfirmPassword);

public sealed record CompletePlatformPasswordResetResponse(
    bool Success,
    string Message);
