using E_POS.Application.Modules.Tenant.CatalogProduct.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace E_POS.UnitTests.CatalogProduct;

public sealed class ExternalProductLookupOptionsTests
{
    [Fact]
    public void Bind_EmptyProviders_IsValidZeroProviderConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalProductLookup:DefaultTimeoutSeconds"] = "5",
            })
            .Build();

        var options = new ExternalProductLookupOptions();
        configuration.GetSection(ExternalProductLookupOptions.SectionName).Bind(options);

        Assert.Equal(5, options.DefaultTimeoutSeconds);
        Assert.Empty(options.Providers);
    }

    [Fact]
    public void Bind_DisabledProvider_DoesNotRequireSecrets()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalProductLookup:DefaultTimeoutSeconds"] = "3",
                ["ExternalProductLookup:Providers:0:Name"] = "future-adapter",
                ["ExternalProductLookup:Providers:0:Enabled"] = "false",
                ["ExternalProductLookup:Providers:0:Priority"] = "10",
                ["ExternalProductLookup:Providers:0:TimeoutSeconds"] = "4",
            })
            .Build();

        var options = new ExternalProductLookupOptions();
        configuration.GetSection(ExternalProductLookupOptions.SectionName).Bind(options);

        Assert.Single(options.Providers);
        Assert.Equal("future-adapter", options.Providers[0].Name);
        Assert.False(options.Providers[0].Enabled);
        Assert.Equal(10, options.Providers[0].Priority);
        Assert.Equal(4, options.Providers[0].TimeoutSeconds);
        Assert.Null(options.GetType().GetProperty("ApiKey"));
        Assert.Null(options.GetType().GetProperty("ApiSecret"));
    }

    [Fact]
    public void Bind_PriorityOrderFields_ArePreserved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExternalProductLookup:Providers:0:Name"] = "second",
                ["ExternalProductLookup:Providers:0:Enabled"] = "true",
                ["ExternalProductLookup:Providers:0:Priority"] = "20",
                ["ExternalProductLookup:Providers:1:Name"] = "first",
                ["ExternalProductLookup:Providers:1:Enabled"] = "true",
                ["ExternalProductLookup:Providers:1:Priority"] = "1",
            })
            .Build();

        var options = new ExternalProductLookupOptions();
        configuration.GetSection(ExternalProductLookupOptions.SectionName).Bind(options);

        Assert.Equal(2, options.Providers.Count);
        var ordered = options.Providers.OrderBy(p => p.Priority).ThenBy(p => p.Name).Select(p => p.Name).ToArray();
        Assert.Equal(["first", "second"], ordered);
    }
}
