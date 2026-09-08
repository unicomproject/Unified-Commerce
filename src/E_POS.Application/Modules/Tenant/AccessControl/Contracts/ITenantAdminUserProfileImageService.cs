using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;

namespace E_POS.Application.Modules.Tenant.AccessControl.Contracts;

public interface ITenantAdminUserProfileImageService
{
    Task<ApplicationResult<TenantAdminUserProfileImageUploadResponse>> UploadAsync(
        TenantRequestContext context,
        MediaUploadFile file,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid mediaAssetId,
        CancellationToken cancellationToken);
}
