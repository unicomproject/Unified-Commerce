using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Infrastructure.Persistence;

namespace E_POS.Infrastructure.Modules.Tenant.TenantFoundation.Repositories;

public sealed class PosLoginBrandingMediaRepository : IPosLoginBrandingMediaRepository
{
    private readonly EPosDbContext _dbContext;

    public PosLoginBrandingMediaRepository(EPosDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken) =>
        _dbContext.MediaAssets.AddAsync(mediaAsset, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
