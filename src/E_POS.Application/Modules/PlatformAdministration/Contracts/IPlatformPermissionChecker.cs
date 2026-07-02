namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformPermissionChecker
{
    Task<bool> HasPermissionAsync(
        Guid platformUserId,
        string permissionCode,
        CancellationToken cancellationToken);
}
