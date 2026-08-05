using System.Text;
using System.Text.Json;
using E_POS.Application.Common.Security;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.Flow4FixtureCli.Tests;

public sealed class Flow4FixtureSecurityTests
{
    private const string Credential = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG";

    [Fact]
    public void Scenario_allow_list_is_exact_and_complete()
    {
        Assert.Equal(17, Enum.GetValues<Flow4FixtureScenario>().Length);
        Assert.DoesNotContain(Enum.GetNames<Flow4FixtureScenario>(), x => x.Contains("CUSTOM", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Production", true, "oneverz_flow4_e2e_aabbccdd", "localhost", "flow4_runner", "SUPPRESSED", 30)]
    [InlineData("Test", false, "oneverz_flow4_e2e_aabbccdd", "localhost", "flow4_runner", "SUPPRESSED", 30)]
    [InlineData("Test", true, "UnifiedCommerceDb", "localhost", "flow4_runner", "SUPPRESSED", 30)]
    [InlineData("Test", true, "oneverz_flow4_e2e_aabbccdd", "production-db", "flow4_runner", "SUPPRESSED", 30)]
    [InlineData("Test", true, "oneverz_flow4_e2e_aabbccdd", "localhost", "postgres", "SUPPRESSED", 30)]
    [InlineData("Test", true, "oneverz_flow4_e2e_aabbccdd", "localhost", "flow4_runner", "LIVE", 30)]
    [InlineData("Test", true, "oneverz_flow4_e2e_aabbccdd", "localhost", "flow4_runner", "SUPPRESSED", 61)]
    public void Unsafe_preconnection_boundaries_are_rejected(string environment, bool enabled, string database,
        string host, string role, string emailMode, int ttl)
    {
        var options = Options(environment, enabled, database, host, role, emailMode, ttl);
        Assert.ThrowsAny<Exception>(() => new Flow4FixtureSecurityGuard().ValidatePreConnection(options, Guid.NewGuid(), Credential));
    }

    [Fact]
    public void Correct_isolated_boundary_passes_preconnection_validation()
    {
        new Flow4FixtureSecurityGuard().ValidatePreConnection(
            Options("E2E", true, "oneverz_flow4_e2e_aabbccdd", "127.0.0.1", "flow4_runner", "TEST_SINK", 60),
            Guid.NewGuid(), Credential);
    }

    [Fact]
    public void Wrong_or_short_bootstrap_credential_is_rejected_without_disclosing_it()
    {
        var exception = Assert.Throws<UnauthorizedAccessException>(() => new Flow4FixtureSecurityGuard().ValidatePreConnection(
            Options("Test", true, "oneverz_flow4_e2e_aabbccdd", "localhost", "flow4_runner", "SUPPRESSED", 30),
            Guid.NewGuid(), "wrong"));
        Assert.DoesNotContain(Credential, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("wrong", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Manifest_pipe_round_trip_preserves_separation_and_ToString_is_redacted()
    {
        var run = Guid.NewGuid(); var now = DateTimeOffset.UtcNow; var token = "raw-token-that-must-not-appear-in-diagnostics";
        var manifest = new Flow4FixtureManifest(new("1.0", "canonical-v1", run, "Test", now, now.AddMinutes(5), null),
            new Dictionary<string, string> { ["AWAITING_PAYMENT.paymentId"] = Guid.NewGuid().ToString("D") },
            new Dictionary<string, string> { ["AWAITING_PAYMENT.paymentToken"] = token },
            new("cleanup-secret", "1", ["AWAITING_PAYMENT"], new Dictionary<string, int> { ["payment"] = 1 }));
        await using var stream = new MemoryStream();
        await SecureManifestTransport.WriteAsync(manifest, stream, true, null);
        stream.Position = 0;
        var copy = await JsonSerializer.DeserializeAsync<Flow4FixtureManifest>(stream, SecureManifestTransport.JsonOptions);
        Assert.Equal(token, copy!.Secrets["AWAITING_PAYMENT.paymentToken"]);
        Assert.DoesNotContain(token, manifest.ToString(), StringComparison.Ordinal);
        await Assert.ThrowsAsync<InvalidOperationException>(() => SecureManifestTransport.WriteAsync(manifest, Stream.Null, false, null));
    }

    [Fact]
    public async Task Ephemeral_fallback_is_restricted_and_deleted()
    {
        var manifest = Manifest();
        var path = Path.Combine(Path.GetTempPath(), "flow4-fixture-tests", Guid.NewGuid().ToString("N"), "manifest.json");
        await SecureManifestTransport.WriteRestrictedFileAsync(manifest, path);
        Assert.True(File.Exists(path));
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        SecureManifestTransport.DeleteFallback(path);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Repository_fallback_path_is_rejected_before_write()
    {
        var path = Path.Combine(FindRepositoryRoot(), "prohibited-flow4-manifest.json");
        await Assert.ThrowsAsync<InvalidOperationException>(() => SecureManifestTransport.WriteRestrictedFileAsync(Manifest(), path));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Production_token_services_generate_random_raw_values_and_distinct_hash_domains()
    {
        ITokenHashService hash = new TokenHashService();
        var options = Microsoft.Extensions.Options.Options.Create(new TenantJwtOptions { SigningKey = new string('k', 64) });
        var payment = new ManualPaymentAccessTokenService(hash, options);
        var invitation = new InvitationTokenService(hash, options);
        var raw = payment.GenerateToken();
        Assert.True(raw.Length >= 43); Assert.NotEqual(raw, payment.GenerateToken());
        Assert.NotEqual(payment.HashToken(raw), invitation.HashToken(raw));
        Assert.DoesNotContain(raw, payment.HashToken(raw), StringComparison.Ordinal);
    }

    [Fact]
    public void Production_api_has_no_fixture_references_or_routes()
    {
        var root = FindRepositoryRoot();
        var apiFiles = Directory.EnumerateFiles(Path.Combine(root, "src", "E_POS.Api"), "*.cs", SearchOption.AllDirectories);
        foreach (var file in apiFiles)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("Flow4Fixture", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("flow4-fixture", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static Flow4FixtureOptions Options(string env, bool enabled, string database, string host, string role,
        string emailMode, int ttl) => new(env, enabled,
        $"Host={host};Database={database};Username={role};Password=not-a-secret", Credential, "marker-nonce", emailMode,
        new string('k', 64), ttl, new HashSet<string>(["localhost", "127.0.0.1"]),
        new HashSet<string>(["production", "prod", "staging", "shared"]),
        new HashSet<string>(["UnifiedCommerceDb", "postgres", "production"]), "flow4_runner");

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "src"))) current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static Flow4FixtureManifest Manifest()
    {
        var run = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        return new(new("1.0", "canonical-v1", run, "Test", now, now.AddMinutes(5), null),
            new Dictionary<string, string> { ["id"] = Guid.NewGuid().ToString("D") },
            new Dictionary<string, string> { ["token"] = "synthetic-secret" },
            new("synthetic-cleanup", "1", ["AWAITING_PAYMENT"], new Dictionary<string, int>()));
    }
}
