using System.Text.Json;
using E_POS.Application.Common.Security;
using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;
using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Services;
using E_POS.Infrastructure.Modules.Tenant.TenantAuth.Options;
using E_POS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace E_POS.Flow4FixtureCli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "";
            if (command == "describe")
            {
                var descriptions = Enum.GetValues<Flow4FixtureScenario>().Select(x => new Flow4FixtureScenarioDescription(
                    x.ToString(), true, x is Flow4FixtureScenario.ACTIVE_INVITATION_READY or Flow4FixtureScenario.COMPLETE_HAPPY_PATH));
                await JsonSerializer.SerializeAsync(Console.OpenStandardOutput(), descriptions, SecureManifestTransport.JsonOptions);
                return 0;
            }
            if (command is not ("create" or "cleanup" or "validate")) throw new InvalidOperationException("Use create, cleanup, validate, or describe.");
            var runId = ParseRunId(args);
            var input = await JsonSerializer.DeserializeAsync<Flow4BootstrapInput>(Console.OpenStandardInput(), SecureManifestTransport.JsonOptions)
                        ?? throw new InvalidOperationException("Bootstrap input is required on stdin.");
            var options = Flow4FixtureOptions.FromEnvironment();
            await new Flow4FixtureSecurityGuard().ValidateAsync(options, runId, input.BootstrapCredential);
            if (command == "validate") { Console.Error.WriteLine("Flow 4 fixture boundary validation succeeded."); return 0; }

            await using var db = new EPosDbContext(new DbContextOptionsBuilder<EPosDbContext>().UseNpgsql(options.ConnectionString).Options);
            ITokenHashService hash = new TokenHashService();
            var jwt = Options.Create(new TenantJwtOptions { SigningKey = options.TenantSigningKey });
            var store = new Flow4FixtureStore(db, new ManualPaymentAccessTokenService(hash, jwt),
                new InvitationTokenService(hash, jwt), new PasswordHashService(), options);
            if (command == "cleanup")
            {
                if (string.IsNullOrWhiteSpace(input.CleanupHandle)) throw new InvalidOperationException("Cleanup handle is required on stdin.");
                var result = await store.CleanupAsync(runId, input.CleanupHandle);
                Console.Error.WriteLine(result);
                return 0;
            }
            var scenarios = ParseScenarios(args);
            var manifest = await store.CreateAsync(runId, scenarios);
            await SecureManifestTransport.WriteAsync(manifest, Console.OpenStandardOutput(), Console.IsOutputRedirected,
                Environment.GetEnvironmentVariable("Flow4TestHost__ManifestFile"));
            Console.Error.WriteLine(manifest);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Flow 4 fixture command failed: {exception.GetType().Name}: {Sanitize(exception.Message)}");
            return 1;
        }
    }

    private static Guid ParseRunId(string[] args)
    {
        var index = Array.IndexOf(args, "--run-id");
        if (index < 0 || index + 1 >= args.Length || !Guid.TryParse(args[index + 1], out var runId) || runId == Guid.Empty)
            throw new InvalidOperationException("A valid --run-id is required.");
        return runId;
    }
    private static Flow4FixtureScenario[] ParseScenarios(string[] args)
    {
        var result = new List<Flow4FixtureScenario>();
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--scenario" && ++i < args.Length && Enum.TryParse<Flow4FixtureScenario>(args[i], false, out var value)) result.Add(value);
            else if (i > 0 && args[i - 1] == "--scenario") throw new InvalidOperationException("Unknown Flow 4 fixture scenario.");
        return result.Count == 0 ? Enum.GetValues<Flow4FixtureScenario>() : result.ToArray();
    }
    private static string Sanitize(string value)
    {
        var newline = value.IndexOfAny(['\r', '\n']);
        return (newline < 0 ? value : value[..newline]).Replace("Password=", "Password=[REDACTED]", StringComparison.OrdinalIgnoreCase);
    }
}
