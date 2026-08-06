using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace E_POS.Flow4FixtureCli;

public static class SecureManifestTransport
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static async Task WriteAsync(Flow4FixtureManifest manifest, Stream output, bool outputRedirected,
        string? fallbackPath, CancellationToken cancellationToken = default)
    {
        manifest.Validate(manifest.Metadata.TestRunId, manifest.Metadata.Environment);
        if (!string.IsNullOrWhiteSpace(fallbackPath))
        {
            await WriteRestrictedFileAsync(manifest, fallbackPath, cancellationToken);
            return;
        }
        if (!outputRedirected) throw new InvalidOperationException("Secret fixture output requires a redirected process pipe.");
        await JsonSerializer.SerializeAsync(output, manifest, JsonOptions, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }

    public static async Task WriteRestrictedFileAsync(Flow4FixtureManifest manifest, string path,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        if (IsInsideRepository(fullPath) ||
            fullPath.Contains($"{Path.DirectorySeparatorChar}test-results{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
            fullPath.EndsWith(".env", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Fallback manifest path is inside a prohibited repository or artifact location.");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                         4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        RestrictToCurrentUser(fullPath);
    }

    private static bool IsInsideRepository(string fullPath)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(fullPath)!);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git"))) return true;
            directory = directory.Parent;
        }
        return false;
    }

    public static void DeleteFallback(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    private static void RestrictToCurrentUser(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            return;
        }

        var identity = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("Current Windows identity is unavailable.");
        var security = new FileSecurity();
        security.SetOwner(identity);
        security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl, AccessControlType.Allow));
        FileSystemAclExtensions.SetAccessControl(new FileInfo(path), security);
    }
}
