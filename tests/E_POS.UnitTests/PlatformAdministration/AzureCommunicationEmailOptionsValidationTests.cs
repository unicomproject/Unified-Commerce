using E_POS.Infrastructure.Integrations.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class AzureCommunicationEmailOptionsValidationTests
{
    [Fact]
    public void ValidateOnStart_Fails_WhenEndpointConfiguredWithoutSender()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidateOptions<AzureCommunicationEmailOptions>, AzureCommunicationEmailOptionsValidator>();
        services.AddOptions<AzureCommunicationEmailOptions>()
            .Configure(options =>
            {
                options.Endpoint = "https://example.communication.azure.com";
                options.SenderAddress = string.Empty;
            })
            .ValidateOnStart();

        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<AzureCommunicationEmailOptions>>().Value);

        Assert.Contains(ex.Failures, f => f.Contains("SenderAddress", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateOnStart_Succeeds_WhenAcsDisabled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidateOptions<AzureCommunicationEmailOptions>, AzureCommunicationEmailOptionsValidator>();
        services.AddOptions<AzureCommunicationEmailOptions>()
            .Configure(options =>
            {
                options.ConnectionString = string.Empty;
                options.Endpoint = string.Empty;
                options.SenderAddress = string.Empty;
            })
            .ValidateOnStart();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureCommunicationEmailOptions>>().Value;

        Assert.Equal(string.Empty, options.SenderAddress);
    }
}
