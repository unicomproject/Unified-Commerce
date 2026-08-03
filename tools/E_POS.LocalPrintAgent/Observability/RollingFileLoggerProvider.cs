using System.Collections.Concurrent;
using System.Text;
using E_POS.LocalPrintAgent.Configuration;

namespace E_POS.LocalPrintAgent.Observability;

public sealed class RollingFileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly long _maxBytes;
    private readonly int _retentionDays;
    private readonly long _minimumFreeDiskBytes;
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new();
    private long _droppedLogEntries;

    public RollingFileLoggerProvider(PrintAgentOptions options, string contentRoot)
    {
        _directory = Path.GetFullPath(options.LoggingDirectory, contentRoot);
        _maxBytes = options.MaxLogFileBytes;
        _retentionDays = options.LogRetentionDays;
        _minimumFreeDiskBytes = options.MinimumFreeDiskBytes;
        Directory.CreateDirectory(_directory);
        Cleanup();
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, category => new FileLogger(this, category));

    internal void Write(LogLevel level, string category, EventId eventId, string message)
    {
        if (level < LogLevel.Information) return;
        var safeCategory = category.Length <= 120 ? category : category[..120];
        var line =
            $"{DateTimeOffset.UtcNow:O}\t{level}\t{eventId.Id}\t{safeCategory}\t{Sanitize(message)}{Environment.NewLine}";
        lock (_gate)
        {
            try
            {
                var root = Path.GetPathRoot(_directory);
                if (!string.IsNullOrWhiteSpace(root) &&
                    new DriveInfo(root).AvailableFreeSpace <
                    _minimumFreeDiskBytes)
                {
                    Interlocked.Increment(ref _droppedLogEntries);
                    return;
                }
                var path = CurrentPath();
                File.AppendAllText(path, line, Encoding.UTF8);
            }
            catch (IOException)
            {
                Interlocked.Increment(ref _droppedLogEntries);
            }
            catch (UnauthorizedAccessException)
            {
                Interlocked.Increment(ref _droppedLogEntries);
            }
        }
    }

    public long DroppedLogEntries =>
        Interlocked.Read(ref _droppedLogEntries);

    private string CurrentPath()
    {
        var baseName = $"print-agent-{DateTime.UtcNow:yyyyMMdd}";
        var path = Path.Combine(_directory, $"{baseName}.log");
        if (!File.Exists(path) || new FileInfo(path).Length < _maxBytes) return path;
        for (var index = 1; index < 1000; index++)
        {
            path = Path.Combine(_directory, $"{baseName}-{index:D3}.log");
            if (!File.Exists(path) || new FileInfo(path).Length < _maxBytes) return path;
        }
        throw new IOException("The rolling log file limit was exceeded.");
    }

    private void Cleanup()
    {
        var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);
        foreach (var path in Directory.EnumerateFiles(_directory, "print-agent-*.log"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(path) < cutoff) File.Delete(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static string Sanitize(string value) =>
        value.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    public void Dispose() => _loggers.Clear();

    private sealed class FileLogger(RollingFileLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
                owner.Write(logLevel, category, eventId, formatter(state, null));
        }
    }
}
