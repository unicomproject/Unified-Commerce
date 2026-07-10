using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using E_POS.Application.Modules.Platform.PlatformAdmin.Mappers;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Constants;
using E_POS.Domain.Modules.Platform.PlatformAdmin.Entities;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Services;

public sealed class PlatformRoleService : IPlatformRoleService
{
    private static readonly ApplicationError AccessDenied = new(
        "platform_roles.access_denied",
        "Platform role access denied.");

    private static readonly ApplicationError NotFound = new(
        "platform_roles.not_found",
        "Platform role was not found.");

    private static readonly ApplicationError ValidationFailed = new(
        "platform_roles.validation_failed",
        "Platform role validation failed.");

    private static readonly ApplicationError Conflict = new(
        "platform_roles.conflict",
        "Platform role conflict.");

    private static readonly ApplicationError SystemRoleProtected = new(
        "platform_roles.system_role_protected",
        "System platform role is protected.");

    private readonly IPlatformRoleRepository _roleRepository;
    private readonly IPlatformPermissionCatalogRepository _permissionCatalogRepository;
    private readonly IPlatformPermissionChecker _permissionChecker;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlatformRoleService(
        IPlatformRoleRepository roleRepository,
        IPlatformPermissionCatalogRepository permissionCatalogRepository,
        IPlatformPermissionChecker permissionChecker,
        IDateTimeProvider dateTimeProvider)
    {
        _roleRepository = roleRepository;
        _permissionCatalogRepository = permissionCatalogRepository;
        _permissionChecker = permissionChecker;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<PlatformRoleListResponse>> GetRolesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.RolesView, cancellationToken))
        {
            return ApplicationResult<PlatformRoleListResponse>.Failure(AccessDenied);
        }

        var roles = await _roleRepository.GetRolesAsync(cancellationToken);
        return ApplicationResult<PlatformRoleListResponse>.Success(roles);
    }

    public async Task<ApplicationResult<PlatformRoleDetailResponse>> GetRoleAsync(
        Guid roleId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.RolesView, cancellationToken))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(AccessDenied);
        }

        var role = await _roleRepository.GetRoleByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(NotFound);
        }

        return ApplicationResult<PlatformRoleDetailResponse>.Success(role);
    }

    public async Task<ApplicationResult<PlatformRoleDetailResponse>> CreateRoleAsync(
        CreatePlatformRoleRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.RolesCreate, cancellationToken))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(AccessDenied);
        }

        var roleCode = NormalizeRoleCode(request.RoleCode ?? request.Code);
        var name = NormalizeRequiredText(request.Name);
        var status = NormalizeStatus(request.Status);

        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(
                ValidationFailed with { Message = "Role code is required." });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(
                ValidationFailed with { Message = "Role name is required." });
        }

        if (!IsValidStatus(status))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(
                ValidationFailed with { Message = "Role status must be ACTIVE or INACTIVE." });
        }

        if (PlatformRoleProtection.IsProtectedRole(roleCode))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(
                SystemRoleProtected with { Message = "System platform role codes cannot be created." });
        }

        if (await _roleRepository.RoleCodeExistsAsync(roleCode, cancellationToken))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(
                Conflict with { Message = "A platform role with this code already exists." });
        }

        var now = _dateTimeProvider.UtcNow;
        var role = PlatformRole.Create(
            Guid.NewGuid(),
            roleCode,
            name,
            NormalizeOptionalText(request.Description),
            status,
            now);

        await _roleRepository.AddRoleAsync(role, cancellationToken);

        var created = await _roleRepository.GetRoleByIdAsync(role.Id, cancellationToken);
        return ApplicationResult<PlatformRoleDetailResponse>.Success(created!);
    }

    public async Task<ApplicationResult<PlatformRoleDetailResponse>> UpdateRoleAsync(
        Guid roleId,
        UpdatePlatformRoleRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(platformUserId, PlatformPermissionCodes.RolesUpdate, cancellationToken))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(AccessDenied);
        }

        var role = await _roleRepository.GetRoleEntityByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(NotFound);
        }

        if (PlatformRoleProtection.IsProtectedRole(role.RoleCode))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(
                SystemRoleProtected with { Message = "System platform roles cannot be edited." });
        }

        var name = NormalizeRequiredText(request.Name);
        var status = NormalizeStatus(request.Status);

        if (string.IsNullOrWhiteSpace(name))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(
                ValidationFailed with { Message = "Role name is required." });
        }

        if (!IsValidStatus(status))
        {
            return ApplicationResult<PlatformRoleDetailResponse>.Failure(
                ValidationFailed with { Message = "Role status must be ACTIVE or INACTIVE." });
        }

        role.UpdateDetails(
            name,
            NormalizeOptionalText(request.Description),
            status,
            _dateTimeProvider.UtcNow);

        await _roleRepository.UpdateRoleAsync(role, cancellationToken);

        var updated = await _roleRepository.GetRoleByIdAsync(role.Id, cancellationToken);
        return ApplicationResult<PlatformRoleDetailResponse>.Success(updated!);
    }

    public async Task<ApplicationResult<PlatformRolePermissionsResponse>> GetRolePermissionsAsync(
        Guid roleId,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        if (!await HasPermissionAsync(
                platformUserId,
                PlatformPermissionCodes.RolePermissionsView,
                cancellationToken))
        {
            return ApplicationResult<PlatformRolePermissionsResponse>.Failure(AccessDenied);
        }

        var availablePermissions = await BuildAvailablePermissionsAsync(cancellationToken);
        var rolePermissions = await _roleRepository.GetRolePermissionsAsync(
            availablePermissions,
            roleId,
            cancellationToken);

        if (rolePermissions is null)
        {
            return ApplicationResult<PlatformRolePermissionsResponse>.Failure(NotFound);
        }

        return ApplicationResult<PlatformRolePermissionsResponse>.Success(rolePermissions);
    }

    public async Task<ApplicationResult<PlatformRolePermissionsResponse>> UpdateRolePermissionsAsync(
        Guid roleId,
        UpdatePlatformRolePermissionsRequest request,
        Guid platformUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await HasPermissionAsync(
                platformUserId,
                PlatformPermissionCodes.RolePermissionsUpdate,
                cancellationToken))
        {
            return ApplicationResult<PlatformRolePermissionsResponse>.Failure(AccessDenied);
        }

        var role = await _roleRepository.GetRoleEntityByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return ApplicationResult<PlatformRolePermissionsResponse>.Failure(NotFound);
        }

        if (PlatformRoleProtection.IsProtectedRole(role.RoleCode))
        {
            return ApplicationResult<PlatformRolePermissionsResponse>.Failure(
                SystemRoleProtected with { Message = "System platform role permissions cannot be replaced." });
        }

        var permissionIdMap = await _roleRepository.GetActiveBusinessPermissionIdMapAsync(cancellationToken);
        var resolvedCodes = ResolvePermissionCodes(request.PermissionCodes, out var invalidCodes);

        if (invalidCodes.Count > 0)
        {
            return ApplicationResult<PlatformRolePermissionsResponse>.Failure(
                ValidationFailed with
                {
                    Message = invalidCodes.Any(code =>
                        string.Equals(code, PlatformBootstrapPermissionCodes.AdminAccess, StringComparison.Ordinal))
                        ? "Bootstrap permission platform.admin.access cannot be assigned to roles."
                        : $"Unknown platform permission codes: {string.Join(", ", invalidCodes)}."
                });
        }

        var resolvedPermissionIds = resolvedCodes
            .Select(code => permissionIdMap[code])
            .Distinct()
            .ToList();

        await _roleRepository.ReplaceRolePermissionsAsync(
            roleId,
            resolvedPermissionIds,
            _dateTimeProvider.UtcNow,
            platformUserId,
            cancellationToken);

        var availablePermissions = await BuildAvailablePermissionsAsync(cancellationToken);
        var updated = await _roleRepository.GetRolePermissionsAsync(
            availablePermissions,
            roleId,
            cancellationToken);

        return ApplicationResult<PlatformRolePermissionsResponse>.Success(updated!);
    }

    private async Task<bool> HasPermissionAsync(
        Guid platformUserId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return await _permissionChecker.HasPermissionAsync(
            platformUserId,
            permissionCode,
            cancellationToken);
    }

    private async Task<IReadOnlyList<PlatformPermissionDto>> BuildAvailablePermissionsAsync(
        CancellationToken cancellationToken)
    {
        var permissions = await _permissionCatalogRepository.GetActiveBusinessPermissionsAsync(cancellationToken);
        return PlatformPermissionCatalogMapper.BuildFlat(permissions).Permissions;
    }

    private static IReadOnlyList<string> ResolvePermissionCodes(
        IReadOnlyList<string>? permissionCodes,
        out IReadOnlyList<string> invalidCodes)
    {
        var invalid = new List<string>();
        var resolved = new List<string>();
        var allowedCodes = PlatformPermissionCodes.All.ToHashSet(StringComparer.Ordinal);

        if (permissionCodes is null || permissionCodes.Count == 0)
        {
            invalidCodes = invalid;
            return resolved;
        }

        foreach (var rawCode in permissionCodes)
        {
            if (string.IsNullOrWhiteSpace(rawCode))
            {
                continue;
            }

            var code = rawCode.Trim();

            if (PlatformBootstrapPermissionCodes.AdminAccess.Equals(code, StringComparison.Ordinal))
            {
                invalid.Add(code);
                continue;
            }

            var matchedCode = allowedCodes.FirstOrDefault(
                allowedCode => string.Equals(allowedCode, code, StringComparison.OrdinalIgnoreCase));

            if (matchedCode is null)
            {
                invalid.Add(code);
                continue;
            }

            resolved.Add(matchedCode);
        }

        invalidCodes = invalid;
        return resolved.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string NormalizeRoleCode(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string NormalizeRequiredText(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PlatformAuthConstants.ActiveStatus;
        }

        var normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "ACTIVE" => PlatformAuthConstants.ActiveStatus,
            "INACTIVE" => "INACTIVE",
            _ => normalized
        };
    }

    private static bool IsValidStatus(string status)
    {
        return status is PlatformAuthConstants.ActiveStatus or "INACTIVE";
    }
}


