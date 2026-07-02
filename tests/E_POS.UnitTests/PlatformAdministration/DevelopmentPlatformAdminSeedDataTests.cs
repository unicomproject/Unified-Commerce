using E_POS.Infrastructure.Common.Security;
using E_POS.Infrastructure.Persistence.Seed;
using Xunit;

namespace E_POS.UnitTests.PlatformAdministration;

public sealed class DevelopmentPlatformAdminSeedDataTests
{
    [Fact]
    public void DocumentedDevelopmentPasswordHash_VerifiesWithPasswordHashService()
    {
        var service = new PasswordHashService();

        Assert.True(service.VerifyPassword(DevelopmentPlatformAdminSeedData.Password, DevelopmentPlatformAdminSeedData.PasswordHash));
    }
}
