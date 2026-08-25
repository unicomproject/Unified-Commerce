using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Common.Idempotency;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.AccessControl.Constants;
using E_POS.Domain.Modules.Tenant.AccessControl.Entities;

namespace E_POS.Application.Modules.Tenant.AccessControl.Services;

public sealed class TenantAdminRoleService : ITenantAdminRoleService
{
    private const string CreateRoleOperation = "TENANT_ADMIN_CREATE_ROLE";

    private static readonly ApplicationError PermissionDenied = new(
        "tenant_roles.permission_denied",
        "Permission denied for role management.");

    private static readonly ApplicationError NotFound = new(
        "tenant_roles.not_found",
        "Role was not found for this tenant.");

    private static readonly ApplicationError InvalidIdempotencyKey = new(
        "tenant_roles.invalid_idempotency_key",
        "A valid Idempotency-Key header is required to create a role.");

    private static readonly ApplicationError DelegationCeilingExceeded = new(
        "tenant_roles.delegation_ceiling_exceeded",
        "You cannot grant permissions outside your current access or tenant entitlements.");

    private static readonly ApplicationError LastAdminProtected = new(
        "tenant_roles.last_admin_protected",
        "This change would remove the tenant's last administrative access path.");

    private static readonly ApplicationError ConcurrencyConflict = new(
        "tenant_roles.concurrency_conflict",
        "Role was changed by another request. Refresh and try again.");

    private readonly ITenantAdminRoleRepository _repository;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TenantAdminRoleService(
        ITenantAdminRoleRepository repository,
        IIdempotencyService idempotencyService,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _idempotencyService = idempotencyService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApplicationResult<TenantAdminRoleListResponse>> ListAsync(
        TenantRequestContext context,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(context, TenantAdminUserPermissions.RolesView, TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantAdminRoleListResponse>.Failure(accessError);

        var response = await _repository.ListAsync(
            context.TenantId,
            search,
            status,
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            cancellationToken);

        return ApplicationResult<TenantAdminRoleListResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminRoleDetailResponse>> GetByIdAsync(
        TenantRequestContext context,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(context, TenantAdminUserPermissions.RolesView, TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(accessError);

        var response = await _repository.GetDetailAsync(context.TenantId, roleId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminRoleDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminRoleDetailResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantRoleSetupOptionsResponse>> GetSetupOptionsAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.RolesView,
            TenantAdminUserPermissions.RolesManage,
            TenantAdminUserPermissions.RolesPermissionsView,
            TenantAdminUserPermissions.RolesAssignmentsView);
        if (accessError is not null) return ApplicationResult<TenantRoleSetupOptionsResponse>.Failure(accessError);

        var roles = await _repository.GetSetupRoleOptionsAsync(context.TenantId, cancellationToken);
        var filtered = roles
            .Where(role => TenantRoleSetupCatalog.IsSupportedSetupRoleCode(role.RoleCode))
            .OrderBy(role => TenantRoleSetupCatalog.IsTenantAdminRoleCode(role.RoleCode) ? 0 : 1)
            .ThenBy(role => role.RoleName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return ApplicationResult<TenantRoleSetupOptionsResponse>.Success(
            new TenantRoleSetupOptionsResponse(filtered));
    }

    public async Task<ApplicationResult<TenantAdminRoleDetailResponse>> CreateAsync(
        TenantRequestContext context,
        TenantAdminRoleCreateRequest request,
        CancellationToken cancellationToken,
        string? idempotencyKey = null)
    {
        var accessError = ValidateAccessAny(context, TenantAdminUserPermissions.RolesCreate, TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(accessError);

        var keyError = ValidateIdempotencyKey(idempotencyKey);
        if (keyError is not null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(keyError);

        return await _idempotencyService.ExecuteAsync(
            context.TenantId,
            context.UserId,
            CreateRoleOperation,
            idempotencyKey!.Trim(),
            ComputeCreateRequestHash(request),
            ct => CreateCoreAsync(context, request, ct),
            cancellationToken);
    }

    private async Task<ApplicationResult<TenantAdminRoleDetailResponse>> CreateCoreAsync(
        TenantRequestContext context,
        TenantAdminRoleCreateRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeRoleRequest(request.RoleName, GenerateRoleCode(request.RoleName), request.RoleDescription);
        if (normalized.Error is not null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(normalized.Error);

        if (await _repository.RoleCodeExistsAsync(context.TenantId, normalized.RoleCode!, null, cancellationToken))
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(new ApplicationError(
                "tenant_roles.duplicate_role_code",
                "Role code already exists for this tenant."));
        }

        if (await _repository.RoleNameExistsAsync(context.TenantId, normalized.RoleName!, null, cancellationToken))
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(new ApplicationError(
                "tenant_roles.duplicate_role_name",
                "Role name already exists for this tenant."));
        }

        var now = _dateTimeProvider.UtcNow;
        var permissionCodes = NormalizePermissionCodes(request.PermissionCodes);
        if (permissionCodes.Count == 0)
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(
                ValidationFailed("Select at least one permission before creating a role."));
        }

        var permissions = await _repository.GetAssignablePermissionsByCodeAsync(
            context.TenantId,
            permissionCodes,
            context.Permissions,
            now,
            cancellationToken);

        if (permissions.Count != permissionCodes.Count)
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(DelegationCeilingExceeded);
        }

        var ceilingError = ValidateRolePermissionCeiling(
            normalized.RoleCode!,
            permissions.Select(permission => permission.PermissionCode));
        if (ceilingError is not null)
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(ceilingError);
        }

        var assignments = NormalizeAssignments(request.Assignments);
        var assignmentValidation = await _repository.ValidateAssignmentsAsync(context.TenantId, assignments, cancellationToken);
        if (!assignmentValidation.IsValid)
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(ToAssignmentError(assignmentValidation.Failure));
        }

        var role = TenantRole.Create(
            Guid.NewGuid(),
            context.TenantId,
            null,
            null,
            normalized.RoleCode!,
            normalized.RoleName!,
            normalized.RoleDescription,
            true,
            true,
            context.UserId,
            now);

        await _repository.AddAsync(role, cancellationToken);
        await _repository.ReplacePermissionsAsync(
            context.TenantId,
            role.Id,
            permissions.Select(permission => permission.Id).ToArray(),
            context.UserId,
            now,
            cancellationToken);
        await _repository.ReplaceAssignmentsAsync(context.TenantId, role.Id, assignments, context.UserId, now, cancellationToken);
        await _repository.AddAuditAsync(context.TenantId, context.UserId, role.Id, "tenant.role.created", new
        {
            role.RoleCode,
            role.RoleName,
            permissionCodes,
            assignments
        }, now, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        var created = await _repository.GetDetailAsync(context.TenantId, role.Id, cancellationToken);
        return created is null
            ? ApplicationResult<TenantAdminRoleDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminRoleDetailResponse>.Success(created);
    }

    public async Task<ApplicationResult<TenantAdminRoleDetailResponse>> UpdateAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantAdminRoleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(context, TenantAdminUserPermissions.RolesUpdate, TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(accessError);

        var role = await _repository.GetEditableAsync(context.TenantId, roleId, cancellationToken);
        if (role is null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(NotFound);
        if (HasConcurrencyConflict(role.UpdatedAt, request.ExpectedUpdatedAt)) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(ConcurrencyConflict);

        var normalized = NormalizeRoleRequest(request.RoleName, role.RoleCode, request.RoleDescription);
        if (normalized.Error is not null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(normalized.Error);

        if (await _repository.RoleNameExistsAsync(context.TenantId, normalized.RoleName!, roleId, cancellationToken))
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(new ApplicationError(
                "tenant_roles.duplicate_role_name",
                "Role name already exists for this tenant."));
        }

        var now = _dateTimeProvider.UtcNow;
        role.Update(normalized.RoleName!, normalized.RoleCode!, normalized.RoleDescription, context.UserId, now);
        await _repository.AddAuditAsync(context.TenantId, context.UserId, role.Id, "tenant.role.updated", new
        {
            role.RoleCode,
            role.RoleName
        }, now, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return await GetUpdatedDetailAsync(context.TenantId, role.Id, cancellationToken);
    }

    public async Task<ApplicationResult<TenantAdminRoleDetailResponse>> UpdateStatusAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantAdminRoleStatusRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(context, TenantAdminUserPermissions.RolesUpdate, TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(accessError);

        var role = await _repository.GetEditableAsync(context.TenantId, roleId, cancellationToken);
        if (role is null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(NotFound);
        if (HasConcurrencyConflict(role.UpdatedAt, request.ExpectedUpdatedAt)) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(ConcurrencyConflict);

        if (!request.IsActive && await _repository.WouldRemoveLastAdminAsync(context.TenantId, roleId, null, false, cancellationToken))
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(LastAdminProtected);
        }

        var now = _dateTimeProvider.UtcNow;
        role.SetActive(request.IsActive, context.UserId, now);
        await _repository.AddAuditAsync(context.TenantId, context.UserId, role.Id, "tenant.role.status_changed", new
        {
            isActive = request.IsActive
        }, now, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return await GetUpdatedDetailAsync(context.TenantId, role.Id, cancellationToken);
    }

    public async Task<ApplicationResult> DeleteAsync(
        TenantRequestContext context,
        Guid roleId,
        DateTimeOffset? expectedUpdatedAt,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(context, TenantAdminUserPermissions.RolesDelete, TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult.Failure(accessError);

        var role = await _repository.GetEditableAsync(context.TenantId, roleId, cancellationToken);
        if (role is null) return ApplicationResult.Failure(NotFound);
        if (HasConcurrencyConflict(role.UpdatedAt, expectedUpdatedAt)) return ApplicationResult.Failure(ConcurrencyConflict);

        if (await _repository.WouldRemoveLastAdminAsync(context.TenantId, roleId, null, false, cancellationToken))
        {
            return ApplicationResult.Failure(LastAdminProtected);
        }

        var now = _dateTimeProvider.UtcNow;
        role.SetActive(false, context.UserId, now);
        await _repository.AddAuditAsync(context.TenantId, context.UserId, role.Id, "tenant.role.deleted_or_deactivated", null, now, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    public async Task<ApplicationResult<TenantPermissionCatalogResponse>> GetPermissionCatalogAsync(
        TenantRequestContext context,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.RolesPermissionsView,
            TenantAdminUserPermissions.PermissionsView,
            TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantPermissionCatalogResponse>.Failure(accessError);

        var response = await _repository.GetPermissionCatalogAsync(
            context.TenantId,
            context.Permissions,
            _dateTimeProvider.UtcNow,
            cancellationToken);
        return ApplicationResult<TenantPermissionCatalogResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantRolePermissionsResponse>> GetPermissionsAsync(
        TenantRequestContext context,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.RolesPermissionsView,
            TenantAdminUserPermissions.PermissionsView,
            TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantRolePermissionsResponse>.Failure(accessError);

        var response = await _repository.GetPermissionsAsync(context.TenantId, roleId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantRolePermissionsResponse>.Failure(NotFound)
            : ApplicationResult<TenantRolePermissionsResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantRolePermissionsResponse>> ReplacePermissionsAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantRolePermissionsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.RolesPermissionsUpdate,
            TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantRolePermissionsResponse>.Failure(accessError);

        var role = await _repository.GetEditableAsync(context.TenantId, roleId, cancellationToken);
        if (role is null) return ApplicationResult<TenantRolePermissionsResponse>.Failure(NotFound);
        if (HasConcurrencyConflict(role.UpdatedAt, request.ExpectedUpdatedAt)) return ApplicationResult<TenantRolePermissionsResponse>.Failure(ConcurrencyConflict);

        var permissionCodes = NormalizePermissionCodes(request.PermissionCodes);
        if (permissionCodes.Count == 0)
        {
            return ApplicationResult<TenantRolePermissionsResponse>.Failure(
                ValidationFailed("A role must retain at least one permission."));
        }

        var now = _dateTimeProvider.UtcNow;
        var permissions = await _repository.GetAssignablePermissionsByCodeAsync(
            context.TenantId,
            permissionCodes,
            context.Permissions,
            now,
            cancellationToken);

        if (permissions.Count != permissionCodes.Count)
        {
            return ApplicationResult<TenantRolePermissionsResponse>.Failure(DelegationCeilingExceeded);
        }

        var ceilingError = ValidateRolePermissionCeiling(
            role.RoleCode,
            permissions.Select(permission => permission.PermissionCode));
        if (ceilingError is not null)
        {
            return ApplicationResult<TenantRolePermissionsResponse>.Failure(ceilingError);
        }

        var replacementIds = permissions.Select(permission => permission.Id).ToArray();
        if (await _repository.WouldRemoveLastAdminAsync(context.TenantId, roleId, replacementIds, null, cancellationToken))
        {
            return ApplicationResult<TenantRolePermissionsResponse>.Failure(LastAdminProtected);
        }

        await _repository.ReplacePermissionsAsync(context.TenantId, roleId, replacementIds, context.UserId, now, cancellationToken);
        role.UpdateAudit(context.UserId, now);
        await _repository.AddAuditAsync(context.TenantId, context.UserId, roleId, "tenant.role.permissions_replaced", new
        {
            permissionCodes
        }, now, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetPermissionsAsync(context.TenantId, roleId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantRolePermissionsResponse>.Failure(NotFound)
            : ApplicationResult<TenantRolePermissionsResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantRoleAssignmentsResponse>> GetAssignmentsAsync(
        TenantRequestContext context,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.RolesAssignmentsView,
            TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantRoleAssignmentsResponse>.Failure(accessError);

        var response = await _repository.GetAssignmentsAsync(context.TenantId, roleId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantRoleAssignmentsResponse>.Failure(NotFound)
            : ApplicationResult<TenantRoleAssignmentsResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantRoleAssignmentsResponse>> ReplaceAssignmentsAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantRoleAssignmentsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.RolesAssignmentsUpdate,
            TenantAdminUserPermissions.RolesManage);
        if (accessError is not null) return ApplicationResult<TenantRoleAssignmentsResponse>.Failure(accessError);

        var role = await _repository.GetEditableAsync(context.TenantId, roleId, cancellationToken);
        if (role is null) return ApplicationResult<TenantRoleAssignmentsResponse>.Failure(NotFound);
        if (HasConcurrencyConflict(role.UpdatedAt, request.ExpectedUpdatedAt)) return ApplicationResult<TenantRoleAssignmentsResponse>.Failure(ConcurrencyConflict);

        var assignments = NormalizeAssignments(request.Assignments);
        var validation = await _repository.ValidateAssignmentsAsync(context.TenantId, assignments, cancellationToken);
        if (!validation.IsValid)
        {
            return ApplicationResult<TenantRoleAssignmentsResponse>.Failure(ToAssignmentError(validation.Failure));
        }

        if (await _repository.WouldReplaceAssignmentsRemoveLastAdminAsync(
                context.TenantId,
                roleId,
                assignments,
                cancellationToken))
        {
            return ApplicationResult<TenantRoleAssignmentsResponse>.Failure(LastAdminProtected);
        }

        var now = _dateTimeProvider.UtcNow;
        await _repository.ReplaceAssignmentsAsync(context.TenantId, roleId, assignments, context.UserId, now, cancellationToken);
        role.UpdateAudit(context.UserId, now);
        await _repository.AddAuditAsync(context.TenantId, context.UserId, roleId, "tenant.role.assignments_replaced", new
        {
            assignments
        }, now, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = await _repository.GetAssignmentsAsync(context.TenantId, roleId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantRoleAssignmentsResponse>.Failure(NotFound)
            : ApplicationResult<TenantRoleAssignmentsResponse>.Success(response);
    }

    public async Task<ApplicationResult<TenantAdminRoleDetailResponse>> SaveSetupAsync(
        TenantRequestContext context,
        Guid roleId,
        TenantRoleSetupSaveRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = ValidateAccessAny(
            context,
            TenantAdminUserPermissions.RolesManage,
            TenantAdminUserPermissions.RolesPermissionsUpdate,
            TenantAdminUserPermissions.RolesAssignmentsUpdate);
        if (accessError is not null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(accessError);

        var role = await _repository.GetEditableAsync(context.TenantId, roleId, cancellationToken);
        if (role is null) return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(NotFound);
        if (!TenantRoleSetupCatalog.IsSupportedSetupRoleCode(role.RoleCode))
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(ValidationFailed(
                "Only canonical Tenant Admin and Cashier roles can be configured through role setup."));
        }

        if (HasConcurrencyConflict(role.UpdatedAt, request.ExpectedUpdatedAt))
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(ConcurrencyConflict);
        }

        var now = _dateTimeProvider.UtcNow;
        var permissionCodes = NormalizePermissionCodes(request.PermissionCodes);
        if (permissionCodes.Count == 0)
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(
                ValidationFailed("A role must retain at least one permission."));
        }

        var permissions = await _repository.GetAssignablePermissionsByCodeAsync(
            context.TenantId,
            permissionCodes,
            context.Permissions,
            now,
            cancellationToken);

        if (permissions.Count != permissionCodes.Count)
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(DelegationCeilingExceeded);
        }

        var ceilingError = ValidateRolePermissionCeiling(
            role.RoleCode,
            permissions.Select(permission => permission.PermissionCode));
        if (ceilingError is not null)
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(ceilingError);
        }

        var assignments = NormalizeAssignments(request.Assignments);
        var assignmentValidation = await _repository.ValidateAssignmentsAsync(context.TenantId, assignments, cancellationToken);
        if (!assignmentValidation.IsValid)
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(ToAssignmentError(assignmentValidation.Failure));
        }

        var replacementIds = permissions.Select(permission => permission.Id).ToArray();
        if (await _repository.WouldRemoveLastAdminAsync(context.TenantId, roleId, replacementIds, null, cancellationToken) ||
            await _repository.WouldReplaceAssignmentsRemoveLastAdminAsync(context.TenantId, roleId, assignments, cancellationToken))
        {
            return ApplicationResult<TenantAdminRoleDetailResponse>.Failure(LastAdminProtected);
        }

        await _repository.ReplacePermissionsAsync(context.TenantId, roleId, replacementIds, context.UserId, now, cancellationToken);
        await _repository.ReplaceAssignmentsAsync(context.TenantId, roleId, assignments, context.UserId, now, cancellationToken);
        role.UpdateAudit(context.UserId, now);
        await _repository.AddAuditAsync(context.TenantId, context.UserId, roleId, "tenant.role.setup_saved", new
        {
            role.RoleCode,
            permissionCodes,
            assignments
        }, now, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return await GetUpdatedDetailAsync(context.TenantId, roleId, cancellationToken);
    }

    private async Task<ApplicationResult<TenantAdminRoleDetailResponse>> GetUpdatedDetailAsync(
        Guid tenantId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var response = await _repository.GetDetailAsync(tenantId, roleId, cancellationToken);
        return response is null
            ? ApplicationResult<TenantAdminRoleDetailResponse>.Failure(NotFound)
            : ApplicationResult<TenantAdminRoleDetailResponse>.Success(response);
    }

    private static IReadOnlyList<string> NormalizePermissionCodes(IReadOnlyCollection<string>? permissionCodes) =>
        permissionCodes is null
            ? []
            : permissionCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToArray();

    private static IReadOnlyList<TenantAdminRoleAssignmentRequest> NormalizeAssignments(
        IReadOnlyCollection<TenantAdminRoleAssignmentRequest>? assignments) =>
        assignments is null
            ? []
            : assignments
                .Where(item => item.UserId != Guid.Empty)
                .GroupBy(item => item.UserId)
                .Select(group =>
                {
                    var item = group.Last();
                    return item with
                    {
                        AccessScope = string.IsNullOrWhiteSpace(item.AccessScope)
                            ? "TENANT_WIDE"
                            : item.AccessScope.Trim().ToUpperInvariant(),
                        OutletIds = item.OutletIds?
                            .Where(id => id != Guid.Empty)
                            .Distinct()
                            .OrderBy(id => id)
                            .ToArray()
                    };
                })
                .ToArray();

    private static NormalizedRoleRequest NormalizeRoleRequest(string roleName, string roleCode, string? roleDescription)
    {
        if (string.IsNullOrWhiteSpace(roleName) || roleName.Trim().Length > 120)
        {
            return new(null, null, null, ValidationFailed("Role name is required and must be 120 characters or less."));
        }

        if (string.IsNullOrWhiteSpace(roleCode) || roleCode.Trim().Length > 80)
        {
            return new(null, null, null, ValidationFailed("Role code is required and must be 80 characters or less."));
        }

        if (!string.IsNullOrWhiteSpace(roleDescription) && roleDescription.Trim().Length > 500)
        {
            return new(null, null, null, ValidationFailed("Role description must be 500 characters or less."));
        }

        var code = roleCode.Trim().ToUpperInvariant().Replace(' ', '_');
        if (!code.All(character => char.IsLetterOrDigit(character) || character is '_' or '-' or '.'))
        {
            return new(null, null, null, ValidationFailed("Role code may contain only letters, numbers, dashes, underscores, and dots."));
        }

        return new(roleName.Trim(), code, string.IsNullOrWhiteSpace(roleDescription) ? null : roleDescription.Trim(), null);
    }

    private static ApplicationError ToAssignmentError(RoleAssignmentValidationFailure failure) =>
        failure switch
        {
            RoleAssignmentValidationFailure.UserNotFound => new ApplicationError(
                "tenant_roles.user_not_found",
                "One or more selected users were not found for this tenant."),
            RoleAssignmentValidationFailure.OutletNotFound => new ApplicationError(
                "tenant_roles.outlet_not_found",
                "One or more selected outlets were not found for this tenant."),
            RoleAssignmentValidationFailure.OutletInactive => new ApplicationError(
                "tenant_roles.outlet_inactive",
                "One or more selected outlets are inactive or deleted."),
            RoleAssignmentValidationFailure.MissingOutletSelection => new ApplicationError(
                "tenant_roles.outlet_selection_required",
                "Selected outlet access requires at least one outlet."),
            RoleAssignmentValidationFailure.InvalidAccessScope => new ApplicationError(
                "tenant_roles.invalid_access_scope",
                "Access scope must be TENANT_WIDE or SELECTED_OUTLETS."),
            _ => ValidationFailed("Role assignment request is invalid."),
        };

    private static bool HasConcurrencyConflict(DateTimeOffset? updatedAt, DateTimeOffset? expectedUpdatedAt) =>
        expectedUpdatedAt.HasValue &&
        (!updatedAt.HasValue || updatedAt.Value.ToUniversalTime() != expectedUpdatedAt.Value.ToUniversalTime());

    private static ApplicationError ValidationFailed(string message) =>
        new("tenant_roles.validation_failed", message);

    private static ApplicationError? ValidateRolePermissionCeiling(
        string roleCode,
        IEnumerable<string> permissionCodes)
    {
        var normalizedCodes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!TenantRoleSetupCatalog.IsCashierRoleCode(roleCode))
        {
            return null;
        }

        if (normalizedCodes.Any(code => !TenantRoleSetupCatalog.IsAllowedForRole(roleCode, code)) ||
            normalizedCodes.Any(code => TenantRoleSetupCatalog.AdministrativePermissionCodes.Contains(code)))
        {
            return DelegationCeilingExceeded;
        }

        return null;
    }

    private static ApplicationError? ValidateAccessAny(TenantRequestContext context, params string[] permissions)
    {
        if (context.TenantId == Guid.Empty || context.UserId == Guid.Empty)
        {
            return new ApplicationError("tenant_roles.invalid_tenant_context", "Invalid tenant context.");
        }

        return permissions.Any(context.HasPermission) ? null : PermissionDenied;
    }

    private static ApplicationError? ValidateIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return InvalidIdempotencyKey;
        var normalized = idempotencyKey.Trim();
        return normalized.Length > 100 || !normalized.All(IsSafeIdempotencyKeyCharacter)
            ? InvalidIdempotencyKey
            : null;
    }

    private static bool IsSafeIdempotencyKeyCharacter(char value) =>
        char.IsLetterOrDigit(value) || value is '-' or '_' or '.' or ':';

    private static string ComputeCreateRequestHash(TenantAdminRoleCreateRequest request)
    {
        var canonical = new
        {
            roleName = request.RoleName?.Trim() ?? string.Empty,
            roleCode = GenerateRoleCode(request.RoleName),
            roleDescription = string.IsNullOrWhiteSpace(request.RoleDescription) ? null : request.RoleDescription.Trim(),
            permissionCodes = NormalizePermissionCodes(request.PermissionCodes),
            assignments = NormalizeAssignments(request.Assignments)
        };

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical))))
            .ToLowerInvariant();
    }

    private sealed record NormalizedRoleRequest(
        string? RoleName,
        string? RoleCode,
        string? RoleDescription,
        ApplicationError? Error);

    private static string GenerateRoleCode(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return string.Empty;

        var builder = new StringBuilder(roleName.Trim().Length);
        var previousWasSeparator = false;
        foreach (var character in roleName.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('_');
                previousWasSeparator = true;
            }
        }

        var code = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(code)
            ? "CUSTOM_ROLE"
            : code[..Math.Min(code.Length, 80)];
    }
}
