using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;

namespace E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;

public interface IPosLoginBrandingMediaService
{
    Task<ApplicationResult<PosLoginBrandingMediaUploadResponse>> UploadAsync(
        TenantRequestContext context,
        string purpose,
        MediaUploadFile file,
        CancellationToken cancellationToken);
}
