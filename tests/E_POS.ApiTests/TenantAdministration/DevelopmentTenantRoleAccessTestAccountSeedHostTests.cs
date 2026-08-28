using E_POS.Api.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace E_POS.ApiTests.TenantAdministration;

public sealed class DevelopmentTenantRoleAccessTestAccountSeedHostTests
{
    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    [InlineData("Staging", false)]
    public void ShouldSeed_RespectsEnvironmentName(string environmentName, bool expected)
    {
        var environment = new StubHostEnvironment(environmentName);

        Assert.Equal(expected, DevelopmentTenantRoleAccessTestAccountSeedHost.ShouldSeed(environment));
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "E_POS.ApiTests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
