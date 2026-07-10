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
            "pos_receipts.receipt_not_found" => NotFound(CreateError(error)),
            "pos_receipts.invalid_sale_id" or
            "pos_receipts.invalid_copies" or
            "pos_receipts.invalid_print_status"
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
