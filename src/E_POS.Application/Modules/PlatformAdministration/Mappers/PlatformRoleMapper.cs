using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Mappers;

public static class PlatformRoleMapper
{
    public static bool IsProtectedRole(string roleCode)
    {
        return PlatformRoleProtection.IsProtectedRole(roleCode);
    }

    public static PlatformRoleListItemDto ToListItem(
        Guid id,
        string roleCode,
        string name,
        string? description,
        string status,
        int permissionCount,
        int userCount,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var isProtected = IsProtectedRole(roleCode);

        return new PlatformRoleListItemDto(
            id,
            roleCode,
            name,
            description,
            status,
            permissionCount,
            userCount,
            IsSystem: isProtected,
            IsProtected: isProtected,
            createdAt,
            updatedAt);
    }

    public static PlatformRoleDetailResponse ToDetail(PlatformRoleListItemDto listItem)
    {
        return new PlatformRoleDetailResponse(
            listItem.Id,
            listItem.RoleCode,
            listItem.Name,
            listItem.Description,
            listItem.Status,
            listItem.PermissionCount,
            listItem.UserCount,
            listItem.IsSystem,
            listItem.IsProtected,
            listItem.CreatedAt,
            listItem.UpdatedAt);
    }

    public static PlatformRoleListItemDto ToListItem(
        PlatformRole role,
        int permissionCount,
        int userCount)
    {
        return ToListItem(
            role.Id,
            role.RoleCode,
            role.Name,
            role.Description,
            role.Status,
            permissionCount,
            userCount,
            role.CreatedAt,
            role.UpdatedAt ?? role.CreatedAt);
    }

    public static PlatformRoleDetailResponse ToDetail(
        PlatformRole role,
        int permissionCount,
        int userCount)
    {
        return ToDetail(ToListItem(role, permissionCount, userCount));
    }
}
