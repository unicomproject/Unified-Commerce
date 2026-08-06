using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

namespace E_POS.Flow4FixtureCli;

public sealed class Flow4FixtureSecurityGuard
{
    private static readonly Regex DatabasePattern = new(Flow4FixtureOptions.DatabaseNamePattern,
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public async Task ValidateAsync(Flow4FixtureOptions options, Guid runId, string suppliedCredential,
        CancellationToken cancellationToken = default)
    {
        ValidatePreConnection(options, runId, suppliedCredential);
        var builder = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var identity = new NpgsqlCommand("SELECT current_database(), current_user", connection);
        await using var reader = await identity.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Database identity is unavailable.");
        var connectedDatabase = reader.GetString(0);
        var connectedRole = reader.GetString(1);
        await reader.CloseAsync();

        if (!string.Equals(connectedDatabase, builder.Database, StringComparison.Ordinal) ||
            !string.Equals(connectedRole, options.RequiredDatabaseRole, StringComparison.Ordinal))
            throw new InvalidOperationException("Connected database identity is not the approved test identity.");

        await using var marker = new NpgsqlCommand("""
            SELECT environment, database_name, database_role, marker_nonce, expires_at
            FROM flow4_test_control.environment_marker WHERE marker_id = 1
            """, connection);
        try
        {
            await using var markerReader = await marker.ExecuteReaderAsync(cancellationToken);
            if (!await markerReader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Flow 4 test marker is missing.");
            if (!string.Equals(markerReader.GetString(0), options.Environment, StringComparison.Ordinal) ||
                !string.Equals(markerReader.GetString(1), connectedDatabase, StringComparison.Ordinal) ||
                !string.Equals(markerReader.GetString(2), connectedRole, StringComparison.Ordinal) ||
                !FixedEquals(markerReader.GetString(3), options.ExpectedMarkerNonce) ||
                markerReader.GetFieldValue<DateTimeOffset>(4) <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Flow 4 test marker is invalid or expired.");
        }
        catch (PostgresException exception) when (exception.SqlState is "3F000" or "42P01")
        {
            throw new InvalidOperationException("Flow 4 test marker is missing.", exception);
        }
    }

    public void ValidatePreConnection(Flow4FixtureOptions options, Guid runId, string suppliedCredential)
    {
        if (options.Environment is not ("Test" or "E2E"))
            throw new InvalidOperationException("Flow 4 fixture execution requires the exact Test or E2E environment.");
        if (!options.Enabled) throw new InvalidOperationException("Flow 4 fixture bootstrap is disabled.");
        if (runId == Guid.Empty) throw new InvalidOperationException("A non-empty test run ID is required.");
        if (!HasMinimumEntropyShape(options.ExpectedBootstrapCredential) || !HasMinimumEntropyShape(suppliedCredential) ||
            !FixedEquals(options.ExpectedBootstrapCredential, suppliedCredential))
            throw new UnauthorizedAccessException("Flow 4 fixture bootstrap authorization failed.");
        if (options.TokenTtlMinutes is < 1 or > 60) throw new InvalidOperationException("Fixture token TTL must be between 1 and 60 minutes.");
        if (!string.Equals(options.EmailMode, "SUPPRESSED", StringComparison.Ordinal) &&
            !string.Equals(options.EmailMode, "TEST_SINK", StringComparison.Ordinal))
            throw new InvalidOperationException("Real email delivery is not allowed for Flow 4 fixtures.");
        if (string.IsNullOrWhiteSpace(options.TenantSigningKey)) throw new InvalidOperationException("Token hashing key is missing.");
        if (string.IsNullOrWhiteSpace(options.ExpectedMarkerNonce)) throw new InvalidOperationException("Test marker nonce is missing.");
        if (string.IsNullOrWhiteSpace(options.RequiredDatabaseRole)) throw new InvalidOperationException("Required test database role is missing.");

        NpgsqlConnectionStringBuilder builder;
        try { builder = new(options.ConnectionString); }
        catch (Exception exception) { throw new InvalidOperationException("Test database connection is invalid.", exception); }
        if (!DatabasePattern.IsMatch(builder.Database ?? string.Empty) || options.DeniedDatabases.Contains(builder.Database ?? string.Empty))
            throw new InvalidOperationException("Database name is not an approved isolated Flow 4 E2E database.");
        if (!IsAllowedHost(builder.Host ?? string.Empty, options.AllowedHosts, options.DeniedHosts))
            throw new InvalidOperationException("Database host is not an approved isolated Flow 4 E2E host.");
        if (!string.Equals(builder.Username, options.RequiredDatabaseRole, StringComparison.Ordinal))
            throw new InvalidOperationException("Connection role is not the dedicated Flow 4 fixture role.");
    }

    private static bool HasMinimumEntropyShape(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 43) return false;
        return value.Distinct().Count() >= 16;
    }

    private static bool IsAllowedHost(string host, IReadOnlySet<string> allowed, IReadOnlySet<string> denied)
    {
        if (string.IsNullOrWhiteSpace(host) || denied.Any(x => host.Contains(x, StringComparison.OrdinalIgnoreCase))) return false;
        if (allowed.Contains(host)) return true;
        if (!IPAddress.TryParse(host, out var address)) return false;
        return IPAddress.IsLoopback(address);
    }

    internal static bool FixedEquals(string left, string right)
    {
        var leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left ?? string.Empty));
        var rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }
}
