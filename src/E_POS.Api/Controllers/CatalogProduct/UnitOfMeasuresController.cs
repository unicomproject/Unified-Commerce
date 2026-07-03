using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.CatalogProduct.Contracts;
using E_POS.Application.Modules.CatalogProduct.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/unit-of-measures")]
public sealed class UnitOfMeasuresController : ControllerBase
{
    private readonly IUnitOfMeasureService _unitOfMeasureService;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public UnitOfMeasuresController(IUnitOfMeasureService unitOfMeasureService, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _unitOfMeasureService = unitOfMeasureService;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UnitOfMeasureListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context))
        {
            return Unauthorized(CreateError(new ApplicationError("unit_of_measure.invalid_tenant_context", "Invalid tenant context.")));
        }

        var result = await _unitOfMeasureService.ListAsync(context, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "unit_of_measure.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "unit_of_measure.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
        };
    }

    private object CreateError(ApplicationError error)
    {
        return new { code = error.Code, message = error.Message, details = Array.Empty<string>(), traceId = HttpContext.TraceIdentifier, timestamp = DateTimeOffset.UtcNow };
    }
}