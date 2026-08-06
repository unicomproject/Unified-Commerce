using System.Collections.Concurrent;
using System.Text.Json;
using E_POS.LocalPrintAgent.Configuration;
using Microsoft.Extensions.Options;

namespace E_POS.LocalPrintAgent.Idempotency;

public sealed class FilePrintRequestStore : IPrintRequestStore
{
    private readonly string _directory;
    private readonly string _quarantineDirectory;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public FilePrintRequestStore(IOptions<PrintAgentOptions> options, IHostEnvironment environment)
    {
        _directory = Path.GetFullPath(options.Value.IdempotencyDirectory, environment.ContentRootPath);
        _quarantineDirectory = Path.Combine(_directory, "quarantine");
        Directory.CreateDirectory(_directory);
        Directory.CreateDirectory(_quarantineDirectory);
        DeleteExpiredFiles(options.Value.OperationRetentionDays);
    }

    public async Task<PrintRequestClaim> TryClaimAsync(
        Guid requestId, string payloadHash, CancellationToken cancellationToken)
    {
        var gate = _locks.GetOrAdd(requestId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var path = PathFor(requestId);
            if (File.Exists(path))
            {
                var existing = await ReadRequiredAsync(requestId, path, cancellationToken);
                return new(false, existing.PayloadHash != payloadHash, existing.ResultCode);
            }

            var claim = new StoredRequest(payloadHash, "processing", DateTimeOffset.UtcNow);
            await WriteAtomicAsync(path, claim, createNew: true, cancellationToken);
            return new(true, false, null);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task CompleteAsync(
        Guid requestId, bool success, string resultCode, CancellationToken cancellationToken)
    {
        var gate = _locks.GetOrAdd(requestId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var path = PathFor(requestId);
            if (!File.Exists(path))
                throw new InvalidOperationException("Print request claim is missing.");
            var existing = await ReadRequiredAsync(requestId, path, cancellationToken);
            var completed = existing with
            {
                ResultCode = resultCode,
                Success = success,
                CompletedAt = DateTimeOffset.UtcNow
            };
            await WriteAtomicAsync(path, completed, createNew: false, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<PrintRequestStatus?> GetStatusAsync(
        Guid requestId, CancellationToken cancellationToken)
    {
        var gate = _locks.GetOrAdd(requestId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var path = PathFor(requestId);
            if (!File.Exists(path)) return null;

            var stored = await ReadRequiredAsync(requestId, path, cancellationToken);

            var state = stored.CompletedAt is null
                ? "accepted"
                : stored.Success == true
                    ? "completed"
                    : "failed";
            return new PrintRequestStatus(
                requestId,
                state,
                stored.ResultCode,
                stored.Success,
                stored.ClaimedAt,
                stored.CompletedAt);
        }
        finally
        {
            gate.Release();
        }
    }

    private string PathFor(Guid requestId) => Path.Combine(_directory, $"{requestId:N}.json");

    public async Task<bool> ProbeAsync(CancellationToken cancellationToken)
    {
        var probe = Path.Combine(_directory, $".probe-{Guid.NewGuid():N}");
        try
        {
            await File.WriteAllTextAsync(probe, "ok", cancellationToken);
            File.Delete(probe);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public long CountCompleted()
    {
        return Directory.EnumerateFiles(_directory, "*.json")
            .LongCount(path =>
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(path));
                    return document.RootElement.TryGetProperty("CompletedAt", out var value) &&
                           value.ValueKind != JsonValueKind.Null;
                }
                catch
                {
                    return false;
                }
            });
    }

    public long CountUnresolved()
    {
        return Directory.EnumerateFiles(_directory, "*.json")
            .LongCount(path =>
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(path));
                    return !document.RootElement.TryGetProperty("CompletedAt", out var value) ||
                           value.ValueKind == JsonValueKind.Null;
                }
                catch
                {
                    return true;
                }
            });
    }

    public long RecordAgentStart()
    {
        var path = Path.Combine(_directory, ".agent-start-count");
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            var current = 0L;
            if (File.Exists(path))
                _ = long.TryParse(File.ReadAllText(path), out current);
            var next = checked(current + 1);
            File.WriteAllText(temporaryPath, next.ToString());
            File.Move(temporaryPath, path, overwrite: true);
            return next;
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private async Task<StoredRequest> ReadRequiredAsync(
        Guid requestId,
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var stored = JsonSerializer.Deserialize<StoredRequest>(
                await File.ReadAllTextAsync(path, cancellationToken));
            if (stored is null ||
                string.IsNullOrWhiteSpace(stored.PayloadHash) ||
                string.IsNullOrWhiteSpace(stored.ResultCode))
                throw new JsonException("Required idempotency fields are missing.");
            return stored;
        }
        catch (JsonException)
        {
            Quarantine(path, requestId);
            throw new IdempotencyRecordCorruptedException(requestId);
        }
    }

    private static async Task WriteAtomicAsync(
        string path,
        StoredRequest value,
        bool createNew,
        CancellationToken cancellationToken)
    {
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                             temporaryPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, 4096, FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream, value, cancellationToken: cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            if (createNew && File.Exists(path))
                throw new IOException("The print request was concurrently claimed.");
            File.Move(temporaryPath, path, overwrite: !createNew);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private void Quarantine(string path, Guid requestId)
    {
        var target = Path.Combine(
            _quarantineDirectory,
            $"{requestId:N}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.corrupt");
        try
        {
            File.Move(path, target, overwrite: false);
        }
        catch (IOException)
        {
            // Preserve the original if quarantine cannot be completed.
        }
    }

    private void DeleteExpiredFiles(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(path) >= cutoff) continue;

                // Never age out an unresolved operation. Its request ID is the
                // durable guard against a second physical print after recovery.
                var stored = JsonSerializer.Deserialize<StoredRequest>(
                    File.ReadAllText(path));
                if (stored?.CompletedAt is not null) File.Delete(path);
            }
            catch (JsonException)
            {
                // Corrupt records remain fail-closed and are quarantined only
                // when addressed by request ID. Never silently delete them.
            }
            catch (IOException)
            {
                // A live operation may own the file. It will be considered next start.
            }
            catch (UnauthorizedAccessException)
            {
                // Retention cleanup must never prevent the print agent from starting.
            }
        }
    }

    private sealed record StoredRequest(
        string PayloadHash,
        string ResultCode,
        DateTimeOffset ClaimedAt,
        bool? Success = null,
        DateTimeOffset? CompletedAt = null);
}
