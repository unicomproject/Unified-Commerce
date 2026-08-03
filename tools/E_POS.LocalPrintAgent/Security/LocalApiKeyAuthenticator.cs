using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using E_POS.LocalPrintAgent.Configuration;
using E_POS.LocalPrintAgent.Observability;
using Microsoft.Extensions.Options;

namespace E_POS.LocalPrintAgent.Security;

public sealed class LocalApiKeyAuthenticator(
    IOptions<PrintAgentOptions> options,
    AgentDiagnosticsCounters counters,
    ILogger<LocalApiKeyAuthenticator> logger)
{
    private readonly PrintAgentOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, FailureWindow> _failures = new();

    public AuthenticationResult Authenticate(HttpRequest request)
    {
        var remote = request.HttpContext.Connection.RemoteIpAddress;
        var key = remote?.ToString() ?? "unknown";
        var now = DateTimeOffset.UtcNow;
        if (_failures.TryGetValue(key, out var window) &&
            window.BlockedUntil > now)
            return new(false, true);

        var valid = request.Headers.TryGetValue("X-Local-Print-Key", out var supplied) &&
                    FixedTimeEquals(_options.LocalApiKey, supplied.ToString());
        if (valid)
        {
            _failures.TryRemove(key, out _);
            return new(true, false);
        }

        counters.AuthenticationFailed();
        var updated = _failures.AddOrUpdate(
            key,
            _ => new FailureWindow(1, now, null),
            (_, current) => now - current.StartedAt >
                            TimeSpan.FromMinutes(_options.FailedAuthenticationWindowMinutes)
                ? new FailureWindow(1, now, null)
                : current with { Count = current.Count + 1 });
        if (updated.Count >= _options.FailedAuthenticationLimit)
        {
            updated = updated with
            {
                BlockedUntil = now.AddMinutes(_options.FailedAuthenticationWindowMinutes)
            };
            _failures[key] = updated;
        }
        logger.LogWarning(
            "Local Print Agent authentication failed for remote address {RemoteAddress}; rateLimited={RateLimited}.",
            SafeAddress(remote), updated.BlockedUntil > now);
        return new(false, updated.BlockedUntil > now);
    }

    public static bool FixedTimeEquals(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }

    private static string SafeAddress(IPAddress? address) =>
        address is null ? "unknown" : address.ToString();

    private sealed record FailureWindow(
        int Count,
        DateTimeOffset StartedAt,
        DateTimeOffset? BlockedUntil);
}

public sealed record AuthenticationResult(bool IsAuthenticated, bool IsRateLimited);
