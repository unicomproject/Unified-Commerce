using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/return-policies")]
public sealed class ReturnPoliciesController : ControllerBase
{
    private readonly IReturnPolicyService _service;
    private readonly ITenantRequestContextFactory _tenantRequestContextFactory;

    public ReturnPoliciesController(IReturnPolicyService service, ITenantRequestContextFactory tenantRequestContextFactory)
    {
        _service = service;
        _tenantRequestContextFactory = tenantRequestContextFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReturnPolicyCreateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("return_policies.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _service.CreateAsync(context, request, cancellationToken);
        return result.IsSuccess && result.Value is not null ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("return_policies.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _service.ListAsync(context, pageNumber, pageSize, search, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("return_policies.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _service.GetByIdAsync(context, id, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ReturnPolicyUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("return_policies.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _service.UpdateAsync(context, id, request, cancellationToken);
        return result.IsSuccess && result.Value is not null ? Ok(result.Value) : ToErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantRequestContextFactory.TryCreate(User, out var context)) return Unauthorized(CreateError(new ApplicationError("return_policies.invalid_tenant_context", "Invalid tenant context.")));
        var result = await _service.DeleteAsync(context, id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorResult(result.Error);
    }

    private IActionResult ToErrorResult(ApplicationError error)
    {
        return error.Code switch
        {
            "return_policies.permission_denied" => StatusCode(StatusCodes.Status403Forbidden, CreateError(error)),
            "return_policies.not_found" => NotFound(CreateError(error)),
            "return_policies.duplicate_code" => Conflict(CreateError(error)),
            "return_policies.invalid_tenant_context" => Unauthorized(CreateError(error)),
            _ => BadRequest(CreateError(error))
        };
    }

    private object CreateError(ApplicationError error)
    {
        return new { code = error.Code, message = error.Message, details = Array.Empty<string>(), traceId = HttpContext.TraceIdentifier, timestamp = DateTimeOffset.UtcNow };
    }
}

