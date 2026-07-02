using E_POS.Application.Common.Models;
using E_POS.Application.Modules.PlatformAdministration.Dtos;

namespace E_POS.Application.Modules.PlatformAdministration.Contracts;

public interface IPlatformAuthRequestValidator
{
    ApplicationError? ValidateLogin(PlatformAdminLoginRequest request);
    ApplicationError? ValidateLogout(Guid platformUserId, Guid sessionId);
}