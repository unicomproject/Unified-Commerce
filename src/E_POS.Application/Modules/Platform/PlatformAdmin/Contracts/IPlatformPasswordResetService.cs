using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformPasswordResetService
{
    Task<ApplicationResult<PlatformPasswordResetTokenIssueResult>> CreatePendingResetTokenAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<PlatformPasswordResetTokenValidationResult>> ValidateResetTokenAsync(
        string rawToken,
        CancellationToken cancellationToken);

    Task<ApplicationResult> MarkTokenUsedAsync(
        string rawToken,
        CancellationToken cancellationToken);

    Task<ApplicationResult<int>> RevokeActivePendingTokensAsync(
        Guid platformUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<InitiatePlatformPasswordResetResponse>> InitiateAdminPasswordResetAsync(
        Guid targetUserId,
        Guid actorPlatformUserId,
        PlatformAuthClientContext? clientContext,
        CancellationToken cancellationToken);

    Task<ApplicationResult<ValidatePlatformPasswordResetTokenResponse>> ValidatePublicTokenAsync(
        string rawToken,
        CancellationToken cancellationToken);

    Task<ApplicationResult<CompletePlatformPasswordResetResponse>> CompletePasswordResetAsync(
        CompletePlatformPasswordResetRequest request,
        PlatformAuthClientContext? clientContext,
        CancellationToken cancellationToken);
}
