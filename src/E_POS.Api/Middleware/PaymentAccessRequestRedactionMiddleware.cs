using Microsoft.AspNetCore.Http.Features;

namespace E_POS.Api.Middleware;

public sealed class PaymentAccessRequestRedactionMiddleware
{
    private const string Prefix = "/api/v1/tenant-onboarding/payment-access/";
    private readonly RequestDelegate _next;
    public PaymentAccessRequestRedactionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var sensitive = context.Request.Path.StartsWithSegments(Prefix.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        try
        {
            await _next(context);
        }
        finally
        {
            if (sensitive)
            {
                context.Request.Path = $"{Prefix}redacted";
                var feature = context.Features.Get<IHttpRequestFeature>();
                if (feature is not null) feature.RawTarget = $"{Prefix}redacted";
            }
        }
    }
}
