using System.Diagnostics;
using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Models;

namespace E_POS.LocalPrintAgent.Observability;

public sealed class AgentDiagnosticsCounters
{
    private long _totalPrintRequests;
    private long _successfulSpoolSubmissions;
    private long _confirmedFailures;
    private long _duplicateRequests;
    private long _unknownOutcomes;
    private long _authenticationFailures;
    private long _operationStatusQueries;
    private long _idempotencyStoreErrors;
    private long _totalElapsedMilliseconds;
    private long _serviceStarts;

    public void PrintRequested() => Interlocked.Increment(ref _totalPrintRequests);
    public void SpoolSucceeded() => Interlocked.Increment(ref _successfulSpoolSubmissions);
    public void PrintFailed() => Interlocked.Increment(ref _confirmedFailures);
    public void Duplicate() => Interlocked.Increment(ref _duplicateRequests);
    public void UnknownOutcome() => Interlocked.Increment(ref _unknownOutcomes);
    public void AuthenticationFailed() => Interlocked.Increment(ref _authenticationFailures);
    public void OperationQueried() => Interlocked.Increment(ref _operationStatusQueries);
    public void StoreError() => Interlocked.Increment(ref _idempotencyStoreErrors);
    public void AddElapsed(TimeSpan elapsed) =>
        Interlocked.Add(ref _totalElapsedMilliseconds, (long)elapsed.TotalMilliseconds);
    public void SetServiceStarts(long value) =>
        Interlocked.Exchange(ref _serviceStarts, value);

    public AgentDiagnostics Snapshot(
        long completedRequestCount,
        long unresolvedOperationCount,
        long droppedLogEntries)
    {
        var total = Interlocked.Read(ref _totalPrintRequests);
        var elapsed = Interlocked.Read(ref _totalElapsedMilliseconds);
        return new(
        ThisAssembly.Version,
        PrintAgentOptions.ApiVersion,
        PrintAgentOptions.ReceiptContractVersion,
        Interlocked.Read(ref _totalPrintRequests),
        Interlocked.Read(ref _successfulSpoolSubmissions),
        Interlocked.Read(ref _confirmedFailures),
        Interlocked.Read(ref _duplicateRequests),
        Interlocked.Read(ref _unknownOutcomes),
        Interlocked.Read(ref _authenticationFailures),
        Interlocked.Read(ref _operationStatusQueries),
        Interlocked.Read(ref _idempotencyStoreErrors),
        Interlocked.Read(ref _serviceStarts),
        droppedLogEntries,
        total == 0 ? 0 : (double)elapsed / total,
        unresolvedOperationCount,
        completedRequestCount);
    }
}

public static class ThisAssembly
{
    public static string Version
    {
        get
        {
            var informational = typeof(ThisAssembly).Assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
            {
                var plus = informational.IndexOf('+');
                return plus > 0 ? informational[..plus] : informational;
            }

            return typeof(ThisAssembly).Assembly.GetName().Version?.ToString() ?? "unknown";
        }
    }
}
