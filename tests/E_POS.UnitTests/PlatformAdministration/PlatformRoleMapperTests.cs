using E_POS.Application.Modules.PlatformAdministration.Mappers;
using E_POS.Domain.Modules.PlatformAdministration.Constants;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class PlatformRoleMapperTests
{
    [Fact]
    public void IsProtectedRole_IdentifiesSuperAdministrator()
    {
        Assert.True(PlatformRoleMapper.IsProtectedRole(PlatformRoleCodes.SuperAdministrator));
        Assert.False(PlatformRoleMapper.IsProtectedRole("support_operator"));
    }
}
