using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace E_POS.Api.Common;

public sealed class HardwareEntitlementFilter(ITenantRequestContextFactory contexts,
    ITenantFeatureEntitlementEvaluator entitlements, IDateTimeProvider clock) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext action, ActionExecutionDelegate next)
    {
        if (!contexts.TryCreate(action.HttpContext.User, out var context)) { action.Result = new UnauthorizedResult(); return; }
        if (!await entitlements.IsEnabledAsync(context.TenantId, PlatformTenantFeatureCodes.HardwareDeviceManagement,
            clock.UtcNow, action.HttpContext.RequestAborted))
        {
            action.Result = new ObjectResult(new { code = "hardware.entitlement_denied", message = "Hardware device management is not enabled for this tenant." }) { StatusCode = 403 };
            return;
        }
        await next();
    }
}
