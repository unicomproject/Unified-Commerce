using E_POS.Application.Modules.PlatformAdministration.Contracts;

namespace E_POS.Application.Modules.PlatformAdministration.Services;

public sealed class PlatformPermissionChecker : IPlatformPermissionChecker
{
    private readonly IPlatformPermissionRepository _permissionRepository;

    public PlatformPermissionChecker(IPlatformPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<bool> HasPermissionAsync(
        Guid platformUserId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        var activePermissionCodes = await _permissionRepository.GetActivePermissionCodesAsync(
            platformUserId,
            cancellationToken);

        return activePermissionCodes.Contains(permissionCode.Trim());
    }
}
