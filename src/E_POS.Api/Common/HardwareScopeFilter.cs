using System.Text.Json;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace E_POS.Api.Common;

/// <summary>Additional server-owned hardware restrictions. Never grants a permission or entitlement.</summary>
public sealed class HardwareScopeRestriction
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid OutletId { get; set; }
    public Guid TillId { get; set; }
}

/// <summary>
/// Named accounts can be narrowed to one hardware outlet/till without changing their
/// non-hardware access. Lists use a request-scoped database restriction. Native POS tests
/// require verified device proof and an active assignment to the permitted till.
/// </summary>
public sealed class HardwareScopeFilter(ITenantRequestContextFactory contexts,
    IConfiguration configuration, ITenantAdminHardwareRepository repository, HardwareQueryScope? queryScope = null) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext action, ActionExecutionDelegate next)
    {
        if (!contexts.TryCreate(action.HttpContext.User, out var actor))
        {
            action.Result = new UnauthorizedResult();
            return;
        }
        var rules = configuration.GetSection("HardwareAccess:Restrictions").Get<HardwareScopeRestriction[]>() ?? [];
        var matching = rules.Where(r => r.TenantId == actor.TenantId && r.UserId == actor.UserId).ToArray();
        if (matching.Length == 0) { await next(); return; }
        var scope = matching[0];
        var ct = action.HttpContext.RequestAborted;
        // Duplicate or incomplete rules are configuration errors, never permission to broaden access.
        if (matching.Length != 1 || scope.OutletId == Guid.Empty || scope.TillId == Guid.Empty)
        { Deny(action); return; }
        var till = await repository.GetTillAsync(actor.TenantId, scope.TillId, ct);
        if (till is null || till.OutletId != scope.OutletId || till.Status != "ACTIVE")
        { Deny(action); return; }
        var name = (action.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo.Name;
        if (action.Controller is E_POS.Api.Controllers.PosHardwareController &&
            name is "GetConfigurations" or "SaveConfiguration" or "CreateTest" or "CompleteTest" or "GetHistory" or "GetRemoteTests")
        {
            if (action.HttpContext.Items["VerifiedPosDeviceId"] is not Guid posId ||
                !await repository.IsTrustedPosAssignedToTillAsync(actor.TenantId, scope.OutletId, scope.TillId, posId, ct))
            { Deny(action); return; }
            await next();
            return;
        }
        var args = action.ActionArguments;
        Guid Id(string key) => args.TryGetValue(key, out var value) && value is Guid id ? id : Guid.Empty;
        args.TryGetValue("request", out var body);
        bool allowed = false;
        if (action.Controller is E_POS.Api.Controllers.V1.Tenant.HardwareCash.PosHardwareTelemetryController)
        {
            if (action.HttpContext.Items["VerifiedPosDeviceId"] is not Guid posId ||
                !await repository.IsTrustedPosAssignedToTillAsync(actor.TenantId, scope.OutletId, scope.TillId, posId, ct))
            { Deny(action); return; }
            if (name == "ReportTest" && body is PosHardwareTestResultRequest result)
                allowed = await DeviceAllowed(result.HardwareDeviceId);
            if (name == "HardwareHeartbeat" && body is PosHardwareHeartbeatRequest heartbeat && heartbeat.Hardware is not null)
            {
                allowed = true;
                foreach (var item in heartbeat.Hardware)
                    if (!await DeviceAllowed(item.HardwareDeviceId)) { allowed = false; break; }
            }
            if (!allowed) { Deny(action); return; }
            await next();
            return;
        }
        switch (name)
        {
            case "List":
            case "Dashboard":
                allowed = queryScope is not null &&
                    (!args.TryGetValue("outletId", out var requestedOutlet) || requestedOutlet is null ||
                     requestedOutlet is Guid requestedId && requestedId == scope.OutletId);
                if (allowed) queryScope!.Restrict(actor.TenantId, actor.UserId, scope.OutletId, scope.TillId);
                break;
            case "CreateOptions":
                allowed = queryScope is not null;
                if (allowed) queryScope!.Restrict(actor.TenantId, actor.UserId, scope.OutletId, scope.TillId);
                break;
            case "StartTestAll":
            case "GetTestAll": allowed = Id("tillId") == scope.TillId && queryScope is not null;
                if (allowed) queryScope!.Restrict(actor.TenantId, actor.UserId, scope.OutletId, scope.TillId);
                break;
            case "GetHardwareReadiness": allowed = Id("id") == scope.TillId; break;
            case "GetById":
            case "Update": allowed = await DeviceAllowed(Id("id")); break;
            case "TestHistory":
            case "Activity":
                allowed = queryScope is not null && await DeviceAllowed(Id("id"));
                if (allowed) queryScope!.Restrict(actor.TenantId, actor.UserId, scope.OutletId, scope.TillId);
                break;
            case "Create" when body is TenantAdminHardwareDeviceCreateRequest create:
                allowed = create.OutletId == scope.OutletId && await ParentAllowed(create.ConfigJson);
                break;
            case "AssignToTill" when body is TenantAdminHardwareAssignmentRequest assign:
                allowed = Id("tillId") == scope.TillId && await DeviceAllowed(assign.HardwareDeviceId);
                break;
            case "Release":
                var assignment = await repository.GetAssignmentAsync(actor.TenantId, Id("assignmentId"), ct);
                allowed = assignment is not null && assignment.OutletId == scope.OutletId &&
                    assignment.TillId == scope.TillId && assignment.PosDeviceId is null && assignment.ReleasedAt is null &&
                    await DeviceAllowed(assignment.HardwareDeviceId);
                break;
        }
        if (!allowed) { Deny(action); return; }
        await next();

        async Task<bool> ParentAllowed(string? config)
        {
            if (string.IsNullOrWhiteSpace(config)) return true;
            try
            {
                using var json = JsonDocument.Parse(config);
                if (json.RootElement.ValueKind != JsonValueKind.Object) return false;
                if (!json.RootElement.TryGetProperty("parentPrinterId", out var parent)) return true;
                return parent.ValueKind == JsonValueKind.String && parent.TryGetGuid(out var id) && await DeviceAllowed(id);
            }
            catch (JsonException) { return false; }
        }
        async Task<bool> DeviceAllowed(Guid id)
        {
            if (id == Guid.Empty) return false;
            var row = await repository.GetDetailAsync(actor.TenantId, id, ct);
            if (row is null || row.Device.TenantId != actor.TenantId || row.Device.OutletId != scope.OutletId) return false;
            var assignment = row.ActiveAssignment;
            if (assignment is not null)
                return assignment.TenantId == actor.TenantId && assignment.OutletId == scope.OutletId &&
                    assignment.TillId == scope.TillId && assignment.PosDeviceId is null && assignment.ReleasedAt is null;
            // Unassigned hardware created by another operator is not a shared pool for this scope.
            return row.Device.CreatedByTenantUserId == actor.UserId;
        }
    }
    private static void Deny(ActionExecutingContext action) => action.Result = new ObjectResult(new {
        code = "hardware.scope_denied", message = "This hardware operation is outside your permitted outlet/till scope."
    }) { StatusCode = StatusCodes.Status403Forbidden };
}


