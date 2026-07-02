using E_POS.Application.Modules.PlatformAdministration.Dtos;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using E_POS.Domain.Modules.PlatformAdministration.Entities;

namespace E_POS.Application.Modules.PlatformAdministration.Mappers;

public static class PlatformUserMapper
{
    public static string? DeriveDisplayName(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return null;
        }

        return email[..atIndex];
    }

    public static PlatformUserListItemDto ToListItem(
        Guid id,
        string email,
        string status,
        string passwordHash,
        IReadOnlyList<string> roleCodes,
        IReadOnlyList<string> roleNames,
        int permissionCount,
        DateTimeOffset? lastLoginAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new PlatformUserListItemDto(
            id,
            email,
            DeriveDisplayName(email),
            status,
            roleCodes,
            roleNames,
            permissionCount,
            lastLoginAt,
            createdAt,
            updatedAt);
    }

    public static PlatformUserDetailResponse ToDetail(
        Guid id,
        string email,
        string status,
        string passwordHash,
        IReadOnlyList<string> roleCodes,
        IReadOnlyList<string> roleNames,
        int permissionCount,
        DateTimeOffset? lastLoginAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new PlatformUserDetailResponse(
            id,
            email,
            DeriveDisplayName(email),
            status,
            PlatformUserProtection.IsPendingInvitePasswordHash(passwordHash),
            roleCodes,
            roleNames,
            permissionCount,
            lastLoginAt,
            createdAt,
            updatedAt);
    }

    public static PlatformUserDetailResponse ToDetail(PlatformUserListItemDto listItem, bool invitePending)
    {
        return new PlatformUserDetailResponse(
            listItem.Id,
            listItem.Email,
            listItem.DisplayName,
            listItem.Status,
            invitePending,
            listItem.RoleCodes,
            listItem.RoleNames,
            listItem.PermissionCount,
            listItem.LastLoginAt,
            listItem.CreatedAt,
            listItem.UpdatedAt);
    }
}
