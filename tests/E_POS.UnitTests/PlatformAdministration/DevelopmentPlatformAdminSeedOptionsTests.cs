using E_POS.Infrastructure.Modules.Platform.PlatformAdmin.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class DevelopmentPlatformAdminSeedOptionsTests
{
    [Fact]
    public void SectionName_MatchesDocumentedConfigurationRoot()
    {
        Assert.Equal("DevelopmentSeed:PlatformAdmin", DevelopmentPlatformAdminSeedOptions.SectionName);
    }

    [Fact]
    public void CompleteBillingViewerConfiguration_BindsThroughOptionsModel()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["BillingViewer:Email"] = "billing.viewer.dev@local.test",
            ["BillingViewer:Password"] = "ViewerSecret-1",
            ["BillingViewer:DisplayName"] = "Billing Viewer Development"
        });

        Assert.Equal("billing.viewer.dev@local.test", options.BillingViewer.Email);
        Assert.Equal("ViewerSecret-1", options.BillingViewer.Password);
        Assert.Equal("Billing Viewer Development", options.BillingViewer.DisplayName);
        Assert.True(options.BillingViewer.HasCompleteCredentials);
    }

    [Fact]
    public void CompleteNoBillingConfiguration_BindsThroughOptionsModel()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["NoBilling:Email"] = "billing.none.dev@local.test",
            ["NoBilling:Password"] = "NoBillingSecret-1",
            ["NoBilling:DisplayName"] = "No Billing Development"
        });

        Assert.Equal("billing.none.dev@local.test", options.NoBilling.Email);
        Assert.Equal("NoBillingSecret-1", options.NoBilling.Password);
        Assert.Equal("No Billing Development", options.NoBilling.DisplayName);
        Assert.True(options.NoBilling.HasCompleteCredentials);
    }

    [Fact]
    public void HasCompleteCredentials_MissingPassword_IsFalse()
    {
        var options = new DevelopmentPlatformAdminSeedOptions
        {
            BillingViewer = new DevelopmentPlatformAdminAccountOptions
            {
                Email = "billing.viewer.dev@local.test"
            }
        };

        Assert.False(options.BillingViewer.HasCompleteCredentials);
    }

    [Fact]
    public void HasCompleteCredentials_MissingEmail_IsFalse()
    {
        var options = new DevelopmentPlatformAdminSeedOptions
        {
            NoBilling = new DevelopmentPlatformAdminAccountOptions
            {
                Password = "NoBillingSecret-1"
            }
        };

        Assert.False(options.NoBilling.HasCompleteCredentials);
    }

    private static DevelopmentPlatformAdminSeedOptions Bind(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .Add(new DictionaryConfigurationSource(values))
            .Build();

        var options = new DevelopmentPlatformAdminSeedOptions();
        configuration.Bind(options);
        return options;
    }

    private sealed class DictionaryConfigurationSource : IConfigurationSource
    {
        private readonly IDictionary<string, string?> _data;

        public DictionaryConfigurationSource(IDictionary<string, string?> data)
        {
            _data = data;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new DictionaryConfigurationProvider(_data);
    }

    private sealed class DictionaryConfigurationProvider : ConfigurationProvider
    {
        public DictionaryConfigurationProvider(IDictionary<string, string?> data)
        {
            foreach (var pair in data)
            {
                Data[pair.Key] = pair.Value;
            }
        }
    }
}
