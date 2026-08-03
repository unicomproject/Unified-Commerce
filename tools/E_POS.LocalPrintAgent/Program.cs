using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Idempotency;
using E_POS.LocalPrintAgent.Models;
using E_POS.LocalPrintAgent.Observability;
using E_POS.LocalPrintAgent.Printing;
using E_POS.LocalPrintAgent.Security;
using E_POS.LocalPrintAgent.Validation;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(options => options.ServiceName = "E_POS.LocalPrintAgent");

var startupOptions = builder.Configuration
    .GetSection(PrintAgentOptions.SectionName)
    .Get<PrintAgentOptions>() ?? new PrintAgentOptions();
builder.WebHost.UseUrls(startupOptions.ListenUrl);
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = startupOptions.RequestBodyLimit);

var rollingFileLogger = new RollingFileLoggerProvider(
    startupOptions, builder.Environment.ContentRootPath);
builder.Logging.AddProvider(rollingFileLogger);
builder.Services.AddSingleton(rollingFileLogger);
builder.Services.AddOptions<PrintAgentOptions>()
    .Bind(builder.Configuration.GetSection(PrintAgentOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(x => !string.Equals(x.LocalApiKey, "CHANGE_ME", StringComparison.OrdinalIgnoreCase),
        "PrintAgent:LocalApiKey must be supplied securely and cannot use a placeholder.")
    .Validate(x => x.AllowedNetworkRanges.Length > 0,
        "PrintAgent:AllowedNetworkRanges must contain at least one explicit CIDR range.")
    .Validate(x =>
    {
        try
        {
            _ = new NetworkRangeAllowList(x.AllowedNetworkRanges);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }, "PrintAgent:AllowedNetworkRanges contains an invalid CIDR range.")
    .ValidateOnStart();
builder.Services.AddSingleton(sp =>
    new NetworkRangeAllowList(sp.GetRequiredService<IOptions<PrintAgentOptions>>()
        .Value.AllowedNetworkRanges));
builder.Services.AddSingleton<AgentDiagnosticsCounters>();
builder.Services.AddSingleton<LocalApiKeyAuthenticator>();
builder.Services.AddSingleton<ReceiptPrintRequestValidator>();
builder.Services.AddSingleton<DrawerOpenRequestValidator>();
builder.Services.AddSingleton<IEscPosReceiptBuilder, EscPosReceiptBuilder>();
builder.Services.AddSingleton<IDrawerPulseBuilder, EscPosDrawerPulseBuilder>();
builder.Services.AddSingleton<IPrinterService, WindowsRawPrinterService>();
builder.Services.AddSingleton<IPrintRequestStore, FilePrintRequestStore>();

var app = builder.Build();
var startedAt = DateTimeOffset.UtcNow;
var diagnostics = app.Services.GetRequiredService<AgentDiagnosticsCounters>();
var requestStore = app.Services.GetRequiredService<IPrintRequestStore>();
diagnostics.SetServiceStarts(requestStore.RecordAgentStart());
var stopping = false;
app.Lifetime.ApplicationStopping.Register(() =>
{
    stopping = true;
    app.Logger.LogInformation("E_POS Local Print Agent shutdown started.");
});
app.Lifetime.ApplicationStopped.Register(() =>
    app.Logger.LogInformation("E_POS Local Print Agent stopped."));

app.Use(async (context, next) =>
{
    try
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Cache-Control"] = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";

        var allowList = context.RequestServices.GetRequiredService<NetworkRangeAllowList>();
        if (!allowList.IsAllowed(context.Connection.RemoteIpAddress))
        {
            app.Logger.LogWarning(
                "Request rejected because remote address is outside the configured LAN allow-list.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(false, "network_not_allowed", "This device is not allowed to access the print agent."));
            return;
        }
        if (stopping)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(false, "agent_stopping", "The print agent is shutting down."));
            return;
        }
        await next();
    }
    catch (BadHttpRequestException)
    {
        await WriteSafeError(context, 400, "invalid_request", "The request body is invalid.");
    }
    catch (IdempotencyRecordCorruptedException exception)
    {
        app.Logger.LogError(
            "Corrupted idempotency record detected for request {RequestId}; the record was not treated as missing.",
            exception.RequestId);
        context.RequestServices.GetRequiredService<AgentDiagnosticsCounters>().StoreError();
        await WriteSafeError(context, 503, "idempotency_record_corrupted",
            "The saved print operation requires administrator recovery.");
    }
    catch (UnauthorizedAccessException)
    {
        context.RequestServices.GetRequiredService<AgentDiagnosticsCounters>()
            .StoreError();
        app.Logger.LogError(
            "Local Print Agent filesystem access was denied.");
        await WriteSafeError(context, 503, "storage_access_denied",
            "The print agent cannot access its protected operational storage.");
    }
    catch (IOException exception)
    {
        context.RequestServices.GetRequiredService<AgentDiagnosticsCounters>()
            .StoreError();
        var diskFull = IsDiskFull(exception);
        app.Logger.LogError(
            "Local Print Agent storage I/O failed. category={Category}",
            diskFull ? "disk_full" : "storage_io_failure");
        await WriteSafeError(context, 503,
            diskFull ? "disk_full" : "storage_io_failure",
            diskFull
                ? "The print agent disk does not have enough free space."
                : "The print agent operational storage is unavailable.");
    }
    catch (Exception exception)
    {
        app.Logger.LogError(
            "Unhandled Local Print Agent error. category={Category}",
            exception.GetType().Name);
        await WriteSafeError(context, 500, "agent_error",
            "The local print agent could not complete the request.");
    }
});

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "live",
    agentVersion = ThisAssembly.Version,
    apiVersion = PrintAgentOptions.ApiVersion
}));

app.MapGet("/health/ready", async (
    IPrinterService printer,
    IPrintRequestStore store,
    CancellationToken cancellationToken) =>
{
    var storeReady = await store.ProbeAsync(cancellationToken);
    var health = await printer.GetHealthAsync(cancellationToken);
    var ready = storeReady && health.PrinterExists && health.Ready;
    return Results.Json(new AgentReadiness(
            ready,
            ready ? "ready" : "not_ready",
            ThisAssembly.Version,
            PrintAgentOptions.ApiVersion,
            PrintAgentOptions.ReceiptContractVersion,
            storeReady,
            health.PrinterExists,
            health.Ready,
            ready ? null : "Configuration, idempotency storage, or printer readiness requires attention."),
        statusCode: ready ? 200 : 503);
});

app.MapGet("/api/print/health", async (
    HttpContext context,
    IPrinterService printer,
    IOptions<PrintAgentOptions> options,
    LocalApiKeyAuthenticator authenticator,
    CancellationToken cancellationToken) =>
{
    var unauthorized = Authenticate(context, authenticator);
    if (unauthorized is not null) return unauthorized;
    var health = await printer.GetHealthAsync(cancellationToken);
    return Results.Ok(health with
    {
        AgentVersion = ThisAssembly.Version,
        ApiVersion = PrintAgentOptions.ApiVersion,
        ReceiptContractVersion = PrintAgentOptions.ReceiptContractVersion,
        PaperWidth = options.Value.PaperWidth,
        AutoCut = options.Value.AutoCut,
        FeedLinesBeforeCut = options.Value.FeedLinesBeforeCut,
        StartupTimestamp = startedAt.ToString("O")
    });
});

app.MapGet("/api/print/diagnostics", (
    HttpContext context,
    LocalApiKeyAuthenticator authenticator,
    AgentDiagnosticsCounters counters,
    IPrintRequestStore store,
    RollingFileLoggerProvider fileLogger) =>
{
    var unauthorized = Authenticate(context, authenticator);
    return unauthorized ?? Results.Ok(counters.Snapshot(
        store.CountCompleted(), store.CountUnresolved(),
        fileLogger.DroppedLogEntries));
});

app.MapGet("/api/print/operations/{requestId:guid}", async (
    HttpContext context,
    Guid requestId,
    IPrintRequestStore requestStore,
    LocalApiKeyAuthenticator authenticator,
    AgentDiagnosticsCounters counters,
    CancellationToken cancellationToken) =>
{
    var unauthorized = Authenticate(context, authenticator);
    if (unauthorized is not null) return unauthorized;
    counters.OperationQueried();
    var status = await requestStore.GetStatusAsync(requestId, cancellationToken);
    app.Logger.LogInformation(
        "Print operation status queried. requestId={RequestId} state={State}",
        requestId, status?.State ?? "not_found");
    return status is null
        ? Results.NotFound(new ApiErrorResponse(false, "operation_not_found",
            "No print operation exists for this request ID."))
        : Results.Ok(status);
});

app.MapPost("/api/print/receipt", async (
    HttpContext context,
    ReceiptPrintRequest request,
    ReceiptPrintRequestValidator validator,
    IEscPosReceiptBuilder receiptBuilder,
    IPrinterService printer,
    IPrintRequestStore requestStore,
    LocalApiKeyAuthenticator authenticator,
    AgentDiagnosticsCounters counters,
    IOptions<PrintAgentOptions> options,
    CancellationToken cancellationToken) =>
{
    var unauthorized = Authenticate(context, authenticator);
    if (unauthorized is not null) return unauthorized;
    if (!PrintContractCompatibility.IsSupported(request))
        return Results.BadRequest(new ApiErrorResponse(
            false, "unsupported_contract_version",
            $"Supported API/receipt contract versions are {PrintAgentOptions.ApiVersion}/{PrintAgentOptions.ReceiptContractVersion}."));

    var errors = validator.Validate(request);
    if (errors.Count > 0)
        return Results.BadRequest(new ApiErrorResponse(false, "invalid_request",
            "Receipt request validation failed.", errors));

    counters.PrintRequested();
    var stopwatch = Stopwatch.StartNew();
    app.Logger.LogInformation(
        "Print request received. requestId={RequestId} printer={PrinterName}",
        request.RequestId, options.Value.PrinterName);
    var canonicalPayload = JsonSerializer.Serialize(request);
    var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload)));
    var claim = await requestStore.TryClaimAsync(request.RequestId, payloadHash, cancellationToken);
    if (!claim.Acquired)
    {
        counters.Duplicate();
        app.Logger.LogWarning(
            "Duplicate print request blocked. requestId={RequestId} conflict={PayloadConflict}",
            request.RequestId, claim.PayloadConflict);
        var code = claim.PayloadConflict ? "idempotency_conflict" : "duplicate_request";
        var message = claim.PayloadConflict
            ? "This request ID was already used with different receipt data."
            : "This request ID was already accepted; the receipt was not printed again.";
        return Results.Conflict(new PrintApiResponse(
            false, code, message, request.RequestId, true, options.Value.PrinterName));
    }

    PrintOperationResult result;
    try
    {
        var bytes = receiptBuilder.Build(request);
        app.Logger.LogInformation(
            "Spooler submission started. requestId={RequestId} printer={PrinterName}",
            request.RequestId, options.Value.PrinterName);
        result = await printer.PrintRawAsync(
            $"Receipt {request.ReceiptNumber}", bytes, cancellationToken);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        await requestStore.CompleteAsync(
            request.RequestId, false, "request_cancelled", CancellationToken.None);
        throw;
    }
    catch
    {
        await requestStore.CompleteAsync(
            request.RequestId, false, "print_failed", CancellationToken.None);
        throw;
    }
    finally
    {
        stopwatch.Stop();
        counters.AddElapsed(stopwatch.Elapsed);
    }

    await requestStore.CompleteAsync(
        request.RequestId, result.Success, result.Code, cancellationToken);
    if (result.Success)
    {
        counters.SpoolSucceeded();
        app.Logger.LogInformation(
            "Spooler submission accepted. requestId={RequestId} result={Result} elapsedMs={ElapsedMs}",
            request.RequestId, result.Code, stopwatch.ElapsedMilliseconds);
    }
    else if (result.Code == "spooler_timeout")
    {
        counters.UnknownOutcome();
        app.Logger.LogWarning(
            "Spooler submission outcome is unknown. requestId={RequestId} result={Result} elapsedMs={ElapsedMs}",
            request.RequestId, result.Code, stopwatch.ElapsedMilliseconds);
    }
    else
    {
        counters.PrintFailed();
        app.Logger.LogWarning(
            "Spooler submission failed. requestId={RequestId} result={Result} elapsedMs={ElapsedMs}",
            request.RequestId, result.Code, stopwatch.ElapsedMilliseconds);
    }
    var response = new PrintApiResponse(
        result.Success, result.Code, result.Message, request.RequestId, false,
        result.PrinterName, result.BytesWritten);
    return result.Success
        ? Results.Ok(response)
        : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapPost("/api/drawer/open", async (
    HttpContext context,
    DrawerOpenRequest request,
    DrawerOpenRequestValidator validator,
    IDrawerPulseBuilder pulseBuilder,
    IPrinterService printer,
    IPrintRequestStore requestStore,
    LocalApiKeyAuthenticator authenticator,
    IOptions<PrintAgentOptions> options,
    CancellationToken cancellationToken) =>
{
    var unauthorized = Authenticate(context, authenticator);
    if (unauthorized is not null) return unauthorized;

    var errors = validator.Validate(request);
    if (errors.Count > 0)
        return Results.BadRequest(new ApiErrorResponse(
            false, "invalid_drawer_request",
            "Drawer-open request validation failed.", errors));

    var canonicalPayload = JsonSerializer.Serialize(request);
    var payloadHash = Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes($"drawer:{canonicalPayload}")));
    var claim = await requestStore.TryClaimAsync(
        request.RequestId, payloadHash, cancellationToken);
    if (!claim.Acquired)
    {
        var code = claim.PayloadConflict ? "request_conflict" : "request_duplicate";
        var message = claim.PayloadConflict
            ? "This request ID was already used with different drawer data."
            : "This drawer request was already accepted; no second pulse was sent.";
        return Results.Conflict(new DrawerOpenApiResponse(
            false, code, message, request.RequestId, request.DrawerOperationId,
            true, options.Value.PrinterName));
    }

    PrintOperationResult result;
    try
    {
        var bytes = pulseBuilder.Build(request);
        app.Logger.LogInformation(
            "Drawer pulse submission started. requestId={RequestId} operationId={OperationId} purpose={Purpose} printer={PrinterName}",
            request.RequestId, request.DrawerOperationId, request.DrawerPurpose,
            options.Value.PrinterName);
        result = await printer.PrintRawAsync(
            $"Cash drawer {request.DrawerOperationId}", bytes, cancellationToken);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        await requestStore.CompleteAsync(
            request.RequestId, false, "spooler_timeout", CancellationToken.None);
        throw;
    }
    catch
    {
        await requestStore.CompleteAsync(
            request.RequestId, false, "spooler_rejected", CancellationToken.None);
        throw;
    }

    await requestStore.CompleteAsync(
        request.RequestId, result.Success, result.Code, cancellationToken);
    var response = new DrawerOpenApiResponse(
        result.Success, result.Code,
        result.Success
            ? "Drawer pulse was accepted by the Windows spooler; verify the drawer physically."
            : result.Message,
        request.RequestId, request.DrawerOperationId, false,
        result.PrinterName, result.BytesWritten);
    return result.Success
        ? Results.Ok(response)
        : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.Logger.LogInformation(
    "E_POS Local Print Agent started. version={Version} apiVersion={ApiVersion} receiptContractVersion={ReceiptContractVersion}",
    ThisAssembly.Version, PrintAgentOptions.ApiVersion,
    PrintAgentOptions.ReceiptContractVersion);
app.Run();

static IResult? Authenticate(
    HttpContext context,
    LocalApiKeyAuthenticator authenticator)
{
    var result = authenticator.Authenticate(context.Request);
    if (result.IsAuthenticated) return null;
    return Results.Json(
        new ApiErrorResponse(false,
            result.IsRateLimited ? "authentication_rate_limited" : "unauthorized",
            result.IsRateLimited
                ? "Too many failed authentication attempts. Try again later."
                : "A valid local print API key is required."),
        statusCode: result.IsRateLimited ? 429 : 401);
}

static async Task WriteSafeError(
    HttpContext context, int status, string code, string message)
{
    if (context.Response.HasStarted) return;
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(
        new ApiErrorResponse(false, code, message));
}

static bool IsDiskFull(IOException exception)
{
    const int ErrorDiskFull = 0x70;
    const int ErrorHandleDiskFull = 0x27;
    var code = exception.HResult & 0xFFFF;
    return code is ErrorDiskFull or ErrorHandleDiskFull;
}

public partial class Program;
