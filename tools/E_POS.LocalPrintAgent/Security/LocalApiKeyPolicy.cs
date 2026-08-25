using System.Diagnostics.CodeAnalysis;

namespace E_POS.LocalPrintAgent.Security;

/// <summary>
/// Production fail-closed rules for the store-local API key.
/// </summary>
public static class LocalApiKeyPolicy
{
    private static readonly string[] PlaceholderTokens =
    [
        "CHANGE_ME",
        "CHANGEME",
        "CHANGEIT",
        "PASSWORD",
        "SECRET",
        "DEFAULT",
        "LOCAL-PRINT-KEY",
        "LOCALPRINTKEY",
        "YOUR_API_KEY",
        "REPLACE_ME",
        "TODO",
        "TEST",
        "SAMPLE",
        "EXAMPLE"
    ];

    public static bool IsAcceptable(string? key, out string reason)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            reason = "LocalApiKey is missing.";
            return false;
        }

        if (key.Length < 24)
        {
            reason = "LocalApiKey must contain at least 24 characters.";
            return false;
        }

        var normalized = key.Trim();
        foreach (var token in PlaceholderTokens)
        {
            if (normalized.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                reason = "LocalApiKey must not use a placeholder or default value.";
                return false;
            }
        }

        // Reject trivial repeated characters (e.g. all zeros).
        if (normalized.Distinct().Count() < 8)
        {
            reason = "LocalApiKey must not be a low-entropy placeholder.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public static bool IsLoopbackOnlyAllowList(IEnumerable<string> ranges)
    {
        var list = ranges.Select(static x => x.Trim()).Where(static x => x.Length > 0).ToArray();
        if (list.Length == 0) return false;
        return list.All(static range =>
            range is "127.0.0.1/32" or "::1/128" or "127.0.0.0/8");
    }

    public static bool TryGetPreferredListenUrl(
        int port,
        bool useHttps,
        IEnumerable<string> allowedRanges,
        [NotNullWhen(true)] out string? listenUrl)
    {
        if (port is < 1 or > 65535)
        {
            listenUrl = null;
            return false;
        }

        var scheme = useHttps ? "https" : "http";
        listenUrl = IsLoopbackOnlyAllowList(allowedRanges)
            ? $"{scheme}://127.0.0.1:{port}"
            : $"{scheme}://0.0.0.0:{port}";
        return true;
    }
}
