using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace E_POS.Api.Common;

/// <summary>Requires tenant authentication and possession of the high-entropy proof registered during native device activation.</summary>
public sealed class PosDeviceProofFilter(ITenantRequestContextFactory contexts,
    IDeviceContextRepository devices, IPosHardwareRepository hardware) : IAsyncActionFilter
{
    public const string HeaderName = "X-Pos-Device-Proof";
    public async Task OnActionExecutionAsync(ActionExecutingContext action, ActionExecutionDelegate next)
    {
        var request = action.HttpContext.Request;
        var proof = request.Headers[HeaderName].ToString();
        if (!contexts.TryCreate(action.HttpContext.User, out var context) ||
            proof.Length != 78 || !proof.StartsWith("pos-device-v2-", StringComparison.Ordinal) ||
            !proof.AsSpan(14).ToString().All(char.IsAsciiHexDigit))
        {
            Deny(action);
            return;
        }
        var device = await devices.GetEditableByFingerprintAsync(context.TenantId, proof, action.HttpContext.RequestAborted);
        if (device is null || !device.IsTrusted || device.Status != "ACTIVE" ||
            device.Platform?.ToLowerInvariant() is not ("android" or "ios" or "windows"))
        {
            Deny(action);
            return;
        }
        Guid? target = action.ActionArguments.TryGetValue("posDeviceId", out var id) ? id as Guid? : null;
        if (action.ActionArguments.TryGetValue("request", out var body))
            target = body switch {
                SavePosHardwareConfigurationRequest save => save.PosDeviceId,
                CreateHardwareTestRequest create => create.PosDeviceId,
                PosHardwareTestResultRequest result => result.PosDeviceId,
                _ => target
            };
        if (body is CompleteHardwareTestRequest && action.ActionArguments.TryGetValue("testId", out var testId) && testId is Guid test)
            target = await hardware.GetTestPosDeviceIdAsync(context.TenantId, test, action.HttpContext.RequestAborted);
        if (target != device.Id)
        {
            Deny(action);
            return;
        }
        action.HttpContext.Items["VerifiedPosDeviceId"] = device.Id;
        await next();
    }
    private static void Deny(ActionExecutingContext action) => action.Result = new ObjectResult(new {
        code = "hardware.device_proof_required",
        message = "Activate this native POS device again before reporting or testing hardware.",
        traceId = action.HttpContext.TraceIdentifier
    }) { StatusCode = StatusCodes.Status403Forbidden };
}
