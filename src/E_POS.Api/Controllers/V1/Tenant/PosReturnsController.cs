using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/returns")]
public sealed class PosReturnsController : ControllerBase
{
    private readonly IPosReturnService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PosReturnsController(
        IPosReturnService service,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost("sales/{saleId:guid}/complete")]
    [ProducesResponseType(typeof(PosReturnReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteReturn(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosReturnCompleteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.CompleteReturnAsync(
            context,
            saleId,
            deviceId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("completions/{returnId:guid}")]
    [ProducesResponseType(typeof(PosReturnReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetCompletion(
        Guid returnId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetCompletionAsync(
            context,
            returnId,
            deviceId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("sales/{saleId:guid}/credit-preview")]
    [ProducesResponseType(typeof(PosReturnCreditPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewCredit(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosReturnCreditPreviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.PreviewCreditAsync(
            context,
            saleId,
            deviceId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("sales/{saleId:guid}/eligibility")]
    [ProducesResponseType(typeof(PosReturnSaleEligibilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSaleEligibility(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetSaleEligibilityAsync(
            context,
            saleId,
            deviceId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("sales/{saleId:guid}/eligibility-check")]
    [ProducesResponseType(typeof(PosReturnSaleEligibilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckSelectedSaleEligibility(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosReturnEligibilityCheckRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.CheckSelectedSaleEligibilityAsync(
            context,
            saleId,
            deviceId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("sales/{saleId:guid}/reasons/validate")]
    [ProducesResponseType(typeof(PosReturnReasonsValidateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateReturnReasons(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosReturnReasonsValidateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.ValidateReturnReasonsAsync(
            context,
            saleId,
            deviceId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("reasons")]
    [ProducesResponseType(typeof(IReadOnlyList<PosReturnReasonOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReturnReasons(
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetReturnReasonsAsync(
            context,
            deviceId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("inspection/conditions")]
    [ProducesResponseType(typeof(IReadOnlyList<PosReturnInspectionConditionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInspectionConditions(
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetInspectionConditionsAsync(
            context,
            deviceId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("sales/{saleId:guid}/inspection/validate")]
    [ProducesResponseType(typeof(PosReturnInspectionValidateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateInspection(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosReturnInspectionValidateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.ValidateInspectionAsync(
            context,
            saleId,
            deviceId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPut("sales/{saleId:guid}/inspection/draft")]
    [ProducesResponseType(typeof(PosReturnInspectionDraftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SaveInspectionDraft(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosReturnInspectionDraftSaveRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pos_returns.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _service.SaveInspectionDraftAsync(context, saleId, deviceId, request, cancellationToken);
        return result.IsSuccess ? Ok(new { data = result.Value }) : ToErrorResult(result.Error);
    }

    [HttpGet("sales/{saleId:guid}/inspection/draft")]
    [ProducesResponseType(typeof(PosReturnInspectionDraftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInspectionDraft(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError("pos_returns.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _service.GetInspectionDraftAsync(context, saleId, deviceId, cancellationToken);
        return result.IsSuccess ? Ok(new { data = result.Value }) : ToErrorResult(result.Error);
    }

    [HttpPost("sales/{saleId:guid}/inspection/media")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PosReturnInspectionMediaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadInspectionMedia(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromQuery] Guid saleLineId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(CreateError(new ApplicationError(
                "pos_returns.inspection_media_required",
                "An inspection photo file is required.")));
        }

        await using var stream = file.OpenReadStream();
        var result = await _service.UploadInspectionMediaAsync(
            context,
            saleId,
            saleLineId,
            deviceId,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpDelete("inspection/media/{mediaId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteInspectionMedia(
        Guid mediaId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.DeleteInspectionMediaAsync(
            context,
            mediaId,
            deviceId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return NoContent();
    }

    [HttpGet("inspection/media/{mediaId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInspectionMedia(
        Guid mediaId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetInspectionMediaAsync(
            context,
            mediaId,
            deviceId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPut("sales/{saleId:guid}/resolution")]
    [ProducesResponseType(typeof(PosReturnResolutionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveResolution(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosReturnResolutionSaveRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.SaveResolutionAsync(
            context,
            saleId,
            deviceId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("sales/{saleId:guid}/resolution")]
    [ProducesResponseType(typeof(PosReturnResolutionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResolution(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetResolutionAsync(
            context,
            saleId,
            deviceId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("sales/{saleId:guid}/refund-methods")]
    [ProducesResponseType(typeof(PosReturnRefundMethodsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetRefundMethods(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetRefundMethodsAsync(
            context,
            saleId,
            deviceId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPut("sales/{saleId:guid}/refund-method")]
    [ProducesResponseType(typeof(PosReturnRefundMethodSaveResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveRefundMethod(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosReturnRefundMethodSaveRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.SaveRefundMethodAsync(
            context,
            saleId,
            deviceId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("sales/{saleId:guid}/exchange/products")]
    [ProducesResponseType(typeof(PosExchangeProductsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SearchExchangeProducts(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.SearchExchangeProductsAsync(
            context,
            saleId,
            deviceId,
            search,
            page,
            pageSize,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPut("sales/{saleId:guid}/exchange/replacement")]
    [ProducesResponseType(typeof(PosExchangeReplacementSaveResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveExchangeReplacement(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosExchangeReplacementSaveRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.SaveExchangeReplacementAsync(
            context,
            saleId,
            deviceId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("sales/{saleId:guid}/exchange/replacement")]
    [ProducesResponseType(typeof(PosExchangeReplacementSaveResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetExchangeReplacement(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.GetExchangeReplacementAsync(
            context,
            saleId,
            deviceId,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpPost("sales/{saleId:guid}/exchange-preview")]
    [ProducesResponseType(typeof(PosExchangePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PreviewExchange(
        Guid saleId,
        [FromQuery] Guid? deviceId,
        [FromBody] PosExchangePreviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.PreviewExchangeAsync(
            context,
            saleId,
            deviceId,
            request,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    [HttpGet("sales/search")]
    [ProducesResponseType(typeof(PosReturnSaleSearchPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SearchOriginalSales(
        [FromQuery] Guid? deviceId,
        [FromQuery] string? searchType,
        [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? paymentMethodCode,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError(
                "pos_returns.invalid_tenant_context",
                "Invalid tenant context.")));
        }

        var result = await _service.SearchOriginalSalesAsync(
            context,
            deviceId,
            searchType,
            search,
            fromDate,
            toDate,
            paymentMethodCode,
            minAmount,
            maxAmount,
            page,
            pageSize,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    private IActionResult ToErrorResult(ApplicationError error) => error.Code switch
    {
        "pos_returns.permission_denied" or "pos_returns.device_not_trusted" =>
            StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
        "pos_returns.sale_not_found" or "pos_returns.device_not_found" or
            "pos_returns.till_not_assigned" or
            "pos_returns.open_till_required" or
            "pos_returns.media_not_found" or
            "pos_returns.inspection_draft_not_found" or
            "pos_returns.resolution_not_found" or
            "pos_returns.completion_not_found" => NotFound(CreateError(error)),
            "pos_returns.concurrency_conflict" or
            "pos_returns.quantity_exceeds_available" or
            "pos_returns.line_not_returnable" or
            "pos_returns.inspection_stale" or
            "pos_returns.inspection_not_validated" or
            "pos_returns.inspection_draft_required" or
            "pos_returns.inspection_draft_conflict" or
            "pos_returns.inspection_draft_expired" or
            "pos_returns.inspection_draft_consumed" or
            "pos_returns.inspection_media_limit_reached" or
            "pos_returns.inspection_media_consumed" or
            "pos_returns.approval_required" or
            "pos_returns.idempotency_conflict" or
            "pos_returns.duplicate_completion" or
            "pos_returns.refund_preview_stale" or
            "pos_returns.exchange_cannot_proceed" or
            "pos_returns.exchange_price_changed" or
            "pos_returns.exchange_tax_changed" or
            "pos_returns.exchange_discount_changed" or
            "pos_returns.exchange_preview_stale" or
            "pos_returns.insufficient_outlet_stock" or
            "pos_returns.insufficient_stock" or
            "pos_returns.completion_not_ready" =>
            Conflict(CreateError(error)),
        "pos_returns.refund_method_not_allowed" or
            "pos_returns.refund_methods_failed" or
            "pos_returns.refund_method_save_failed" or
            "pos_returns.exchange_incomplete" or
            "pos_returns.exchange_cash_payment_required" =>
            UnprocessableEntity(CreateError(error)),
        "pos_returns.invalid_resolution" or
            "pos_returns.resolution_save_failed" or
            "pos_returns.invalid_replacement" or
            "pos_returns.invalid_expected_version" or
            "pos_returns.replacement_save_failed" or
            "pos_returns.product_not_sellable" or
            "pos_returns.exchange_preview_failed" or
            "pos_returns.exchange_products_failed" =>
            UnprocessableEntity(CreateError(error)),
        "pos_returns.replacement_not_found" or
            "pos_returns.product_not_found" or
            "pos_returns.price_not_found" =>
            NotFound(CreateError(error)),
        "pos_returns.inspection_media_too_large" =>
            StatusCode(StatusCodes.Status413PayloadTooLarge, CreateError(error)),
        "pos_returns.inspection_media_invalid_type" =>
            StatusCode(StatusCodes.Status415UnsupportedMediaType, CreateError(error)),
        "pos_returns.invalid_search_type" or
            "pos_returns.search_required" or
            "pos_returns.search_too_long" or
            "pos_returns.invalid_date_range" or
            "pos_returns.invalid_amount_range" or
            "pos_returns.payment_method_not_found" or
            "pos_returns.invalid_pagination" =>
            UnprocessableEntity(CreateError(error)),
        "pos_returns.invalid_tenant_context" => Unauthorized(CreateError(error)),
        "pos_returns.eligibility_check_failed" or
            "pos_returns.reasons_load_failed" or
            "pos_returns.reasons_validate_failed" or
            "pos_returns.inspection_media_storage_failed" or
            "pos_returns.inspection_operation_failed" or
            "pos_returns.inspection_media_failed" =>
            StatusCode(StatusCodes.Status500InternalServerError, CreateError(error)),
        _ => BadRequest(CreateError(error))
    };

    private object CreateError(ApplicationError error) => new
    {
        code = error.Code,
        message = error.Message,
        details = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier,
        timestamp = DateTimeOffset.UtcNow
    };
}
