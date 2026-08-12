using System.Text.Json;
using E_POS.Infrastructure.Persistence;
using E_POS.InventoryMaintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var command = args.FirstOrDefault()?.Trim().ToLowerInvariant();
if (command is not ("inspect" or "top-up" or "seed-dev" or "clean-products"))
{
    Console.Error.WriteLine(
        "Usage: inspect | top-up --tenant-code <code> --outlet-code <code> " +
        "--location-code <code> --actor-email <email> [--minimum 100] " +
        "--confirm-local-development | seed-dev --confirm-local-development | clean-products --confirm-local-development");
    return 2;
}

var values = ParseArguments(args.Skip(1).ToArray());
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("src/E_POS.Api/appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
var parsedConnection = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
if (!IsLocalHost(parsedConnection.Host ?? string.Empty))
{
    throw new InvalidOperationException(
        "Inventory maintenance is restricted to a localhost development database.");
}

var dbOptions = new DbContextOptionsBuilder<EPosDbContext>()
    .UseNpgsql(connectionString)
    .Options;
await using var dbContext = new EPosDbContext(dbOptions);
var service = new DevelopmentInventoryTopUpService(dbContext);
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

if (command == "inspect")
{
    var context = await service.InspectAsync(CancellationToken.None);
    Console.WriteLine(JsonSerializer.Serialize(context, jsonOptions));
    return 0;
}

if (command == "clean-products")
{
    if (!values.ContainsKey("confirm-local-development"))
    {
        throw new InvalidOperationException(
            "Refusing to clean products without --confirm-local-development.");
    }
    var deletedCount = await service.CleanTestProductsAsync(CancellationToken.None);
    Console.WriteLine($"Successfully cleaned up {deletedCount} test product(s) and all associated images, variants, and data from the database.");
    return 0;
}

if (command == "seed-dev")
{
    if (!values.ContainsKey("confirm-local-development"))
    {
        throw new InvalidOperationException(
            "Refusing to seed database without --confirm-local-development.");
    }
    await service.SeedDevPopularAndOffersAsync(CancellationToken.None);
    Console.WriteLine("Successfully seeded 10 Popular products and 10 Discount/Offer products!");
    return 0;
}

if (!values.ContainsKey("confirm-local-development"))
{
    throw new InvalidOperationException(
        "Refusing to modify inventory without --confirm-local-development.");
}

var options = new DevelopmentInventoryTopUpOptions(
    Required(values, "tenant-code"),
    Required(values, "outlet-code"),
    Required(values, "location-code"),
    Required(values, "actor-email"),
    values.TryGetValue("minimum", out var minimumText) &&
    decimal.TryParse(minimumText, out var minimum)
        ? minimum
        : 100m);

var result = await service.ExecuteAsync(options, CancellationToken.None);
Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
return 0;

static Dictionary<string, string?> ParseArguments(string[] arguments)
{
    var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < arguments.Length; index++)
    {
        var key = arguments[index];
        if (!key.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unexpected argument '{key}'.");
        }

        key = key[2..];
        if (index + 1 < arguments.Length &&
            !arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            result[key] = arguments[++index];
        }
        else
        {
            result[key] = null;
        }
    }

    return result;
}

static string Required(IReadOnlyDictionary<string, string?> values, string key) =>
    values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new ArgumentException($"--{key} is required.");

static bool IsLocalHost(string host) =>
    string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
