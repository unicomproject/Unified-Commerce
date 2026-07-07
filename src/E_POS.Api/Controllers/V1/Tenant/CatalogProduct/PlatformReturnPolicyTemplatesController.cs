using System.Security.Claims;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "PlatformOnly")]
[Route("api/v1/platform/return-policy-templates")]
public sealed class PlatformReturnPolicyTemplatesController : ControllerBase
{
    private readonly IReturnPolicyTemplateService _service;

    public PlatformReturnPolicyTemplatesController(IReturnPolicyTemplateService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReturnPolicyTemplateCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId)) return Unauthorized(CreateLegacyError("platform_auth.invalid_session", "Invalid platform session."));
        var result = await _service.CreateAsync(platformUserId, request, cancellationToken);
        if (result.IsSuccess && result.Value is not null) return StatusCode(StatusCodes.Status201Created, LegacyApiResponse<ReturnPolicyTemplateResponse>.Ok("Return policy template created successfully.", result.Value));
        return MapError(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetPlatformUserId(out var platformUserId)) return Unauthorized(CreateLegacyError("platform_auth.invalid_session", "Invalid platform session."));
        var result = await _service.ListAsync(platformUserId, pageNumber, pageSize, search, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(LegacyApiResponse<ReturnPolicyTemplateListResponse>.Ok("Return policy templates loaded successfully.", result.Value)) : MapError(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId)) return Unauthorized(CreateLegacyError("platform_auth.invalid_session", "Invalid platform session."));
        var result = await _service.GetByIdAsync(platformUserId, id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(LegacyApiResponse<ReturnPolicyTemplateResponse>.Ok("Return policy template loaded successfully.", result.Value)) : MapError(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ReturnPolicyTemplateUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId)) return Unauthorized(CreateLegacyError("platform_auth.invalid_session", "Invalid platform session."));
        var result = await _service.UpdateAsync(platformUserId, id, request, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(LegacyApiResponse<ReturnPolicyTemplateResponse>.Ok("Return policy template updated successfully.", result.Value)) : MapError(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId)) return Unauthorized(CreateLegacyError("platform_auth.invalid_session", "Invalid platform session."));
        var result = await _service.DeleteAsync(platformUserId, id, cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private IActionResult MapError(ApplicationError error)
    {
        return error.Code switch
        {
            "return_policy_templates.not_found" => NotFound(CreateLegacyError(error.Code, error.Message)),
            "return_policy_templates.validation_failed" => BadRequest(CreateLegacyError(error.Code, error.Message)),
            "return_policy_templates.conflict" => Conflict(CreateLegacyError(error.Code, error.Message)),
            _ => StatusCode(StatusCodes.Status403Forbidden, CreateLegacyError(error.Code, error.Message))
        };
    }

    private bool TryGetPlatformUserId(out Guid platformUserId)
    {
        var platformUserIdValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(platformUserIdValue, out platformUserId);
    }

    private object CreateLegacyError(string errorCode, string message)
    {
        return new { success = false, message, errorCode, errors = Array.Empty<object>(), traceId = HttpContext.TraceIdentifier };
    }
}

