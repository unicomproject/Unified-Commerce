using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.PlatformAdmin.Dtos;

namespace E_POS.Application.Modules.Platform.PlatformAdmin.Contracts;

public interface IPlatformAuthRequestValidator
{
    ApplicationError? ValidateLogin(PlatformAdminLoginRequest request);
    ApplicationError? ValidateLogout(Guid platformUserId, Guid sessionId);
}
