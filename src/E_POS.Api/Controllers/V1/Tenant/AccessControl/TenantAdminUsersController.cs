using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.AccessControl.Contracts;
using E_POS.Application.Modules.Tenant.AccessControl.Dtos.TenantAdmin;
using E_POS.Application.Modules.Shared.Media.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.Tenant.AccessControl;

[ApiController]
[Route("api/v1/tenant-admin/users")]
[Authorize(Policy = "TenantOnly")]
public sealed class TenantAdminUsersController : ControllerBase
{
    private readonly ITenantAdminUserService _tenantAdminUserService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;
    private readonly ITenantAdminUserProfileImageService? _profileImageService;

    public TenantAdminUsersController(
        ITenantAdminUserService tenantAdminUserService,
        ITenantRequestContextFactory tenantRequestContextFactory,
        ITenantAdminUserProfileImageService? profileImageService = null)
    {
        _tenantAdminUserService = tenantAdminUserService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
        _profileImageService = profileImageService;
    }

    [HttpPost("profile-image-uploads")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TenantAdminUserProfileImageUploadResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadProfileImage(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError("user.invalid_tenant_context", "Invalid tenant context.")));
        }

        if (file is null)
        {
            return BadRequest(CreateError(new ApplicationError(
                "user.profile_image_invalid",
                "Image file is required.",
                [new ApplicationFieldError("file", "Image file is required.")])));
        }

        if (_profileImageService is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, CreateError(new ApplicationError(
                "user.profile_image_storage_unavailable",
                "Image upload service is unavailable.")));
        }

        await using var stream = file.OpenReadStream();
        var result = await _profileImageService.UploadAsync(
            context,
            new MediaUploadFile(stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpDelete("profile-image-uploads/{mediaAssetId:guid}")]
    public async Task<IActionResult> DeleteProfileImage(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError("user.invalid_tenant_context", "Invalid tenant context.")));
        }

        if (_profileImageService is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, CreateError(new ApplicationError(
                "user.profile_image_storage_unavailable",
                "Image upload service is unavailable.")));
        }

        var result = await _profileImageService.DeleteAsync(context, mediaAssetId, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? roleId = null,
        [FromQuery] Guid? outletId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "user.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminUserService.ListAsync(
            context,
            search,
            status,
            roleId,
            outletId,
            page,
            pageSize,
            sortBy,
            sortDirection,
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("create-options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCreateOptions(CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "user.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminUserService.GetCreateOptionsAsync(context, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] TenantAdminUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "user.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var result = await _tenantAdminUserService.CreateAsync(context, request, cancellationToken, idempotencyKey);
        if (result.IsSuccess && result.Value is not null)
        {
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value.UserId },
                new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "user.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminUserService.GetByIdAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TenantAdminUserUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "user.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminUserService.UpdateAsync(context, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/resend-invite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendInvite(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "user.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminUserService.ResendInviteAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/revoke-invite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeInvite(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "user.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminUserService.RevokeInviteAsync(context, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "user.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _tenantAdminUserService.DeleteAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(new { data = result.Value });
        }

        return ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "user.permission_denied" or "user.role_not_delegable" or "user.permission_not_delegable" => StatusCode(
                StatusCodes.Status403Forbidden,
                CreateError(error)),
            "user.not_found" or "user.role_not_found" or "user.outlet_not_found" or "user.till_not_found" => NotFound(CreateError(error)),
            "user.duplicate_email" or "user.delete_conflict" or "user.cannot_delete_self" or
                "user.idempotency_conflict" or "user.idempotency_in_progress" or "user.invite_not_available" or
                "user.permission_catalog_mismatch" => Conflict(CreateError(error)),
            "subscription_limit_reached" or "subscription_limit_configuration_missing" or "subscription_limit_invalid" or "subscription_limit_evaluation_failed" or "subscription_limit_unknown_key" or "subscription_limit_not_enforced" => Conflict(CreateError(error)),
            "user.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error)),
        };
    }

    private object CreateError(ApplicationError error)
    {
        return new
        {
            code = error.Code,
            message = error.Message,
            details = error.FieldErrors?
                .Select(item => new { field = item.Field, message = item.Message })
                .ToArray<object>() ?? Array.Empty<object>(),
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow,
        };
    }
}
