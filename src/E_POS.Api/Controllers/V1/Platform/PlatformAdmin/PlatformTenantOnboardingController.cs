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
[Route("api/v1/platform-admin/tenant-onboarding")]
public sealed class PlatformTenantOnboardingController : ControllerBase
{
    private readonly IPlatformTenantOnboardingService _service;
    private readonly IPlatformTenantService _tenantService;
    public PlatformTenantOnboardingController(IPlatformTenantOnboardingService service, IPlatformTenantService tenantService)
    { _service = service; _tenantService = tenantService; }

    [HttpGet("create-options")]
    public async Task<IActionResult> GetCreateOptions(CancellationToken ct)
    {
        if (!TryActor(out var actor)) return Unauthorized();
        var result = await _tenantService.GetCreateOptionsAsync(actor, ct);
        return ToResult(result, "Tenant onboarding options loaded.");
    }

    [HttpPost("drafts")]
    public async Task<IActionResult> CreateDraft([FromBody] CreateTenantOnboardingDraftRequest request, CancellationToken ct)
    {
        if (!TryActor(out var actor)) return Unauthorized();
        var result = await _service.CreateDraftAsync(request, actor, ct);
        if (result.IsFailure || result.Value is null) return Error(result.Error);
        SetEtag(result.Value.Version);
        return StatusCode(201, LegacyApiResponse<TenantOnboardingDraftResponse>.Ok("Tenant onboarding draft created.", result.Value));
    }

    [HttpGet("drafts")]
    public async Task<IActionResult> ListDrafts([FromQuery] bool mine = true, CancellationToken ct = default)
    {
        if (!TryActor(out var actor)) return Unauthorized();
        return ToResult(await _service.ListDraftsAsync(actor, !mine, ct), "Tenant onboarding drafts loaded.");
    }

    [HttpGet("drafts/{draftId:guid}")]
    public async Task<IActionResult> GetDraft(Guid draftId, CancellationToken ct)
    {
        if (!TryActor(out var actor)) return Unauthorized();
        var result = await _service.GetDraftAsync(draftId, actor, ct);
        if (result.IsFailure || result.Value is null) return Error(result.Error);
        SetEtag(result.Value.Version);
        return Ok(LegacyApiResponse<TenantOnboardingDraftResponse>.Ok("Tenant onboarding draft loaded.", result.Value));
    }

    [HttpPatch("drafts/{draftId:guid}")]
    public async Task<IActionResult> UpdateDraft(Guid draftId, [FromBody] UpdateTenantOnboardingDraftRequest request, CancellationToken ct)
    {
        if (!TryActor(out var actor)) return Unauthorized();
        if (!TryVersion(out var version)) return PreconditionRequired();
        var result = await _service.UpdateDraftAsync(draftId, request, version, actor, ct);
        if (result.IsFailure || result.Value is null) return Error(result.Error);
        SetEtag(result.Value.Version);
        return Ok(LegacyApiResponse<TenantOnboardingDraftResponse>.Ok("Tenant onboarding draft saved.", result.Value));
    }

    [HttpDelete("drafts/{draftId:guid}")]
    public async Task<IActionResult> DiscardDraft(Guid draftId, CancellationToken ct)
    {
        if (!TryActor(out var actor)) return Unauthorized();
        if (!TryVersion(out var version)) return PreconditionRequired();
        var result = await _service.DiscardDraftAsync(draftId, version, actor, ct);
        return result.IsSuccess ? NoContent() : Error(result.Error);
    }

    [HttpPost("drafts/{draftId:guid}/validate")]
    public async Task<IActionResult> ValidateDraft(Guid draftId, CancellationToken ct)
    {
        if (!TryActor(out var actor)) return Unauthorized();
        return ToResult(await _service.ValidateDraftAsync(draftId, actor, ct), "Tenant onboarding draft validated.");
    }

    [HttpPost("drafts/{draftId:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid draftId, [FromBody] FinalizeTenantOnboardingRequest request, CancellationToken ct)
    {
        if (!TryActor(out var actor)) return Unauthorized();
        if (!TryVersion(out var version)) return PreconditionRequired();
        var key = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key)) return PreconditionRequired();
        var result = await _service.FinalizeAsync(draftId, request, version, key, actor, ct);
        if (result.IsFailure || result.Value is null) return Error(result.Error);
        return StatusCode(result.Value.IdempotentReplay ? 200 : 201,
            LegacyApiResponse<TenantOnboardingReceiptResponse>.Ok("Tenant onboarding finalized.", result.Value));
    }

    [HttpGet("operations/{operationId:guid}")]
    public async Task<IActionResult> GetOperation(Guid operationId, CancellationToken ct)
    {
        if (!TryActor(out var actor)) return Unauthorized();
        return ToResult(await _service.GetOperationAsync(operationId, actor, ct), "Tenant onboarding operation loaded.");
    }

    private IActionResult ToResult<T>(ApplicationResult<T> result, string message) =>
        result.IsSuccess && result.Value is not null ? Ok(LegacyApiResponse<T>.Ok(message, result.Value)) : Error(result.Error);
    private IActionResult Error(ApplicationError error)
    {
        var body = new { success = false, message = error.Message, errorCode = error.Code,
            errors = error.FieldErrors?.Select(x => new { field = x.Field, message = x.Message }) ?? [], traceId = HttpContext.TraceIdentifier };
        return error.Code.Split('.').LastOrDefault() switch
        {
            "not_found" => NotFound(body), "access_denied" => StatusCode(403, body),
            "validation_failed" => UnprocessableEntity(body), "precondition_required" => StatusCode(428, body),
            "concurrency_conflict" or "idempotency_conflict" or "duplicate_conflict" => Conflict(body),
            _ => StatusCode(500, body)
        };
    }
    private IActionResult PreconditionRequired() => StatusCode(428, new { success = false, message = "If-Match and Idempotency-Key preconditions are required where applicable.", errorCode = "platform_tenant_onboarding.precondition_required", traceId = HttpContext.TraceIdentifier });
    private bool TryActor(out Guid id) => Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    private bool TryVersion(out long version)
    {
        var raw = Request.Headers.IfMatch.ToString().Trim().Trim('"');
        return long.TryParse(raw, out version) && version > 0;
    }
    private void SetEtag(long version) => Response.Headers.ETag = $"\"{version}\"";
}
