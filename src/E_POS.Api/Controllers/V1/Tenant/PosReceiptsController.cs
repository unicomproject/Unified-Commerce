using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;
using E_POS.Application.Modules.Tenant.POSOperations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/pos/receipts")]
public sealed class PosReceiptsController : ControllerBase
{
    private readonly IPosReceiptService _posReceiptService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public PosReceiptsController(
        IPosReceiptService posReceiptService,
        ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _posReceiptService = posReceiptService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PosReceiptSearchResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] PosReceiptSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError(
                "pos_receipts.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _posReceiptService.SearchAsync(context, request, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpGet("{receiptId:guid}")]
    [ProducesResponseType(typeof(PosReceiptDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(
        Guid receiptId,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError(
                "pos_receipts.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _posReceiptService.GetDetailAsync(context, receiptId, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpPost("{receiptId:guid}/reprint/authorize")]
    [ProducesResponseType(typeof(PosReceiptReprintAuthorizationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AuthorizeReprint(
        Guid receiptId,
        [FromBody] PosReceiptReprintAuthorizationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
            return Unauthorized(CreateError(new ApplicationError(
                "pos_receipts.invalid_tenant_context", "Invalid tenant context.")));

        var result = await _posReceiptService.AuthorizeReprintAsync(
            context, receiptId, request, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Ok(new { data = result.Value })
            : ToErrorResult(result.Error);
    }

    [HttpPost("{saleId:guid}/print")]
    [ProducesResponseType(typeof(PosReceiptPrintResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordPrint(
        Guid saleId,
        [FromBody] PosReceiptPrintRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(
                new ApplicationError("pos_receipts.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _posReceiptService.RecordPrintAsync(
            context,
            saleId,
            request,
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(new { data = result.Value });
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "pos_receipts.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pos_receipts.reprint_permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "pos_receipts.receipt_not_found" => NotFound(CreateError(error)),
            "pos_receipts.receipt_not_completed" => UnprocessableEntity(CreateError(error)),
            "pos_receipts.reprint_not_allowed" => UnprocessableEntity(CreateError(error)),
            "pos_receipts.reprint_not_authorized" => UnprocessableEntity(CreateError(error)),
            "pos_receipts.duplicate_reprint_operation" => Conflict(CreateError(error)),
            "pos_receipts.invalid_sale_id" or
            "pos_receipts.invalid_copies" or
            "pos_receipts.invalid_print_status" or
            "pos_receipts.invalid_receipt_id" or
            "pos_receipts.invalid_search" or
            "pos_receipts.invalid_reprint_reason" or
            "pos_receipts.invalid_reprint_audit"
            or "pos_receipts.invalid_receipt_purpose"
            or "pos_receipts.invalid_printer_configuration"
            or "pos_receipts.invalid_copy_identity"
                => BadRequest(CreateError(error)),
            "pos_receipts.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
        };
    }

    private object CreateError(ApplicationError error)
    {
        return new
        {
            code = error.Code,
            message = error.Message,
            details = Array.Empty<string>(),
            traceId = HttpContext.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        };
    }
}
