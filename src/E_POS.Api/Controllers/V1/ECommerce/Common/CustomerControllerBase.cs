using System.Security.Claims;
using E_POS.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers.V1.ECommerce;

public abstract class CustomerControllerBase : ControllerBase
{
    protected bool TryGetCustomerContext(out Guid tenantId, out Guid customerId)
    {
        tenantId = Guid.Empty;
        customerId = Guid.Empty;
        var customerValue = User.FindFirstValue("sub") ??
                            User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId) &&
               Guid.TryParse(customerValue, out customerId);
    }

    protected IActionResult InvalidSession() =>
        Unauthorized(CreateError(new ApplicationError(
            "customer.invalid_session",
            "A valid customer session is required.")));

    protected object CreateError(ApplicationError error) => new
    {
        success = false,
        message = error.Message,
        errorCode = error.Code,
        errors = Array.Empty<string>(),
        traceId = HttpContext.TraceIdentifier
    };
}
