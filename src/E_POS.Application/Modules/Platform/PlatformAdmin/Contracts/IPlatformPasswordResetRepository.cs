using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformPasswordResetRepository
{
    Task<bool> PlatformUserExistsAsync(Guid platformUserId, CancellationToken cancellationToken);

    Task AddPendingTokenAsync(PlatformPasswordResetToken token, CancellationToken cancellationToken);

    Task<PlatformPasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<bool> MarkUsedAsync(Guid tokenId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<int> RevokeActivePendingTokensAsync(Guid platformUserId, DateTimeOffset now, CancellationToken cancellationToken);
}
