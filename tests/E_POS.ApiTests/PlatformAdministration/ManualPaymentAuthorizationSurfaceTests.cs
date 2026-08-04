using System.Reflection;
using E_POS.Api.Controllers;
using E_POS.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace E_POS.ApiTests.PlatformAdministration;

public sealed class ManualPaymentAuthorizationSurfaceTests
{
    [Fact]
    public void PlatformAdminManualPaymentRoutes_RequirePlatformAuthentication()
    {
        var authorize = Assert.Single(typeof(PlatformAdminManualPaymentsController)
            .GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal("PlatformOnly", authorize.Policy);
        Assert.Null(typeof(PlatformAdminManualPaymentsController).GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void TenantPaymentStatusRoute_RequiresPlatformAuthentication()
    {
        var authorize = Assert.Single(typeof(PlatformTenantOnboardingPaymentStatusController)
            .GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal("PlatformOnly", authorize.Policy);
    }

    [Fact]
    public void RecipientPaymentRoutes_UseAnonymousPurposeBoundAccessAndRateLimit()
    {
        Assert.NotNull(typeof(ManualPaymentAccessController).GetCustomAttribute<AllowAnonymousAttribute>());
        var limiter = Assert.Single(typeof(ManualPaymentAccessController)
            .GetCustomAttributes<EnableRateLimitingAttribute>());

        Assert.Equal(RateLimitingPolicies.PaymentAccess, limiter.PolicyName);
        Assert.Equal("api/v1/tenant-onboarding/payment-access/{accessToken}",
            Assert.Single(typeof(ManualPaymentAccessController).GetCustomAttributes<RouteAttribute>()).Template);
    }

    [Fact]
    public void EvidenceMutationRoutes_AreMultipartOnly()
    {
        var submit = typeof(ManualPaymentAccessController).GetMethod(nameof(ManualPaymentAccessController.Submit))!;
        var update = typeof(ManualPaymentAccessController).GetMethod(nameof(ManualPaymentAccessController.Update))!;

        Assert.Equal("multipart/form-data", Assert.Single(submit.GetCustomAttributes<ConsumesAttribute>()).ContentTypes.Single());
        Assert.Equal("multipart/form-data", Assert.Single(update.GetCustomAttributes<ConsumesAttribute>()).ContentTypes.Single());
        Assert.Single(submit.GetCustomAttributes<HttpPostAttribute>());
        Assert.Single(update.GetCustomAttributes<HttpPutAttribute>());
    }
}
