using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

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
}
