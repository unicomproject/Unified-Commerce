using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Contracts;
using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Validators;

public sealed class PlatformAuthRequestValidator : IPlatformAuthRequestValidator
{
    private static readonly ApplicationError InvalidSession = new(
        "platform_auth.invalid_session",
        "Invalid platform session.");

    public ApplicationError? ValidateLogin(PlatformAdminLoginRequest request)
    {
        return string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)
            ? new ApplicationError("platform_auth.validation_failed", "Email and password are required.")
            : null;
    }

    public ApplicationError? ValidateLogout(Guid platformUserId, Guid sessionId)
    {
        return platformUserId == Guid.Empty || sessionId == Guid.Empty
            ? InvalidSession
            : null;
    }
}