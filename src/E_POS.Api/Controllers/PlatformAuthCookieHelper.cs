using E_POS.Api.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

public static class PlatformAuthCookieHelper
{
    public const string RefreshTokenCookieName = "platform_refresh_token";
    public const string PlatformAuthCookiePath = "/api/v1/platform-auth";
    public const string LegacyAuthCookiePath = "/api/v1/auth";

    public static void AppendRefreshTokenCookie(HttpResponse response, PlatformAdminLoginResponse loginResponse, string cookiePath)
    {
        response.Cookies.Append(RefreshTokenCookieName, loginResponse.RefreshToken, CreateCookieOptions(response, cookiePath, loginResponse.RefreshTokenExpiresAt));
    }

    public static void ClearRefreshTokenCookie(HttpResponse response, string cookiePath)
    {
        response.Cookies.Delete(RefreshTokenCookieName, CreateCookieOptions(response, cookiePath));
    }

    private static CookieOptions CreateCookieOptions(HttpResponse response, string cookiePath, DateTimeOffset? expires = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = response.HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = cookiePath
        };

        if (expires is not null)
        {
            options.Expires = expires;
        }

        return options;
    }

    public static LegacyPlatformLoginResponse ToLegacyResponse(PlatformAdminLoginResponse response)
    {
        return new LegacyPlatformLoginResponse(
            response.AccessToken,
            "Bearer",
            response.AccessTokenExpiresAt,
            response.RefreshTokenExpiresAt,
            new LegacyPlatformLoginUserResponse(
                response.User.Id,
                response.User.Email,
                string.Empty,
                response.User.Status,
                response.Permissions));
    }
}
