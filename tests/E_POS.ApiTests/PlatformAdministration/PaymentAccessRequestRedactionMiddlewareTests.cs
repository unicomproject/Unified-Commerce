using E_POS.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PaymentAccessRequestRedactionMiddlewareTests
{
    [Fact]
    public async Task PaymentAccessRoute_IsAvailableToEndpointButRedactedBeforeHostingCompletion()
    {
        var rawToken = new string('s', 43);
        var context = new DefaultHttpContext();
        context.Request.Path = $"/api/v1/tenant-onboarding/payment-access/{rawToken}/history";
        string? observedByEndpoint = null;
        var middleware = new PaymentAccessRequestRedactionMiddleware(next: ctx =>
        {
            observedByEndpoint = ctx.Request.Path;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Contains(rawToken, observedByEndpoint);
        Assert.Equal("/api/v1/tenant-onboarding/payment-access/redacted", context.Request.Path);
        Assert.DoesNotContain(rawToken, context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestFeature>()!.RawTarget);
    }
}
