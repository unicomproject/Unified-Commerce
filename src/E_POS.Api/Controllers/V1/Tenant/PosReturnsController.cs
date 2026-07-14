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

    [HttpGet("sales/search")]
    [ProducesResponseType(typeof(PosReturnSaleSearchPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SearchOriginalSales(
        [FromQuery] Guid? deviceId,
        [FromQuery] string? searchType,
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

        var result = await _service.SearchOriginalSalesAsync(
            context,
            deviceId,
            searchType,
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

    private IActionResult ToErrorResult(ApplicationError error) => error.Code switch
    {
        "pos_returns.permission_denied" or "pos_returns.device_not_trusted" =>
            StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
        "pos_returns.sale_not_found" or "pos_returns.device_not_found" or
            "pos_returns.till_not_assigned" or
            "pos_returns.open_till_required" => NotFound(CreateError(error)),
        "pos_returns.concurrency_conflict" or
            "pos_returns.quantity_exceeds_available" =>
            Conflict(CreateError(error)),
        "pos_returns.invalid_tenant_context" => Unauthorized(CreateError(error)),
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
