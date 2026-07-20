using E_POS.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class PlatformPasswordResetApiSurfaceTests
{
    [Fact]
    public void ApiAssembly_ExposesPasswordResetControllers()
    {
        var apiAssembly = typeof(PlatformPasswordResetController).Assembly;
        var controllerTypes = apiAssembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && type is { IsAbstract: false })
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(nameof(PlatformPasswordResetController), controllerTypes);
        Assert.Contains(nameof(PlatformPasswordResetLegacyController), controllerTypes);
    }

    [Fact]
    public void PlatformAdminUsersController_ExposesPasswordResetAction()
    {
        var method = typeof(PlatformAdminUsersController).GetMethod(
            nameof(PlatformAdminUsersController.InitiatePasswordReset));

        Assert.NotNull(method);
        Assert.Contains(
            method!.GetCustomAttributes(inherit: true),
            attribute => attribute is HttpPostAttribute);
    }
}
