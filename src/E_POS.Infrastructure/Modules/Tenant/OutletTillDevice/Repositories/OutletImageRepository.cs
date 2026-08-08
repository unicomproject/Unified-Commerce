using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Shared.Media.Entities;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_POS.Infrastructure.Modules.Tenant.OutletTillDevice.Repositories;

public sealed class OutletImageRepository : IOutletImageRepository
{
    private readonly EPosDbContext _db;
    public OutletImageRepository(EPosDbContext db) => _db = db;
    public Task AddAsync(MediaAsset asset, CancellationToken cancellationToken) { _db.MediaAssets.Add(asset); return Task.CompletedTask; }
    public Task<MediaAsset?> GetAsync(Guid tenantId, Guid mediaAssetId, CancellationToken cancellationToken) => _db.MediaAssets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == mediaAssetId, cancellationToken);
    public Task<bool> IsAttachedAsync(Guid tenantId, Guid mediaAssetId, CancellationToken cancellationToken) => _db.Outlets.AnyAsync(x => x.TenantId == tenantId && x.PrimaryImageMediaAssetId == mediaAssetId && x.Status != "DELETED", cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
