using E_POS.Domain.Modules.Shared.Media.Entities;

namespace E_POS.Application.Modules.Tenant.AccessControl.Contracts;

public interface ITenantAdminUserProfileImageRepository
{
    Task AddAsync(MediaAsset asset, CancellationToken cancellationToken);
    Task<MediaAsset?> GetAsync(Guid tenantId, Guid mediaAssetId, CancellationToken cancellationToken);
    Task<bool> IsAttachedAsync(Guid tenantId, Guid mediaAssetId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
