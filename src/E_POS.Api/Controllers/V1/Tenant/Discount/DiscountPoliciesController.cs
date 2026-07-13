using E_POS.Api.Common;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.Discount.Contracts;
using E_POS.Application.Modules.Tenant.Discount.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantOnly")]
[Route("api/v1/discount-policies")]
public sealed class DiscountPoliciesController : ControllerBase
{
    private readonly IDiscountPolicyAdminService _service;
    private readonly ITenantRequestContextFactory _contexts;
    public DiscountPoliciesController(IDiscountPolicyAdminService service, ITenantRequestContextFactory contexts)
    { _service = service; _contexts = contexts; }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Respond(await WithContext((x) => _service.ListAsync(x, ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Respond(await WithContext((x) => _service.GetAsync(x, id, ct)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DiscountPolicyAdminRequestDto request, CancellationToken ct)
    {
        var result = await WithContext((x) => _service.CreateAsync(x, request, ct));
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, new { data = result.Value }) : Respond(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DiscountPolicyAdminRequestDto request, CancellationToken ct) =>
        Respond(await WithContext((x) => _service.UpdateAsync(x, id, request, ct)));

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct) =>
        Respond(await WithContext((x) => _service.SetActiveAsync(x, id, true, ct)));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct) =>
        Respond(await WithContext((x) => _service.SetActiveAsync(x, id, false, ct)));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        Respond(await WithContext((x) => _service.DeleteAsync(x, id, ct)));

    private async Task<ApplicationResult<T>> WithContext<T>(Func<TenantRequestContext, Task<ApplicationResult<T>>> action)
    {
        if (!_contexts.TryCreate(User, out var context))
            return ApplicationResult<T>.Failure(new("discount_policy.invalid_tenant_context", "Invalid tenant context."));
        return await action(context);
    }

    private IActionResult Respond<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess) return Ok(new { data = result.Value });
        var body = new { code = result.Error.Code, message = result.Error.Message,
            details = Array.Empty<string>(), traceId = HttpContext.TraceIdentifier, timestamp = DateTimeOffset.UtcNow };
        if (result.Error.Code.EndsWith("permission_denied")) return StatusCode(403, body);
        if (result.Error.Code.EndsWith("not_found")) return NotFound(body);
        if (result.Error.Code.Contains("duplicate") || result.Error.Code.Contains("concurrency")) return Conflict(body);
        if (result.Error.Code.EndsWith("invalid_tenant_context")) return Unauthorized(body);
        return BadRequest(body);
    }
}
