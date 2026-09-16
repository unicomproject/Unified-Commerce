using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Application.Common.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Domain.Modules.Platform.Subscription.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace E_POS.ApiTests.HardwareCash;

public sealed class HardwareEntitlementFilterTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UsesCanonicalHardwareEntitlementAndDeniesDisabled(bool enabled)
    {
        var tenant = Guid.NewGuid();
        var evaluator = DispatchProxy.Create<ITenantFeatureEntitlementEvaluator, EvaluatorProxy>();
        var proxy = (EvaluatorProxy)(object)evaluator;
        proxy.Enabled = enabled;
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", Guid.NewGuid().ToString()), new Claim("tenant_id", tenant.ToString())], "test")) };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(action, [], new Dictionary<string, object?>(), new object());
        var invoked = false;
        await new HardwareEntitlementFilter(new TenantRequestContextFactory(), evaluator, new Clock())
            .OnActionExecutionAsync(executing, () => {
                invoked = true;
                return Task.FromResult(new ActionExecutedContext(action, [], new object()));
            });
        Assert.Equal(enabled, invoked);
        Assert.Equal(tenant, proxy.Tenant);
        Assert.Equal(PlatformTenantFeatureCodes.HardwareDeviceManagement, proxy.Feature);
        if (!enabled) Assert.Equal(403, Assert.IsType<ObjectResult>(executing.Result).StatusCode);
    }

    public class EvaluatorProxy : DispatchProxy
    {
        public bool Enabled;
        public Guid Tenant;
        public string? Feature;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            Assert.Equal("IsEnabledAsync", method?.Name);
            Tenant = (Guid)args![0]!;
            Feature = (string)args[1]!;
            return Task.FromResult(Enabled);
        }
    }

    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
