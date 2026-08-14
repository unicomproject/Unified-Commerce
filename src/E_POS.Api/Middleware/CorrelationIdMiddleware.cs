using E_POS.Application.Common.Contracts;

namespace E_POS.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxCorrelationIdLength = 128;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestCorrelationAccessor correlationAccessor)
    {
        var correlationId = ResolveCorrelationId(context);
        correlationAccessor.Set(correlationId);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await _next(context);
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            var candidate = headerValue.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= MaxCorrelationIdLength)
            {
                return candidate;
            }
        }

        if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
        {
            return context.TraceIdentifier;
        }

        return Guid.NewGuid().ToString("N");
    }
}
