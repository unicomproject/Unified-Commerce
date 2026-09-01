using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Services;
using System.Security.Claims;
using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.Customer.Contracts.Services;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.Customer;

[ApiController]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v1/ecommerce/storefront/customer/profile")]
public sealed class CustomerProfileController : CustomerControllerBase
{
    private readonly ICustomerAuthService _service;

    public CustomerProfileController(ICustomerAuthService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

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
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

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
}

