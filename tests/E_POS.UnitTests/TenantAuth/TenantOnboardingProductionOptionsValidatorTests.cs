using E_POS.Infrastructure.Integrations.Email;
using E_POS.Infrastructure.Modules.Shared.Integration;
using E_POS.Infrastructure.Modules.Shared.Integration.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace E_POS.UnitTests.TenantAuth;

public sealed class TenantOnboardingProductionOptionsValidatorTests
{
    [Fact]
    public void Production_RejectsMissingHttpsBaseUrl()
    {
        var validator = new TenantOnboardingOutboxOptionsValidator(new FakeEnv(Environments.Production));
        var result = validator.Validate(null, new TenantOnboardingOutboxOptions
        {
            TenantAdminAppBaseUrl = "http://admin.example.com"
        });
        Assert.True(result.Failed);
    }

    [Fact]
    public void Development_AllowsEmptyBaseUrl()
    {
        var validator = new TenantOnboardingOutboxOptionsValidator(new FakeEnv(Environments.Development));
        var result = validator.Validate(null, new TenantOnboardingOutboxOptions
        {
            TenantAdminAppBaseUrl = null
        });
        Assert.False(result.Failed);
    }

    [Fact]
    public void Production_RequiresAcsConfiguration()
    {
        var validator = new ProductionAzureCommunicationEmailOptionsValidator(new FakeEnv(Environments.Production));
        var result = validator.Validate(null, new AzureCommunicationEmailOptions());
        Assert.True(result.Failed);
    }

    [Fact]
    public void Development_AllowsUnconfiguredAcs()
    {
        var validator = new ProductionAzureCommunicationEmailOptionsValidator(new FakeEnv(Environments.Development));
        var result = validator.Validate(null, new AzureCommunicationEmailOptions());
        Assert.False(result.Failed);
    }

    private sealed class FakeEnv(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "E_POS.Api";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
