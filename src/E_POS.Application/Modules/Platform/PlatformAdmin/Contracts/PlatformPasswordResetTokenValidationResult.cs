namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public sealed record PlatformPasswordResetTokenValidationResult(
    Guid TokenId,
    Guid PlatformUserId);
