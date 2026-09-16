using E_POS.Application.Modules.Tenant.CatalogProduct.Contracts;
using E_POS.Application.Modules.Tenant.CatalogProduct.Dtos.ExternalLookup;
using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using E_POS.Application.Modules.Tenant.CatalogProduct.Services;
using E_POS.Domain.Modules.Tenant.CatalogProduct.Constants;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ExternalProductLookupCoordinatorTests
{
    private const string Identifier = "04006381333931";

    [Fact]
    public async Task LookupAsync_ZeroConfiguredProviders_ReturnsNoMatch()
    {
        var coordinator = CreateCoordinator(providers: [], options: new ExternalProductLookupOptions());

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Null(result.Suggestion);
        Assert.False(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_DisabledProvider_NotInvoked_ReturnsNoMatch()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("Cola"),
        };
        var options = OptionsWith(("alpha", false, 1));
        var coordinator = CreateCoordinator([provider], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task LookupAsync_OneProviderFound_ReturnsFound()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("House Lemon Juice", brand: "House", category: "Drinks", unit: "500ml"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.False(result.RetryAllowed);
        Assert.Equal("House Lemon Juice", result.Suggestion!.ProductName);
        Assert.Equal("House", result.Suggestion.BrandText);
        Assert.Equal("Drinks", result.Suggestion.CategoryText);
        Assert.Equal("500ml", result.Suggestion.UnitText);
        Assert.Equal(Identifier, result.Suggestion.PrimaryGtin);
        Assert.Equal("ref-1", result.SourceReference);
        Assert.Null(result.Suggestion.GetType().GetProperty("BrandId"));
        Assert.Null(result.Suggestion.GetType().GetProperty("CategoryId"));
    }

    [Fact]
    public async Task LookupAsync_OneProviderNoMatch_ReturnsNoMatch()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.False(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_OneProviderTemporaryFailure_ReturnsTemporaryFailure()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, "timeout"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_ProviderThrows_MapsToTemporaryFailure()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Exception = new InvalidOperationException("boom"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_Timeout_ReturnsTemporaryFailure()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Delay = TimeSpan.FromSeconds(2),
            Result = FoundResult("Slow"),
        };
        var options = new ExternalProductLookupOptions
        {
            DefaultTimeoutSeconds = 1,
            Providers =
            [
                new ExternalProductLookupProviderOptions
                {
                    Name = "alpha",
                    Enabled = true,
                    Priority = 1,
                    TimeoutSeconds = 1,
                },
            ],
        };
        var coordinator = CreateCoordinator([provider], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_Cancellation_Propagates()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Delay = TimeSpan.FromSeconds(5),
            Result = FoundResult("X"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            coordinator.LookupAsync(CreateRequest(), cts.Token));
    }

    [Fact]
    public async Task LookupAsync_PreservesLeadingZeroIdentifier()
    {
        string? seen = null;
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            OnLookup = req =>
            {
                seen = req.Identifier;
                return FoundResult("Cola", primaryGtin: req.Identifier);
            },
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(
            new ExternalProductLookupRequest(Identifier, "GTIN14", "UNKNOWN"),
            CancellationToken.None);

        Assert.Equal(Identifier, seen);
        Assert.Equal(Identifier, result.Suggestion!.PrimaryGtin);
    }

    [Fact]
    public async Task LookupAsync_MalformedFound_WithoutProductName_TreatedAsNoMatch()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.Found,
                new ExternalProductSuggestion(null, null, null, null, null, null, null, null, null, Identifier, "GTIN13"),
                "ref",
                null),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
    }

    [Fact]
    public async Task LookupAsync_MismatchedIdentifier_RejectedAsNoMatch()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("Wrong", primaryGtin: "9999999999999"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
        Assert.Null(result.Suggestion);
    }

    [Fact]
    public async Task LookupAsync_ImageCandidate_RemainsUrlOnly()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("Cola", imageCandidate: "https://cdn.example/p.png"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal("https://cdn.example/p.png", result.Suggestion!.ImageCandidate);
    }

    [Fact]
    public async Task LookupAsync_InvalidImageCandidate_Omitted()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult("Cola", imageCandidate: "javascript:alert(1)"),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Null(result.Suggestion!.ImageCandidate);
    }

    [Fact]
    public async Task LookupAsync_DoesNotReturnProviderCredentials()
    {
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.Found,
                new ExternalProductSuggestion(
                    "Cola", null, "Brand", null, null, null, null, null,
                    "https://cdn.example/p.png", Identifier, "GTIN13"),
                ProviderReference: "opaque-ref",
                FailureCategory: null),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);
        var json = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.DoesNotContain("apiKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authorization", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("opaque-ref", result.SourceReference);
    }

    [Fact]
    public async Task LookupAsync_TruncatesProductNameToCanonicalLimit()
    {
        var longName = new string('A', ProductConstants.ProductNameMaxLength + 50);
        var provider = new FakeExternalProductLookupProvider("alpha")
        {
            Result = FoundResult(longName),
        };
        var coordinator = CreateCoordinator([provider], OptionsWith(("alpha", true, 1)));

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ProductConstants.ProductNameMaxLength, result.Suggestion!.ProductName!.Length);
    }

    [Fact]
    public async Task LookupAsync_PriorityOrder_Honored_StopsOnFirstFound()
    {
        var low = new FakeExternalProductLookupProvider("low")
        {
            Result = FoundResult("FromLow"),
        };
        var high = new FakeExternalProductLookupProvider("high")
        {
            Result = FoundResult("FromHigh"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "low", Enabled = true, Priority = 20 },
                new ExternalProductLookupProviderOptions { Name = "high", Enabled = true, Priority = 1 },
            ],
        };
        var coordinator = CreateCoordinator([low, high], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal("FromHigh", result.Suggestion!.ProductName);
        Assert.Equal(1, high.CallCount);
        Assert.Equal(0, low.CallCount);
    }

    [Fact]
    public async Task LookupAsync_ANoMatch_BFound_ReturnsFound()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = FoundResult("FromB"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
        Assert.Equal("FromB", result.Suggestion!.ProductName);
    }

    [Fact]
    public async Task LookupAsync_ATemporaryFailure_BFound_ReturnsFound()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, "down"),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = FoundResult("FromB"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.Found, result.Status);
    }

    [Fact]
    public async Task LookupAsync_AllNoMatch_ReturnsNoMatch()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.NoMatch, result.Status);
    }

    [Fact]
    public async Task LookupAsync_AllTemporaryFailure_ReturnsTemporaryFailure()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Exception = new TimeoutException(),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, "5xx"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    [Fact]
    public async Task LookupAsync_MixedNoMatchAndTemporaryFailure_ReturnsTemporaryFailure()
    {
        var a = new FakeExternalProductLookupProvider("a")
        {
            Result = new ExternalProductLookupProviderResult(ExternalProductLookupStatuses.NoMatch, null, null, null),
        };
        var b = new FakeExternalProductLookupProvider("b")
        {
            Result = new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.TemporaryFailure, null, null, "down"),
        };
        var options = new ExternalProductLookupOptions
        {
            Providers =
            [
                new ExternalProductLookupProviderOptions { Name = "a", Enabled = true, Priority = 1 },
                new ExternalProductLookupProviderOptions { Name = "b", Enabled = true, Priority = 2 },
            ],
        };
        var coordinator = CreateCoordinator([a, b], options);

        var result = await coordinator.LookupAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ExternalProductLookupStatuses.TemporaryFailure, result.Status);
        Assert.True(result.RetryAllowed);
    }

    private static ExternalProductLookupRequest CreateRequest() =>
        new(Identifier, "GTIN14", "UNKNOWN");

    private static ExternalProductLookupOptions OptionsWith(params (string Name, bool Enabled, int Priority)[] providers) =>
        new()
        {
            DefaultTimeoutSeconds = 5,
            Providers = providers
                .Select(p => new ExternalProductLookupProviderOptions
                {
                    Name = p.Name,
                    Enabled = p.Enabled,
                    Priority = p.Priority,
                })
                .ToList(),
        };

    private static ExternalProductLookupCoordinator CreateCoordinator(
        IEnumerable<IExternalProductLookupProvider> providers,
        ExternalProductLookupOptions options) =>
        new(
            providers,
            Options.Create(options),
            NullLogger<ExternalProductLookupCoordinator>.Instance);

    private static ExternalProductLookupProviderResult FoundResult(
        string productName,
        string? brand = null,
        string? category = null,
        string? unit = null,
        string? primaryGtin = Identifier,
        string? imageCandidate = null) =>
        new(
            ExternalProductLookupStatuses.Found,
            new ExternalProductSuggestion(
                productName,
                ShortName: null,
                BrandText: brand,
                CategoryText: category,
                UnitText: unit,
                CountryCode: null,
                ShortDescription: null,
                LongDescription: null,
                ImageCandidate: imageCandidate,
                PrimaryGtin: primaryGtin,
                IdentifierStandard: "GTIN13"),
            ProviderReference: "ref-1",
            FailureCategory: null);

    private sealed class FakeExternalProductLookupProvider : IExternalProductLookupProvider
    {
        public FakeExternalProductLookupProvider(string name) => Name = name;

        public string Name { get; }
        public int CallCount { get; private set; }
        public ExternalProductLookupProviderResult? Result { get; init; }
        public Exception? Exception { get; init; }
        public TimeSpan? Delay { get; init; }
        public Func<ExternalProductLookupRequest, ExternalProductLookupProviderResult>? OnLookup { get; init; }

        public bool CanHandle(ExternalProductLookupRequest request) => true;

        public async Task<ExternalProductLookupProviderResult> LookupAsync(
            ExternalProductLookupRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (Delay is not null)
            {
                await Task.Delay(Delay.Value, cancellationToken);
            }

            if (Exception is not null)
            {
                throw Exception;
            }

            if (OnLookup is not null)
            {
                return OnLookup(request);
            }

            return Result ?? new ExternalProductLookupProviderResult(
                ExternalProductLookupStatuses.NoMatch, null, null, null);
        }
    }
}
