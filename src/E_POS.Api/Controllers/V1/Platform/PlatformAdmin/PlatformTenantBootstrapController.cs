using System.Security.Claims;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "PlatformOnly")]
[Route("api/v1/platform-admin/tenants/{tenantId:guid}/bootstrap")]
public sealed class PlatformTenantBootstrapController : ControllerBase
{
    private readonly IPlatformTenantBootstrapService _bootstrapService;

    public PlatformTenantBootstrapController(IPlatformTenantBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(Guid tenantId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        var result = await _bootstrapService.GetSummaryAsync(tenantId, platformUserId, cancellationToken);
        return ToActionResult(result, "Selected-tenant bootstrap summary loaded successfully.");
    }

    [HttpPost("outlets")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapOutletResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOutlet(
        Guid tenantId,
        [FromBody] PlatformTenantBootstrapOutletCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        if (!TryGetRequiredIdempotencyKey(out var idempotencyKey, out var missingKeyResult))
        {
            return missingKeyResult!;
        }

        var result = await _bootstrapService.CreateOutletAsync(
            tenantId,
            platformUserId,
            request,
            idempotencyKey,
            cancellationToken);
        return ToMutationActionResult(result, "Bootstrap outlet created successfully.", StatusCodes.Status201Created);
    }

    [HttpPost("tills")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapTillResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTill(
        Guid tenantId,
        [FromBody] PlatformTenantBootstrapTillCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        if (!TryGetRequiredIdempotencyKey(out var idempotencyKey, out var missingKeyResult))
        {
            return missingKeyResult!;
        }

        var result = await _bootstrapService.CreateTillAsync(
            tenantId,
            platformUserId,
            request,
            idempotencyKey,
            cancellationToken);
        return ToMutationActionResult(result, "Bootstrap till created successfully.", StatusCodes.Status201Created);
    }

    [HttpPost("roles")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapRoleResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole(
        Guid tenantId,
        [FromBody] PlatformTenantBootstrapRoleCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        if (!TryGetRequiredIdempotencyKey(out var idempotencyKey, out var missingKeyResult))
        {
            return missingKeyResult!;
        }

        var result = await _bootstrapService.CreateRoleAsync(
            tenantId,
            platformUserId,
            request,
            idempotencyKey,
            cancellationToken);
        return ToMutationActionResult(result, "Bootstrap role created successfully.", StatusCodes.Status201Created);
    }

    [HttpPost("users")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapUserResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUser(
        Guid tenantId,
        [FromBody] PlatformTenantBootstrapUserCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        if (!TryGetRequiredIdempotencyKey(out var idempotencyKey, out var missingKeyResult))
        {
            return missingKeyResult!;
        }

        var result = await _bootstrapService.CreateUserAsync(
            tenantId,
            platformUserId,
            request,
            idempotencyKey,
            cancellationToken);
        return ToMutationActionResult(result, "Bootstrap user created successfully.", StatusCodes.Status201Created);
    }

    [HttpPost("products")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapProductResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateProduct(
        Guid tenantId,
        [FromBody] PlatformTenantBootstrapProductCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        if (!TryGetRequiredIdempotencyKey(out var idempotencyKey, out var missingKeyResult))
        {
            return missingKeyResult!;
        }

        var result = await _bootstrapService.CreateProductAsync(
            tenantId,
            platformUserId,
            request,
            idempotencyKey,
            cancellationToken);
        return ToMutationActionResult(result, "Bootstrap product created successfully.", StatusCodes.Status201Created);
    }

    [HttpGet("products/import/template")]
    [Produces("text/csv")]
    public async Task<IActionResult> GetProductImportTemplate(Guid tenantId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        var result = await _bootstrapService.GetProductImportTemplateAsync(tenantId, platformUserId, cancellationToken);
        if (result.IsFailure)
        {
            return MapError(result.Error);
        }

        return File(result.Value!, "text/csv", "OVZ-ST-PRODUCT-IMPORT-v1.csv");
    }

    [HttpPost("products/import/validate")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapProductImportValidateResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> ValidateProductImport(
        Guid tenantId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        if (!TryGetRequiredIdempotencyKey(out _, out var missingKeyResult))
        {
            return missingKeyResult!;
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(CreateLegacyError(new ApplicationError(
                "platform_tenants.validation_failed",
                "CSV file is required.")));
        }

        await using var stream = file.OpenReadStream();
        var result = await _bootstrapService.ValidateProductImportAsync(
            tenantId,
            platformUserId,
            stream,
            file.FileName,
            cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                LegacyApiResponse<PlatformTenantBootstrapProductImportValidateResponse>.Ok(
                    "Bootstrap product import validated successfully.",
                    result.Value));
        }

        return MapError(result.Error);
    }

    [HttpPost("products/import/{importId:guid}/commit")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapProductImportCommitResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CommitProductImport(
        Guid tenantId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        if (!TryGetRequiredIdempotencyKey(out var idempotencyKey, out var missingKeyResult))
        {
            return missingKeyResult!;
        }

        var result = await _bootstrapService.CommitProductImportAsync(
            tenantId,
            platformUserId,
            importId,
            idempotencyKey,
            cancellationToken);
        return ToActionResult(result, "Bootstrap product import committed successfully.");
    }

    [HttpGet("products/import/{importId:guid}/errors.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> GetProductImportErrorsCsv(
        Guid tenantId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        var result = await _bootstrapService.GetProductImportErrorsCsvAsync(
            tenantId,
            platformUserId,
            importId,
            cancellationToken);
        if (result.IsFailure)
        {
            return MapError(result.Error);
        }

        return File(result.Value!, "text/csv", $"bootstrap-import-{importId:N}-errors.csv");
    }

    [HttpGet("online-store")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapOnlineStoreResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnlineStore(Guid tenantId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        var result = await _bootstrapService.GetOnlineStoreAsync(tenantId, platformUserId, cancellationToken);
        return ToActionResult(result, "Selected-tenant online store bootstrap loaded successfully.");
    }

    [HttpPut("online-store")]
    [ProducesResponseType(typeof(LegacyApiResponse<PlatformTenantBootstrapOnlineStoreResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertOnlineStore(
        Guid tenantId,
        [FromBody] PlatformTenantBootstrapOnlineStoreUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(new ApplicationError("platform_auth.invalid_session", "Invalid platform session.")));
        }

        if (!TryGetRequiredIdempotencyKey(out var idempotencyKey, out var missingKeyResult))
        {
            return missingKeyResult!;
        }

        var result = await _bootstrapService.UpsertOnlineStoreAsync(
            tenantId,
            platformUserId,
            request,
            idempotencyKey,
            cancellationToken);
        return ToActionResult(result, "Bootstrap online store saved successfully.");
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result, string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(LegacyApiResponse<T>.Ok(successMessage, result.Value));
        }

        return MapError(result.Error);
    }

    private IActionResult ToMutationActionResult<T>(ApplicationResult<T> result, string successMessage, int successStatusCode)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return StatusCode(successStatusCode, LegacyApiResponse<T>.Ok(successMessage, result.Value));
        }

        return MapError(result.Error);
    }

    private IActionResult MapError(ApplicationError error) =>
        error.Code switch
        {
            "platform_tenants.not_found" or "import.not_found" => NotFound(CreateLegacyError(error)),
            "platform_tenants.validation_failed" => BadRequest(CreateLegacyError(error)),
            "platform_tenants.bootstrap.tenant_suspended" or
            "platform_tenants.bootstrap.conflict" or
            "platform_tenants.bootstrap.dependency_missing" or
            "platform_tenants.bootstrap.limit_reached" or
            "import.batch_in_progress" => StatusCode(StatusCodes.Status409Conflict, CreateLegacyError(error)),
            _ => StatusCode(StatusCodes.Status403Forbidden, CreateLegacyError(error))
        };

    private bool TryGetPlatformUserId(out Guid platformUserId)
    {
        var platformUserIdValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(platformUserIdValue, out platformUserId);
    }

    private bool TryGetRequiredIdempotencyKey(out string idempotencyKey, out IActionResult? errorResult)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var headerValue) ||
            string.IsNullOrWhiteSpace(headerValue))
        {
            idempotencyKey = string.Empty;
            errorResult = BadRequest(CreateLegacyError(new ApplicationError(
                "platform_tenants.validation_failed",
                "Idempotency-Key header is required.")));
            return false;
        }

        idempotencyKey = headerValue.ToString();
        errorResult = null;
        return true;
    }

    // Correlation: HttpContext.TraceIdentifier is returned on every legacy error payload as traceId.
    private Guid CorrelationId() => Guid.TryParse(HttpContext.TraceIdentifier, out var id) ? id : Guid.NewGuid();

    private object CreateLegacyError(ApplicationError error)
    {
        var fieldErrors = error.FieldErrors?
            .Select(item => new { field = item.Field, message = item.Message })
            .ToArray<object>() ?? Array.Empty<object>();

        return new
        {
            success = false,
            message = error.Message,
            errorCode = error.Code,
            errors = fieldErrors,
            traceId = HttpContext.TraceIdentifier,
            correlationId = CorrelationId()
        };
    }
}
