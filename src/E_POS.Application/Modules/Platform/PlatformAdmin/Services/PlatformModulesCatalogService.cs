using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformModulesCatalogService : IPlatformModulesCatalogService
{
    private static readonly ApplicationError AccessDenied = new(
        "platform_modules_catalog.access_denied",
        "Platform modules catalog access denied.");

    private readonly IPlatformModulesCatalogRepository _repository;
    private readonly IPlatformPermissionChecker _permissionChecker;

    public PlatformModulesCatalogService(
        IPlatformModulesCatalogRepository repository,
        IPlatformPermissionChecker permissionChecker)
    {
        _repository = repository;
        _permissionChecker = permissionChecker;
    }

    public async Task<ApplicationResult<PlatformModulesCatalogResponse>> GetModulesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasModulesViewPermissionAsync(platformUserId, cancellationToken))
        {
            return ApplicationResult<PlatformModulesCatalogResponse>.Failure(AccessDenied);
        }

        var includeFeatures = await HasFeaturesViewPermissionAsync(platformUserId, cancellationToken);
        var modules = await _repository.GetActiveModulesAsync(cancellationToken);
        var responseModules = modules
            .Select(module => MapModule(module, includeFeatures))
            .ToList();

        return ApplicationResult<PlatformModulesCatalogResponse>.Success(
            new PlatformModulesCatalogResponse(responseModules));
    }

    private static PlatformModulesCatalogModuleDto MapModule(
        PlatformModulesCatalogModuleDto module,
        bool includeFeatures)
    {
        return module with
        {
            Features = includeFeatures ? module.Features : []
        };
    }

    private Task<bool> HasModulesViewPermissionAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return _permissionChecker.HasPermissionAsync(
            platformUserId,
            PlatformPermissionCodes.ModulesView,
            cancellationToken);
    }

    private Task<bool> HasFeaturesViewPermissionAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        return _permissionChecker.HasPermissionAsync(
            platformUserId,
            PlatformPermissionCodes.FeaturesView,
            cancellationToken);
    }
}


