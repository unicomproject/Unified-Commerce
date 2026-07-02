using System.Security.Claims;
using E_POS.Application.Common.Models;

namespace E_POS.Api.Common;

public interface ITenantRequestContextFactory
{
    bool TryCreate(ClaimsPrincipal principal, out TenantRequestContext context);
}