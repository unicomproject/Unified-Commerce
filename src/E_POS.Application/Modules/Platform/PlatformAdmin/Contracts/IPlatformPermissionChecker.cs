namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformPermissionChecker
{
    Task<bool> HasPermissionAsync(
        Guid platformUserId,
        string permissionCode,
        CancellationToken cancellationToken);
}

