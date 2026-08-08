using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Dtos;

namespace E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;

public interface IOutletImageService
{
    Task<ApplicationResult<OutletImageUploadResponse>> UploadAsync(TenantRequestContext context, MediaUploadFile file, CancellationToken cancellationToken);
    Task<ApplicationResult> DeleteAsync(TenantRequestContext context, Guid mediaAssetId, CancellationToken cancellationToken);
}
