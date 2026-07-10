using E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

namespace E_POS.Api.Common.Auth;

public static class PlatformAuthClientContextFactory
{
    public static PlatformAuthClientContext FromHttpContext(HttpContext httpContext)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        return new PlatformAuthClientContext(
            string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress,
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            null);
    }
}
