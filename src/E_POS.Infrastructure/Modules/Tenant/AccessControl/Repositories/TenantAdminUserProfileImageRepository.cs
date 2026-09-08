using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.AccessControl.Repositories;

public sealed class TenantAdminUserProfileImageRepository : ITenantAdminUserProfileImageRepository
{
    private readonly EPosDbContext _dbContext;

    public TenantAdminUserProfileImageRepository(EPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(MediaAsset asset, CancellationToken cancellationToken)
    {
        _dbContext.MediaAssets.Add(asset);
        return Task.CompletedTask;
    }

    public Task<MediaAsset?> GetAsync(
        Guid tenantId,
        Guid mediaAssetId,
        CancellationToken cancellationToken) =>
        _dbContext.MediaAssets.FirstOrDefaultAsync(
            asset => asset.TenantId == tenantId && asset.Id == mediaAssetId,
            cancellationToken);

    public Task<bool> IsAttachedAsync(
        Guid tenantId,
        Guid mediaAssetId,
        CancellationToken cancellationToken) =>
        _dbContext.TenantUsers.AnyAsync(
            user => user.TenantId == tenantId && user.ProfileImageUrl == mediaAssetId,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
