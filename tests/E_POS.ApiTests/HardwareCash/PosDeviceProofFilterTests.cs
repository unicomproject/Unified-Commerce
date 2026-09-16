using System.Security.Claims;
using System.Reflection;
using E_POS.Api.Common;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.OutletTillDevice.Contracts;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace E_POS.ApiTests.HardwareCash;

public class PosDeviceProofFilterTests
{
    [Theory]
    [InlineData("", "android", true, false)]
    [InlineData("pos-android-build-id", "android", true, false)]
    [InlineData("valid", "web", true, false)]
    [InlineData("valid", "android", false, false)]
    [InlineData("wrong", "android", true, false)]
    [InlineData("valid", "android", true, true)]
    public async Task RequiresRegisteredNativeProofAndMatchingTarget(string supplied, string platform, bool sameTarget, bool allowed)
    {
        var tenant = Guid.NewGuid();
        var user = Guid.NewGuid();
        var proof = "pos-device-v2-" + new string('a', 64);
        var device = PosDevice.Create(Guid.NewGuid(), tenant, Guid.NewGuid(), "P1", "POS", "TABLET", "ACTIVE", user, DateTimeOffset.UtcNow);
        device.PairForActivation("POS", "TABLET", platform, "test", "hashed-proof", user, DateTimeOffset.UtcNow);
        var repository = DispatchProxy.Create<IDeviceContextRepository, RepoProxy>();
        ((RepoProxy)(object)repository).Resolve = args => (Guid)args[0]! == tenant && (string)args[1]! == proof ? device : null;
        var hardware = DispatchProxy.Create<IPosHardwareRepository, RepoProxy>();
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", user.ToString()), new Claim("tenant_id", tenant.ToString())], "test")) };
        http.Request.Headers[PosDeviceProofFilter.HeaderName] = supplied switch {
            "valid" => proof, "wrong" => "pos-device-v2-" + new string('b', 64), _ => supplied
        };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(action, [], new Dictionary<string, object?> {
            ["posDeviceId"] = sameTarget ? device.Id : Guid.NewGuid()
        }, new object());
        var invoked = false;
        await new PosDeviceProofFilter(new TenantRequestContextFactory(), repository, hardware)
            .OnActionExecutionAsync(executing, () => {
                invoked = true;
                return Task.FromResult(new ActionExecutedContext(action, [], new object()));
            });
        Assert.Equal(allowed, invoked);
        if (!allowed) Assert.Equal(403, Assert.IsType<ObjectResult>(executing.Result).StatusCode);
    }

    public class RepoProxy : DispatchProxy
    {
        public Func<object?[], PosDevice?> Resolve { get; set; } = _ => null;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "GetEditableByFingerprintAsync"
                ? Task.FromResult(Resolve(args!))
                : throw new InvalidOperationException("Unexpected repository call");
    }
}
