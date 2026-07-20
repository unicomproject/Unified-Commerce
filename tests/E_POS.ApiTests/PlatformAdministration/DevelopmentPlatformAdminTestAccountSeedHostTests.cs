using E_POS.Api.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class DevelopmentPlatformAdminTestAccountSeedHostTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    [InlineData("Testing", false)]
    public void ShouldSeed_RespectsEnvironmentName(string environmentName, bool expected)
    {
        var environment = new StubHostEnvironment(environmentName);

        Assert.Equal(expected, DevelopmentPlatformAdminTestAccountSeedHost.ShouldSeed(environment));
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public StubHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "E_POS.ApiTests";

        public string ContentRootPath { get; set; } = ".";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
