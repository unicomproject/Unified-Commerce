using System.Text.Json;
using E_POS.Application.Modules.Tenant.POSOperations.Contracts;

namespace E_POS.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {

            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "Unhandled exception after the response started. TraceId: {TraceId}", context.TraceIdentifier);
                throw;
            }

            await WriteSafeErrorResponseAsync(context, exception);
        }
    }

    private async Task WriteSafeErrorResponseAsync(HttpContext context, Exception exception)
    {
        // Return a safe standard error without leaking stack traces or infrastructure details.
        if (exception is MissingSystemPosSalesChannelException configurationException)
        {
            _logger.LogError(
                exception,
                "Required system configuration unavailable. TenantId: {TenantId}; Operation: {Operation}; ConfigurationType: {ConfigurationType}; TraceId: {TraceId}",
                configurationException.TenantId,
                configurationException.Operation,
                configurationException.ConfigurationType,
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception while processing request. TraceId: {TraceId}", context.TraceIdentifier);
        }

        var mapped = DatabaseExceptionMapper.Map(exception);

        context.Response.Clear();
        context.Response.StatusCode = mapped.StatusCode;
        context.Response.ContentType = "application/json";

        var error = new
        {
            code = mapped.Code,
            message = mapped.Message,
            details = new[] { exception.ToString() },
            traceId = context.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, error, cancellationToken: context.RequestAborted);
    }
}
