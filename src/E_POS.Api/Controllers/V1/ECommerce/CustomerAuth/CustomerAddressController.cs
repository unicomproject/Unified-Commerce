using System.Security.Claims;
using E_POS.Api.Extensions;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Contracts.Interfaces;
using E_POS.Application.Modules.ECommerce.CustomerAuth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce.CustomerAuth;

[ApiController]
[Route("api/v1/ecommerce/storefront/customer/addresses")]
[Authorize(Policy = "CustomerOnly")]
public sealed class CustomerAddressController : CustomerControllerBase
{
    private readonly ICustomerAddressService _addressService;

    public CustomerAddressController(ICustomerAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _addressService.GetAddressesAsync(tenantId, customerId, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAddressById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _addressService.GetAddressByIdAsync(tenantId, customerId, id, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] CreateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _addressService.CreateAddressAsync(tenantId, customerId, request, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateCustomerAddressRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _addressService.UpdateAddressAsync(tenantId, customerId, id, request, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _addressService.DeleteAddressAsync(tenantId, customerId, id, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/default")]
    public async Task<IActionResult> SetDefaultAddress(Guid id, [FromQuery] string type, CancellationToken cancellationToken)
    {
        if (!TryGetCustomerContext(out var tenantId, out var customerId))
            return InvalidSession();

        var result = await _addressService.SetDefaultAddressAsync(tenantId, customerId, id, type, cancellationToken);
        return HandleResult(result);
    }

    private IActionResult HandleResult<T>(ApplicationResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true, data = result.Value });
        }
        return BadRequest(CreateError(result.Error));
    }

    private IActionResult HandleResult(ApplicationResult result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true });
        }
        return BadRequest(CreateError(result.Error));
    }
}
