using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Application.Modules.PlatformAdministration.Mappers;
using E_POS.Domain.Modules.PlatformAdministration.Constants;

namespace E_POS.Application.Modules.PlatformAdministration.Services;

public sealed class PlatformPermissionCatalogService : IPlatformPermissionCatalogService
{
    private static readonly ApplicationError AccessDenied = new(
        "platform_permission_catalog.access_denied",
        "Platform permission catalog access denied.");

    private readonly IPlatformPermissionCatalogRepository _repository;
    private readonly IPlatformPermissionChecker _permissionChecker;

    public PlatformPermissionCatalogService(
        IPlatformPermissionCatalogRepository repository,
        IPlatformPermissionChecker permissionChecker)
    {
        _repository = repository;
        _permissionChecker = permissionChecker;
    }

    public async Task<ApplicationResult<PlatformPermissionCatalogResponse>> GetCatalogAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasViewPermissionAsync(platformUserId, cancellationToken))
        {
            return ApplicationResult<PlatformPermissionCatalogResponse>.Failure(AccessDenied);
        }

        var permissions = await _repository.GetActiveBusinessPermissionsAsync(cancellationToken);
        return ApplicationResult<PlatformPermissionCatalogResponse>.Success(
            PlatformPermissionCatalogMapper.BuildCatalog(permissions));
    }

    public async Task<ApplicationResult<PlatformPermissionFlatResponse>> GetFlatCatalogAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasViewPermissionAsync(platformUserId, cancellationToken))
        {
            return ApplicationResult<PlatformPermissionFlatResponse>.Failure(AccessDenied);
        }

        var permissions = await _repository.GetActiveBusinessPermissionsAsync(cancellationToken);
        return ApplicationResult<PlatformPermissionFlatResponse>.Success(
            PlatformPermissionCatalogMapper.BuildFlat(permissions));
    }

    private async Task<bool> HasViewPermissionAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return await _permissionChecker.HasPermissionAsync(
            platformUserId,
            PlatformPermissionCodes.PermissionsView,
            cancellationToken);
    }
}
