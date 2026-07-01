using System.Security.Claims;

namespace E_POS.Application.Common.Security;

public interface IAuthSessionValidator
{
    Task<bool> IsCurrentSessionActiveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}