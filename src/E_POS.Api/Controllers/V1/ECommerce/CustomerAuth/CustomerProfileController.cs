using System.Security.Claims;
using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.CustomerAuth;

[ApiController]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v1/ecommerce/storefront/customer/profile")]
public sealed class CustomerProfileController : ControllerBase
{
    private readonly ICustomerAuthService _service;

    public CustomerProfileController(ICustomerAuthService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (!TryGetSessionContext(out var tenantId, out var customerId))
            return Unauthorized(CreateError(new ApplicationError("customer.invalid_session", "Invalid customer session.")));

        var result = await _service.GetProfileAsync(tenantId, customerId, cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok(new
            {
                success = true,
                data = result.Value
            });
        }

        return BadRequest(CreateError(result.Error));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] CustomerProfileUpdateRequest request, 
        CancellationToken cancellationToken)
    {
        if (!TryGetSessionContext(out var tenantId, out var customerId))
            return Unauthorized(CreateError(new ApplicationError("customer.invalid_session", "Invalid customer session.")));

        var result = await _service.UpdateProfileAsync(tenantId, customerId, request, cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok(new
            {
                success = true,
                message = "Profile updated successfully."
            });
        }

        return BadRequest(CreateError(result.Error));
    }

    private bool TryGetSessionContext(out Guid tenantId, out Guid customerId)
    {
        tenantId = Guid.Empty;
        customerId = Guid.Empty;
        var customerValue = User.FindFirstValue("sub") ??
                            User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId) &&
               Guid.TryParse(customerValue, out customerId);
    }

    private object CreateError(ApplicationError error) => new
    {
        success = false,
        message = error.Message,
        errorCode = error.Code,
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    };
}
