using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Shared.Media.Dtos;
using E_POS.Application.Modules.Tenant.TenantFoundation.Contracts;
using E_POS.Application.Modules.Tenant.TenantFoundation.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/tenant-admin/settings/pos-login-branding")]
public sealed class TenantAdminPosLoginBrandingController : ControllerBase
{
    private readonly IPosLoginBrandingService _service;
    private readonly IPosLoginBrandingMediaService _mediaService;
    private readonly ITenantRequestContextFactory _contextFactory;

    public TenantAdminPosLoginBrandingController(
        IPosLoginBrandingService service,
        IPosLoginBrandingMediaService mediaService,
        ITenantRequestContextFactory contextFactory)
    {
        _service = service;
        _mediaService = mediaService;
        _contextFactory = contextFactory;
    }

    [HttpPost("media/{purpose}")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PosLoginBrandingMediaUploadResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadMedia(
        string purpose,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized();
        if (file is null)
            return BadRequest(Error(new ApplicationError(
                "pos_login_branding.media_invalid",
                "Image file is required.",
                [new ApplicationFieldError("file", "Image file is required.")])));
        await using var stream = file.OpenReadStream();
        var result = await _mediaService.UploadAsync(
            context,
            purpose,
            new MediaUploadFile(stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);
        if (result.IsSuccess && result.Value is not null) return Ok(result.Value);
        if (result.Error.Code == "pos_login_branding.permission_denied") return StatusCode(403, Error(result.Error));
        if (result.Error.Code == "pos_login_branding.media_storage_unavailable") return StatusCode(503, Error(result.Error));
        return BadRequest(Error(result.Error));
    }

    [HttpGet]
    [ProducesResponseType(typeof(TenantAdminPosLoginBrandingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized();
        var result = await _service.GetAdminAsync(context, cancellationToken);
        return ToResult(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(TenantAdminPosLoginBrandingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Put(
        [FromBody] UpdatePosLoginBrandingRequest request,
        CancellationToken cancellationToken)
    {
        if (!_contextFactory.TryCreate(User, out var context)) return Unauthorized();
        var result = await _service.UpdateAdminAsync(context, request, cancellationToken);
        return ToResult(result);
    }

    private IActionResult ToResult(ApplicationResult<TenantAdminPosLoginBrandingResponse> result)
    {
        if (result.IsSuccess && result.Value is not null) return Ok(result.Value);
        var body = Error(result.Error);
        if (result.Error.Code == "pos_login_branding.permission_denied") return StatusCode(403, body);
        if (result.Error.Code.EndsWith("_media_invalid", StringComparison.Ordinal)) return UnprocessableEntity(body);
        if (result.Error.Code == "pos_login_branding.unavailable") return NotFound(body);
        return BadRequest(body);
    }

    private object Error(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        details = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };
}
