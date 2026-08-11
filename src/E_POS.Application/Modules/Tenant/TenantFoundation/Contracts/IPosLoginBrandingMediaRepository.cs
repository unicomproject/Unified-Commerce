using E_POS.Domain.Modules.Shared.Media.Entities;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface IPosLoginBrandingMediaRepository
{
    Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
