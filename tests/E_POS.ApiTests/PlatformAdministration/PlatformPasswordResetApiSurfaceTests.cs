using E_POS.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformPasswordResetApiSurfaceTests
{
    [Fact]
    public void ApiAssembly_DoesNotExposePasswordResetEndpoints()
    {
        var apiAssembly = typeof(PlatformAuthController).Assembly;
        var controllerTypes = apiAssembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && type is { IsAbstract: false });

        Assert.DoesNotContain(
            controllerTypes,
            type => type.Name.Contains("PasswordReset", StringComparison.OrdinalIgnoreCase));
    }
}
